using System;
using DM.Models;

namespace DM.Baseline.Harness.Infrastructure
{
    /// <summary>
    /// Small, shared Arrange helpers for the Dental-domain entities (Patient/Prescription/
    /// MedicalService/etc.), used by several scenario test classes so each one's own Arrange step
    /// stays a one-liner rather than re-deriving entity construction rules independently in every
    /// file. Every field populated here is either required by a DataAnnotation
    /// (confirmed by reading DM.Models directly this run) or is the specific value a scenario's own
    /// Arrange text calls for.
    /// </summary>
    public static class DomainSeed
    {
        private static int _patientSequence;

        public static Patient CreatePatient(DentalDbContext db, string name = "Test Patient", int age = 30, string gender = "Male", string code = null)
        {
            _patientSequence++;
            var patient = new Patient
            {
                Name = name,
                Age = age,
                Gender = gender,
                Code = code ?? ("P9" + _patientSequence.ToString("00000")),
                Created = DateTime.Now,
                LastUpdate = DateTime.Now
            };

            db.Patients.Add(patient);
            db.SaveChanges();
            return patient;
        }

        public static Prescription CreatePrescription(
            DentalDbContext db,
            Guid patientId,
            string patientCode,
            int statusId,
            double totalDue = 0,
            double totalPaid = 0,
            double discountPercent = 0,
            double discountAmount = 0,
            double fixedDiscount = 0,
            string codeSuffix = "001")
        {
            var prescription = new Prescription
            {
                Code = "BILL" + codeSuffix + "-" + patientCode,
                PatientId = patientId,
                StatusId = statusId,
                TotalDue = totalDue,
                TotalPaid = totalPaid,
                DiscountPercent = discountPercent,
                DiscountAmount = discountAmount,
                FixedDiscount = fixedDiscount,
                Created = DateTime.Now,
                LastUpdate = DateTime.Now
            };

            db.Prescriptions.Add(prescription);
            db.SaveChanges();
            return prescription;
        }
    }
}
