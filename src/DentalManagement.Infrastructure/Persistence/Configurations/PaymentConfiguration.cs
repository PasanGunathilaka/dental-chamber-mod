using DentalManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalManagement.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payment");

        builder.HasKey(payment => payment.Id);
        builder.Property(payment => payment.Id).ValueGeneratedNever();

        builder.Property(payment => payment.Amount)
            .HasColumnType(DentalDbContext.MoneyColumnType);

        builder.Property(payment => payment.Comment).HasMaxLength(500);

        // GM-012: payments go with the bill, which goes with the patient.
        builder.HasOne(payment => payment.Prescription)
            .WithMany(prescription => prescription.Payments)
            .HasForeignKey(payment => payment.PrescriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(payment => payment.PrescriptionId);
    }
}
