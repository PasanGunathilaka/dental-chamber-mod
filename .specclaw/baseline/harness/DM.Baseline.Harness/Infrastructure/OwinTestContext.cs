using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Web;
using DM.AuthServer;
using DM.AuthServer.Models;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;

namespace DM.Baseline.Harness.Infrastructure
{
    /// <summary>
    /// Every MOD-005 service (UserService, ProfileService, PermissionService, RoleService) and every
    /// MOD-005 repository (UserRepository, ProfileRepository, ResourceRepository via PermissionService)
    /// reads either `HttpContext.Current.GetOwinContext().GetUserManager&lt;ApplicationUserManager&gt;()`
    /// or `HttpContext.Current.User`/`.Identity.GetUserId()` directly in its own constructor or method
    /// body (confirmed by reading DM.Server/Service/UserService.cs, ProfileService.cs,
    /// PermissionService.cs and DM.Server/Repository/UserRepository.cs, ProfileRepository.cs this run).
    /// None of that is reachable through a real OWIN/HTTP pipeline in this harness (CONTRACT.md (i)
    /// explicitly excludes `http` as this design's seam layer), so this class fakes just enough of
    /// ASP.NET's ambient ownership of HttpContext.Current for those constructors to succeed.
    ///
    /// Both storage-key conventions below were confirmed by disassembling the actual referenced NuGet
    /// binaries with ildasm this run (`Microsoft.Owin.Host.SystemWeb.dll`,
    /// `Microsoft.AspNet.Identity.Owin.dll`), not merely assumed from a published write-up:
    ///  - `Microsoft.Owin.Host.SystemWeb`'s `HttpContextBaseExtensions.GetOwinContext()` reads the OWIN
    ///    environment dictionary from `HttpContext.Items["owin.Environment"]` (the literal string key,
    ///    `Microsoft.Owin.Host.SystemWeb.HttpContextItemKeys.OwinEnvironmentKey`) and throws
    ///    `InvalidOperationException` if that entry is absent -- this is why `Arrange` below sets it
    ///    directly rather than relying on any ambient OWIN pipeline.
    ///  - `GetUserManager&lt;TManager&gt;()` calls `OwinContextExtensions.Get&lt;TManager&gt;(context)`, which
    ///    reads `context.Get&lt;TManager&gt;("AspNet.Identity.Owin:" + typeof(TManager).AssemblyQualifiedName)`.
    ///    `OwinContextExtensions.Set&lt;T&gt;(context, value)` writes under that exact same computed key --
    ///    called here via its explicit static form (`OwinContextExtensions.Set(owinContext, manager)`)
    ///    rather than `owinContext.Set(manager)`, because `IOwinContext`/`OwinContext` themselves already
    ///    declare an instance method literally named `Set` (`Set&lt;T&gt;(string key, T value)`) which hides
    ///    the extension overload from ordinary dot-invocation, per C#'s own member-lookup-before-extension
    ///    rule -- confirmed by this exact call failing to compile (`CS7036`) before this fix.
    ///
    /// Also a deliberate simplification, stated plainly rather than left implicit: this constructs
    /// `ApplicationUserManager` directly (`new ApplicationUserManager(new UserStore&lt;ApplicationUser&gt;(db))`)
    /// rather than via `DM.AuthServer.ApplicationUserManager.Create(...)` (the factory
    /// `Startup.Auth.cs`'s real OWIN pipeline uses), which additionally wires a UserValidator,
    /// PasswordValidator, and DataProtectorTokenProvider. None of the 41 scenarios in this harness
    /// exercise Identity's own password-strength validation or a password-reset token, so this
    /// simplification does not change any of their captured behaviour.
    /// </summary>
    public static class OwinTestContext
    {
        public static ApplicationUserManager CreateUserManager(ApplicationDbContext db)
        {
            return new ApplicationUserManager(new UserStore<ApplicationUser>(db));
        }

        /// <summary>
        /// Arranges HttpContext.Current so that:
        ///  - HttpContext.Current.GetOwinContext().GetUserManager&lt;ApplicationUserManager&gt;() returns
        ///    <paramref name="manager"/>;
        ///  - HttpContext.Current.User.Identity.GetUserId() returns <paramref name="authenticatedUserId"/>
        ///    (when supplied), with the given role claims for IsInRole(...) checks
        ///    (RoleService.GetAll, UserService.GetUsers).
        /// </summary>
        public static void Arrange(ApplicationUserManager manager, string authenticatedUserId = null, params string[] roles)
        {
            var environment = new Dictionary<string, object>();
            var owinContext = new OwinContext(environment);
            OwinContextExtensions.Set(owinContext, manager);

            var httpContext = new HttpContext(
                new HttpRequest("", "http://localhost/", ""),
                new HttpResponse(new StringWriter()));

            httpContext.Items["owin.Environment"] = environment;

            if (authenticatedUserId != null)
            {
                var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, authenticatedUserId) };
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            }

            HttpContext.Current = httpContext;
        }
    }
}
