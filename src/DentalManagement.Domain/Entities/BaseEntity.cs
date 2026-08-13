namespace DentalManagement.Domain.Entities;

/// <summary>
/// Shared identity and audit fields for every domain entity, mirroring the
/// legacy <c>BaseModel</c>.
/// </summary>
/// <remarks>
/// The legacy <c>BaseModel.Id</c> was marked
/// <c>[DatabaseGenerated(DatabaseGeneratedOption.Identity)]</c>, which is what
/// made the seeded-Doctor defect possible: EF discarded the GUID the seeder
/// assigned, so the client's hardcoded doctor id never matched the row that
/// actually existed and appointment booking failed its FK constraint. Here the
/// id is assigned in application code and persisted as assigned
/// (spec FR-12, FR-18, design D-4).
/// </remarks>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime Created { get; set; }

    public DateTime LastUpdate { get; set; }
}
