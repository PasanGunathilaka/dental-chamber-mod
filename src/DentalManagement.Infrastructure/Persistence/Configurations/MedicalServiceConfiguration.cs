using DentalManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalManagement.Infrastructure.Persistence.Configurations;

public class MedicalServiceConfiguration : IEntityTypeConfiguration<MedicalService>
{
    public void Configure(EntityTypeBuilder<MedicalService> builder)
    {
        builder.ToTable("MedicalService");

        builder.HasKey(service => service.Id);
        builder.Property(service => service.Id).ValueGeneratedNever();

        builder.HasIndex(service => service.Code)
            .IsUnique()
            .HasDatabaseName("IX_MedicalService_Code");

        // DR-017: catalog names must be unique.
        builder.Property(service => service.Name).IsRequired().HasMaxLength(50);
        builder.HasIndex(service => service.Name)
            .IsUnique()
            .HasDatabaseName("IX_MedicalService_Name");

        // CQ-008's fix, at the column: legacy stored this as a string and
        // truncated it through Convert.ToInt32.
        builder.Property(service => service.Charge)
            .HasColumnType(DentalDbContext.MoneyColumnType);

        // Both were [NotMapped] in legacy: a request-shaped quantity and the
        // computed total.
        builder.Ignore(service => service.Quantity);
        builder.Ignore(service => service.TotalCharge);
    }
}
