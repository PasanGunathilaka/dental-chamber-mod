using DentalManagement.Domain.Entities;
using DentalManagement.Infrastructure.Identity;
using DentalManagement.Infrastructure.Persistence;
using DentalManagement.Infrastructure.Persistence.Seeding;
using DentalManagement.Infrastructure.Tests.Harness;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DentalManagement.Infrastructure.Tests;

/// <summary>
/// Fresh-install seeding, its idempotency, and how the first administrator
/// account comes into existence.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class SeedingTests(PostgresContainerFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>
    /// AC-14 / GM-039 — a fresh install grants permissions to SystemAdmin only,
    /// one per private resource, and no other role holds any.
    /// </summary>
    /// <remarks>
    /// The fixture's captured numbers are 26 private resources and 26 rows. The
    /// count itself is a property of the legacy catalog, and the rebuild's catalog
    /// is smaller because CQ-003 dropped the broken About and Contact screens — so
    /// this asserts the fixture's actual invariant (one row per private resource,
    /// all SystemAdmin, nobody else granted) rather than the literal 26. Asserting
    /// the constant would fail for a reason that has nothing to do with DR-016.
    /// </remarks>
    [Fact]
    public async Task GM039_fresh_install_grants_only_SystemAdmin_against_every_private_resource()
    {
        var connectionString = await postgres.CreateMigratedDatabaseAsync(Ct);
        await using var harness = SeederHarness.Create(connectionString);

        await harness.Resolve<DatabaseSeeder>().SeedAsync(Ct);

        var context = harness.Resolve<DentalDbContext>();

        var privateResourceCount = await context.Resources.CountAsync(r => !r.IsPublic, Ct);
        var permissions = await context.Permissions.ToListAsync(Ct);

        Assert.Equal(privateResourceCount, permissions.Count);

        var systemAdmin = await harness.Resolve<RoleManager<IdentityRole>>()
            .FindByNameAsync(SeedCatalog.SystemAdminRole);
        Assert.NotNull(systemAdmin);

        Assert.All(permissions, permission => Assert.Equal(systemAdmin.Id, permission.RoleId));
        Assert.All(permissions, permission =>
            Assert.Equal(SeedCatalog.SystemAdminRole, permission.RoleName));

        // No other role holds any grant.
        Assert.DoesNotContain(permissions, permission => permission.RoleId != systemAdmin.Id);

        // Every private resource is covered exactly once.
        var grantedResourceIds = permissions.Select(p => p.ResourceId).ToHashSet();
        var privateResourceIds = await context.Resources
            .Where(r => !r.IsPublic)
            .Select(r => r.Id)
            .ToListAsync(Ct);
        Assert.Equal(privateResourceIds.ToHashSet(), grantedResourceIds);
    }

    /// <summary>
    /// AC-14 — public resources get no grant, because DR-015 short-circuits on them.
    /// </summary>
    [Fact]
    public async Task Public_resources_receive_no_permission_rows()
    {
        var connectionString = await postgres.CreateMigratedDatabaseAsync(Ct);
        await using var harness = SeederHarness.Create(connectionString);

        await harness.Resolve<DatabaseSeeder>().SeedAsync(Ct);

        var context = harness.Resolve<DentalDbContext>();
        var publicResourceIds = await context.Resources
            .Where(resource => resource.IsPublic)
            .Select(resource => resource.Id)
            .ToListAsync(Ct);

        Assert.NotEmpty(publicResourceIds);

        var grantedResourceIds = await context.Permissions
            .Select(permission => permission.ResourceId)
            .ToListAsync(Ct);

        Assert.DoesNotContain(grantedResourceIds, id => publicResourceIds.Contains(id));
    }

    /// <summary>
    /// AC-15 / GM-040 — re-running the seeder creates zero further permission rows.
    /// </summary>
    [Fact]
    public async Task GM040_reseeding_creates_no_additional_permission_rows()
    {
        var connectionString = await postgres.CreateMigratedDatabaseAsync(Ct);
        await using var harness = SeederHarness.Create(connectionString);

        var seeder = harness.Resolve<DatabaseSeeder>();
        await seeder.SeedAsync(Ct);

        var context = harness.Resolve<DentalDbContext>();
        var afterFirstRun = await context.Permissions.CountAsync(Ct);
        Assert.True(afterFirstRun > 0, "first seed run should have created grants");

        await seeder.SeedAsync(Ct);

        Assert.Equal(afterFirstRun, await context.Permissions.CountAsync(Ct));
    }

    /// <summary>
    /// AC-15 — the roles, resources, and doctor seeds are re-runnable too, not just
    /// the permission step whose guard a fixture happens to pin.
    /// </summary>
    [Fact]
    public async Task Reseeding_does_not_duplicate_roles_resources_or_doctors()
    {
        var connectionString = await postgres.CreateMigratedDatabaseAsync(Ct);
        await using var harness = SeederHarness.Create(connectionString);

        var seeder = harness.Resolve<DatabaseSeeder>();
        await seeder.SeedAsync(Ct);
        await seeder.SeedAsync(Ct);
        await seeder.SeedAsync(Ct);

        var context = harness.Resolve<DentalDbContext>();

        Assert.Equal(SeedCatalog.RoleNames.Length, await context.Roles.CountAsync(Ct));
        Assert.Equal(SeedCatalog.Resources.Length, await context.Resources.CountAsync(Ct));
        Assert.Equal(1, await context.Doctors.CountAsync(Ct));
    }

    /// <summary>All eight legacy role names are seeded, spelled exactly as legacy had them.</summary>
    [Fact]
    public async Task All_eight_legacy_roles_are_seeded()
    {
        var connectionString = await postgres.CreateMigratedDatabaseAsync(Ct);
        await using var harness = SeederHarness.Create(connectionString);

        await harness.Resolve<DatabaseSeeder>().SeedAsync(Ct);

        var context = harness.Resolve<DentalDbContext>();
        var roleNames = await context.Roles.Select(role => role.Name).ToListAsync(Ct);

        Assert.Equal(8, roleNames.Count);
        Assert.All(SeedCatalog.RoleNames, expected => Assert.Contains(expected, roleNames));
    }

    /// <summary>
    /// AC-16 — the seeded doctor is retrievable by the exact id the seeder assigned,
    /// and an appointment referencing it inserts without an FK violation.
    /// </summary>
    /// <remarks>
    /// This is the legacy defect stated as a test. There, <c>BaseModel.Id</c> was
    /// database-generated, so EF discarded the seeder's GUID and the client's
    /// hardcoded doctor id matched nothing — booking always failed
    /// <c>FK_dbo.Appointment_dbo.Doctor_DoctorId</c> on a freshly migrated database.
    /// </remarks>
    [Fact]
    public async Task AC16_seeded_doctor_id_persists_and_accepts_an_appointment()
    {
        var connectionString = await postgres.CreateMigratedDatabaseAsync(Ct);
        await using var harness = SeederHarness.Create(connectionString);

        await harness.Resolve<DatabaseSeeder>().SeedAsync(Ct);

        await using var verify = PostgresContainerFixture.CreateContext(connectionString);

        var seededDoctor = await verify.Doctors
            .SingleOrDefaultAsync(doctor => doctor.Id == SeedCatalog.SeededDoctorId, Ct);

        Assert.NotNull(seededDoctor);
        Assert.Equal(SeedCatalog.SeededDoctorCode, seededDoctor.Code);

        verify.Appointments.Add(TestEntities.Appointment(SeedCatalog.SeededDoctorId));
        await verify.SaveChangesAsync(Ct);

        Assert.Equal(1, await verify.Appointments.CountAsync(Ct));
    }

    /// <summary>
    /// An appointment pointing at a doctor id that does not exist is rejected —
    /// proving the previous test passes because the id is right, not because the
    /// constraint is missing.
    /// </summary>
    [Fact]
    public async Task Appointment_referencing_an_unknown_doctor_is_rejected()
    {
        var connectionString = await postgres.CreateMigratedDatabaseAsync(Ct);
        await using var harness = SeederHarness.Create(connectionString);
        await harness.Resolve<DatabaseSeeder>().SeedAsync(Ct);

        await using var context = PostgresContainerFixture.CreateContext(connectionString);
        context.Appointments.Add(TestEntities.Appointment(Guid.NewGuid()));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(Ct));
    }

    /// <summary>
    /// AC-17 / CQ-015 — a user holds exactly one primary role; a second assignment
    /// is rejected by the database.
    /// </summary>
    [Fact]
    public async Task AC17_a_user_cannot_hold_a_second_role()
    {
        var connectionString = await postgres.CreateMigratedDatabaseAsync(Ct);
        await using var harness = SeederHarness.Create(connectionString);
        await harness.Resolve<DatabaseSeeder>().SeedAsync(Ct);

        var userManager = harness.Resolve<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = "one.role", Email = "one.role@localhost" };

        Assert.True((await userManager.CreateAsync(user, "Valid!Password!2026")).Succeeded);
        Assert.True((await userManager.AddToRoleAsync(user, "Admin")).Succeeded);

        // The unique index on the Identity user-roles join table is what makes this
        // fail; without it a second role would silently widen the user's access.
        await Assert.ThrowsAnyAsync<Exception>(() => userManager.AddToRoleAsync(user, "Manager"));

        await using var verify = PostgresContainerFixture.CreateContext(connectionString);
        var roleCount = await verify.Database
            .SqlQuery<int>($"""
                SELECT COUNT(*)::int AS "Value" FROM identity."AspNetUserRoles"
                WHERE "UserId" = {user.Id}
                """)
            .SingleAsync(Ct);

        Assert.Equal(1, roleCount);
    }

    /// <summary>
    /// AC-18 — the production bootstrap refuses to run without configured
    /// credentials, rather than falling back to a default. This is the legacy
    /// default-credential exposure closed (CQ-017).
    /// </summary>
    [Fact]
    public async Task AC18_production_bootstrap_fails_clearly_without_configured_credentials()
    {
        var connectionString = await postgres.CreateMigratedDatabaseAsync(Ct);
        await using var harness = SeederHarness.Create(
            connectionString,
            new AdminBootstrapOptions { AllowDevelopmentDemoAccounts = false });

        await harness.Resolve<DatabaseSeeder>().SeedAsync(Ct);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => harness.Resolve<AdminAccountSeeder>().SeedAsync(Ct));

        Assert.Contains("environment configuration", exception.Message, StringComparison.Ordinal);

        await using var verify = PostgresContainerFixture.CreateContext(connectionString);
        Assert.Equal(0, await verify.Users.CountAsync(Ct));
    }

    /// <summary>
    /// AC-18 — with credentials supplied, the administrator is created and holds
    /// SystemAdmin.
    /// </summary>
    [Fact]
    public async Task Production_bootstrap_creates_the_administrator_from_configured_credentials()
    {
        var connectionString = await postgres.CreateMigratedDatabaseAsync(Ct);
        await using var harness = SeederHarness.Create(
            connectionString,
            new AdminBootstrapOptions
            {
                AllowDevelopmentDemoAccounts = false,
                UserName = "clinic.admin",
                Password = "Configured!Secret!2026",
            });

        await harness.Resolve<DatabaseSeeder>().SeedAsync(Ct);
        await harness.Resolve<AdminAccountSeeder>().SeedAsync(Ct);

        var userManager = harness.Resolve<UserManager<ApplicationUser>>();
        var admin = await userManager.FindByNameAsync("clinic.admin");

        Assert.NotNull(admin);
        Assert.Equal([SeedCatalog.SystemAdminRole], await userManager.GetRolesAsync(admin));
    }

    /// <summary>
    /// AC-18 — the demo accounts exist only on the explicitly-development path.
    /// </summary>
    [Fact]
    public async Task Demo_accounts_are_created_only_on_the_development_path()
    {
        var connectionString = await postgres.CreateMigratedDatabaseAsync(Ct);
        await using var harness = SeederHarness.Create(
            connectionString,
            new AdminBootstrapOptions { AllowDevelopmentDemoAccounts = true });

        await harness.Resolve<DatabaseSeeder>().SeedAsync(Ct);
        await harness.Resolve<AdminAccountSeeder>().SeedAsync(Ct);

        var userManager = harness.Resolve<UserManager<ApplicationUser>>();

        Assert.NotNull(await userManager.FindByNameAsync("superadmin"));
        Assert.NotNull(await userManager.FindByNameAsync("admin"));
    }

    /// <summary>
    /// AC-18 — the legacy shared password is gone from the codebase entirely, on
    /// every path.
    /// </summary>
    [Fact]
    public async Task The_legacy_shared_seed_password_is_not_accepted_anywhere()
    {
        var connectionString = await postgres.CreateMigratedDatabaseAsync(Ct);
        await using var harness = SeederHarness.Create(
            connectionString,
            new AdminBootstrapOptions { AllowDevelopmentDemoAccounts = true });

        await harness.Resolve<DatabaseSeeder>().SeedAsync(Ct);
        await harness.Resolve<AdminAccountSeeder>().SeedAsync(Ct);

        var userManager = harness.Resolve<UserManager<ApplicationUser>>();
        var superadmin = await userManager.FindByNameAsync("superadmin");

        Assert.NotNull(superadmin);
        Assert.False(await userManager.CheckPasswordAsync(superadmin, "123qwe"));
    }

    /// <summary>The seeded doctor carries the fixed clock's timestamp, not a live one.</summary>
    [Fact]
    public async Task Seeded_rows_take_their_timestamps_from_the_injected_clock()
    {
        var connectionString = await postgres.CreateMigratedDatabaseAsync(Ct);
        await using var harness = SeederHarness.Create(connectionString);

        await harness.Resolve<DatabaseSeeder>().SeedAsync(Ct);

        await using var verify = PostgresContainerFixture.CreateContext(connectionString);
        var doctor = await verify.Doctors.SingleAsync(Ct);

        Assert.Equal(TestEntities.FixedNow, doctor.Created);
        Assert.Equal(TestEntities.FixedNow, doctor.LastUpdate);
    }

    /// <summary>Resource ids are stable across runs, so re-seeding matches rather than duplicates.</summary>
    [Fact]
    public async Task Seeded_resource_ids_are_deterministic()
    {
        var first = await SeedAndReadResourceIdsAsync();
        var second = await SeedAndReadResourceIdsAsync();

        Assert.Equal(first, second);
    }

    private async Task<List<string>> SeedAndReadResourceIdsAsync()
    {
        var connectionString = await postgres.CreateMigratedDatabaseAsync(Ct);
        await using var harness = SeederHarness.Create(connectionString);

        await harness.Resolve<DatabaseSeeder>().SeedAsync(Ct);

        return await harness.Resolve<DentalDbContext>().Resources
            .OrderBy(resource => resource.Route)
            .Select(resource => resource.Id)
            .ToListAsync(Ct);
    }
}
