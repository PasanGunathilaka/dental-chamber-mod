namespace DentalManagement.Domain.Entities;

/// <summary>
/// A grant of one <see cref="Resource"/> to one role — the join table the whole
/// authorization model runs on (DR-015).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="RoleName"/> is denormalized alongside <see cref="RoleId"/>, exactly
/// as legacy had it. Keeping both preserves the existing rows' shape through
/// migration; DR-016's seeding and DR-015's check both read this table.
/// </para>
/// <para>
/// There is no navigation property to the role type here: the role lives in the
/// ASP.NET Core Identity model, and this project holds no reference to Identity
/// (spec FR-02). The foreign key to it is configured in the infrastructure layer
/// without a navigation, so the domain stays free of framework types.
/// </para>
/// </remarks>
public class Permission
{
    public string Id { get; set; } = null!;

    public string RoleId { get; set; } = null!;

    public string? RoleName { get; set; }

    public string ResourceId { get; set; } = null!;

    public Resource Resource { get; set; } = null!;
}
