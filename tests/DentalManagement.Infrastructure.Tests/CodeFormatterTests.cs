using DentalManagement.Domain.Patients;

namespace DentalManagement.Infrastructure.Tests;

/// <summary>
/// Replays <c>GM-001</c> and <c>GM-004</c> at the <c>pure-function</c> seam
/// layer they were captured at — no database, no clock (spec AC-03, AC-04;
/// design D-2).
/// </summary>
public class CodeFormatterTests
{
    /// <summary>
    /// AC-03 — GM-001: the patient-code formatter's four pinned inputs/outputs,
    /// including the no-truncation cases wider than the 6-digit minimum.
    /// </summary>
    [Theory]
    [InlineData(1, "P000001", 7)]
    [InlineData(999999, "P999999", 7)]
    [InlineData(9999999, "P9999999", 8)]
    [InlineData(99999999, "P99999999", 9)]
    public void Patient_code_formatter_replays_GM_001(long sequence, string expectedCode, int expectedLength)
    {
        var code = PatientCodeFormatter.Format(sequence);

        Assert.Equal(expectedCode, code);
        Assert.Equal(expectedLength, code.Length);
    }

    /// <summary>
    /// AC-04 — GM-004: the bill-code formatter's three pinned inputs/outputs,
    /// including the no-truncation case wider than the 3-digit minimum.
    /// </summary>
    [Theory]
    [InlineData("P000001", 1, "BILL001-P000001")]
    [InlineData("P000001", 999, "BILL999-P000001")]
    [InlineData("P000001", 1000, "BILL1000-P000001")]
    public void Bill_code_formatter_replays_GM_004(string patientCode, long sequence, string expectedCode)
    {
        var code = BillCodeFormatter.Format(patientCode, sequence);

        Assert.Equal(expectedCode, code);
    }
}
