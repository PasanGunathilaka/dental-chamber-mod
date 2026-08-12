using DentalManagement.Domain.Enums;

namespace DentalManagement.Domain.Entities;

/// <summary>
/// A scheduled visit slot, deliberately decoupled from <see cref="Patient"/>.
/// </summary>
public class Appointment : BaseEntity
{
    public string? Code { get; set; }

    /// <summary>
    /// Free text, required, 2–40 characters — <b>not</b> a foreign key to
    /// <see cref="Patient"/>.
    /// </summary>
    /// <remarks>
    /// domain-model.md confirms the absence rather than assuming it:
    /// "there is no code path anywhere ... that joins an Appointment to a Patient
    /// row. Appointments and patient records are entirely independent data." The
    /// field exists so staff can book a slot for someone not yet registered.
    /// Normalizing it into a real FK would change booking behaviour with no
    /// decided CQ to sanction it (spec FR-05).
    /// </remarks>
    public string PatientNameOrId { get; set; } = null!;

    public int Age { get; set; }

    public string? Phone { get; set; }

    public DateTime Date { get; set; }

    /// <summary>
    /// Time of day for the slot.
    /// </summary>
    /// <remarks>
    /// Typed <see cref="DateTime"/> because the legacy capture widget is
    /// <c>uib-timepicker</c> (ui-inventory.md), which binds a full JavaScript
    /// Date — the value the legacy model could bind from. No captured fixture
    /// pins this field's type, and domain-model.md lists it without one, so this
    /// is a grounded inference rather than a confirmed fact; flagged here so it
    /// stays traceable.
    /// </remarks>
    public DateTime Time { get; set; }

    public Guid DoctorId { get; set; }

    public Doctor Doctor { get; set; } = null!;

    public AppointmentStatus Status { get; set; }
}
