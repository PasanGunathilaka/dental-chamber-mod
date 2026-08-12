namespace DentalManagement.Domain.Entities;

/// <summary>
/// Links a medical condition to a patient.
/// </summary>
/// <remarks>
/// <para>
/// <b>Deliberately holds plain <see cref="Guid"/> keys with no navigation
/// properties and no database-level foreign key</b>, unlike every other join
/// entity here. This is not an oversight being carried forward casually — it is
/// load-bearing behaviour.
/// </para>
/// <para>
/// GM-019 is a captured persistence-layer fixture asserting that deleting a
/// patient leaves this entity's rows in place
/// (<c>patient_medical_info_rows_remaining_count = 2</c>), still referencing the
/// deleted patient id. Every foreign-key option changes that: a cascading FK
/// deletes the rows, and a restricting FK makes the patient delete fail outright
/// — which would break GM-012 as well. Either way the divergence has no
/// sanctioning CQ, and SQ-012 requires that "every intentional divergence from
/// legacy behaviour must be tied to a decided CQ".
/// </para>
/// <para>
/// So: raise and decide a CQ before adding the FK. See spec assumption A5 and
/// design risk R-4.
/// </para>
/// </remarks>
public class PatientMedicalInfo : BaseEntity
{
    public Guid PatientId { get; set; }

    public Guid MedicalInfoId { get; set; }
}
