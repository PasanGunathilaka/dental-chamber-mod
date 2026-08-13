using DentalManagement.Domain.Enums;

namespace DentalManagement.Domain.Entities;

/// <summary>
/// A patient's running bill / visit tab. Despite the legacy name this is not a
/// pharmacological prescription — it accumulates service charges, discounts, and
/// payments for one open visit.
/// </summary>
public class Prescription : BaseEntity
{
    /// <summary>
    /// <c>"BILL" + zero-padded sequence + "-" + PatientCode</c> — DR-003. Unique,
    /// 12–18 characters.
    /// </summary>
    public string Code { get; set; } = null!;

    public Guid PatientId { get; set; }

    public Patient Patient { get; set; } = null!;

    public decimal TotalCharge { get; set; }

    /// <summary>
    /// Deliberately carries no 0–100 range constraint. DR-004's bounds are
    /// enforced client-side only in legacy, and GM-005 captures the server
    /// accepting <c>150</c> and <c>-25</c> unchanged. Server-side enforcement is
    /// CQ-011's work in a later backlog item, not this schema's.
    /// </summary>
    public decimal DiscountPercent { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal FixedDiscount { get; set; }

    /// <summary>
    /// Computed, not persisted: <c>DiscountAmount + FixedDiscount</c>.
    /// </summary>
    /// <remarks>
    /// Plain unguarded addition — no floor at zero and no negative guard.
    /// GM-041 pins the three captured outcomes exactly: <c>0</c>, <c>15.5</c>,
    /// and <c>-5</c>. Adding a guard here would break a captured fixture with no
    /// CQ sanctioning the change (spec FR-11, SQ-012).
    /// </remarks>
    public decimal TotalDiscountAmount => DiscountAmount + FixedDiscount;

    public decimal TotalPayable { get; set; }

    public decimal TotalPaid { get; set; }

    public decimal TotalDue { get; set; }

    public BillStatus Status { get; set; }

    public ICollection<PatientMedicalService> PatientMedicalServices { get; set; } = [];

    public ICollection<Payment> Payments { get; set; } = [];
}
