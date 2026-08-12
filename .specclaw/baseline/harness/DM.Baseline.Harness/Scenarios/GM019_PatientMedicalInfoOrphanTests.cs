using System;
using System.Linq;
using DM.Baseline.Harness.Infrastructure;
using DM.Models;
using DM.Repository;
using DM.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DM.Baseline.Harness.Scenarios
{
    /// <summary>
    /// GM-019 — deleting a Patient does NOT cascade-clean their tagged medical conditions, unlike
    /// Prescriptions (GM-012). DM.Models/PatientMedicalInfo.cs carries a plain Guid PatientId with no
    /// [ForeignKey]/navigation, so EF6 never configures a DB-level relationship to cascade in the
    /// first place. Seam layer: persistence. Modules: MOD-001, MOD-002.
    /// </summary>
    [TestClass]
    public class GM019_PatientMedicalInfoOrphanTests
    {
        [TestInitialize]
        public void Setup()
        {
            DbCleaner.ClearDentalDomainTables();
        }

        [TestMethod]
        public void GM019_DeletePatient_DoesNotCascadeCleanTaggedMedicalConditions()
        {
            // Arrange: a Patient with 2 tagged PatientMedicalInfo rows.
            Guid patientId;
            using (var seedDb = TestDatabase.NewDentalDbContext())
            {
                var patient = DomainSeed.CreatePatient(seedDb, name: "GM019 Patient");
                patientId = patient.Id;

                var medicalInfo1 = new MedicalInfo { Name = "GM019 Condition A", Created = DateTime.Now, LastUpdate = DateTime.Now };
                var medicalInfo2 = new MedicalInfo { Name = "GM019 Condition B", Created = DateTime.Now, LastUpdate = DateTime.Now };
                seedDb.MedicalInfos.Add(medicalInfo1);
                seedDb.MedicalInfos.Add(medicalInfo2);
                seedDb.SaveChanges();

                seedDb.PatientMedicalInfos.Add(new PatientMedicalInfo { PatientId = patientId, MedicalInfoId = medicalInfo1.Id });
                seedDb.PatientMedicalInfos.Add(new PatientMedicalInfo { PatientId = patientId, MedicalInfoId = medicalInfo2.Id });
                seedDb.SaveChanges();
            }

            // Act: Delete the Patient.
            bool deleted;
            using (var db = TestDatabase.NewDentalDbContext())
            {
                var service = new PatientCreateService(new PatientCreateRepository(db));
                deleted = service.Delete(patientId);
            }

            int remainingCount;
            bool allReferenceDeletedPatientId;
            using (var readDb = TestDatabase.NewDentalDbContext())
            {
                var remaining = readDb.PatientMedicalInfos.ToList();
                remainingCount = remaining.Count;
                allReferenceDeletedPatientId = remaining.Count > 0 && remaining.All(x => x.PatientId == patientId);
            }

            var output = new Fields
            {
                { "outcome", deleted ? "OK" : "REJECTED" },
                { "error_code", null },
                { "threw", false },
                { "patient_deleted", deleted },
                { "patient_medical_info_rows_remaining_count", remainingCount },
                { "orphaned_rows_reference_deleted_patient_id", allReferenceDeletedPatientId }
            };

            FixtureWriter.Write("GM-019", new Fields { { "tagged_row_count_before_delete", 2 } }, output);
        }
    }
}
