namespace DentalManagement.Domain.Entities;

/// <summary>
/// One line item: a dental service billed to a patient within one bill.
/// </summary>
public class PatientMedicalService : BaseEntity
{
    public Guid PatientId { get; set; }

    public Patient Patient { get; set; } = null!;

    public Guid PrescriptionId { get; set; }

    public Prescription Prescription { get; set; } = null!;

    public Guid MedicalServiceId { get; set; }

    public MedicalService MedicalService { get; set; } = null!;

    public int Quantity { get; set; } = 1;
}
