using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Http.Results;
using DM.AuthServer.Controllers;
using DM.Baseline.Harness.Infrastructure;
using DM.Models;
using DM.Repository;
using DM.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DM.Baseline.Harness.Scenarios
{
    /// <summary>
    /// GM-002 / GM-003 — PatientCreateController.Post's two-write (Patient + auto-provisioned
    /// Prescription) orchestration. Seam layer: service (a plain in-process controller call, no
    /// OWIN/HTTP pipeline needed — see seams.md's own reasoning for classifying this controller
    /// method as the innermost observable layer for DR-001/DR-002).
    /// </summary>
    [TestClass]
    public class GM002_GM003_PatientCreateTests
    {
        [TestInitialize]
        public void Setup()
        {
            DbCleaner.ClearDentalDomainTables();
        }

        private static PatientCreateController BuildController(out DentalDbContext db)
        {
            db = TestDatabase.NewDentalDbContext();
            var patientService = new PatientCreateService(new PatientCreateRepository(db));
            var prescriptionService = new PrescriptionService(new PrescriptionRepository(db));
            return new PatientCreateController(patientService, prescriptionService);
        }

        [TestMethod]
        public void GM002_PatientCreate_SilentSucceedsOverCodeCollision()
        {
            // Arrange: create Patient A via a normal Post (auto-generated Code = "P000001").
            DentalDbContext db;
            var controller = BuildController(out db);

            var patientA = new Patient { Name = "Patient A", Age = 30, Gender = "Male", Created = DateTime.Now, LastUpdate = DateTime.Now };
            var postA = (OkNegotiatedContentResult<Guid>)controller.Post(patientA);
            var patientAId = postA.Content;

            Patient storedPatientA;
            using (var readDb = TestDatabase.NewDentalDbContext())
            {
                storedPatientA = readDb.Patients.Single(x => x.Id == patientAId);
            }

            // PUT-edit Patient A's own Code field to "P000002" -- the exact value the count-based
            // formula will generate next, since exactly one patient row exists. PatientCreateController.Put
            // (PatientCreateController.cs:81-89) performs no uniqueness pre-check beyond ModelState.IsValid.
            storedPatientA.Code = "P000002";
            controller.Put(storedPatientA);

            // Act: Post a new Patient B, which will independently compute Code = "P000002" too
            // (GetPatientViewModel().Count() + 1 == 2 patients, so GetThisPatientCode("2") == "P000002"),
            // colliding with Patient A's just-edited Code under Patient.Code's own unique index
            // (DM.Models/DentalDbContext.cs:50).
            var patientB = new Patient { Name = "Patient B", Age = 40, Gender = "Female", Created = DateTime.Now, LastUpdate = DateTime.Now };
            var postB = (OkNegotiatedContentResult<Guid>)controller.Post(patientB);
            var patientBId = postB.Content;

            bool patientBPersisted;
            using (var readDb = TestDatabase.NewDentalDbContext())
            {
                patientBPersisted = readDb.Patients.Any(x => x.Id == patientBId);
            }

            var output = new Fields
            {
                { "http_status", 200 },
                { "outcome", "OK" },
                { "error_code", null },
                { "threw", false },
                { "patient_b_persisted", patientBPersisted },
                { "patient_b_id_returned", patientBId }
            };

            FixtureWriter.Write(
                "GM-002",
                new Fields
                {
                    { "patient_a_edited_code", "P000002" },
                    { "patient_b_name", patientB.Name }
                },
                output,
                normalizedFields: new[] { "patient_b_id_returned" });
        }

        [TestMethod]
        public void GM003_PatientCreate_AutoProvisionsInitialActiveBill()
        {
            // Arrange: empty Prescriptions table for the new patient (true of any brand-new patient,
            // guaranteed here by TestInitialize's ClearDentalDomainTables).
            DentalDbContext db;
            var controller = BuildController(out db);

            // Act
            var patient = new Patient { Name = "New Patient", Age = 25, Gender = "Male", Created = DateTime.Now, LastUpdate = DateTime.Now };
            var post = (OkNegotiatedContentResult<Guid>)controller.Post(patient);
            var patientId = post.Content;

            Patient storedPatient;
            Prescription storedPrescription;
            using (var readDb = TestDatabase.NewDentalDbContext())
            {
                storedPatient = readDb.Patients.Single(x => x.Id == patientId);
                storedPrescription = readDb.Prescriptions.Single(x => x.PatientId == patientId);
            }

            var billCodePattern = new Regex("^BILL\\d+-" + Regex.Escape(storedPatient.Code) + "$");

            var output = new Fields
            {
                { "outcome", "OK" },
                { "error_code", null },
                { "threw", false },
                { "patient", new Fields
                    {
                        { "id", storedPatient.Id },
                        { "code", storedPatient.Code },
                        { "created", storedPatient.Created },
                        { "last_update", storedPatient.LastUpdate }
                    }
                },
                { "prescription_created", true },
                { "prescription", new Fields
                    {
                        { "id", storedPrescription.Id },
                        { "status_id", storedPrescription.StatusId },
                        { "code_format_matches_bill_pattern", billCodePattern.IsMatch(storedPrescription.Code) },
                        { "total_due", storedPrescription.TotalDue },
                        { "created", storedPrescription.Created },
                        { "last_update", storedPrescription.LastUpdate }
                    }
                }
            };

            FixtureWriter.Write(
                "GM-003",
                new Fields { { "patient_name", patient.Name } },
                output,
                normalizedFields: new[]
                {
                    "patient.id", "patient.created", "patient.last_update",
                    "prescription.id", "prescription.created", "prescription.last_update"
                });
        }
    }
}
