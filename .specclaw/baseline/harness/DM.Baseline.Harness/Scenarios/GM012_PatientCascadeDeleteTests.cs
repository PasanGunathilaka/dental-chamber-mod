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
    /// GM-012 — deleting a Patient cascades to delete every Prescription, PatientMedicalService, and
    /// Payment row for that patient (EF6/SQL Server's default cascade-delete convention on every
    /// [Required] Guid FK -- DM.Models/Prescription.cs:20-21, PatientMedicalService.cs:16-17,
    /// Payment.cs:13-14). Seam layer: persistence.
    /// </summary>
    [TestClass]
    public class GM012_PatientCascadeDeleteTests
    {
        [TestInitialize]
        public void Setup()
        {
            DbCleaner.ClearDentalDomainTables();
        }

        [TestMethod]
        public void GM012_DeletePatient_CascadesToPrescriptionsMedicalServicesAndPayments()
        {
            // Arrange: a Patient with 2 Prescriptions, each with 1 PatientMedicalService row and 1
            // Payment row (4 total dependent rows).
            Guid patientId;
            using (var seedDb = TestDatabase.NewDentalDbContext())
            {
                var patient = DomainSeed.CreatePatient(seedDb, name: "GM012 Patient");
                patientId = patient.Id;
                var medicalService = new MedicalService { Name = "GM012 Service", Charge = "10", Created = DateTime.Now, LastUpdate = DateTime.Now };
                seedDb.MedicalServices.Add(medicalService);
                seedDb.SaveChanges();

                for (var i = 1; i <= 2; i++)
                {
                    var prescription = DomainSeed.CreatePrescription(seedDb, patient.Id, patient.Code, TestDatabase.StatusIds.Active, codeSuffix: "00" + i);

                    seedDb.PatientMedicalServices.Add(new PatientMedicalService
                    {
                        PatientId = patient.Id,
                        PrescriptionId = prescription.Id,
                        MedicalServiceId = medicalService.Id,
                        Created = DateTime.Now,
                        LastUpdate = DateTime.Now
                    });

                    seedDb.Payments.Add(new Payment
                    {
                        PrescriptionId = prescription.Id,
                        Amount = 10,
                        Created = DateTime.Now,
                        LastUpdate = DateTime.Now
                    });
                }

                seedDb.SaveChanges();
            }

            // Act: Delete the Patient.
            bool deleted;
            using (var db = TestDatabase.NewDentalDbContext())
            {
                var service = new PatientCreateService(new PatientCreateRepository(db));
                deleted = service.Delete(patientId);
            }

            int prescriptionsRemaining;
            int patientMedicalServicesRemaining;
            int paymentsRemaining;
            using (var readDb = TestDatabase.NewDentalDbContext())
            {
                // TestInitialize's ClearDentalDomainTables guarantees these tables hold only this
                // test's own rows at this point, so a plain global count is exact -- no join needed,
                // and no risk of the cascade-deleted Prescription itself making a join-based count
                // for Payments/PatientMedicalServices ambiguous.
                prescriptionsRemaining = readDb.Prescriptions.Count();
                patientMedicalServicesRemaining = readDb.PatientMedicalServices.Count();
                paymentsRemaining = readDb.Payments.Count();
            }

            var output = new Fields
            {
                { "outcome", deleted ? "OK" : "REJECTED" },
                { "error_code", null },
                { "threw", false },
                { "patient_deleted", deleted },
                { "prescriptions_remaining_count", prescriptionsRemaining },
                { "patient_medical_services_remaining_count", patientMedicalServicesRemaining },
                { "payments_remaining_count", paymentsRemaining }
            };

            FixtureWriter.Write("GM-012", new Fields { { "prescription_count_before_delete", 2 } }, output);
        }
    }
}
