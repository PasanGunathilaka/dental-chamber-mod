using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace DentalManagement.Api.DevelopmentOnly;

/// <summary>
/// ST-002 — authenticates every request as <see cref="StubCurrentUser.DevelopmentUserName"/>,
/// standing in for BL-002's real authentication until it lands.
/// </summary>
/// <remarks>
/// Registered only inside the same development-and-flag gate as <see cref="StubCurrentUser"/>
/// and <see cref="StubPermissionChecker"/> — see <c>Program.cs</c>. Every request that reaches
/// this handler authenticates unconditionally; it does not read credentials from the request at
/// all, which is the point — BL-002 replaces this scheme with real ASP.NET Core Identity token
/// issuance per SQ-004 (spec ST-002 in <c>module-stubs.md</c>).
///
/// The issued principal carries an explicit <c>stub=ST-002</c> claim (spec N-4) so any log or
/// audit trail reading the authenticated claims can see at a glance that the identity came from
/// this stub rather than a real login.
/// </remarks>
public sealed class StubAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>The authentication scheme name this handler is registered under.</summary>
    public const string SchemeName = "DevelopmentStub";

    /// <summary>The claim type marking a principal as issued by this stub.</summary>
    public const string StubClaimType = "stub";

    /// <summary>The claim value identifying this specific stub (spec N-4).</summary>
    public const string StubClaimValue = "ST-002";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, StubCurrentUser.DevelopmentUserName),
            new Claim(ClaimTypes.Role, StubCurrentUser.DevelopmentRole),
            new Claim(StubClaimType, StubClaimValue),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
