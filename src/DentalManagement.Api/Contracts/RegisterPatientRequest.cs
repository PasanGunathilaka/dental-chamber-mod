using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using DentalManagement.Domain.Enums;

namespace DentalManagement.Api.Contracts;

/// <summary>
/// The client-supplied fields for <c>POST /api/patients</c> (spec FR-09).
/// </summary>
/// <remarks>
/// <c>Code</c> and <c>Id</c> are absent from this type entirely — DR-001's
/// "never client-supplied" is enforced by the contract's shape, not by a check
/// someone could delete. Length attributes mirror the maximums configured in
/// <c>PatientConfiguration.cs</c> only; legacy's minimum lengths are
/// deliberately not reintroduced (spec FR-11) — a 3-character <see cref="Name"/>
/// is valid.
/// </remarks>
public sealed class RegisterPatientRequest
{
    [Required]
    [MaxLength(30)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Nullable so a missing value fails <see cref="RequiredAttribute"/> rather
    /// than silently binding to <c>0</c> (edge case: "a missing or non-numeric
    /// Age is a 400").
    /// </summary>
    [Required]
    public int? Age { get; set; }

    /// <summary>
    /// One of <see cref="Gender"/>'s named values, or omitted — an omitted
    /// Gender is valid (<c>Patient.Gender</c> is nullable). Any other string
    /// value fails JSON deserialization with a field-scoped error, so it never
    /// reaches <c>CK_Patient_Gender</c> (spec FR-10).
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Gender? Gender { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(200)]
    public string? Address { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}
