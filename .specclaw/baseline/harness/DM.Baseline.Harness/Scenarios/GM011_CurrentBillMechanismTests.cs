using System.Linq;
using DM.Baseline.Harness.Infrastructure;
using DM.Repository;
using DM.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DM.Baseline.Harness.Scenarios
{
    /// <summary>
    /// GM-011 — the two independently-coexisting "current bill" resolution mechanisms
    /// (PatientController.Get()'s `.Last()` vs. PrescriptionController.GetPatientCurrentPrescription's
    /// `.LastOrDefault(x =&gt; x.StatusId == 5)`) disagree once a patient has no Active prescription.
    /// Seam layer: service. ⚠ PROVISIONAL — pending PQ-008 (scenarios.md).
    ///
    /// Per seams.md Capture Blocker #4, this Arrange seeds two Prescriptions with distinct Code
    /// values (avoiding the unspecified OrderBy(x =&gt; x.Code) tie condition).
    /// </summary>
    [TestClass]
    public class GM011_CurrentBillMechanismTests
    {
        [TestInitialize]
        public void Setup()
        {
            DbCleaner.ClearDentalDomainTables();
        }

        [TestMethod]
        public void GM011_TwoCurrentBillMechanisms_DisagreeWithNoActivePrescription()
        {
            // Arrange: a patient whose only two Prescriptions are both StatusId = 6 (Closed) --
            // simulating the documented failure mode where a "close bill" write succeeded but the
            // "open new bill" write never completed (DR-002/DR-007's un-transacted two-write workflow).
            System.Guid patientId;
            using (var seedDb = TestDatabase.NewDentalDbContext())
            {
                var patient = DomainSeed.CreatePatient(seedDb, name: "GM011 Patient");
                patientId = patient.Id;
                DomainSeed.CreatePrescription(seedDb, patient.Id, patient.Code, TestDatabase.StatusIds.Closed, codeSuffix: "001");
                DomainSeed.CreatePrescription(seedDb, patient.Id, patient.Code, TestDatabase.StatusIds.Closed, codeSuffix: "002");
            }

            // Act: call both mechanisms against the same underlying query
            // (PrescriptionService.GetPatientCurrentPrescription, which both controllers call before
            // applying their own, different tail operator).
            System.Collections.Generic.List<DM.Models.Prescription> prescriptions;
            using (var db = TestDatabase.NewDentalDbContext())
            {
                var service = new PrescriptionService(new PrescriptionRepository(db));
                prescriptions = service.GetPatientCurrentPrescription(patientId);
            }

            var mechanismAResult = prescriptions.Last(); // PatientController.Get()/Search()'s mechanism
            var mechanismBResult = prescriptions.LastOrDefault(x => x.StatusId == TestDatabase.StatusIds.Active); // PrescriptionController.GetPatientCurrentPrescription's mechanism

            var mechanismsAgree = (mechanismBResult == null && mechanismAResult == null) ||
                                   (mechanismBResult != null && mechanismAResult != null && mechanismBResult.Id == mechanismAResult.Id);

            var output = new Fields
            {
                { "outcome", "OK" },
                { "error_code", null },
                { "threw", false },
                { "mechanism_a_result_is_null", mechanismAResult == null },
                { "mechanism_a_result_status_id", mechanismAResult?.StatusId },
                { "mechanism_b_result_is_null", mechanismBResult == null },
                { "mechanisms_agree", mechanismsAgree }
            };

            FixtureWriter.Write(
                "GM-011",
                new Fields { { "prescription_count", prescriptions.Count }, { "all_closed", prescriptions.All(x => x.StatusId == TestDatabase.StatusIds.Closed) } },
                output);
        }
    }
}
