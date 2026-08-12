namespace DentalManagement.Domain.Entities;

/// <summary>
/// A clinic practitioner assignable to appointments.
/// </summary>
public class Doctor : BaseEntity
{
    public string? Code { get; set; }

    public string? Name { get; set; }

    public string? Phone { get; set; }

    public ICollection<Appointment> Appointments { get; set; } = [];
}
