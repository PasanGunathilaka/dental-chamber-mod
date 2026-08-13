using Microsoft.AspNetCore.Identity;

namespace DentalManagement.Infrastructure.Identity;

/// <summary>
/// A clinic-staff login account, extending ASP.NET Core Identity with the two
/// name fields the legacy model added.
/// </summary>
/// <remarks>
/// Lives in the infrastructure project rather than the domain project because it
/// derives from <see cref="IdentityUser"/>, and the domain project deliberately
/// holds no package reference beyond the BCL (spec FR-02). <c>Resource</c> and
/// <c>Permission</c> are plain POCOs and do stay in the domain.
///
/// One primary role per user is enforced at the schema level by a unique index on
/// the Identity user-roles join table — see <c>DentalDbContext.OnModelCreating</c>
/// (CQ-015, spec FR-14).
/// </remarks>
public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }
}
