# Error Map: Dental Management System (DentalManagement.sln)

**Date created:** 2026-08-12
**Grounded in:** the legacy application's own source — every entry cites the
line that raises the condition it names.

<!--
  THIS FILE IS PER-PROJECT DATA. It lives in the target repo at
  .specclaw/baseline/error-map.md and belongs to this project alone. See
  $CLAUDE_PLUGIN_ROOT/templates/error-map.md for the format contract this
  file follows; that skeleton is not duplicated here.
-->

## Codes

### DUPLICATE_NAME

- **Condition:** An `Add` was rejected because another row already exists
  with the same `Name` (a unique-index violation), for either
  `MedicalService.Name` or `MedicalInfo.Name`.
- **Legacy source:** `DM.Models/MedicalService.cs:20` (`[Index("IX_Name", IsUnique = true)]`),
  `DM.Models/MedicalInfo.cs:12` (same attribute) — enforced by the SQL Server
  unique index EF6 creates from it, surfaced (or swallowed) through
  `DM.Service/BaseService.cs:40-55`'s blanket `catch (Exception)`.
- **Rebuild source:** not yet mapped
- **Raised as (legacy):** at the persistence layer (GM-014), a `DbUpdateException`
  wrapping a `SqlException` (unique-constraint violation). At the service
  layer (GM-013, GM-015), swallowed to a plain `false` / `threw: false` by
  `BaseService<T>.Add`'s catch-all, which cannot itself distinguish this
  specific cause from any other database failure — see seams.md's Capture
  Blocker #5. This code is assigned here because *this harness's own Arrange
  step* guarantees the only reachable failure in GM-013/GM-014/GM-015's
  controlled test database is the duplicate-name collision it deliberately
  creates, not because `BaseService<T>.Add` itself can tell the two apart at
  runtime.
- **Pinned by:** GM-013, GM-014, GM-015

### CANNOT_DELETE_OWN_ACCOUNT

- **Condition:** `DeleteUser` was rejected because the caller's own
  authenticated user id matches the id being deleted.
- **Legacy source:** `DM.Server/Controllers/UserController.cs:62-63`
- **Rebuild source:** not yet mapped
- **Raised as (legacy):** a plain `return BadRequest();` (HTTP 400, no body) —
  a designed rejection, never an exception.
- **Pinned by:** GM-026

### PASSWORD_RETYPE_MISMATCH

- **Condition:** A submitted password does not match its retype
  confirmation, on either user creation or a self-service password change.
- **Legacy source:** `DM.Server/Service/UserService.cs:73` (`CreateUser`),
  `DM.Server/Service/ProfileService.cs:68` (`UpdatePassword`)
- **Rebuild source:** not yet mapped
- **Raised as (legacy):** on `ProfileService.UpdatePassword`, a plain
  `return false;` (never throws). On `UserService.CreateUser`, the mismatch
  guard forwards `null` into `_repository.CreateUser(null)` →
  `UserManager.CreateAsync`, which ASP.NET Identity 2.x's own null-guard
  convention is *expected* (per PQ-009, not confirmed by executing the
  legacy app) to reject with an exception rather than a graceful
  `IdentityResult.Failed(...)`. The business condition is identical either
  way — a password/retype mismatch — which is exactly what this single code
  captures; the representation split (clean rejection vs. crash) is exactly
  what `threw` and the four (b.2) fields are for, and exactly what PQ-009
  leaves open for a human to confirm from the actual capture.
- **Pinned by:** GM-028 (⚠ representation only — see PQ-009), GM-031

### INVALID_CURRENT_PASSWORD

- **Condition:** A self-service password change was rejected because the
  supplied `CurrentPassword` does not verify against the user's stored hash.
- **Legacy source:** `DM.Server/Service/ProfileService.cs:72-74`
  (`VerifyHashedPassword(...) != PasswordVerificationResult.Success`)
- **Rebuild source:** not yet mapped
- **Raised as (legacy):** a plain `return false;` (`success` stays `false`) —
  never throws.
- **Pinned by:** GM-030

### EMPTY_MEDICAL_INFO_LIST

- **Condition:** `SavePatientMedicalInfos` was submitted an empty list, so no
  `PatientId` can be inferred from `.First()`.
- **Legacy source:** `DM.Service/MedicalInfoService.cs:42`
  (`var patientId = patientMedicalInfos.First().PatientId;`)
- **Rebuild source:** not yet mapped
- **Raised as (legacy):** an unhandled `InvalidOperationException`
  ("Sequence contains no elements") — a genuine crash, not a designed
  rejection; see PQ-006 for whether this should be fixed in the rebuild.
- **Pinned by:** GM-018 (⚠ PROVISIONAL — see PQ-006, already marked in scenarios.md)

### NON_INTEGER_CHARGE

- **Condition:** `MedicalService.TotalCharge` could not convert its
  `Charge` string to a whole number of currency units.
- **Legacy source:** `DM.Models/MedicalService.cs:36`
  (`Convert.ToInt32(Charge) * Quantity`)
- **Rebuild source:** not yet mapped
- **Raised as (legacy):** expected to be a `FormatException` for a
  non-numeric or fractional `Charge` string, per .NET's own documented
  `Convert.ToInt32(string)` contract — not confirmed by executing the
  legacy app; GM-017's own note in scenarios.md and CQ-008's decision to fix
  this in the rebuild both apply here. This code is assigned so that *if*
  the capture shows a rejection, the fixture records a stable business
  code rather than `null` — the open question is only ever whether the
  rejection happens at all, never what it would mean if it does.
- **Pinned by:** GM-017

## Unmapped Conditions

None — every observed error condition above is mapped. No new pending
question was needed for any of the 41 scenarios in this harness generation
run: every REJECTED/threw outcome this harness can produce has a confident,
citable business-condition code above, even where the *representation*
(clean rejection vs. framework exception) is still open per PQ-006/PQ-009
and is recorded as such.
