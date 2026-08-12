using System.Linq;

namespace DM.Baseline.Harness.Infrastructure
{
    /// <summary>
    /// Every scenario in this harness runs against the one shared LocalDB database
    /// <see cref="TestDatabase"/> seeds once per test-run (MSTest v1 has no per-assembly database
    /// snapshot/rollback primitive to fall back on). Any scenario whose own assertion depends on an
    /// *exact* row count over a table other tests can also touch (Roles/Users/Permissions counts --
    /// GM-033..GM-040) must call the matching Clear* method as its own first Arrange step, so the
    /// scenario is self-contained and order-independent regardless of what ran before it in the same
    /// process. Scenarios scoped to their own freshly-created Guid-keyed rows (almost all of the
    /// Dental-domain ones) do not need this at all and do not call it.
    ///
    /// Deletes are ordered child-before-parent to satisfy the legacy app's own FK constraints
    /// (DM.Models/DentalDbContext.cs's OnModelCreating adds no cascade override beyond EF6's own
    /// default -- see seams.md's persistence-seam findings -- so a parent-before-child delete would
    /// throw a live FK-violation SqlException here, not silently succeed).
    /// </summary>
    public static class DbCleaner
    {
        /// <summary>
        /// Clears every Dental-domain table this harness's Patient/Prescription/Product/Inventory/
        /// Appointment scenarios can write to. Leaves Statuses and Doctors alone -- both are one-time
        /// migration seed data (DM.Models/Migrations/Configuration.cs) that no scenario in this
        /// harness mutates.
        /// </summary>
        public static void ClearDentalDomainTables()
        {
            using (var db = TestDatabase.NewDentalDbContext())
            {
                db.Payments.RemoveRange(db.Payments.ToList());
                db.PatientMedicalServices.RemoveRange(db.PatientMedicalServices.ToList());
                db.PatientMedicalInfos.RemoveRange(db.PatientMedicalInfos.ToList());
                db.Prescriptions.RemoveRange(db.Prescriptions.ToList());
                db.Patients.RemoveRange(db.Patients.ToList());
                db.MedicalServices.RemoveRange(db.MedicalServices.ToList());
                db.MedicalInfos.RemoveRange(db.MedicalInfos.ToList());
                db.Inventories.RemoveRange(db.Inventories.ToList());
                db.Products.RemoveRange(db.Products.ToList());
                db.Appointments.RemoveRange(db.Appointments.ToList());
                db.SaveChanges();
            }
        }

        /// <summary>
        /// Clears every Permission row -- the exact precondition GM-039's own Arrange step
        /// ("zero Permission rows exist yet") requires, regardless of whether AssemblyInit's own
        /// seed pass, or an earlier test, already created some.
        /// </summary>
        public static void ClearPermissions()
        {
            using (var db = TestDatabase.NewApplicationDbContext())
            {
                db.Permissions.RemoveRange(db.Permissions.ToList());
                db.SaveChanges();
            }
        }

        /// <summary>
        /// Deletes every ApplicationUser except the two migration-seeded accounts
        /// ("superadmin"/"admin", DM.Server/Migrations/Configuration.cs's AddUsers) -- used by
        /// GM-033/GM-034/GM-035, whose assertions depend on an exact total user count.
        /// </summary>
        public static void ClearNonSeedUsers()
        {
            using (var db = TestDatabase.NewApplicationDbContext())
            {
                var nonSeedUsers = db.Users
                    .Where(u => u.UserName != "superadmin" && u.UserName != "admin")
                    .ToList();

                foreach (var user in nonSeedUsers)
                {
                    db.Users.Remove(user);
                }

                db.SaveChanges();
            }
        }
    }
}
