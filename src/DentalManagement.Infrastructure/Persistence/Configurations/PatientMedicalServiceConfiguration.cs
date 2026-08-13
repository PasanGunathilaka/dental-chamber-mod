using DentalManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalManagement.Infrastructure.Persistence.Configurations;

public class PatientMedicalServiceConfiguration : IEntityTypeConfiguration<PatientMedicalService>
{
    public void Configure(EntityTypeBuilder<PatientMedicalService> builder)
    {
        builder.ToTable("PatientMedicalService");

        builder.HasKey(lineItem => lineItem.Id);
        builder.Property(lineItem => lineItem.Id).ValueGeneratedNever();

        // GM-012 pins that a patient delete clears these rows. Both the direct
        // Patient path and the Prescription path cascade, mirroring the legacy
        // EF6 configuration where every required relationship cascaded by default.
        // PostgreSQL permits multiple cascade paths, so both can stand as legacy
        // declared them.
        builder.HasOne(lineItem => lineItem.Patient)
            .WithMany()
            .HasForeignKey(lineItem => lineItem.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(lineItem => lineItem.Prescription)
            .WithMany(prescription => prescription.PatientMedicalServices)
            .HasForeignKey(lineItem => lineItem.PrescriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(lineItem => lineItem.MedicalService)
            .WithMany()
            .HasForeignKey(lineItem => lineItem.MedicalServiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(lineItem => lineItem.PrescriptionId);
        builder.HasIndex(lineItem => lineItem.PatientId);
        builder.HasIndex(lineItem => lineItem.MedicalServiceId);
    }
}
