using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Results;
using DM.AuthServer.Controllers;
using DM.Baseline.Harness.Infrastructure;
using DM.Models;
using DM.Repository;
using DM.RequestModels;
using DM.Service;
using DM.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace DM.Baseline.Harness.Scenarios
{
    /// <summary>
    /// GM-022 / GM-023 — DR-020: InventoryReportController.GetReport's OnHand fallback chain when a
    /// product has zero movements inside the requested window. Seam layer: service (a plain
    /// in-process controller call).
    /// </summary>
    [TestClass]
    public class GM022_GM023_InventoryReportOnHandTests
    {
        [TestInitialize]
        public void Setup()
        {
            DbCleaner.ClearDentalDomainTables();
        }

        private static InventoryReportController BuildController(out DentalDbContext db)
        {
            db = TestDatabase.NewDentalDbContext();
            var productService = new ProductService(new ProductRepository(db));
            var inventoryService = new InventoryService(new InventoryRepository(db));
            return new InventoryReportController(productService, inventoryService);
        }

        [TestMethod]
        public void GM022_OnHandFallback_ZeroMovementsInWindow_LaterMovementWithinOneMonthAfter()
        {
            // Arrange: a Product with zero Inventory movements inside the report window [From, To],
            // and exactly one movement dated after To but within To.AddMonths(1), with
            // StatusId = 3 (Received), OnHand = 5, ReceivedOrShippedQuantity = 3.
            var from = new DateTime(2026, 1, 1);
            var to = new DateTime(2026, 1, 31);
            var laterMovementDate = new DateTime(2026, 2, 15); // after `to`, well within to.AddMonths(1) == 2026-02-28

            Product product;
            using (var seedDb = TestDatabase.NewDentalDbContext())
            {
                product = new Product
                {
                    Name = "GM022 Product", Code = "PR022",
                    StartingInventory = 0, Received = 0, Shipped = 0, OnHand = 0,
                    StatusId = TestDatabase.StatusIds.InStock,
                    Created = DateTime.Now, LastUpdate = DateTime.Now
                };
                seedDb.Products.Add(product);
                seedDb.SaveChanges();

                seedDb.Inventories.Add(new Inventory
                {
                    ProductId = product.Id,
                    CashMemoNo = "GM022-MEMO",
                    StatusId = TestDatabase.StatusIds.Received,
                    OnHand = 5,
                    ReceivedOrShippedQuantity = 3,
                    Created = laterMovementDate,
                    LastUpdate = laterMovementDate
                });
                seedDb.SaveChanges();
            }

            // Act
            DentalDbContext db;
            var controller = BuildController(out db);
            var request = JsonConvert.SerializeObject(new InventoryReportRequestModel { From = from, To = to });
            var result = (OkNegotiatedContentResult<List<InventoryReportViewModel>>)controller.GetReport(request);
            var reportRow = result.Content.Single(x => x.Name == product.Name);

            var output = new Fields
            {
                { "outcome", "OK" },
                { "error_code", null },
                { "threw", false },
                { "report", new List<Fields>
                    {
                        new Fields
                        {
                            { "on_hand", reportRow.OnHand },
                            { "received", reportRow.Received },
                            { "shipped", reportRow.Shipped }
                        }
                    }
                }
            };

            FixtureWriter.Write(
                "GM-022",
                new Fields { { "from", from }, { "to", to }, { "later_movement_date", laterMovementDate } },
                output);
        }

        [TestMethod]
        public void GM023_OnHandFallback_ZeroMovementsEver_FallsBackToProductLiveOnHand()
        {
            // Arrange: a Product with zero Inventory movements, ever, OnHand = 42.
            var from = new DateTime(2026, 1, 1);
            var to = new DateTime(2026, 1, 31);

            Product product;
            using (var seedDb = TestDatabase.NewDentalDbContext())
            {
                product = new Product
                {
                    Name = "GM023 Product", Code = "PR023",
                    StartingInventory = 42, Received = 0, Shipped = 0, OnHand = 42,
                    StatusId = TestDatabase.StatusIds.InStock,
                    Created = DateTime.Now, LastUpdate = DateTime.Now
                };
                seedDb.Products.Add(product);
                seedDb.SaveChanges();
            }

            // Act
            DentalDbContext db;
            var controller = BuildController(out db);
            var request = JsonConvert.SerializeObject(new InventoryReportRequestModel { From = from, To = to });
            var result = (OkNegotiatedContentResult<List<InventoryReportViewModel>>)controller.GetReport(request);
            var reportRow = result.Content.Single(x => x.Name == product.Name);

            var output = new Fields
            {
                { "outcome", "OK" },
                { "error_code", null },
                { "threw", false },
                { "report", new List<Fields> { new Fields { { "on_hand", reportRow.OnHand } } } }
            };

            FixtureWriter.Write("GM-023", new Fields { { "from", from }, { "to", to } }, output);
        }
    }
}
