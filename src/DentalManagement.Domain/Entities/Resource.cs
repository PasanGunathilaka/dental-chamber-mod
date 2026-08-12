namespace DentalManagement.Domain.Entities;

/// <summary>
/// A protected screen/route in the authorization catalog that DR-015 reads.
/// </summary>
/// <remarks>
/// Keeps the legacy <c>string</c> primary key rather than adopting
/// <see cref="BaseEntity"/>'s <see cref="Guid"/>: nothing decides to change it,
/// and the legacy identity schema carried no <c>Created</c>/<c>LastUpdate</c>
/// on this entity either.
/// </remarks>
public class Resource
{
    public string Id { get; set; } = null!;

    public string? Name { get; set; }

    /// <summary>
    /// Required. In legacy this matched an AngularJS UI-Router state name
    /// (e.g. <c>"root.patient"</c>). The rebuild's own route naming is a later
    /// item's concern; the seed catalog preserves the legacy values so existing
    /// <c>Permission</c> grants keep meaning across migration.
    /// </summary>
    public string Route { get; set; } = null!;

    /// <summary>
    /// When true, DR-015 grants access unconditionally without consulting
    /// <see cref="Permission"/>.
    /// </summary>
    public bool IsPublic { get; set; }

    public ICollection<Permission> Permissions { get; set; } = [];
}
