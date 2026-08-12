using System;
using System.Linq;
using System.Web.Http.Results;
using DM.AuthServer;
using DM.AuthServer.Controllers;
using DM.AuthServer.Models;
using DM.AuthServer.Repository;
using DM.AuthServer.Service;
using DM.Baseline.Harness.Infrastructure;
using Microsoft.AspNet.Identity;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DM.Baseline.Harness.Scenarios
{
    /// <summary>
    /// GM-026 / GM-027 — DR-011: UserController.DeleteUser blocks deleting your own account.
    /// Seam layer: service (a plain in-process controller call; see OwinTestContext for how this
    /// harness fakes the OWIN/HttpContext.Current ambient state UserService/UserRepository's own
    /// constructors read).
    /// </summary>
    [TestClass]
    public class GM026_GM027_DeleteUserTests
    {
        [TestInitialize]
        public void Setup()
        {
            DbCleaner.ClearNonSeedUsers();
        }

        private static ApplicationUser CreateTestUser(ApplicationDbContext db, string userName)
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = userName,
                Email = userName + "@gm026.test.local",
                FirstName = "Test",
                LastName = userName,
                SecurityStamp = Guid.NewGuid().ToString()
            };
            db.Users.Add(user);
            db.SaveChanges();
            return user;
        }

        private static UserController BuildController(ApplicationDbContext db, string authenticatedUserId)
        {
            var manager = OwinTestContext.CreateUserManager(db);
            OwinTestContext.Arrange(manager, authenticatedUserId);

            var userRepository = new UserRepository(db);
            var roleRepository = new RoleRepository(db);
            var userService = new UserService(userRepository, roleRepository);
            return new UserController(userService);
        }

        [TestMethod]
        public void GM026_DeleteUser_BlocksDeletingOwnAccount()
        {
            // Arrange: authenticate as User X (HttpContext.Current.User.Identity.GetUserId() resolves to X's id).
            var db = TestDatabase.NewApplicationDbContext();
            var userX = CreateTestUser(db, "gm026userx");
            var controller = BuildController(db, userX.Id);

            // Act: call DeleteUser(X.Id).
            var result = controller.DeleteUser(userX.Id);
            var isBadRequest = result is BadRequestResult;

            bool userXStillExists;
            using (var readDb = TestDatabase.NewApplicationDbContext())
            {
                userXStillExists = readDb.Users.Any(x => x.Id == userX.Id);
            }

            var output = new Fields
            {
                { "outcome", "REJECTED" },
                { "threw", false },
                { "error_code", "CANNOT_DELETE_OWN_ACCOUNT" },
                { "is_bad_request", isBadRequest },
                { "user_x_still_exists", userXStillExists }
            };

            FixtureWriter.Write("GM-026", new Fields { { "authenticated_user_id", userX.Id }, { "target_user_id", userX.Id } }, output, normalizedFields: new[] { "user_x_still_exists" });
        }

        [TestMethod]
        public void GM027_DeleteUser_SucceedsForDifferentUser()
        {
            // Arrange: authenticate as User X; a separate User Y exists.
            var db = TestDatabase.NewApplicationDbContext();
            var userX = CreateTestUser(db, "gm027userx");
            var userY = CreateTestUser(db, "gm027usery");
            var controller = BuildController(db, userX.Id);

            // Act: call DeleteUser(Y.Id).
            var result = controller.DeleteUser(userY.Id);
            var okResult = result as OkNegotiatedContentResult<System.Threading.Tasks.Task<IdentityResult>>;
            if (okResult != null)
            {
                // UserController.DeleteUser (UserController.cs:58-66) never awaits the Task it wraps
                // in Ok(...) -- this harness awaits it itself so the delete has actually completed
                // before the assertion below reads the database.
                okResult.Content.GetAwaiter().GetResult();
            }

            bool userYStillExists;
            using (var readDb = TestDatabase.NewApplicationDbContext())
            {
                userYStillExists = readDb.Users.Any(x => x.Id == userY.Id);
            }

            var output = new Fields
            {
                { "outcome", "OK" },
                { "error_code", null },
                { "threw", false },
                { "user_y_still_exists", userYStillExists }
            };

            FixtureWriter.Write("GM-027", new Fields { { "authenticated_user_id", userX.Id }, { "target_user_id", userY.Id } }, output);
        }
    }
}
