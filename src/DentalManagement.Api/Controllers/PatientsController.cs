using DentalManagement.Api.Authorization;
using DentalManagement.Api.Contracts;
using DentalManagement.Domain.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalManagement.Api.Controllers;

/// <summary>
/// Registers new patients and their auto-provisioned first bill (spec FR-01..FR-13).
/// </summary>
/// <remarks>
/// A thin translation layer over <see cref="IPatientRegistrationService"/> — the
/// registration decision itself lives there so it stays replayable at the
/// service seam GM-003 was captured at (design D-1).
/// </remarks>
[ApiController]
[Route("api/patients")]
[Authorize]
[Permission("root.patient-create")]
public sealed class PatientsController(IPatientRegistrationService registrationService) : ControllerBase
{
    /// <summary>
    /// Registers a new patient. Returns 201 with the created patient's <c>Id</c>,
    /// <c>Code</c>, and the auto-provisioned bill's <c>Code</c> on success. Never
    /// returns 2xx for a write that did not persist (spec FR-03) — a failed
    /// registration is reported as a <see cref="ProblemDetails"/> instead.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<RegisterPatientResponse>> Register(
        RegisterPatientRequest request,
        CancellationToken cancellationToken)
    {
        var result = await registrationService.RegisterAsync(
            new NewPatient(
                request.Name,
                request.Age!.Value,
                request.Gender,
                request.Phone,
                request.Email,
                request.Address,
                request.Note),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return Problem(
                detail: result.FailureReason,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Patient registration failed");
        }

        return StatusCode(
            StatusCodes.Status201Created,
            new RegisterPatientResponse
            {
                Id = result.PatientId,
                Code = result.PatientCode!,
                BillCode = result.BillCode!,
            });
    }
}
