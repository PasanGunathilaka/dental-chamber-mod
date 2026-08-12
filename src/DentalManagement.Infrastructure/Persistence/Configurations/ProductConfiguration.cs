using DentalManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalManagement.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable(
            "Product",
            table => table.HasCheckConstraint(
                "CK_Product_Status",
                @"""Status"" IN (1, 2)"));

        builder.HasKey(product => product.Id);
        builder.Property(product => product.Id).ValueGeneratedNever();

        builder.Property(product => product.Code).HasMaxLength(40);

        builder.Property(product => product.Name).IsRequired().HasMaxLength(40);
        builder.HasIndex(product => product.Name).IsUnique().HasDatabaseName("IX_Product_Name");

        builder.Property(product => product.UnitPrice)
            .HasColumnType(DentalDbContext.MoneyColumnType);
        builder.Property(product => product.SalePrice)
            .HasColumnType(DentalDbContext.MoneyColumnType);
    }
}
