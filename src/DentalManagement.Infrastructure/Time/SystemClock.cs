using DentalManagement.Domain.Abstractions;

namespace DentalManagement.Infrastructure.Time;

/// <inheritdoc />
public sealed class SystemClock : IClock
{
    /// <summary>
    /// Local wall-clock time with <see cref="DateTimeKind.Unspecified"/>.
    /// </summary>
    /// <remarks>
    /// The kind is stripped deliberately. Timestamps persist to
    /// <c>timestamp without time zone</c> (spec A8, design D-8) because nothing in
    /// the legacy artifacts records the clinic's timezone, so interpreting the
    /// stored values as UTC or as any particular offset would be a guess. An
    /// unspecified kind is also what Npgsql maps to that column type without
    /// shifting the value.
    /// </remarks>
    public DateTime Now => DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
}
