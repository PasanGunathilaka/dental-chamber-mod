using DentalManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalManagement.Infrastructure.Persistence.Configurations;

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.ToTable("Doctor");

        builder.HasKey(doctor => doctor.Id);

        // The seeded doctor's id must persist exactly as assigned. Legacy marked
        // this column DatabaseGenerated(Identity), so EF discarded the seed's
        // GUID and the client's hardcoded doctor id matched nothing — every
        // appointment booking then failed FK_dbo.Appointment_dbo.Doctor_DoctorId
        // on a freshly migrated database (spec FR-18, AC-16).
        builder.Property(doctor => doctor.Id).ValueGeneratedNever();

        builder.Property(doctor => doctor.Code).HasMaxLength(20);
        builder.Property(doctor => doctor.Name).HasMaxLength(60);
        builder.Property(doctor => doctor.Phone).HasMaxLength(30);
    }
}
