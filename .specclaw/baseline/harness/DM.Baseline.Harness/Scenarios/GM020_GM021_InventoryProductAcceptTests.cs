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
    /// GM-020 / GM-021 — DR-008 (over-shipment) and DR-009 (blind accept of client-computed Product
    /// totals), both client-side-only checks with no server-side mirror (CQ-011's finding). Seam
    /// layer: service.
    /// </summary>
    [TestClass]
    public class GM020_GM021_InventoryProductAcceptTests
    {
        [TestInitialize]
        public void Setup()
        {
            DbCleaner.ClearDentalDomainTables();
        }

        private static Product SeedProduct(DentalDbContext db, int onHand, int received = 0, int shipped = 0, int statusId = 1)
        {
            var product = new Product
            {
                Name = "GM020_021 Product",
                Code = "PR001",
                StartingInventory = onHand,
                Received = received,
                Shipped = shipped,
                OnHand = onHand,
                StatusId = statusId,
                Created = DateTime.Now,
                LastUpdate = DateTime.Now
            };
            db.Products.Add(product);
            db.SaveChanges();
            return product;
        }

        [TestMethod]
        public void GM020_ShipmentExceedingOnHand_AcceptedServerSide()
        {
            // Arrange: a Product with OnHand = 10.
            Product product;
            using (var seedDb = TestDatabase.NewDentalDbContext())
            {
                product = SeedProduct(seedDb, onHand: 10);
            }

            // Act: Add an Inventory row with StatusId = 4 (Shipped), ReceivedOrShippedQuantity = 999.
            bool added;
            using (var db = TestDatabase.NewDentalDbContext())
            {
                var service = new InventoryService(new InventoryRepository(db));
                added = service.Add(new Inventory
                {
                    ProductId = product.Id,
                    CashMemoNo = "GM020-MEMO",
                    StatusId = TestDatabase.StatusIds.Shipped,
                    OnHand = product.OnHand,
                    ReceivedOrShippedQuantity = 999,
                    Created = DateTime.Now,
                    LastUpdate = DateTime.Now
                });
            }

            Inventory storedInventory;
            Product productAfter;
            using (var readDb = TestDatabase.NewDentalDbContext())
            {
                storedInventory = readDb.Inventories.Single(x => x.ProductId == product.Id);
                productAfter = readDb.Products.Single(x => x.Id == product.Id);
            }

            var output = new Fields
            {
                { "outcome", added ? "OK" : "REJECTED" },
                { "error_code", null },
                { "threw", false },
                { "inventory", new Fields { { "received_or_shipped_quantity", storedInventory.ReceivedOrShippedQuantity } } },
                // This Add call alone does not touch Product.OnHand -- that is a separate,
                // client-driven PUT per DR-009.
                { "product_onhand_changed", productAfter.OnHand != product.OnHand }
            };

            FixtureWriter.Write("GM-020", new Fields { { "product_on_hand_before", 10 }, { "shipped_quantity", 999 } }, output);
        }

        [TestMethod]
        public void GM021_ProductTotals_PersistedVerbatim_NoServerRecomputation()
        {
            // Arrange: a Product with OnHand = 10, Received = 20, Shipped = 10, StatusId = 1
            // (In Stock) -- an internally-consistent starting state.
            Product product;
            using (var seedDb = TestDatabase.NewDentalDbContext())
            {
                product = SeedProduct(seedDb, onHand: 10, received: 20, shipped: 10, statusId: TestDatabase.StatusIds.InStock);
            }

            // Act: Edit the Product with OnHand = 9999 (not derivable from any real Inventory
            // movement) and StatusId = 2 (Out Of Stock, also inconsistent with a positive OnHand).
            bool edited;
            using (var db = TestDatabase.NewDentalDbContext())
            {
                var service = new ProductService(new ProductRepository(db));
                var toEdit = new Product
                {
                    Id = product.Id,
                    Name = product.Name,
                    Code = product.Code,
                    StartingInventory = product.StartingInventory,
                    Received = product.Received,
                    Shipped = product.Shipped,
                    OnHand = 9999,
                    StatusId = TestDatabase.StatusIds.OutOfStock,
                    Created = product.Created,
                    LastUpdate = DateTime.Now
                };
                edited = service.Edit(toEdit);
            }

            Product productAfter;
            using (var readDb = TestDatabase.NewDentalDbContext())
            {
                productAfter = readDb.Products.Single(x => x.Id == product.Id);
            }

            var output = new Fields
            {
                { "outcome", edited ? "OK" : "REJECTED" },
                { "error_code", null },
                { "threw", false },
                { "product", new Fields { { "on_hand", productAfter.OnHand }, { "status_id", productAfter.StatusId } } }
            };

            FixtureWriter.Write(
                "GM-021",
                new Fields { { "attempted_on_hand", 9999 }, { "attempted_status_id", TestDatabase.StatusIds.OutOfStock } },
                output);
        }
    }
}
