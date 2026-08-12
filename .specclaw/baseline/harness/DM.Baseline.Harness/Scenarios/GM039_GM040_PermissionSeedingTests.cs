using System;
using System.Linq;
using DM.Baseline.Harness.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DM.Baseline.Harness.Scenarios
{
    /// <summary>
    /// GM-039 / GM-040 — DR-016: fresh-install permission seeding
    /// (`DM.AuthServer.Migrations.Configuration.AddPermissions(ApplicationDbContext)`) grants only
    /// SystemAdmin, against every private Resource, and only ever runs once. Seam layer: service.
    ///
    /// AddPermissions is `private static`, so this harness invokes it via reflection
    /// (Infrastructure/ReflectionHelpers.cs) -- the same mechanism EF6's own migration tooling uses to
    /// call a migrations Configuration's Seed(...), never a modification of the legacy source.
    /// </summary>
    [TestClass]
    public class GM039_GM040_PermissionSeedingTests
    {
        private static readonly Type ConfigurationType = Type.GetType("DM.AuthServer.Migrations.Configuration, DM.AuthServer");

        [TestMethod]
        public void GM039_AddPermissions_FreshInstall_GrantsOnlySystemAdmin()
        {
            // Arrange: roles and resources already seeded (AssemblyInit's EnsureSchemaAndSeed already
            // ran AddRoles/AddResources); zero Permission rows exist yet -- this test clears
            // Permissions itself as its own Arrange step so it is self-contained regardless of
            // whatever already ran in this same test process (see DbCleaner's own doc comment).
            DbCleaner.ClearPermissions();

            var db = TestDatabase.NewApplicationDbContext();
            var privateResourceCountBefore = db.Resources.Count(x => !x.IsPublic);

            // Act
            ReflectionHelpers.InvokePrivateStatic(ConfigurationType, "AddPermissions", db);

            int permissionRowsCreated;
            bool allCreatedRowsRoleIsSystemAdmin;
            bool otherRolesHaveAnyPermission;
            using (var readDb = TestDatabase.NewApplicationDbContext())
            {
                var permissions = readDb.Permissions.ToList();
                permissionRowsCreated = permissions.Count;
                allCreatedRowsRoleIsSystemAdmin = permissions.Count > 0 && permissions.All(x => x.RoleName == "SystemAdmin");
                otherRolesHaveAnyPermission = permissions.Any(x => x.RoleName != "SystemAdmin");
            }

            var output = new Fields
            {
                { "outcome", "OK" },
                { "error_code", null },
                { "threw", false },
                { "permission_rows_created_count", permissionRowsCreated },
                { "all_created_rows_role_is_system_admin", allCreatedRowsRoleIsSystemAdmin },
                { "other_roles_have_any_permission", otherRolesHaveAnyPermission }
            };

            FixtureWriter.Write("GM-039", new Fields { { "private_resource_count", privateResourceCountBefore }, { "permission_rows_before", 0 } }, output);
        }

        [TestMethod]
        public void GM040_AddPermissions_AlreadySeeded_IsNoOp()
        {
            // Arrange: one Permission row already exists (any role/resource).
            DbCleaner.ClearPermissions();

            string existingRoleId;
            string existingResourceId;
            using (var seedDb = TestDatabase.NewApplicationDbContext())
            {
                existingRoleId = TestDatabase.RoleId(seedDb, "SystemAdmin");
                existingResourceId = seedDb.Resources.First(x => !x.IsPublic).Id;

                seedDb.Permissions.Add(new DM.AuthServer.Models.SecurityModels.Permission
                {
                    Id = Guid.NewGuid().ToString(),
                    RoleId = existingRoleId,
                    RoleName = "SystemAdmin",
                    ResourceId = existingResourceId
                });
                seedDb.SaveChanges();
            }

            var db = TestDatabase.NewApplicationDbContext();

            // Act: call AddPermissions(db) again.
            ReflectionHelpers.InvokePrivateStatic(ConfigurationType, "AddPermissions", db);

            int permissionRowsAfter;
            using (var readDb = TestDatabase.NewApplicationDbContext())
            {
                permissionRowsAfter = readDb.Permissions.Count();
            }

            var output = new Fields
            {
                { "outcome", "OK" },
                { "error_code", null },
                { "threw", false },
                // Configuration.cs:70's `if (!db.Permissions.Any())` guard skips the whole build-list
                // step entirely once any row exists -- 1 row before, 1 row after (0 newly created).
                { "permission_rows_created_count", permissionRowsAfter - 1 }
            };

            FixtureWriter.Write("GM-040", new Fields { { "permission_rows_before", 1 } }, output);
        }
    }
}
