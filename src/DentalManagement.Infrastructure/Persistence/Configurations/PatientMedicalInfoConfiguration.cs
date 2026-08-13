using DentalManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalManagement.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the patient/condition link table with <b>no foreign keys</b>.
/// </summary>
/// <remarks>
/// <para>
/// This is the one entity here that deliberately has no relational integrity, and
/// it is not an omission. GM-019 is a captured persistence-layer fixture asserting
/// that deleting a patient leaves these rows behind
/// (<c>patient_medical_info_rows_remaining_count = 2</c>, still pointing at the
/// deleted patient id) — the legacy model declared plain <c>Guid</c> columns with
/// no <c>[ForeignKey]</c> or navigation, so EF6 never created a database
/// relationship to cascade.
/// </para>
/// <para>
/// Adding a foreign key breaks that fixture whichever behaviour is chosen: cascade
/// deletes the rows, restrict makes the patient delete fail (which also breaks
/// GM-012). SQ-012 requires every intentional divergence be tied to a decided CQ,
/// and none covers this. Raise a CQ first — see spec A5, design R-4.
/// </para>
/// </remarks>
public class PatientMedicalInfoConfiguration : IEntityTypeConfiguration<PatientMedicalInfo>
{
    public void Configure(EntityTypeBuilder<PatientMedicalInfo> builder)
    {
        builder.ToTable("PatientMedicalInfo");

        builder.HasKey(tag => tag.Id);
        builder.Property(tag => tag.Id).ValueGeneratedNever();

        // Indexed for lookup, but carrying no FK constraint: querying by patient
        // is a read concern, orphan-ability is the pinned behaviour.
        builder.HasIndex(tag => tag.PatientId);
        builder.HasIndex(tag => tag.MedicalInfoId);
    }
}
