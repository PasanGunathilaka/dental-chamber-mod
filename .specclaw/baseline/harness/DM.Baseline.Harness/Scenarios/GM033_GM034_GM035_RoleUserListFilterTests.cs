using System.Collections.Generic;
using System.Linq;
using DM.AuthServer.Models;
using DM.AuthServer.Repository;
using DM.AuthServer.Service;
using DM.Baseline.Harness.Infrastructure;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DM.Baseline.Harness.Scenarios
{
    /// <summary>
    /// GM-033 / GM-034 / GM-035 — DR-014: RoleService.GetAll and UserService.GetUsers both hide the
    /// SystemAdmin role/its users from a non-SystemAdmin caller, and show everything to a SystemAdmin
    /// caller. Seam layer: service.
    ///
    /// Reuses the two migration-seeded ApplicationUsers ("superadmin" = SystemAdmin,
    /// "admin" = Admin, DM.Server/Migrations/Configuration.cs's AddUsers) as the "two seeded users"
    /// scenarios.md's own Arrange text calls for, rather than creating new ones -- DbCleaner's own
    /// ClearNonSeedUsers guarantees exactly these two exist and nothing else.
    /// </summary>
    [TestClass]
    public class GM033_GM034_GM035_RoleUserListFilterTests
    {
        [TestInitialize]
        public void Setup()
        {
            DbCleaner.ClearNonSeedUsers();
        }

        private static UserService BuildUserService(ApplicationDbContext db, params string[] roles)
        {
            var manager = OwinTestContext.CreateUserManager(db);
            OwinTestContext.Arrange(manager, "test-caller", roles);

            var userRepository = new UserRepository(db);
            var roleRepository = new RoleRepository(db);
            return new UserService(userRepository, roleRepository);
        }

        private static RoleService BuildRoleService(ApplicationDbContext db, params string[] roles)
        {
            var manager = OwinTestContext.CreateUserManager(db);
            OwinTestContext.Arrange(manager, "test-caller", roles);

            return new RoleService(new RoleRepository(db));
        }

        [TestMethod]
        public void GM033_RoleServiceGetAll_HidesSystemAdminFromNonSystemAdminCaller()
        {
            // Arrange: the seeded 8 roles exist; authenticate as a caller in the "Admin" role
            // (not "SystemAdmin").
            var db = TestDatabase.NewApplicationDbContext();
            var service = BuildRoleService(db, "Admin");

            // Act
            var roles = service.GetAll();

            var output = new Fields
            {
                { "outcome", "OK" },
                { "error_code", null },
                { "threw", false },
                { "roles_returned_count", roles.Count },
                { "roles_include_system_admin", roles.Any(x => x.Name == "SystemAdmin") }
            };

            FixtureWriter.Write("GM-033", new Fields { { "caller_role", "Admin" } }, output);
        }

        [TestMethod]
        public void GM034_UserServiceGetUsers_HidesSystemAdminUsersFromNonSystemAdminCaller()
        {
            // Arrange: two seeded users, one SystemAdmin ("superadmin"), one Admin ("admin");
            // authenticate as the Admin caller.
            var db = TestDatabase.NewApplicationDbContext();
            var service = BuildUserService(db, "Admin");

            // Act. Open question, not resolved here (scenarios.md's own note): UserService.GetUsers'
            // `users.Remove(user)` (UserService.cs:54-57) relies on the two independently-queried
            // lists containing the exact same object instance per user for reference-equality removal
            // to succeed -- this test records whatever the real EF6 identity map actually does, rather
            // than assuming either way.
            List<ApplicationUser> users = service.GetUsers();

            var output = new Fields
            {
                { "outcome", "OK" },
                { "error_code", null },
                { "threw", false },
                { "users_returned_count", users.Count },
                { "users_include_system_admin_user", users.Any(x => x.UserName == "superadmin") }
            };

            FixtureWriter.Write("GM-034", new Fields { { "caller_role", "Admin" } }, output);
        }

        [TestMethod]
        public void GM035_BothListFilters_ShowEverythingWhenCallerIsSystemAdmin()
        {
            // Arrange: same seed data as GM-033/GM-034; authenticate as a SystemAdmin caller.
            var db = TestDatabase.NewApplicationDbContext();
            var roleService = BuildRoleService(db, "SystemAdmin");
            var userService = BuildUserService(db, "SystemAdmin");

            // Act
            var roles = roleService.GetAll();
            var users = userService.GetUsers();

            var output = new Fields
            {
                { "outcome", "OK" },
                { "error_code", null },
                { "threw", false },
                { "roles_returned_count", roles.Count },
                { "users_returned_count", users.Count }
            };

            FixtureWriter.Write("GM-035", new Fields { { "caller_role", "SystemAdmin" } }, output);
        }
    }
}
