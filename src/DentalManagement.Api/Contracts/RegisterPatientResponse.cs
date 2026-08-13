namespace DentalManagement.Api.Contracts;

/// <summary>
/// The response body for a successful <c>POST /api/patients</c> (spec FR-12).
/// </summary>
public sealed class RegisterPatientResponse
{
    public required Guid Id { get; init; }

    public required string Code { get; init; }

    public required string BillCode { get; init; }
}
