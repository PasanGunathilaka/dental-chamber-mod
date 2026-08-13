using DentalManagement.Domain.Enums;

namespace DentalManagement.Domain.Abstractions;

/// <summary>
/// Performs the whole patient-registration decision — generate the patient
/// code, insert the <c>Patient</c>, generate the bill code, insert the
/// auto-provisioned <c>Prescription</c> — as a single unit (spec FR-01).
/// </summary>
/// <remarks>
/// This is the seam <c>GM-003</c> was captured at (design D-1). BCL-only, per
/// NFR-03/learning L1 — the implementation (with its transaction and database
/// access) lives in <c>DentalManagement.Infrastructure</c>.
/// </remarks>
public interface IPatientRegistrationService
{
    /// <summary>
    /// Registers a new patient and opens their first bill. Never throws for an
    /// ordinary write failure — see <see cref="RegistrationResult"/>.
    /// </summary>
    Task<RegistrationResult> RegisterAsync(NewPatient patient, CancellationToken cancellationToken = default);
}

/// <summary>
/// The data needed to register a new patient. Carries no <c>Code</c> or
/// <c>Id</c> — both are server-generated (DR-001, spec FR-09).
/// </summary>
public sealed record NewPatient(
    string Name,
    int Age,
    Gender? Gender,
    string? Phone,
    string? Email,
    string? Address,
    string? Note);

/// <summary>
/// The outcome of <see cref="IPatientRegistrationService.RegisterAsync"/>,
/// distinguishing success from failure explicitly.
/// </summary>
/// <remarks>
/// This is what closes GM-002's defect (DR-001): legacy's controller returned
/// 200 regardless of whether <c>Add()</c> actually persisted a row. A caller
/// here cannot mistake a failed write for a successful one — there is no
/// ambient state to misread, only this result (spec FR-03, A2).
/// </remarks>
public sealed class RegistrationResult
{
    private RegistrationResult(
        bool isSuccess,
        Guid patientId,
        string? patientCode,
        Guid prescriptionId,
        string? billCode,
        string? failureReason)
    {
        IsSuccess = isSuccess;
        PatientId = patientId;
        PatientCode = patientCode;
        PrescriptionId = prescriptionId;
        BillCode = billCode;
        FailureReason = failureReason;
    }

    /// <summary>
    /// <c>true</c> only when both the <c>Patient</c> and its <c>Prescription</c>
    /// were committed. Never <c>true</c> for a write that did not persist.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>The new patient's id. Only meaningful when <see cref="IsSuccess"/>.</summary>
    public Guid PatientId { get; }

    /// <summary>The new patient's server-generated code. Only meaningful when <see cref="IsSuccess"/>.</summary>
    public string? PatientCode { get; }

    /// <summary>The auto-provisioned bill's id. Only meaningful when <see cref="IsSuccess"/>.</summary>
    public Guid PrescriptionId { get; }

    /// <summary>The auto-provisioned bill's server-generated code. Only meaningful when <see cref="IsSuccess"/>.</summary>
    public string? BillCode { get; }

    /// <summary>Why the registration failed. Only meaningful when <see cref="IsSuccess"/> is <c>false</c>.</summary>
    public string? FailureReason { get; }

    public static RegistrationResult Success(
        Guid patientId,
        string patientCode,
        Guid prescriptionId,
        string billCode) =>
        new(true, patientId, patientCode, prescriptionId, billCode, null);

    public static RegistrationResult Failure(string reason) =>
        new(false, default, null, default, null, reason);
}
