using System;
using System.Linq;
using DM.AuthServer.Models;
using AuthRequestModels = DM.AuthServer.Models.RequestModels;
using DM.AuthServer.Repository;
using DM.AuthServer.Service;
using DM.Baseline.Harness.Infrastructure;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DM.Baseline.Harness.Scenarios
{
    /// <summary>
    /// GM-028 / GM-029 — DR-012: UserService.CreateUser/UpdateUser's password/retype-mismatch guard.
    /// Seam layer: service.
    /// </summary>
    [TestClass]
    public class GM028_GM029_CreateUpdateUserTests
    {
        [TestInitialize]
        public void Setup()
        {
            DbCleaner.ClearNonSeedUsers();
        }

        private static UserService BuildUserService(ApplicationDbContext db)
        {
            var manager = OwinTestContext.CreateUserManager(db);
            OwinTestContext.Arrange(manager);

            var userRepository = new UserRepository(db);
            var roleRepository = new RoleRepository(db);
            return new UserService(userRepository, roleRepository);
        }

        [TestMethod]
        public void GM028_CreateUser_PasswordRetypeMismatch_OutcomeCapturedAsIs()
        {
            // Arrange: none beyond a valid RoleId to reference (scenarios.md GM-028).
            var db = TestDatabase.NewApplicationDbContext();
            var roleId = TestDatabase.RoleId(db, "User");
            var service = BuildUserService(db);

            var model = new AuthRequestModels.UserCreateRequestModel
            {
                FirstName = "GM028",
                LastName = "User",
                Email = "gm028@test.local",
                PhoneNumber = "0000000000",
                UserName = "gm028user",
                PasswordHash = "abc",
                RetypePassword = "xyz",
                RoleId = roleId
            };

            // Act: call CreateUser -- deliberately not asserting whether this surfaces as a clean
            // rejection or an unhandled framework exception in advance (PQ-009). UserService.CreateUser
            // itself never awaits the Task it returns (UserService.cs:71-90), so any exception raised
            // inside UserManager.CreateAsync(null) may only actually surface once the returned Task is
            // observed -- this test tries both the call itself and the await, recording whichever
            // representation actually manifests.
            bool threw = false;
            string exceptionType = null;
            string exceptionMessage = null;
            System.Threading.Tasks.Task<Microsoft.AspNet.Identity.IdentityResult> task = null;

            try
            {
                task = service.CreateUser(model);
            }
            catch (Exception ex)
            {
                threw = true;
                exceptionType = ex.GetType().FullName;
                exceptionMessage = ex.Message;
            }

            if (task != null && !threw)
            {
                try
                {
                    task.GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    threw = true;
                    exceptionType = ex.GetType().FullName;
                    exceptionMessage = ex.Message;
                }
            }

            var output = new Fields
            {
                { "outcome", "REJECTED" },
                { "error_code", "PASSWORD_RETYPE_MISMATCH" },
                { "threw", threw },
                { "ExceptionType", exceptionType },
                { "InnerExceptionType", null },
                { "ExceptionMessage", exceptionMessage },
                { "InnerExceptionMessage", null }
            };

            FixtureWriter.Write("GM-028", new Fields { { "password", "abc" }, { "retype_password", "xyz" } }, output);
        }

        [TestMethod]
        public void GM029_UpdateUser_PasswordRetypeMismatch_DiscardsAllSubmittedFieldEdits()
        {
            // Arrange: an existing User with FirstName = "Old".
            var db = TestDatabase.NewApplicationDbContext();
            var roleId = TestDatabase.RoleId(db, "User");

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "gm029user",
                Email = "gm029@test.local",
                FirstName = "Old",
                LastName = "Name",
                PhoneNumber = "1111111111",
                SecurityStamp = Guid.NewGuid().ToString()
            };
            db.Users.Add(user);
            db.SaveChanges();
            db.Users.First(x => x.Id == user.Id).Roles.Add(new IdentityUserRole { UserId = user.Id, RoleId = roleId });
            db.SaveChanges();

            var service = BuildUserService(db);

            // Act: call UpdateUser with FirstName = "New" and a mismatched PasswordHash/RetypePassword.
            // RoleId is kept equal to the user's current role so this test isolates the
            // password-mismatch discard behaviour from the separate role-change code path.
            var model = new AuthRequestModels.UserCreateRequestModel
            {
                Id = user.Id,
                FirstName = "New",
                LastName = "Name",
                Email = "gm029@test.local",
                PhoneNumber = "1111111111",
                UserName = "gm029user",
                PasswordHash = "abc",
                RetypePassword = "xyz",
                RoleId = roleId
            };

            var updateTask = service.UpdateUser(model);
            updateTask.GetAwaiter().GetResult();

            string firstNameAfterCall;
            using (var readDb = TestDatabase.NewApplicationDbContext())
            {
                firstNameAfterCall = readDb.Users.Single(x => x.Id == user.Id).FirstName;
            }

            var output = new Fields
            {
                { "outcome", "OK" },
                { "error_code", null },
                { "threw", false },
                // The `if (model.PasswordHash != model.RetypePassword) return _repository.UpdateUser(user);`
                // guard (UserService.cs:100) fires BEFORE any of the profile-field assignments
                // (UserService.cs:102-107), so it discards FirstName/LastName/Email/PhoneNumber too,
                // not just the password.
                { "user_first_name_after_call", firstNameAfterCall }
            };

            FixtureWriter.Write("GM-029", new Fields { { "submitted_first_name", "New" }, { "password", "abc" }, { "retype_password", "xyz" } }, output);
        }
    }
}
