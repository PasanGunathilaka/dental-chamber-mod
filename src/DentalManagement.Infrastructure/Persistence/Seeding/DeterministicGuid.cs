using System.Security.Cryptography;
using System.Text;

namespace DentalManagement.Infrastructure.Persistence.Seeding;

/// <summary>
/// Derives a stable <see cref="Guid"/> from a string key.
/// </summary>
/// <remarks>
/// Seed rows need ids that are the same on every run so re-seeding is a genuine
/// no-op rather than a duplicate insert, and so a test can assert a seeded row is
/// retrievable by a known id (spec FR-17, FR-18, AC-15, AC-16). Deriving them from
/// the natural key beats maintaining a hand-written table of literal GUIDs.
/// </remarks>
internal static class DeterministicGuid
{
    public static Guid From(string key)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return new Guid(hash.AsSpan(0, 16));
    }

    public static string StringFrom(string key) => From(key).ToString();
}
