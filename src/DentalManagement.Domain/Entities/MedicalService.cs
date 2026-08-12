namespace DentalManagement.Domain.Entities;

/// <summary>
/// The clinic's priced catalog of dental treatments (e.g. "Scaling",
/// "Extraction") that can be billed to a patient.
/// </summary>
public class MedicalService : BaseEntity
{
    /// <summary>
    /// Sequential integer code, unique.
    /// </summary>
    public int Code { get; set; }

    /// <summary>
    /// Required, unique, 2–50 characters — DR-017.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// The service price.
    /// </summary>
    /// <remarks>
    /// Legacy declared this <c>[DataType(DataType.Currency)] string</c> and
    /// derived <c>TotalCharge</c> via <c>Convert.ToInt32(Charge) * Quantity</c>,
    /// which truncated fractional currency and threw <c>FormatException</c> for
    /// anything non-integer (DR-019; GM-017 captures <c>"10.50"</c> being
    /// rejected). CQ-008 fixes that defect: a real decimal over a
    /// fixed-precision numeric column, no truncation (spec FR-07).
    /// </remarks>
    public decimal Charge { get; set; }

    /// <summary>
    /// Not persisted — a request-shaped quantity the legacy model carried on the
    /// catalog entity itself.
    /// </summary>
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Computed, not persisted: <c>Charge * Quantity</c>, in decimal.
    /// </summary>
    /// <remarks>
    /// This is the CQ-008 fix in one line. Legacy truncated to <c>int</c>;
    /// here <c>10.50m * 3</c> is <c>31.50m</c> exactly (spec AC-07). GM-017 will
    /// therefore diverge from legacy for fractional charges — a divergence
    /// CQ-008 explicitly sanctions (design R-3).
    /// </remarks>
    public decimal TotalCharge => Charge * Quantity;
}
