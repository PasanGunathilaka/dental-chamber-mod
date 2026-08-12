using DentalManagement.Domain.Entities;

namespace DentalManagement.Infrastructure.Tests;

/// <summary>
/// Computed-property semantics, pinned against the captured pure-function
/// fixtures. No database needed — these are the same seam layer the fixtures were
/// captured at.
/// </summary>
public class ValueSemanticsTests
{
    /// <summary>
    /// AC-13 / GM-041 — <c>TotalDiscountAmount</c> is plain unguarded addition.
    /// </summary>
    /// <remarks>
    /// The three cases are exactly the fixture's three input pairs and its three
    /// captured outputs, including the negative. A floor at zero or a
    /// non-negative guard would break this fixture, and no CQ sanctions either
    /// (spec FR-11).
    /// </remarks>
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(10.5, 5, 15.5)]
    [InlineData(0, -5, -5)]
    public void TotalDiscountAmount_matches_GM041(
        decimal discountAmount,
        decimal fixedDiscount,
        decimal expected)
    {
        var prescription = new Prescription
        {
            DiscountAmount = discountAmount,
            FixedDiscount = fixedDiscount,
        };

        Assert.Equal(expected, prescription.TotalDiscountAmount);
    }

    /// <summary>
    /// AC-07 / CQ-008 — fractional charges multiply exactly instead of being
    /// truncated to an integer.
    /// </summary>
    /// <remarks>
    /// GM-017 captured legacy rejecting <c>"10.50"</c> outright with
    /// <c>NON_INTEGER_CHARGE</c> / <c>FormatException</c>, because
    /// <c>TotalCharge</c> was <c>Convert.ToInt32(Charge) * Quantity</c>. CQ-008
    /// decided that is a defect to fix, so this behaviour is a deliberate,
    /// sanctioned divergence from the captured fixture (design R-3).
    /// </remarks>
    [Theory]
    [InlineData(10.50, 3, 31.50)]
    [InlineData(10.50, 1, 10.50)]
    [InlineData(0.01, 3, 0.03)]
    public void TotalCharge_keeps_fractional_currency(
        decimal charge,
        int quantity,
        decimal expected)
    {
        var service = new MedicalService { Charge = charge, Quantity = quantity };

        Assert.Equal(expected, service.TotalCharge);
    }

    /// <summary>
    /// AC-07 / GM-016 — the integer-charge cases legacy handled correctly still
    /// produce the same answers, so the CQ-008 fix is not a wholesale change of
    /// behaviour.
    /// </summary>
    [Theory]
    [InlineData(10, 0, 0)]
    [InlineData(10, 1, 10)]
    [InlineData(10, 5, 50)]
    public void TotalCharge_still_matches_GM016_for_integer_charges(
        decimal charge,
        int quantity,
        decimal expected)
    {
        var service = new MedicalService { Charge = charge, Quantity = quantity };

        Assert.Equal(expected, service.TotalCharge);
    }

    /// <summary>
    /// The legacy default: one unit unless told otherwise.
    /// </summary>
    [Fact]
    public void MedicalService_quantity_defaults_to_one()
    {
        Assert.Equal(1, new MedicalService().Quantity);
        Assert.Equal(1, new PatientMedicalService().Quantity);
    }
}
