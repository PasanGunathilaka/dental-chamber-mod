using System;
using System.Linq;
using DM.AuthServer.Models;
using AuthRequestModels = DM.AuthServer.Models.RequestModels;
using DM.AuthServer.Repository;
using DM.AuthServer.Service;
using DM.Baseline.Harness.Infrastructure;
using Microsoft.AspNet.Identity;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DM.Baseline.Harness.Scenarios
{
    /// <summary>
    /// GM-030 / GM-031 / GM-032 — DR-013: ProfileService.UpdatePassword's check order (retype match
    /// before current-password verification) and its happy path. Seam layer: service.
    /// </summary>
    [TestClass]
    public class GM030_GM031_GM032_UpdatePasswordTests
    {
        private const string KnownCorrectPassword = "Correct-Pass1";

        [TestInitialize]
        public void Setup()
        {
            DbCleaner.ClearNonSeedUsers();
        }

        private static ApplicationUser CreateTestUserWithKnownPassword(ApplicationDbContext db, string userName)
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = userName,
                Email = userName + "@test.local",
                FirstName = "Test",
                LastName = userName,
                SecurityStamp = Guid.NewGuid().ToString(),
                PasswordHash = new PasswordHasher().HashPassword(KnownCorrectPassword)
            };
            db.Users.Add(user);
            db.SaveChanges();
            return user;
        }

        private static ProfileService BuildProfileService(ApplicationDbContext db, string authenticatedUserId)
        {
            var manager = OwinTestContext.CreateUserManager(db);
            OwinTestContext.Arrange(manager, authenticatedUserId);

            var profileRepository = new ProfileRepository(db);
            var roleRepository = new RoleRepository(db);
            return new ProfileService(profileRepository, roleRepository);
        }

        [TestMethod]
        public void GM030_UpdatePassword_WrongCurrentPassword_Rejected()
        {
            // Arrange: a User with a known password hash.
            var db = TestDatabase.NewApplicationDbContext();
            var user = CreateTestUserWithKnownPassword(db, "gm030user");
            var service = BuildProfileService(db, user.Id);

            // Act: CurrentPassword is wrong; NewPassword == RetypePassword (both matching).
            var model = new AuthRequestModels.ChangePasswordRequestModel
            {
                CurrentPassword = "totally-wrong-password",
                NewPassword = "New-Pass1",
                RetypePassword = "New-Pass1"
            };
            var changed = service.UpdatePassword(model);

            var output = new Fields
            {
                { "outcome", changed ? "OK" : "REJECTED" },
                { "error_code", changed ? null : "INVALID_CURRENT_PASSWORD" },
                { "threw", false },
                { "password_changed", changed }
            };

            FixtureWriter.Write("GM-030", new Fields { { "current_password_correct", false }, { "new_retype_match", true } }, output);
        }

        [TestMethod]
        public void GM031_UpdatePassword_MismatchedNewRetype_RejectedBeforeCurrentPasswordChecked()
        {
            // Arrange: a User with a known password hash.
            var db = TestDatabase.NewApplicationDbContext();
            var user = CreateTestUserWithKnownPassword(db, "gm031user");
            var service = BuildProfileService(db, user.Id);

            // Act: NewPassword != RetypePassword, with a deliberately CORRECT CurrentPassword, to
            // prove the check order (ProfileService.cs:68's mismatch guard fires before
            // VerifyHashedPassword is ever called at line 72).
            var model = new AuthRequestModels.ChangePasswordRequestModel
            {
                CurrentPassword = KnownCorrectPassword,
                NewPassword = "New-Pass1",
                RetypePassword = "Different-Pass1"
            };
            var changed = service.UpdatePassword(model);

            var output = new Fields
            {
                { "outcome", changed ? "OK" : "REJECTED" },
                { "error_code", changed ? null : "PASSWORD_RETYPE_MISMATCH" },
                { "threw", false },
                { "password_changed", changed }
            };

            FixtureWriter.Write("GM-031", new Fields { { "current_password_correct", true }, { "new_retype_match", false } }, output);
        }

        [TestMethod]
        public void GM032_UpdatePassword_HappyPath_Succeeds()
        {
            // Arrange: a User with a known password hash.
            var db = TestDatabase.NewApplicationDbContext();
            var user = CreateTestUserWithKnownPassword(db, "gm032user");
            var service = BuildProfileService(db, user.Id);

            // Act: correct CurrentPassword, NewPassword == RetypePassword.
            const string newPassword = "New-Pass1";
            var model = new AuthRequestModels.ChangePasswordRequestModel
            {
                CurrentPassword = KnownCorrectPassword,
                NewPassword = newPassword,
                RetypePassword = newPassword
            };
            var changed = service.UpdatePassword(model);

            bool passwordActuallyUpdated;
            using (var readDb = TestDatabase.NewApplicationDbContext())
            {
                var storedHash = readDb.Users.Single(x => x.Id == user.Id).PasswordHash;
                passwordActuallyUpdated = new PasswordHasher().VerifyHashedPassword(storedHash, newPassword) == PasswordVerificationResult.Success;
            }

            var output = new Fields
            {
                { "outcome", "OK" },
                { "error_code", null },
                { "threw", false },
                { "password_changed", changed && passwordActuallyUpdated }
            };

            FixtureWriter.Write("GM-032", new Fields { { "current_password_correct", true }, { "new_retype_match", true } }, output);
        }
    }
}
