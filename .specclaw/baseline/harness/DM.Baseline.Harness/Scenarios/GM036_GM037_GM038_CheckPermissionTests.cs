using System;
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
    /// GM-036 / GM-037 / GM-038 — DR-015: PermissionService.CheckPermission's public-resource
    /// shortcut and its Resource/Permission truth table for private resources. Seam layer: service.
    /// </summary>
    [TestClass]
    public class GM036_GM037_GM038_CheckPermissionTests
    {
        [TestInitialize]
        public void Setup()
        {
            DbCleaner.ClearNonSeedUsers();
        }

        private static PermissionService BuildService(ApplicationDbContext db, string authenticatedUserId = null)
        {
            var manager = OwinTestContext.CreateUserManager(db);
            OwinTestContext.Arrange(manager, authenticatedUserId);

            var permissionRepository = new PermissionRepository(db);
            var resourceRepository = new ResourceRepository(db);
            return new PermissionService(permissionRepository, resourceRepository);
        }

        private static SecurityModels.Resource CreateResource(ApplicationDbContext db, string route, bool isPublic)
        {
            var resource = new SecurityModels.Resource { Id = Guid.NewGuid().ToString(), Name = route, Route = route, IsPublic = isPublic };
            db.Resources.Add(resource);
            db.SaveChanges();
            return resource;
        }

        private static ApplicationUser CreateUserWithRole(ApplicationDbContext db, string roleId, string userName)
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = userName,
                Email = userName + "@test.local",
                SecurityStamp = Guid.NewGuid().ToString()
            };
            db.Users.Add(user);
            db.SaveChanges();
            db.Users.Find(user.Id).Roles.Add(new IdentityUserRole { UserId = user.Id, RoleId = roleId });
            db.SaveChanges();
            return user;
        }

        [TestMethod]
        public void GM036_CheckPermission_PublicResource_GrantsUnconditionally()
        {
            // Arrange: a Resource with IsPublic = true and zero Permission rows for any role.
            var db = TestDatabase.NewApplicationDbContext();
            var resource = CreateResource(db, "root.gm036-public", isPublic: true);
            var service = BuildService(db); // no authenticated user needed -- the public-resource check returns before any user lookup

            // Act
            var permitted = service.CheckPermission(new AuthRequestModels.PermissionRequestModel { Route = resource.Route });

            var output = new Fields
            {
                { "outcome", "OK" },
                { "error_code", null },
                { "threw", false },
                { "permitted", permitted }
            };

            FixtureWriter.Write("GM-036", new Fields { { "route", resource.Route }, { "resource_is_public", true } }, output);
        }

        [TestMethod]
        public void GM037_CheckPermission_PrivateResource_NoMatchingPermission_Denied()
        {
            // Arrange: a Resource with IsPublic = false; the caller's role has zero Permission rows
            // for that Resource.
            var db = TestDatabase.NewApplicationDbContext();
            var resource = CreateResource(db, "root.gm037-private", isPublic: false);
            var roleId = TestDatabase.RoleId(db, "User");
            var user = CreateUserWithRole(db, roleId, "gm037user");
            var service = BuildService(db, user.Id);

            // Act
            var permitted = service.CheckPermission(new AuthRequestModels.PermissionRequestModel { Route = resource.Route });

            var output = new Fields
            {
                { "outcome", "OK" },
                { "error_code", null },
                { "threw", false },
                { "permitted", permitted }
            };

            FixtureWriter.Write("GM-037", new Fields { { "route", resource.Route }, { "resource_is_public", false }, { "permission_row_exists", false } }, output);
        }

        [TestMethod]
        public void GM038_CheckPermission_PrivateResource_MatchingPermissionExists_Granted()
        {
            // Arrange: a Resource with IsPublic = false; a Permission row exists granting the
            // caller's role that Resource.
            var db = TestDatabase.NewApplicationDbContext();
            var resource = CreateResource(db, "root.gm038-private", isPublic: false);
            var roleId = TestDatabase.RoleId(db, "User");
            var user = CreateUserWithRole(db, roleId, "gm038user");

            db.Permissions.Add(new SecurityModels.Permission
            {
                Id = Guid.NewGuid().ToString(),
                RoleId = roleId,
                RoleName = "User",
                ResourceId = resource.Id
            });
            db.SaveChanges();

            var service = BuildService(db, user.Id);

            // Act
            var permitted = service.CheckPermission(new AuthRequestModels.PermissionRequestModel { Route = resource.Route });

            var output = new Fields
            {
                { "outcome", "OK" },
                { "error_code", null },
                { "threw", false },
                { "permitted", permitted }
            };

            FixtureWriter.Write("GM-038", new Fields { { "route", resource.Route }, { "resource_is_public", false }, { "permission_row_exists", true } }, output);
        }
    }
}
