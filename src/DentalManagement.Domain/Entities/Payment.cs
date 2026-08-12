namespace DentalManagement.Domain.Entities;

/// <summary>
/// One payment applied against a patient's bill.
/// </summary>
public class Payment : BaseEntity
{
    public Guid PrescriptionId { get; set; }

    public Prescription Prescription { get; set; } = null!;

    /// <summary>
    /// Legacy declared this <c>double</c>; money is <c>decimal</c> over a
    /// fixed-precision numeric column here (spec FR-07, NFR-04). No captured
    /// fixture contains a floating-point artifact, so the change is lossless.
    /// </summary>
    public decimal Amount { get; set; }

    public string? Comment { get; set; }
}
