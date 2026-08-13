using DentalManagement.Domain.Enums;

namespace DentalManagement.Domain.Entities;

/// <summary>
/// A clinic patient record. <see cref="Code"/> is the human-facing identifier
/// used in SPA routes and on printed receipts.
/// </summary>
public class Patient : BaseEntity
{
    /// <summary>
    /// Server-generated patient code (<c>"P" + zero-padded sequence</c>), unique
    /// and never client-supplied — DR-001. Length 7–8 characters.
    /// </summary>
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int Age { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    /// <summary>
    /// Typed per CQ-007, replacing the legacy free-form string.
    /// </summary>
    public Gender? Gender { get; set; }

    public string? Note { get; set; }

    public ICollection<Prescription> Prescriptions { get; set; } = [];
}
