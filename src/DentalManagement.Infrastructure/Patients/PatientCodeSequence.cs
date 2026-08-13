using DentalManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalManagement.Infrastructure.Patients;

/// <summary>
/// The collision-safe source for patient codes — DR-001, spec FR-08, A4.
/// </summary>
/// <remarks>
/// Legacy generated the next code from
/// <c>GetPatientViewModel().Count() + 1</c> — two concurrent registrations can
/// read the same count before either inserts, producing the same code for both.
/// That is the exact mechanism behind <c>GM-002</c>'s duplicate-code fixture.
/// <c>nextval</c> against <see cref="DentalDbContext"/>'s
/// <c>patient_code_seq</c> sequence is non-blocking and transactional-safe:
/// two concurrent callers are guaranteed two different values without either
/// waiting on the other, which is what AC-06 asserts. This deliberately does
/// not fall back to "read the max code and add one" — that reintroduces the
/// same race under concurrency.
/// </remarks>
public sealed class PatientCodeSequence(DentalDbContext dbContext)
{
    /// <summary>
    /// Advances <c>patient_code_seq</c> and returns the new value.
    /// </summary>
    public async Task<long> NextAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Database
            .SqlQuery<long>($"SELECT nextval('patient_code_seq')")
            .SingleAsync(cancellationToken);
    }
}
