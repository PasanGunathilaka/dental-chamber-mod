namespace DentalManagement.Domain.Entities;

/// <summary>
/// Master list of medical conditions / allergies (e.g. "Diabetic") that can be
/// tagged onto a patient's record.
/// </summary>
public class MedicalInfo : BaseEntity
{
    /// <summary>
    /// Required, unique, 2–50 characters — DR-017.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Not persisted — a UI checkbox state the legacy model carried on the entity.
    /// </summary>
    public bool IsChecked { get; set; }
}
