using System;
using System.Data.Entity.Migrations;
using System.Linq;
using DM.AuthServer.Models;
using DM.Models;

namespace DM.Baseline.Harness.Infrastructure
{
    /// <summary>
    /// Owns the one shared LocalDB database every scenario arranges against (the "DefaultConnection"
    /// connection string in App.config, read by both DentalDbContext and ApplicationDbContext -- see
    /// App.config's own comment and CQ-002's finding that both legacy contexts already share one
    /// physical database).
    ///
    /// <see cref="EnsureSchemaAndSeed"/> runs each legacy DbContext's own EF6 code-first migrations
    /// (DM.Models.Migrations.Configuration for DentalDbContext, DM.AuthServer.Migrations.Configuration
    /// for ApplicationDbContext) via <see cref="DbMigrator"/>, exactly as Visual Studio's own
    /// Package-Manager-Console `Update-Database` would. This also creates the database itself if it
    /// does not exist yet (standard EF6 migrator behaviour) and runs each Configuration's own
    /// <c>Seed(...)</c> method once, which is what actually populates the Statuses/Doctor/Roles/
    /// Resources/Permissions/Users rows every scenario's Arrange step depends on.
    ///
    /// DentalDbContext's own static constructor already disables EF's model-hash safety check
    /// (`Database.SetInitializer&lt;DentalDbContext&gt;(null)`, DM.Models/DentalDbContext.cs:17) --
    /// that instructs the *ambient* EF initializer never to run automatically; it does not stop us
    /// from explicitly driving a DbMigrator against the same context type ourselves, which is exactly
    /// what this class does.
    /// </summary>
    public static class TestDatabase
    {
        private static bool _initialized;
        private static readonly object InitLock = new object();

        public static void EnsureSchemaAndSeed()
        {
            lock (InitLock)
            {
                if (_initialized) return;

                var dentalConfig = ReflectionHelpers.CreateMigrationsConfiguration(
                    "DM.Models.Migrations.Configuration", "DM.Models");

                // DEFECT WORKAROUND (legacy repo, not this harness): the migration chain cannot
                // build a database from scratch. 201512281828016_InitialCreate creates dbo.Patient
                // with .Index(t => t.Code, unique: true) -- EF names that index IX_Code -- and
                // 201601070523052_PatientModelCodeFieldModified drops and recreates it under the
                // same name. The newest migration, 202509030639057_Patient_Code_Unique, then runs
                // CreateIndex("dbo.Patient", "Code", unique: true) with no preceding DropIndex, so
                // Update() against an empty database always fails with:
                //   "The operation failed because an index or statistics with name 'IX_Code'
                //    already exists on table 'dbo.Patient'."
                // Existing deployed databases never hit this (they already had IX_Code before that
                // migration was authored), which is why it went unnoticed.
                //
                // We migrate in two steps: up to the last migration that applies cleanly, then drop
                // the pre-existing IX_Code so the final migration's CreateIndex succeeds, then run
                // the chain to completion. The end state is exactly what the full chain intends.
                //
                // It must finish with a no-argument Update(): EF6 runs Configuration.Seed() ONLY when
                // migrating to the latest migration. Passing an explicit target skips Seed entirely,
                // which leaves dbo.Status and dbo.Doctor empty -- and every StatusId FK insert then
                // fails silently inside BaseService's blanket catch, so scenarios fail with an empty
                // table rather than a visible error. Do not "simplify" this back to a single targeted
                // Update().
                const string LastCleanDentalMigration = "202309011822536_PatientMedicalInfo_Entity_Updated";
                var dentalMigrator = new DbMigrator(dentalConfig);
                var alreadyComplete = dentalMigrator.GetPendingMigrations().Any() == false;

                if (!alreadyComplete)
                {
                    dentalMigrator.Update(LastCleanDentalMigration);
                    DropPatientCodeIndexIfPresent();
                }

                new DbMigrator(dentalConfig).Update();

                var authConfig = ReflectionHelpers.CreateMigrationsConfiguration(
                    "DM.AuthServer.Migrations.Configuration", "DM.AuthServer");
                new DbMigrator(authConfig).Update();

                _initialized = true;
            }
        }

        /// <summary>
        /// Drops dbo.Patient's IX_Code if it is already there, so 202509030639057_Patient_Code_Unique's
        /// unguarded CreateIndex can succeed. See the defect note in <see cref="EnsureSchemaAndSeed"/>.
        /// </summary>
        private static void DropPatientCodeIndexIfPresent()
        {
            using (var db = new DentalDbContext())
            {
                db.Database.ExecuteSqlCommand(
                    "IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Code' AND object_id = OBJECT_ID('dbo.Patient')) " +
                    "DROP INDEX IX_Code ON dbo.Patient;");
            }
        }

        public static DentalDbContext NewDentalDbContext()
        {
            return new DentalDbContext();
        }

        public static ApplicationDbContext NewApplicationDbContext()
        {
            return new ApplicationDbContext();
        }

        /// <summary>
        /// Seeded Status.Id values, per DM.Models/Migrations/Configuration.cs:46-62's AddStatus --
        /// an IDENTITY column populated in this literal insertion order, re-confirmed directly this
        /// run rather than assumed.
        /// </summary>
        public static class StatusIds
        {
            public const int InStock = 1;
            public const int OutOfStock = 2;
            public const int Received = 3;
            public const int Shipped = 4;
            public const int Active = 5;
            public const int Closed = 6;
            public const int Appointed = 7;
            public const int Visited = 8;
        }

        /// <summary>
        /// The single seeded Doctor row's id, resolved from the database at runtime.
        ///
        /// It must NOT be hardcoded to 9b6ba3ad-c9be-e511-9bf4-402cf40f4b2f, even though
        /// DM.Models/Migrations/Configuration.cs:35's AddDoctor assigns exactly that literal:
        /// BaseModel.Id carries [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        /// (DM.Models/BaseModel.cs:14), so EF silently DISCARDS the assigned value and SQL Server
        /// generates a different GUID. Verified against a freshly migrated database this run --
        /// the seeded doctor came back as f3f9dfe3-1b96-f111-bb45-900f0ca86b6c, not the literal.
        ///
        /// That divergence is itself a legacy finding, related to but distinct from CQ-014: the
        /// AngularJS client hardcodes 9b6ba3ad-... as DoctorId
        /// (patient-appointment.controller.js:10), so on any freshly migrated database no Doctor
        /// row bearing that id exists and appointment creation fails the
        /// FK_dbo.Appointment_dbo.Doctor_DoctorId constraint.
        /// </summary>
        public static Guid SeededDoctorId
        {
            get
            {
                if (_seededDoctorId == null)
                {
                    using (var db = new DentalDbContext())
                    {
                        var doctor = db.Doctors.OrderBy(d => d.Code).FirstOrDefault();
                        if (doctor == null)
                            throw new InvalidOperationException(
                                "No seeded Doctor row found -- DM.Models.Migrations.Configuration.Seed's " +
                                "AddDoctor did not run. EF6 runs Seed only on a no-argument DbMigrator.Update().");
                        _seededDoctorId = doctor.Id;
                    }
                }
                return _seededDoctorId.Value;
            }
        }

        private static Guid? _seededDoctorId;

        public static string RoleId(ApplicationDbContext db, string roleName)
        {
            var role = db.Roles.FirstOrDefault(r => r.Name == roleName);
            if (role == null)
                throw new InvalidOperationException(
                    "Role '" + roleName + "' was not found. Confirm AddRoles' seed ran (EnsureSchemaAndSeed).");
            return role.Id;
        }

        public static string ResourceId(ApplicationDbContext db, string route)
        {
            var resource = db.Resources.FirstOrDefault(r => r.Route == route);
            if (resource == null)
                throw new InvalidOperationException(
                    "Resource with route '" + route + "' was not found. Confirm AddResources' seed ran (EnsureSchemaAndSeed).");
            return resource.Id;
        }
    }
}
