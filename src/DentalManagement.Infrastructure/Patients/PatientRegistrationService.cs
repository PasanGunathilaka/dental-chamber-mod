using DentalManagement.Domain.Abstractions;
using DentalManagement.Domain.Entities;
using DentalManagement.Domain.Enums;
using DentalManagement.Domain.Patients;
using DentalManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalManagement.Infrastructure.Patients;

/// <summary>
/// The whole registration decision — generate the patient code, insert the
/// <c>Patient</c>, generate the bill code, insert the auto-provisioned
/// <c>Prescription</c> — inside one explicit database transaction (spec FR-01,
/// design D-1/D-3). This is the seam <c>GM-003</c> replays against.
/// </summary>
/// <remarks>
/// Legacy performed these two writes with nothing wrapping them, so a failed
/// second write left a patient with zero bills (DR-002). Here either both rows
/// commit or neither does — see the explicit rollback on failure below.
/// </remarks>
public sealed class PatientRegistrationService(
    DentalDbContext dbContext,
    PatientCodeSequence patientCodeSequence,
    IClock clock) : IPatientRegistrationService
{
    /// <summary>
    /// BL-020 only ever creates a patient's <b>first</b> bill, so the bill
    /// sequence is always 1 here. A general per-patient bill-numbering rule
    /// (for a second, third, ... bill) is BL-027's close/reopen workflow, not
    /// this item's.
    /// </summary>
    private const long FirstBillSequence = 1;

    public async Task<RegistrationResult> RegisterAsync(
        NewPatient patient,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var now = clock.Now;

            var patientSequence = await patientCodeSequence.NextAsync(cancellationToken);
            var patientCode = PatientCodeFormatter.Format(patientSequence);

            var patientEntity = new Patient
            {
                Code = patientCode,
                Name = patient.Name,
                Age = patient.Age,
                Gender = patient.Gender,
                Phone = patient.Phone,
                Email = patient.Email,
                Address = patient.Address,
                Note = patient.Note,
                Created = now,
                LastUpdate = now,
            };

            dbContext.Patients.Add(patientEntity);
            await dbContext.SaveChangesAsync(cancellationToken);

            var billCode = BillCodeFormatter.Format(patientCode, FirstBillSequence);

            var prescriptionEntity = new Prescription
            {
                Code = billCode,
                PatientId = patientEntity.Id,
                Status = BillStatus.Active,
                TotalCharge = 0,
                DiscountPercent = 0,
                DiscountAmount = 0,
                FixedDiscount = 0,
                TotalPayable = 0,
                TotalPaid = 0,
                TotalDue = 0,
                Created = now,
                LastUpdate = now,
            };

            dbContext.Prescriptions.Add(prescriptionEntity);
            await dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return RegistrationResult.Success(patientEntity.Id, patientCode, prescriptionEntity.Id, billCode);
        }
        catch (DbUpdateException ex)
        {
            // Neither write persists — DR-002's gap closed. A unique-index
            // violation on either insert lands here, and the transaction's
            // rollback undoes both, not just the one that failed.
            await transaction.RollbackAsync(cancellationToken);
            return RegistrationResult.Failure(ex.Message);
        }
    }
}
