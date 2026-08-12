using System;
using System.Collections.Generic;
using System.Linq;
using DM.Baseline.Harness.Infrastructure;
using DM.Models;
using DM.Repository;
using DM.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DM.Baseline.Harness.Scenarios
{
    /// <summary>
    /// GM-018 — MedicalInfoService.SavePatientMedicalInfos crashes on an empty submitted list instead
    /// of clearing the prior tags (DM.Service/MedicalInfoService.cs:40-60's `.First()` on an empty
    /// sequence). Seam layer: service. ⚠ PROVISIONAL — pending PQ-006 (scenarios.md).
    /// Error code: EMPTY_MEDICAL_INFO_LIST (error-map.md).
    /// </summary>
    [TestClass]
    public class GM018_MedicalInfoEmptyListTests
    {
        [TestInitialize]
        public void Setup()
        {
            DbCleaner.ClearDentalDomainTables();
        }

        [TestMethod]
        public void GM018_SavePatientMedicalInfos_EmptyList_CrashesInsteadOfClearing()
        {
            // Arrange: a Patient with 2 existing tagged PatientMedicalInfo rows.
            Guid patientId;
            List<Guid> priorRowIds;
            using (var seedDb = TestDatabase.NewDentalDbContext())
            {
                var patient = DomainSeed.CreatePatient(seedDb, name: "GM018 Patient");
                patientId = patient.Id;

                var medicalInfo1 = new MedicalInfo { Name = "GM018 Condition A", Created = DateTime.Now, LastUpdate = DateTime.Now };
                var medicalInfo2 = new MedicalInfo { Name = "GM018 Condition B", Created = DateTime.Now, LastUpdate = DateTime.Now };
                seedDb.MedicalInfos.Add(medicalInfo1);
                seedDb.MedicalInfos.Add(medicalInfo2);
                seedDb.SaveChanges();

                var tag1 = new PatientMedicalInfo { PatientId = patientId, MedicalInfoId = medicalInfo1.Id };
                var tag2 = new PatientMedicalInfo { PatientId = patientId, MedicalInfoId = medicalInfo2.Id };
                seedDb.PatientMedicalInfos.Add(tag1);
                seedDb.PatientMedicalInfos.Add(tag2);
                seedDb.SaveChanges();

                priorRowIds = new List<Guid> { tag1.Id, tag2.Id };
            }

            // Act: call SavePatientMedicalInfos([]) -- the empty list produced by unchecking every
            // previously-tagged medical condition on patient-detail.tpl.html's Medical Condition tab.
            bool threw = false;
            string exceptionType = null;
            string exceptionMessage = null;

            using (var db = TestDatabase.NewDentalDbContext())
            {
                var medicalInfoRepository = new MedicalInfoRepository(db);
                var patientMedicalInfoRepository = new PatientMedicalInfoRepository(db);
                var service = new MedicalInfoService(medicalInfoRepository, patientMedicalInfoRepository);

                try
                {
                    service.SavePatientMedicalInfos(new List<PatientMedicalInfo>());
                }
                catch (Exception ex)
                {
                    threw = true;
                    exceptionType = ex.GetType().FullName;
                    exceptionMessage = ex.Message;
                }
            }

            int survivingPriorRows;
            using (var readDb = TestDatabase.NewDentalDbContext())
            {
                survivingPriorRows = readDb.PatientMedicalInfos.Count(x => priorRowIds.Contains(x.Id));
            }

            var output = new Fields
            {
                { "outcome", threw ? "REJECTED" : "OK" },
                { "error_code", threw ? "EMPTY_MEDICAL_INFO_LIST" : null },
                { "threw", threw },
                { "ExceptionType", exceptionType },
                { "InnerExceptionType", null },
                { "ExceptionMessage", exceptionMessage },
                { "InnerExceptionMessage", null },
                { "prior_tags_survived", survivingPriorRows == priorRowIds.Count }
            };

            FixtureWriter.Write("GM-018", new Fields { { "submitted_list_length", 0 } }, output);
        }
    }
}
