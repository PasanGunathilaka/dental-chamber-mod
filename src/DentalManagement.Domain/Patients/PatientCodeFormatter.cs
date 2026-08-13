namespace DentalManagement.Domain.Patients;

/// <summary>
/// Formats the server-generated patient code — DR-001. Pure and dependency-free
/// so <c>GM-001</c> can replay it at the <c>pure-function</c> seam layer (spec
/// FR-05, FR-07; design D-2).
/// </summary>
public static class PatientCodeFormatter
{
    /// <summary>
    /// Returns <c>"P"</c> followed by <paramref name="sequence"/> left-zero-padded
    /// to a <b>minimum</b> of 6 digits. <c>"D6"</c> is a minimum width, not a
    /// truncating format — <c>9999999.ToString("D6")</c> is <c>"9999999"</c> — so
    /// values wider than 6 digits are never cut down. <c>GM-001</c> pins
    /// <c>1 → "P000001"</c>, <c>999999 → "P999999"</c>,
    /// <c>9999999 → "P9999999"</c>, and <c>99999999 → "P99999999"</c>.
    /// </summary>
    public static string Format(long sequence) => "P" + sequence.ToString("D6");
}
