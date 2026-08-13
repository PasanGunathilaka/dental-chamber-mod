namespace DentalManagement.Domain.Enums;

/// <summary>
/// Status of a <see cref="Entities.Prescription"/> (a patient's running bill).
/// </summary>
/// <remarks>
/// One of the four typed status concepts that replace the legacy single shared
/// <c>Status</c> table, which had nothing partitioning it by entity — an
/// "In Stock"-flavoured status could be assigned to a bill with nothing to stop
/// it (CQ-006). The legacy numeric ids are preserved as the enum values so
/// migrated data keeps its meaning without a translation table, and so the
/// unmappable-status audit is a plain set-membership test (design D-3).
/// </remarks>
public enum BillStatus
{
    Active = 5,
    Closed = 6,
}
