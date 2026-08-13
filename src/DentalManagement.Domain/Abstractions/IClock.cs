namespace DentalManagement.Domain.Abstractions;

/// <summary>
/// Supplies the current wall-clock time for <c>Created</c>/<c>LastUpdate</c> writes.
/// </summary>
/// <remarks>
/// The legacy app called <c>DateTime.Now</c> directly at every write site, which
/// <c>.specclaw/baseline/seams.md</c> records as a capture-fidelity problem:
/// "every one of these unguarded writes implies the rebuild needs an injectable
/// clock". Routing timestamps through this abstraction is what lets tests and
/// golden-master replay pin time (spec FR-13).
///
/// Values are local wall-clock, not UTC, and are persisted to
/// <c>timestamp without time zone</c> — nothing in the legacy artifacts records
/// the clinic's timezone, so converting would be a guess (spec A8, design D-8).
/// </remarks>
public interface IClock
{
    DateTime Now { get; }
}
