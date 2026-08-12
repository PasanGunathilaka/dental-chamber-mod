namespace DentalManagement.Domain.Enums;

/// <summary>
/// Status of a stocked <see cref="Entities.Product"/>. Legacy ids preserved
/// (CQ-006, design D-3).
/// </summary>
public enum ProductStatus
{
    InStock = 1,
    OutOfStock = 2,
}
