using DentalManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalManagement.Infrastructure.Persistence.Configurations;

public class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        builder.ToTable(
            "Inventory",
            table => table.HasCheckConstraint(
                "CK_Inventory_Status",
                @"""Status"" IN (3, 4)"));

        builder.HasKey(movement => movement.Id);
        builder.Property(movement => movement.Id).ValueGeneratedNever();

        builder.Property(movement => movement.CashMemoNo).IsRequired().HasMaxLength(50);

        // GM-024: deleting a product removes all of its movement rows.
        builder.HasOne(movement => movement.Product)
            .WithMany(product => product.Inventories)
            .HasForeignKey(movement => movement.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(movement => movement.ProductId);
        builder.HasIndex(movement => movement.Created);
    }
}
