using DentalManagement.Domain.Abstractions;
using DentalManagement.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DentalManagement.Infrastructure.Persistence.Seeding;

/// <summary>
/// Brings a freshly migrated database to the state a fresh install starts in:
/// roles, the protected-screen catalog, SystemAdmin's grants, and one doctor.
/// </summary>
/// <remarks>
/// Every step is guarded so re-running is a no-op rather than a duplicate insert.
/// The permission step's guard is behaviour a captured fixture pins: GM-040
/// records legacy creating zero rows once any <c>Permission</c> row exists
/// (spec FR-17, AC-15).
/// </remarks>
public class DatabaseSeeder(
    DentalDbContext context,
    RoleManager<IdentityRole> roleManager,
    IClock clock)
{
    /// <summary>
    /// Seeds roles, resources, SystemAdmin permissions, and the default doctor.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync();
        await SeedResourcesAsync(cancellationToken);
        await SeedSystemAdminPermissionsAsync(cancellationToken);
        await SeedDefaultDoctorAsync(cancellationToken);
    }

    private async Task SeedRolesAsync()
    {
        foreach (var roleName in SeedCatalog.RoleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Could not seed role '{roleName}': " +
                        string.Join("; ", result.Errors.Select(error => error.Description)));
                }
            }
        }
    }

    /// <summary>
    /// Seeds the resource catalog, matching on <see cref="Resource.Route"/>.
    /// </summary>
    /// <remarks>
    /// Route is the natural key, not the primary key: a migrated legacy row carries
    /// its own <c>Id</c> but the same route, and matching on route is what keeps the
    /// catalog from doubling after a data migration.
    /// </remarks>
    private async Task SeedResourcesAsync(CancellationToken cancellationToken)
    {
        var existingRoutes = await context.Resources
            .Select(resource => resource.Route)
            .ToListAsync(cancellationToken);

        var missing = SeedCatalog.Resources
            .Where(seed => !existingRoutes.Contains(seed.Route))
            .Select(seed => new Resource
            {
                Id = DeterministicGuid.StringFrom($"Resource:{seed.Route}"),
                Name = seed.Name,
                Route = seed.Route,
                IsPublic = seed.IsPublic,
            })
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        context.Resources.AddRange(missing);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// DR-016: a fresh install grants permissions to SystemAdmin only, against
    /// every private resource. Every other role starts with zero grants.
    /// </summary>
    private async Task SeedSystemAdminPermissionsAsync(CancellationToken cancellationToken)
    {
        // GM-040's guard: once any Permission row exists, this step does nothing.
        if (await context.Permissions.AnyAsync(cancellationToken))
        {
            return;
        }

        var systemAdmin = await roleManager.FindByNameAsync(SeedCatalog.SystemAdminRole)
            ?? throw new InvalidOperationException(
                $"Role '{SeedCatalog.SystemAdminRole}' must be seeded before its permissions.");

        var privateResources = await context.Resources
            .Where(resource => !resource.IsPublic)
            .ToListAsync(cancellationToken);

        var grants = privateResources.Select(resource => new Permission
        {
            Id = DeterministicGuid.StringFrom($"Permission:{systemAdmin.Id}:{resource.Id}"),
            RoleId = systemAdmin.Id,
            RoleName = systemAdmin.Name,
            ResourceId = resource.Id,
        });

        context.Permissions.AddRange(grants);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedDefaultDoctorAsync(CancellationToken cancellationToken)
    {
        if (await context.Doctors.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = clock.Now;

        context.Doctors.Add(new Doctor
        {
            // Persisted exactly as assigned — see SeedCatalog.SeededDoctorId.
            Id = SeedCatalog.SeededDoctorId,
            Code = SeedCatalog.SeededDoctorCode,
            Name = SeedCatalog.SeededDoctorName,
            Created = now,
            LastUpdate = now,
        });

        await context.SaveChangesAsync(cancellationToken);
    }
}
