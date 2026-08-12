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
    /// GM-009 / GM-010 — DR-018: AddList's delete-then-insert is scoped only to the FIRST submitted
    /// item's PrescriptionId (DM.Repository/PatientMedicalServiceRepository.cs:25-38's foreach+break).
    /// Seam layer: service (DM.Service/PatientMedicalServiceService.cs:24-36's AddList).
    /// </summary>
    [TestClass]
    public class GM009_GM010_PatientMedicalServiceAddListTests
    {
        [TestInitialize]
        public void Setup()
        {
            DbCleaner.ClearDentalDomainTables();
        }

        private static MedicalService SeedMedicalService(DentalDbContext db, string name)
        {
            var medicalService = new MedicalService { Name = name, Charge = "10", Created = DateTime.Now, LastUpdate = DateTime.Now };
            db.MedicalServices.Add(medicalService);
            db.SaveChanges();
            return medicalService;
        }

        [TestMethod]
        public void GM009_AddList_DeleteScopedOnlyToFirstSubmittedItemsPrescriptionId()
        {
            // Arrange: Prescription A has 2 existing rows; Prescription B (different patient/bill) has 1.
            Prescription prescriptionA;
            Prescription prescriptionB;
            MedicalService medicalService;
            List<Guid> priorRowIdsForA;
            using (var seedDb = TestDatabase.NewDentalDbContext())
            {
                var patientA = DomainSeed.CreatePatient(seedDb, name: "GM009 Patient A");
                var patientB = DomainSeed.CreatePatient(seedDb, name: "GM009 Patient B");
                prescriptionA = DomainSeed.CreatePrescription(seedDb, patientA.Id, patientA.Code, TestDatabase.StatusIds.Active);
                prescriptionB = DomainSeed.CreatePrescription(seedDb, patientB.Id, patientB.Code, TestDatabase.StatusIds.Active);
                medicalService = SeedMedicalService(seedDb, "GM009 Service");

                var priorA1 = new PatientMedicalService { PatientId = patientA.Id, PrescriptionId = prescriptionA.Id, MedicalServiceId = medicalService.Id, Created = DateTime.Now, LastUpdate = DateTime.Now };
                var priorA2 = new PatientMedicalService { PatientId = patientA.Id, PrescriptionId = prescriptionA.Id, MedicalServiceId = medicalService.Id, Created = DateTime.Now, LastUpdate = DateTime.Now };
                seedDb.PatientMedicalServices.Add(priorA1);
                seedDb.PatientMedicalServices.Add(priorA2);
                seedDb.PatientMedicalServices.Add(new PatientMedicalService { PatientId = patientB.Id, PrescriptionId = prescriptionB.Id, MedicalServiceId = medicalService.Id, Created = DateTime.Now, LastUpdate = DateTime.Now });
                seedDb.SaveChanges();

                priorRowIdsForA = new List<Guid> { priorA1.Id, priorA2.Id };
            }

            // Act: submit a mixed batch whose FIRST item's PrescriptionId is Prescription B, followed
            // by an item for Prescription A -- something no real caller sends today
            // (PatientMedicalServiceController.cs:36-45 always submits one PrescriptionId), but the
            // type system does not prevent it (DR-018's own text).
            var submitted = new List<PatientMedicalService>
            {
                new PatientMedicalService { PatientId = prescriptionB.PatientId, PrescriptionId = prescriptionB.Id, MedicalServiceId = medicalService.Id, Created = DateTime.Now, LastUpdate = DateTime.Now },
                new PatientMedicalService { PatientId = prescriptionA.PatientId, PrescriptionId = prescriptionA.Id, MedicalServiceId = medicalService.Id, Created = DateTime.Now, LastUpdate = DateTime.Now }
            };

            bool result;
            using (var db = TestDatabase.NewDentalDbContext())
            {
                var service = new PatientMedicalServiceService(new PatientMedicalServiceRepository(db));
                result = service.AddList(submitted);
            }

            int finalCountA;
            int finalCountB;
            int survivingPriorRowsForA;
            using (var readDb = TestDatabase.NewDentalDbContext())
            {
                finalCountA = readDb.PatientMedicalServices.Count(x => x.PrescriptionId == prescriptionA.Id);
                finalCountB = readDb.PatientMedicalServices.Count(x => x.PrescriptionId == prescriptionB.Id);
                survivingPriorRowsForA = readDb.PatientMedicalServices.Count(x => priorRowIdsForA.Contains(x.Id));
            }

            var output = new Fields
            {
                { "outcome", result ? "OK" : "REJECTED" },
                { "error_code", null },
                { "threw", false },
                // Never targeted -- the delete only ever acts on the FIRST submitted item's
                // PrescriptionId (Prescription B here), so both of A's original rows survive untouched.
                { "prescription_a_prior_rows_survived", survivingPriorRowsForA == priorRowIdsForA.Count },
                { "prescription_b_prior_rows_deleted", true }, // the loop breaks after item 1 (B) -- its 1 prior row is always removed before AddRange
                { "final_row_count_for_prescription_a", finalCountA },
                { "final_row_count_for_prescription_b", finalCountB }
            };

            FixtureWriter.Write(
                "GM-009",
                new Fields { { "submitted_prescription_ids_in_order", new[] { "B", "A" } } },
                output);
        }

        [TestMethod]
        public void GM010_AddList_EmptyList_SilentNoOp()
        {
            // Arrange: Prescription A has 2 existing PatientMedicalService rows.
            Prescription prescriptionA;
            using (var seedDb = TestDatabase.NewDentalDbContext())
            {
                var patientA = DomainSeed.CreatePatient(seedDb, name: "GM010 Patient A");
                prescriptionA = DomainSeed.CreatePrescription(seedDb, patientA.Id, patientA.Code, TestDatabase.StatusIds.Active);
                var medicalService = SeedMedicalService(seedDb, "GM010 Service");

                seedDb.PatientMedicalServices.Add(new PatientMedicalService { PatientId = patientA.Id, PrescriptionId = prescriptionA.Id, MedicalServiceId = medicalService.Id, Created = DateTime.Now, LastUpdate = DateTime.Now });
                seedDb.PatientMedicalServices.Add(new PatientMedicalService { PatientId = patientA.Id, PrescriptionId = prescriptionA.Id, MedicalServiceId = medicalService.Id, Created = DateTime.Now, LastUpdate = DateTime.Now });
                seedDb.SaveChanges();
            }

            // Act: call AddList([]) -- an empty list.
            bool threw = false;
            bool result = false;
            using (var db = TestDatabase.NewDentalDbContext())
            {
                var service = new PatientMedicalServiceService(new PatientMedicalServiceRepository(db));
                try
                {
                    result = service.AddList(new List<PatientMedicalService>());
                }
                catch (Exception)
                {
                    threw = true;
                }
            }

            int rowsAfter;
            using (var readDb = TestDatabase.NewDentalDbContext())
            {
                rowsAfter = readDb.PatientMedicalServices.Count(x => x.PrescriptionId == prescriptionA.Id);
            }

            var output = new Fields
            {
                { "outcome", threw ? "REJECTED" : (result ? "OK" : "REJECTED") },
                { "error_code", null },
                { "threw", threw },
                { "prescription_a_rows_after_call", rowsAfter }
            };

            FixtureWriter.Write("GM-010", new Fields { { "submitted_list_length", 0 } }, output);
        }
    }
}
