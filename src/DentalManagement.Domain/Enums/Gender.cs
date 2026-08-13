namespace DentalManagement.Domain.Enums;

/// <summary>
/// Patient gender. The legacy column was a plain <c>string</c> even though an
/// unused <c>Gender</c> enum sat in the same file; CQ-007 formalizes it here and
/// requires the migration to report any legacy value outside this set rather
/// than discarding it silently (spec FR-06).
/// </summary>
public enum Gender
{
    Male = 1,
    Female = 2,
    Others = 3,
}
