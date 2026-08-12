using DentalManagement.Domain.Enums;

namespace DentalManagement.Domain.Entities;

/// <summary>
/// A single stock-movement transaction (goods received or shipped) for a
/// product, storing a snapshot of on-hand quantity at the moment of the movement.
/// </summary>
public class Inventory : BaseEntity
{
    public Guid ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public string CashMemoNo { get; set; } = null!;

    public int OnHand { get; set; }

    public int ReceivedOrShippedQuantity { get; set; }

    public InventoryMovementStatus Status { get; set; }
}
