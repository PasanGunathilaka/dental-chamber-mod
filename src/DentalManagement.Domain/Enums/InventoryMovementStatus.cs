namespace DentalManagement.Domain.Enums;

/// <summary>
/// Direction of an <see cref="Entities.Inventory"/> stock movement. Legacy ids
/// preserved (CQ-006, design D-3).
/// </summary>
public enum InventoryMovementStatus
{
    Received = 3,
    Shipped = 4,
}
