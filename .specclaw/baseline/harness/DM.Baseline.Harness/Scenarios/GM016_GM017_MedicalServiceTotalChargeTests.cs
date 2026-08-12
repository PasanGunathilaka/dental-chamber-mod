using System;
using System.Collections.Generic;
using DM.Baseline.Harness.Infrastructure;
using DM.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DM.Baseline.Harness.Scenarios
{
    /// <summary>
    /// GM-016 / GM-017 — DR-019: MedicalService.TotalCharge's computed property
    /// (`Convert.ToInt32(Charge) * Quantity`, DM.Models/MedicalService.cs:36). Seam layer:
    /// pure-function -- no DB, no clock, just constructing the entity and reading the property.
    /// </summary>
    [TestClass]
    public class GM016_GM017_MedicalServiceTotalChargeTests
    {
        [TestMethod]
        public void GM016_TotalCharge_IntegerBoundaryValues()
        {
            // Arrange: none -- construct MedicalService instances directly with Charge = "10" and
            // Quantity = 0, 1, 5 (no validation prevents Quantity = 0, since it is [NotMapped] with
            // only a default of 1).
            var quantities = new[] { 0, 1, 5 };
            var results = new List<Fields>();

            foreach (var quantity in quantities)
            {
                // Act
                var medicalService = new MedicalService { Charge = "10", Quantity = quantity };
                var totalCharge = medicalService.TotalCharge;

                results.Add(new Fields
                {
                    { "charge", "10" },
                    { "quantity", quantity },
                    { "total_charge", totalCharge }
                });
            }

            var output = new Fields
            {
                { "outcome", "OK" },
                { "error_code", null },
                { "threw", false },
                { "results", results }
            };

            FixtureWriter.Write("GM-016", new Fields { { "charge", "10" }, { "quantities", quantities } }, output);
        }

        [TestMethod]
        public void GM017_TotalCharge_NonIntegerChargeStrings_OutcomeCapturedAsIs()
        {
            // Arrange: construct MedicalService instances with Charge = "10.50" (fractional) and
            // Charge = "abc" (non-numeric), Quantity = 1 for both. Deliberately not asserting the
            // outcome in advance -- scenarios.md's own note leaves open whether Convert.ToInt32
            // truncates or throws for these inputs; this test records whatever actually happens.
            var charges = new[] { "10.50", "abc" };
            var results = new List<Fields>();

            foreach (var charge in charges)
            {
                bool threw = false;
                int? totalCharge = null;
                string exceptionType = null;
                string exceptionMessage = null;

                try
                {
                    var medicalService = new MedicalService { Charge = charge, Quantity = 1 };
                    totalCharge = medicalService.TotalCharge;
                }
                catch (Exception ex)
                {
                    threw = true;
                    exceptionType = ex.GetType().FullName;
                    exceptionMessage = ex.Message;
                }

                results.Add(new Fields
                {
                    { "charge", charge },
                    { "outcome", threw ? "REJECTED" : "OK" },
                    { "error_code", threw ? "NON_INTEGER_CHARGE" : null },
                    { "threw", threw },
                    { "total_charge", totalCharge },
                    { "ExceptionType", exceptionType },
                    { "ExceptionMessage", exceptionMessage }
                });
            }

            FixtureWriter.Write(
                "GM-017",
                new Fields { { "charges", charges } },
                new Fields { { "results", results } });
        }
    }
}
