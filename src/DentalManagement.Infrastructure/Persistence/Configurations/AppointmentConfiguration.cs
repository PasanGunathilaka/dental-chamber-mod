using DentalManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalManagement.Infrastructure.Persistence.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable(
            "Appointment",
            table => table.HasCheckConstraint(
                "CK_Appointment_Status",
                @"""Status"" IN (7, 8)"));

        builder.HasKey(appointment => appointment.Id);
        builder.Property(appointment => appointment.Id).ValueGeneratedNever();

        builder.Property(appointment => appointment.Code).HasMaxLength(20);

        // Free text, not a Patient foreign key — see the entity's own remarks.
        builder.Property(appointment => appointment.PatientNameOrId)
            .IsRequired()
            .HasMaxLength(40);

        builder.Property(appointment => appointment.Phone).HasMaxLength(30);

        builder.HasOne(appointment => appointment.Doctor)
            .WithMany(doctor => doctor.Appointments)
            .HasForeignKey(appointment => appointment.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(appointment => appointment.DoctorId);

        // The by-date schedule is the module's hot query (DR-010).
        builder.HasIndex(appointment => appointment.Date);
    }
}
