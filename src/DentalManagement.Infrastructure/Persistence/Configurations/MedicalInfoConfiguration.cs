using DentalManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalManagement.Infrastructure.Persistence.Configurations;

public class MedicalInfoConfiguration : IEntityTypeConfiguration<MedicalInfo>
{
    public void Configure(EntityTypeBuilder<MedicalInfo> builder)
    {
        builder.ToTable("MedicalInfo");

        builder.HasKey(info => info.Id);
        builder.Property(info => info.Id).ValueGeneratedNever();

        // DR-017: catalog names must be unique.
        builder.Property(info => info.Name).IsRequired().HasMaxLength(50);
        builder.HasIndex(info => info.Name).IsUnique().HasDatabaseName("IX_MedicalInfo_Name");

        builder.Ignore(info => info.IsChecked);
    }
}
