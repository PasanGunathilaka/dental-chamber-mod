using DentalManagement.Domain.Enums;

namespace DentalManagement.Domain.Entities;

/// <summary>
/// A stocked clinic consumable tracked for inventory.
/// </summary>
public class Product : BaseEntity
{
    public string? Code { get; set; }

    /// <summary>
    /// Required, unique, 1–40 characters.
    /// </summary>
    public string Name { get; set; } = null!;

    public int StartingInventory { get; set; }

    public int Received { get; set; }

    public int Shipped { get; set; }

    /// <summary>
    /// Persisted exactly as sent, with no server-side recomputation against
    /// movement history — GM-021 captures the server accepting an arbitrary
    /// <c>9999</c>. Recomputation is not this item's mandate.
    /// </summary>
    public int OnHand { get; set; }

    public int MinimumRequired { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal SalePrice { get; set; }

    /// <summary>
    /// Derived from <see cref="OnHand"/> by client code in legacy, never enforced
    /// server-side; GM-021 captures an "Out Of Stock" status persisting alongside
    /// a positive on-hand quantity.
    /// </summary>
    public ProductStatus Status { get; set; }

    public ICollection<Inventory> Inventories { get; set; } = [];
}
