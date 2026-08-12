using System.Collections.Generic;
using DM.Baseline.Harness.Infrastructure;
using DM.RequestModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DM.Baseline.Harness.Scenarios
{
    /// <summary>
    /// GM-001 / GM-004 — pure-function seams, no DB, no clock.
    /// Seam: DM.RequestModels/HelperRequestModel.GetThisPatientCode / GenerateBillCode.
    /// Seam layer: pure-function.
    /// </summary>
    [TestClass]
    public class GM001_GM004_CodeFormatTests
    {
        [TestMethod]
        public void GM001_PatientCodeFormat_ZeroPadRolloverBoundary()
        {
            // Arrange: none -- pure function, no DB, no clock (scenarios.md GM-001).
            var inputs = new[] { "1", "999999", "9999999", "99999999" };
            var results = new List<Fields>();

            foreach (var right in inputs)
            {
                // Act
                var code = HelperRequestModel.GetThisPatientCode(right);

                results.Add(new Fields
                {
                    { "input_right", right },
                    { "generated_code", code },
                    { "generated_code_length", code.Length }
                });
            }

            var output = new Fields
            {
                { "outcome", "OK" },
                { "error_code", null },
                { "threw", false },
                { "results", results }
            };

            FixtureWriter.Write("GM-001", new Fields { { "inputs", inputs } }, output);
        }

        [TestMethod]
        public void GM004_BillCodeFormat_ZeroPadRolloverBoundary()
        {
            // Arrange: none -- pure function, no DB, no clock (scenarios.md GM-004).
            const string patientCode = "P000001";
            var inputs = new[] { "1", "999", "1000" };
            var results = new List<Fields>();

            foreach (var right in inputs)
            {
                // Act
                var code = HelperRequestModel.GenerateBillCode(patientCode, right);

                results.Add(new Fields
                {
                    { "input_right", right },
                    { "generated_code", code }
                });
            }

            var output = new Fields
            {
                { "outcome", "OK" },
                { "error_code", null },
                { "threw", false },
                { "results", results }
            };

            FixtureWriter.Write("GM-004", new Fields { { "patient_code", patientCode }, { "inputs", inputs } }, output);
        }
    }
}
