namespace DentalManagement.Domain.Patients;

/// <summary>
/// Formats the auto-provisioned bill (<c>Prescription</c>) code — DR-003. Pure
/// and dependency-free so <c>GM-004</c> can replay it at the <c>pure-function</c>
/// seam layer (spec FR-06, FR-07; design D-2).
/// </summary>
public static class BillCodeFormatter
{
    /// <summary>
    /// Returns <c>"BILL"</c> followed by <paramref name="sequence"/>
    /// left-zero-padded to a <b>minimum</b> of 3 digits, then <c>"-"</c>, then
    /// <paramref name="patientCode"/>. <c>"D3"</c> is a minimum width, not a
    /// truncating format, so values wider than 3 digits are never cut down.
    /// <c>GM-004</c> pins <c>("P000001", 1) → "BILL001-P000001"</c>,
    /// <c>("P000001", 999) → "BILL999-P000001"</c>, and
    /// <c>("P000001", 1000) → "BILL1000-P000001"</c>.
    /// </summary>
    public static string Format(string patientCode, long sequence) =>
        "BILL" + sequence.ToString("D3") + "-" + patientCode;
}
