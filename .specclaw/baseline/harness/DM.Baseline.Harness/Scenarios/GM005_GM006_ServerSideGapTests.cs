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
    /// GM-005 / GM-006 — client-side-only business rules (DR-004 discount range, DR-005 overpayment)
    /// with no server-side mirror (CQ-011's finding). Seam layer: service.
    /// </summary>
    [TestClass]
    public class GM005_GM006_ServerSideGapTests
    {
        [TestInitialize]
        public void Setup()
        {
            DbCleaner.ClearDentalDomainTables();
        }

        private static Prescription SeedPrescription(double totalDue, double discountPercent)
        {
            using (var db = TestDatabase.NewDentalDbContext())
            {
                var patient = new Patient { Name = "Discount Patient", Age = 33, Gender = "Male", Code = "P900001", Created = DateTime.Now, LastUpdate = DateTime.Now };
                db.Patients.Add(patient);

                var prescription = new Prescription
                {
                    Code = "BILL001-" + patient.Code,
                    PatientId = patient.Id,
                    StatusId = TestDatabase.StatusIds.Active,
                    DiscountPercent = discountPercent,
                    TotalDue = totalDue,
                    Created = DateTime.Now,
                    LastUpdate = DateTime.Now
                };
                db.Prescriptions.Add(prescription);

                db.SaveChanges();
                return prescription;
            }
        }

        [TestMethod]
        public void GM005_DiscountPercentOutsideRange_AcceptedServerSide()
        {
            // Arrange: an existing Active Prescription with DiscountPercent = 0 (scenarios.md GM-005).
            var seeded = SeedPrescription(totalDue: 0, discountPercent: 0);

            // Act: Edit with DiscountPercent = 150, then a second sub-case with -25.
            var results = new System.Collections.Generic.List<Fields>();
            foreach (var attempted in new[] { 150d, -25d })
            {
                using (var db = TestDatabase.NewDentalDbContext())
                {
                    var service = new PrescriptionService(new PrescriptionRepository(db));
                    var toEdit = new Prescription
                    {
                        Id = seeded.Id,
                        Code = seeded.Code,
                        PatientId = seeded.PatientId,
                        StatusId = seeded.StatusId,
                        DiscountPercent = attempted,
                        TotalDue = seeded.TotalDue,
                        Created = seeded.Created,
                        LastUpdate = DateTime.Now
                    };

                    var edited = service.Edit(toEdit);
                    Assert.IsTrue(edited, "BaseService<Prescription>.Edit should not itself report failure for an out-of-range DiscountPercent.");
                }

                using (var readDb = TestDatabase.NewDentalDbContext())
                {
                    var persisted = readDb.Prescriptions.Single(x => x.Id == seeded.Id);
                    results.Add(new Fields
                    {
                        { "attempted_discount_percent", attempted },
                        { "outcome", "OK" },
                        { "error_code", null },
                        { "threw", false },
                        { "prescription", new Fields { { "discount_percent", persisted.DiscountPercent } } }
                    });
                }
            }

            FixtureWriter.Write(
                "GM-005",
                new Fields { { "attempted_discount_percents", new[] { 150d, -25d } } },
                new Fields { { "results", results } });
        }

        [TestMethod]
        public void GM006_PaymentExceedingDue_AcceptedServerSide()
        {
            // Arrange: an Active Prescription with TotalDue = 50 (scenarios.md GM-006).
            var seeded = SeedPrescription(totalDue: 50, discountPercent: 0);

            // Act: Add a Payment with Amount = 500.
            bool added;
            using (var db = TestDatabase.NewDentalDbContext())
            {
                var paymentService = new PaymentService(new PaymentRepository(db));
                var payment = new Payment
                {
                    PrescriptionId = seeded.Id,
                    Amount = 500,
                    Created = DateTime.Now,
                    LastUpdate = DateTime.Now
                };
                added = paymentService.Add(payment);
            }

            Payment storedPayment;
            Prescription prescriptionAfter;
            using (var readDb = TestDatabase.NewDentalDbContext())
            {
                storedPayment = readDb.Payments.Single(x => x.PrescriptionId == seeded.Id);
                prescriptionAfter = readDb.Prescriptions.Single(x => x.Id == seeded.Id);
            }

            // Nothing server-side recomputes Prescription.TotalPaid/TotalDue from the Payments
            // collection (functional-spec.md's "Record Payment Against Bill" workflow attributes that
            // recomputation entirely to the client's own follow-up PUT) -- confirmed here by the fact
            // that TotalDue/TotalPaid are unchanged by this Add call alone.
            var totalsRecomputed = prescriptionAfter.TotalDue != seeded.TotalDue || prescriptionAfter.TotalPaid != 0;

            var output = new Fields
            {
                { "outcome", added ? "OK" : "REJECTED" },
                { "error_code", null },
                { "threw", false },
                { "payment", new Fields { { "id", storedPayment.Id }, { "amount", storedPayment.Amount } } },
                { "prescription_totals_recomputed", totalsRecomputed }
            };

            FixtureWriter.Write(
                "GM-006",
                new Fields { { "prescription_total_due_before", seeded.TotalDue }, { "payment_amount", 500 } },
                output,
                normalizedFields: new[] { "payment.id" });
        }
    }
}
