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
    /// GM-013 / GM-014 / GM-015 — DR-017: duplicate `Name` on MedicalService/MedicalInfo, rejected
    /// two different ways depending on the layer observed. Error code: DUPLICATE_NAME
    /// (error-map.md).
    /// </summary>
    [TestClass]
    public class GM013_GM014_GM015_DuplicateNameTests
    {
        [TestInitialize]
        public void Setup()
        {
            DbCleaner.ClearDentalDomainTables();
        }

        [TestMethod]
        public void GM013_DuplicateMedicalServiceName_RejectedAtServiceLayer()
        {
            // Arrange: an existing MedicalService named "Scaling".
            using (var seedDb = TestDatabase.NewDentalDbContext())
            {
                seedDb.MedicalServices.Add(new MedicalService { Name = "Scaling", Charge = "10", Created = DateTime.Now, LastUpdate = DateTime.Now });
                seedDb.SaveChanges();
            }

            // Act: Add a new MedicalService also named "Scaling".
            bool added;
            using (var db = TestDatabase.NewDentalDbContext())
            {
                var service = new MedicalServiceService(new MedicalServiceRepository(db));
                added = service.Add(new MedicalService { Name = "Scaling", Charge = "20", Created = DateTime.Now, LastUpdate = DateTime.Now });
            }

            var output = new Fields
            {
                // BaseService<T>.Add's catch (Exception) at DM.Service/BaseService.cs:40-55 swallows
                // the underlying DbUpdateException and returns false -- seams.md Capture Blocker #5.
                { "outcome", added ? "OK" : "REJECTED" },
                { "threw", false },
                { "error_code", added ? null : "DUPLICATE_NAME" }
            };

            FixtureWriter.Write("GM-013", new Fields { { "duplicate_name", "Scaling" } }, output);
        }

        [TestMethod]
        public void GM014_DuplicateMedicalServiceName_RawExceptionAtPersistenceLayer()
        {
            // Arrange: an existing MedicalService named "Scaling".
            using (var seedDb = TestDatabase.NewDentalDbContext())
            {
                seedDb.MedicalServices.Add(new MedicalService { Name = "Scaling", Charge = "10", Created = DateTime.Now, LastUpdate = DateTime.Now });
                seedDb.SaveChanges();
            }

            // Act: call the repository's Add + Commit directly -- no service-layer try/catch in front
            // (DM.Repository/BaseRepository.cs:36-54).
            bool threw = false;
            string exceptionType = null;
            string innerExceptionType = null;
            string exceptionMessage = null;
            string innerExceptionMessage = null;

            using (var db = TestDatabase.NewDentalDbContext())
            {
                var repository = new MedicalServiceRepository(db);
                try
                {
                    repository.Add(new MedicalService { Name = "Scaling", Charge = "20", Created = DateTime.Now, LastUpdate = DateTime.Now });
                    repository.Commit();
                }
                catch (Exception ex)
                {
                    threw = true;
                    exceptionType = ex.GetType().FullName;
                    exceptionMessage = ex.Message;
                    innerExceptionType = ex.InnerException?.GetType().FullName;
                    innerExceptionMessage = ex.InnerException?.Message;
                }
            }

            var output = new Fields
            {
                { "outcome", "REJECTED" },
                { "threw", threw },
                { "error_code", "DUPLICATE_NAME" },
                { "ExceptionType", exceptionType },
                { "InnerExceptionType", innerExceptionType },
                { "ExceptionMessage", exceptionMessage },
                { "InnerExceptionMessage", innerExceptionMessage }
            };

            FixtureWriter.Write("GM-014", new Fields { { "duplicate_name", "Scaling" } }, output);
        }

        [TestMethod]
        public void GM015_DuplicateMedicalInfoName_RejectedAtServiceLayer()
        {
            // Arrange: an existing MedicalInfo named "Diabetic".
            using (var seedDb = TestDatabase.NewDentalDbContext())
            {
                seedDb.MedicalInfos.Add(new MedicalInfo { Name = "Diabetic", Created = DateTime.Now, LastUpdate = DateTime.Now });
                seedDb.SaveChanges();
            }

            // Act: Add a new MedicalInfo also named "Diabetic".
            bool added;
            using (var db = TestDatabase.NewDentalDbContext())
            {
                var medicalInfoRepository = new MedicalInfoRepository(db);
                var patientMedicalInfoRepository = new PatientMedicalInfoRepository(db);
                var service = new MedicalInfoService(medicalInfoRepository, patientMedicalInfoRepository);
                added = service.Add(new MedicalInfo { Name = "Diabetic", Created = DateTime.Now, LastUpdate = DateTime.Now });
            }

            var output = new Fields
            {
                { "outcome", added ? "OK" : "REJECTED" },
                { "threw", false },
                { "error_code", added ? null : "DUPLICATE_NAME" }
            };

            FixtureWriter.Write("GM-015", new Fields { { "duplicate_name", "Diabetic" } }, output);
        }
    }
}
