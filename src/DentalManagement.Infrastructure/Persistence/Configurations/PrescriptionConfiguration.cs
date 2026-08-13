using DentalManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalManagement.Infrastructure.Persistence.Configurations;

public class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> builder)
    {
        builder.ToTable(
            "Prescription",
            table => table.HasCheckConstraint(
                "CK_Prescription_Status",
                @"""Status"" IN (5, 6)"));

        builder.HasKey(prescription => prescription.Id);
        builder.Property(prescription => prescription.Id).ValueGeneratedNever();

        // DR-003: "BILL" + zero-padded sequence + "-" + PatientCode.
        builder.Property(prescription => prescription.Code).IsRequired().HasMaxLength(18);
        builder.HasIndex(prescription => prescription.Code)
            .IsUnique()
            .HasDatabaseName("IX_Prescription_Code");

        foreach (var money in new[]
                 {
                     nameof(Prescription.TotalCharge),
                     nameof(Prescription.DiscountPercent),
                     nameof(Prescription.DiscountAmount),
                     nameof(Prescription.FixedDiscount),
                     nameof(Prescription.TotalPayable),
                     nameof(Prescription.TotalPaid),
                     nameof(Prescription.TotalDue),
                 })
        {
            builder.Property(money).HasColumnType(DentalDbContext.MoneyColumnType);
        }

        // Computed from DiscountAmount + FixedDiscount, never stored. GM-041 pins
        // the unguarded result including the negative case.
        builder.Ignore(prescription => prescription.TotalDiscountAmount);

        // GM-012: deleting a patient removes every bill, and through the bill
        // every line item and payment.
        builder.HasOne(prescription => prescription.Patient)
            .WithMany(patient => patient.Prescriptions)
            .HasForeignKey(prescription => prescription.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(prescription => prescription.PatientId);
    }
}
