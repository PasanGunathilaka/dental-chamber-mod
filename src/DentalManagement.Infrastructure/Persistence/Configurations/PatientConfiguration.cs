using DentalManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalManagement.Infrastructure.Persistence.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable(
            "Patient",
            table => table.HasCheckConstraint(
                "CK_Patient_Gender",
                @"""Gender"" IS NULL OR ""Gender"" IN (1, 2, 3)"));

        builder.HasKey(patient => patient.Id);
        builder.Property(patient => patient.Id).ValueGeneratedNever();

        // DR-001: server-generated "P" + zero-padded sequence, unique.
        builder.Property(patient => patient.Code).IsRequired().HasMaxLength(8);
        builder.HasIndex(patient => patient.Code).IsUnique().HasDatabaseName("IX_Patient_Code");

        builder.Property(patient => patient.Name).IsRequired().HasMaxLength(30);
        builder.Property(patient => patient.Phone).HasMaxLength(30);
        builder.Property(patient => patient.Email).HasMaxLength(100);
        builder.Property(patient => patient.Address).HasMaxLength(200);
        builder.Property(patient => patient.Note).HasMaxLength(500);

        // Only the maximum lengths reach the database. Legacy expressed its
        // minimums ([StringLength(8, MinimumLength = 7)], Name 3-30) as model
        // validation, never as a database constraint; reproducing them as check
        // constraints would reject legacy rows the migration must carry.
    }
}
