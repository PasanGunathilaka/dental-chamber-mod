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
    /// GM-025 — DR-010: AppointmentRepository.GetByDate filters to StatusId == 7 ("Appointed") only,
    /// excluding "Visited" (8) (AppointmentRepository.cs:27). Seam layer: service
    /// (DM.Service/AppointmentService.GetByDate, wrapping the repository method).
    ///
    /// Per seams.md Capture Blocker #4, this Arrange seeds the two appointments with distinct Time
    /// values, avoiding the unspecified OrderBy(x =&gt; x.Time) tie condition.
    /// </summary>
    [TestClass]
    public class GM025_AppointmentGetByDateTests
    {
        [TestInitialize]
        public void Setup()
        {
            DbCleaner.ClearDentalDomainTables();
        }

        [TestMethod]
        public void GM025_GetByDate_ExcludesVisitedAppointments()
        {
            // Arrange: two Appointments on the same date, distinct Time values: one
            // StatusId = 7 (Appointed), one StatusId = 8 (Visited).
            var date = new DateTime(2026, 3, 10);
            using (var seedDb = TestDatabase.NewDentalDbContext())
            {
                seedDb.Appointments.Add(new Appointment
                {
                    Code = "AP001", PatientNameOrId = "GM025 Patient A", Age = 30,
                    Date = date, Time = date.AddHours(9),
                    DoctorId = TestDatabase.SeededDoctorId, StatusId = TestDatabase.StatusIds.Appointed,
                    Created = DateTime.Now, LastUpdate = DateTime.Now
                });
                seedDb.Appointments.Add(new Appointment
                {
                    Code = "AP002", PatientNameOrId = "GM025 Patient B", Age = 40,
                    Date = date, Time = date.AddHours(10),
                    DoctorId = TestDatabase.SeededDoctorId, StatusId = TestDatabase.StatusIds.Visited,
                    Created = DateTime.Now, LastUpdate = DateTime.Now
                });
                seedDb.SaveChanges();
            }

            // Act
            List<Appointment> results;
            using (var db = TestDatabase.NewDentalDbContext())
            {
                var service = new AppointmentService(new AppointmentRepository(db));
                results = service.GetByDate(date);
            }

            var output = new Fields
            {
                { "outcome", "OK" },
                { "error_code", null },
                { "threw", false },
                { "appointments_returned_count", results.Count },
                { "appointments", results.Select(x => new Fields { { "status_id", x.StatusId } }).ToList() }
            };

            FixtureWriter.Write("GM-025", new Fields { { "date", date } }, output);
        }
    }
}
