namespace DentalManagement.Domain.Abstractions;

/// <summary>
/// The authenticated identity making the current request.
/// </summary>
/// <remarks>
/// BL-002's seam (ST-002). Plain BCL types only — per learning L1, a
/// framework-derived type placed in <c>DentalManagement.Domain</c> is a design
/// error caught late, so nothing here references ASP.NET Core. The dev-only
/// implementation that answers this contract until BL-002 lands lives in
/// <c>DentalManagement.Api.DevelopmentOnly</c> (spec FR-14, FR-15).
/// </remarks>
public interface ICurrentUser
{
    /// <summary>The authenticated user's name.</summary>
    string UserName { get; }

    /// <summary>The authenticated user's role, as checked by <see cref="IPermissionChecker"/>.</summary>
    string Role { get; }
}
