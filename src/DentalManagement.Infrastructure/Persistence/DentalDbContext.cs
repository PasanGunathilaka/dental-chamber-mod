using DentalManagement.Domain.Entities;
using DentalManagement.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DentalManagement.Infrastructure.Persistence;

/// <summary>
/// The single application context covering both the domain schema and the
/// identity/permission schema.
/// </summary>
/// <remarks>
/// <para>
/// The legacy app ran two independently-migrated contexts
/// (<c>DentalDbContext</c> + <c>ApplicationDbContext</c>) over one physical
/// database, with two migration pipelines and no recorded reason. CQ-002
/// consolidates them: "one PostgreSQL database and one EF Core application
/// DbContext/schema ... Authentication/Identity tables and domain tables may
/// remain logically separated by configuration/naming, but they should share one
/// controlled migration history instead of two independent migration pipelines."
/// </para>
/// <para>
/// That is implemented literally here — Identity tables are named into the
/// <c>identity</c> PostgreSQL schema, domain tables stay in <c>public</c>, and
/// there is exactly one migration history (spec FR-02, AC-02, design D-2).
/// </para>
/// </remarks>
public class DentalDbContext(DbContextOptions<DentalDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public const string IdentitySchema = "identity";

    /// <summary>
    /// Every <c>Created</c>/<c>LastUpdate</c> column uses this type. See
    /// <see cref="Time.SystemClock"/> for why the rebuild does not convert legacy
    /// local timestamps to UTC (spec A8, design D-8).
    /// </summary>
    internal const string TimestampColumnType = "timestamp without time zone";

    /// <summary>Money columns: fixed precision, never floating point (NFR-04).</summary>
    internal const string MoneyColumnType = "numeric(18,2)";

    public DbSet<Patient> Patients => Set<Patient>();

    public DbSet<Prescription> Prescriptions => Set<Prescription>();

    public DbSet<PatientMedicalService> PatientMedicalServices => Set<PatientMedicalService>();

    public DbSet<MedicalService> MedicalServices => Set<MedicalService>();

    public DbSet<MedicalInfo> MedicalInfos => Set<MedicalInfo>();

    public DbSet<PatientMedicalInfo> PatientMedicalInfos => Set<PatientMedicalInfo>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Inventory> Inventories => Set<Inventory>();

    public DbSet<Doctor> Doctors => Set<Doctor>();

    public DbSet<Appointment> Appointments => Set<Appointment>();

    public DbSet<Resource> Resources => Set<Resource>();

    public DbSet<Permission> Permissions => Set<Permission>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(DentalDbContext).Assembly);

        MoveIdentityTablesToOwnSchema(builder);
        EnforceOnePrimaryRolePerUser(builder);
        PinTimestampsToLocalWallClock(builder);
        AddPatientCodeSequence(builder);
    }

    /// <summary>
    /// CQ-002's "logically separated by configuration/naming" half — one context
    /// and one migration history, two schemas.
    /// </summary>
    private static void MoveIdentityTablesToOwnSchema(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>().ToTable("AspNetUsers", IdentitySchema);
        builder.Entity<IdentityRole>().ToTable("AspNetRoles", IdentitySchema);
        builder.Entity<IdentityUserRole<string>>().ToTable("AspNetUserRoles", IdentitySchema);
        builder.Entity<IdentityUserClaim<string>>().ToTable("AspNetUserClaims", IdentitySchema);
        builder.Entity<IdentityUserLogin<string>>().ToTable("AspNetUserLogins", IdentitySchema);
        builder.Entity<IdentityUserToken<string>>().ToTable("AspNetUserTokens", IdentitySchema);
        builder.Entity<IdentityRoleClaim<string>>().ToTable("AspNetRoleClaims", IdentitySchema);
    }

    /// <summary>
    /// CQ-015: "preserve and explicitly enforce one primary role per user in this
    /// rebuild because that matches the observed legacy UI behaviour."
    /// </summary>
    /// <remarks>
    /// Enforced by the database, not by a service-layer check that a later caller
    /// could bypass: a unique index on the join table's user column means a second
    /// role assignment fails outright (spec FR-14, AC-17). Fine-grained
    /// Resource/Permission grants are a separate table and are unaffected.
    /// </remarks>
    private static void EnforceOnePrimaryRolePerUser(ModelBuilder builder)
    {
        builder.Entity<IdentityUserRole<string>>()
            .HasIndex(userRole => userRole.UserId)
            .IsUnique()
            .HasDatabaseName("IX_AspNetUserRoles_UserId_Unique");
    }

    /// <summary>
    /// Maps every <see cref="DateTime"/> property to
    /// <c>timestamp without time zone</c>.
    /// </summary>
    /// <remarks>
    /// Applied model-wide rather than property-by-property so a newly added
    /// timestamp cannot silently fall back to Npgsql's default
    /// <c>timestamp with time zone</c> mapping, which would both reinterpret the
    /// value and reject an unspecified-kind <see cref="DateTime"/> at write time
    /// (design R-6).
    /// </remarks>
    private static void PinTimestampsToLocalWallClock(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetColumnType(TimestampColumnType);
                }
            }
        }
    }

    /// <summary>
    /// DR-001: legacy generated the next patient code from
    /// <c>GetPatientViewModel().Count() + 1</c> — two concurrent registrations can
    /// read the same count before either inserts, producing the same code for
    /// both. That is the exact mechanism behind <c>GM-002</c>'s duplicate-code
    /// fixture.
    /// </summary>
    /// <remarks>
    /// A PostgreSQL sequence replaces it. <c>nextval</c> is non-blocking and
    /// transactional-safe, so two concurrent callers are guaranteed two different
    /// values without either waiting on the other (spec FR-08, A4; AC-06). See
    /// <see cref="DentalManagement.Infrastructure.Patients.PatientCodeSequence"/>
    /// for the read side.
    /// </remarks>
    private static void AddPatientCodeSequence(ModelBuilder builder)
    {
        builder.HasSequence<long>("patient_code_seq").StartsAt(1).IncrementsBy(1);
    }
}
