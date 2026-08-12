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
    /// GM-024 — deleting a Product cascades to delete all of its Inventory movement rows
    /// (DM.Models/Inventory.cs:17-18's required ProductId FK). Seam layer: persistence.
    /// </summary>
    [TestClass]
    public class GM024_ProductCascadeDeleteTests
    {
        [TestInitialize]
        public void Setup()
        {
            DbCleaner.ClearDentalDomainTables();
        }

        [TestMethod]
        public void GM024_DeleteProduct_CascadesToInventoryRows()
        {
            // Arrange: a Product with 3 Inventory rows.
            Guid productId;
            using (var seedDb = TestDatabase.NewDentalDbContext())
            {
                var product = new Product
                {
                    Name = "GM024 Product", Code = "PR024",
                    StartingInventory = 10, Received = 10, Shipped = 0, OnHand = 10,
                    StatusId = TestDatabase.StatusIds.InStock,
                    Created = DateTime.Now, LastUpdate = DateTime.Now
                };
                seedDb.Products.Add(product);
                seedDb.SaveChanges();
                productId = product.Id;

                for (var i = 0; i < 3; i++)
                {
                    seedDb.Inventories.Add(new Inventory
                    {
                        ProductId = productId,
                        CashMemoNo = "GM024-MEMO-" + i,
                        StatusId = TestDatabase.StatusIds.Received,
                        OnHand = 10,
                        ReceivedOrShippedQuantity = 1,
                        Created = DateTime.Now,
                        LastUpdate = DateTime.Now
                    });
                }
                seedDb.SaveChanges();
            }

            // Act: Delete the Product.
            bool deleted;
            using (var db = TestDatabase.NewDentalDbContext())
            {
                var service = new ProductService(new ProductRepository(db));
                deleted = service.Delete(productId);
            }

            int inventoryRowsRemaining;
            using (var readDb = TestDatabase.NewDentalDbContext())
            {
                inventoryRowsRemaining = readDb.Inventories.Count();
            }

            var output = new Fields
            {
                { "outcome", deleted ? "OK" : "REJECTED" },
                { "error_code", null },
                { "threw", false },
                { "product_deleted", deleted },
                { "inventory_rows_remaining_count", inventoryRowsRemaining }
            };

            FixtureWriter.Write("GM-024", new Fields { { "inventory_row_count_before_delete", 3 } }, output);
        }
    }
}
