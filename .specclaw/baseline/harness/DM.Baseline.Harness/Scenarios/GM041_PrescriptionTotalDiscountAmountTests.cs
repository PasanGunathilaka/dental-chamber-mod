using System.Collections.Generic;
using DM.Baseline.Harness.Infrastructure;
using DM.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DM.Baseline.Harness.Scenarios
{
    /// <summary>
    /// GM-041 — Prescription.TotalDiscountAmount computed property
    /// (`DiscountAmount + FixedDiscount`, DM.Models/Prescription.cs:29). Seam layer: pure-function --
    /// no DB, no clock, just constructing the entity and reading the property. No numbered rule;
    /// included per the explicit instruction to cover boundary values of computed read-model
    /// properties identified as pure-function seams.
    /// </summary>
    [TestClass]
    public class GM041_PrescriptionTotalDiscountAmountTests
    {
        [TestMethod]
        public void GM041_TotalDiscountAmount_BoundaryValues()
        {
            // Arrange: none -- construct Prescription instances directly with
            // (DiscountAmount, FixedDiscount) = (0, 0), (10.5, 5), (0, -5) -- no validation prevents
            // a negative FixedDiscount.
            var pairs = new[] { new[] { 0d, 0d }, new[] { 10.5d, 5d }, new[] { 0d, -5d } };
            var results = new List<Fields>();

            foreach (var pair in pairs)
            {
                // Act
                var prescription = new Prescription { DiscountAmount = pair[0], FixedDiscount = pair[1] };
                var total = prescription.TotalDiscountAmount;

                results.Add(new Fields
                {
                    { "discount_amount", pair[0] },
                    { "fixed_discount", pair[1] },
                    { "total_discount_amount", total }
                });
            }

            var output = new Fields
            {
                { "outcome", "OK" },
                { "error_code", null },
                { "threw", false },
                { "results", results }
            };

            FixtureWriter.Write("GM-041", new Fields { { "pairs", pairs } }, output);
        }
    }
}
