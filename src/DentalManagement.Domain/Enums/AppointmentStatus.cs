namespace DentalManagement.Domain.Enums;

/// <summary>
/// Status of a scheduled <see cref="Entities.Appointment"/>. Legacy ids
/// preserved (CQ-006, design D-3).
/// </summary>
public enum AppointmentStatus
{
    Appointed = 7,
    Visited = 8,
}
