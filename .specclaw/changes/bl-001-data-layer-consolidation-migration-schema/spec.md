# Spec: BL-001 — Data Layer Consolidation, Migration & Schema Setup

**Change:** bl-001-data-layer-consolidation-migration-schema
**Created:** 2026-08-12
**Status:** 🟡 Draft

## Overview

Build the PostgreSQL data foundation for the dental-management rebuild: one ASP.NET Core solution skeleton, one EF Core `DbContext` covering all sixteen legacy entities plus the four identity/permission entities, one from-scratch migration history, fresh-install seed data, and the one-time tooling that migrates the legacy SQL Server database into it with reconciliation and a value-audit report.

This is BL-001 in `.specclaw/analysis/rebuild-backlog.md` (module MOD-005, dependency rank 0, Gate CLEAR). Nothing else in the backlog can start before it: *"BL-001 (data layer/migration foundation) precedes everything else in the entire backlog — no other item's schema can exist before this one runs."* The repository currently holds zero source files — all 198 tracked files are `.specclaw/` artifacts — so this item is greenfield in the literal sense.

### Assumptions (Rule 1 — stated, not silently chosen)

The proposal raised seven open questions. `/specclaw:plan` was invoked without answers, so each is recorded here as a named assumption. **A5 reverses the proposal's own recommendation on new evidence** — see the note under it.

| # | Assumption | Basis | Cost if wrong |
|---|---|---|---|
| **A1** | BL-001 owns the **backend** solution skeleton; frontend scaffolding belongs to BL-002 | No backlog item covers scaffolding; BL-001 is rank 0 | Low — scaffolding moves, schema unaffected |
| **A2** | Layout is `src/DentalManagement.{Domain,Infrastructure,Api,DataMigration}` + `tests/` | No ADR records a convention | Medium — rename touches every later item |
| **A3** | BL-001 is accepted against **GM-012, GM-019, GM-024, GM-039, GM-040, GM-041** (its real replayable surface), not the four fixtures bash joined to it | See "Verification surface" below | Medium — wrong fixtures scoped into `/specclaw:verify` |
| **A4** | No legacy database export is available yet; tooling is validated against a synthetic legacy database, with the real reconciliation run recorded as a blocking pre-cutover gate | BL-001 names the export as a human-supplied input; none is present in the repo | Low — if an export appears, swap the source and re-run |
| **A5** | `PatientMedicalInfo` keeps **no** database-level FK constraint, preserving the legacy orphaning behaviour | GM-019 (captured, persistence seam) asserts `patient_medical_info_rows_remaining_count = 2` after the patient is deleted | High — see note |
| **A6** | `Guid` PKs are generated in application code, not by the database | Removes the class of defect behind the legacy `Doctor` seed bug | Medium — affects every entity |
| **A7** | No physical `Status` table; the eight seeded values become four typed enums, mapped during migration | CQ-006; module map leaves `Status` unassigned | Medium — would reshape four entities |
| **A8** | `Created`/`LastUpdate` map to `timestamp without time zone`, preserving legacy values verbatim; new writes go through an injectable clock | Nothing in any artifact records the clinic's timezone, so converting legacy local times to UTC would be a guess | Medium — a later UTC conversion is a data migration of its own |

> **Note on A5 — this reverses the proposal.** The proposal recommended giving `PatientMedicalInfo` proper FK/navigation as "an implementation accident with no behavioural consequence." That is wrong: GM-019 is a **captured persistence-layer fixture** that pins the orphaning behaviour, and every FK option changes it — a cascading FK deletes the rows (asserted count becomes `0`, not `2`), and a restricting FK makes the patient delete fail outright, which would also break GM-012. Either way the divergence has **no sanctioning CQ**, which SQ-012 forbids: *"Every intentional divergence from legacy behaviour must be tied to a decided CQ."* So the legacy shape stands until someone raises and decides a CQ for it. Navigation properties may still be declared for query convenience **provided no FK constraint reaches the database** and GM-019 still replays MATCH.

### Verification surface (why A3)

BL-001's `**Verification:**` line cites GM-001, GM-002, GM-022, GM-023. That join is mechanical — BL-001's acceptance basis quotes DR-001's unique-index text, so bash matched every DR-001/DR-020 fixture. None of the four can replay when this item completes: GM-001 is a pure function in `HelperRequestModel`, GM-002 a service seam on `PatientCreateController.Post`, GM-022/GM-023 service seams on `InventoryReportController.GetReport` — all need application code delivered by BL-016/BL-020/BL-021.

Six other captured fixtures *are* replayable against a schema, a seeder, and a persistence path alone:

| Fixture | Seam layer | What it pins | Expected verdict |
|---|---|---|---|
| GM-012 | persistence | `Patient` delete cascades to `Prescription` → `PatientMedicalService`/`Payment` (all remaining counts `0`) | MATCH |
| GM-019 | persistence | `Patient` delete leaves 2 `PatientMedicalInfo` rows orphaned | MATCH (per A5) |
| GM-024 | persistence | `Product` delete cascades to all `Inventory` rows | MATCH |
| GM-039 | service | Fresh-install permission seeding grants only SystemAdmin, one row per private `Resource` | MATCH |
| GM-040 | service | Re-running permission seeding creates `0` rows | MATCH |
| GM-041 | pure-function | `TotalDiscountAmount` = `0`, `15.5`, `-5` — unguarded addition, no floor at zero | MATCH |

Two further fixtures are touched by this item's money-type change and are expected to **diverge, sanctioned by CQ-008**: GM-016 (integer `Charge = "10"`) still matches, but GM-017 records `"10.50"` → `REJECTED`/`NON_INTEGER_CHARGE`/`FormatException`, which the rebuild must instead accept as `10.50`. That divergence is exactly what CQ-008 decided. It belongs to BL-010's replay run, not this one, but the schema change that causes it lands here — see design risk R-3.

## Requirements

### Functional Requirements

**Solution & context**

- **FR-01** — A buildable ASP.NET Core solution exists with the projects named in A2, targeting the current .NET LTS, restoring and building from a clean clone with no manual steps.
- **FR-02** — Exactly one EF Core `DbContext` covers both the domain and the identity/permission entities, with a single migration history. Identity and domain tables may be separated by PostgreSQL schema or table naming, but not by a second context or a second migration pipeline. *(CQ-002)*
- **FR-03** — The `DbContext` uses the Npgsql provider; the connection string and all secrets come from environment-based configuration, never from a checked-in file. *(SQ-002, SQ-011)*

**Domain entities**

- **FR-04** — All twelve legacy domain entities exist with the fields, types, required flags, and string-length constraints `domain-model.md` documents: `Patient`, `Prescription`, `PatientMedicalService`, `MedicalService`, `MedicalInfo`, `PatientMedicalInfo`, `Payment`, `Product`, `Inventory`, `Doctor`, `Appointment` — and the `Status` concept as enums rather than a table (FR-08).
- **FR-05** — Every relationship in `domain-model.md`'s ER diagram is configured, and the two documented absences are preserved: `Appointment.PatientNameOrId` stays free text with no FK to `Patient`, and no FK links the identity schema to the domain schema.
- **FR-06** — `Patient.Gender` is a typed enum with exactly `Male`, `Female`, `Others`. *(CQ-007)*
- **FR-07** — `MedicalService.Charge` is a .NET `decimal` over a fixed-precision `numeric` column, and `TotalCharge` is `Charge × Quantity` computed in `decimal` with no truncation and no integer conversion. All other monetary fields (`Prescription`'s totals and discounts, `Payment.Amount`, `Product.UnitPrice`/`SalePrice`) are likewise `decimal` over `numeric`. *(CQ-008)*
- **FR-08** — The shared `Status` lookup is replaced by four separate typed status concepts — bill/prescription, product, inventory movement, appointment — each restricted to its own valid values, enforced in the domain layer. The eight legacy semantic values are preserved: `1=In Stock`, `2=Out Of Stock` (product); `3=Received`, `4=Shipped` (inventory movement); `5=Active`, `6=Closed` (bill); `7=Appointed`, `8=Visited` (appointment). No physical `Status` table exists. *(CQ-006, A7)*
- **FR-09** — Unique indexes exist on `Patient.Code`, `Prescription.Code`, `MedicalService.Code`, `MedicalService.Name`, and `MedicalInfo.Name`. *(DR-001, DR-017)*
- **FR-10** — Delete behaviour matches the captured fixtures: `Patient` → `Prescription` → `PatientMedicalService`/`Payment` cascades (GM-012); `Product` → `Inventory` cascades (GM-024); `PatientMedicalInfo` carries no database-level FK, so patient deletion orphans its rows (GM-019, A5).
- **FR-11** — `Prescription.TotalDiscountAmount` is a computed, unmapped property equal to `DiscountAmount + FixedDiscount`, with no floor at zero and no guard against negative values. *(GM-041)*
- **FR-12** — `Guid` primary keys are assigned in application code; no entity relies on database-generated identity values. *(A6)*
- **FR-13** — `Created`/`LastUpdate` are written through an injectable clock abstraction rather than a direct `DateTime.Now` call, so tests and fixture replay can pin time. *(seams.md: "every one of these unguarded writes implies the rebuild needs an injectable clock")*

**Identity & permissions**

- **FR-14** — ASP.NET Core Identity provides `ApplicationUser` (adding `FirstName`, `LastName`) and roles, with exactly one primary role per user explicitly enforced. Fine-grained `Resource`/`Permission` grants stay separate from that primary role. *(CQ-015, SQ-004)*
- **FR-15** — `Resource` (`Name`, `Route`, `IsPublic`) and `Permission` (role + resource) entities exist as the tables DR-015/DR-016 read. No social-login provider wiring is present. *(CQ-016)*

**Migrations & seed**

- **FR-16** — A from-scratch EF Core migration chain applies cleanly to a genuinely empty PostgreSQL database, in one pass, with no failure and no duplicate-index step. It is authored fresh and never ported from the legacy EF6 history. *(SQ-002; guards the harness-confirmed `IX_Code` defect)*
- **FR-17** — Fresh-install seed data creates the eight role names (`SystemAdmin, Admin, Manager, User, Inventory, Patient, Doctor, Compounder`), the `Resource` route catalog, and `Permission` rows for SystemAdmin only against every private `Resource`. Re-running the seeder creates zero additional permission rows. *(DR-016, GM-039, GM-040)*
- **FR-18** — Seeded entities receive application-assigned ids that are actually persisted. The seeded `Doctor` in particular must be retrievable by the id the seeder assigned. *(the harness-confirmed legacy defect where EF discarded the seed's literal GUID and appointment booking failed `FK_dbo.Appointment_dbo.Doctor_DoctorId`)*
- **FR-19** — Demo/known credentials exist only in explicitly local/development seed data. Production bootstrap takes admin credentials from environment configuration or forces a first-login password reset; no shared hardcoded password ships. *(CQ-017)*

**Data migration tooling**

- **FR-20** — A one-time migration tool moves all sixteen legacy entities' data from the legacy SQL Server database into PostgreSQL, preserving primary keys and relationships so historical patient, billing, payment, appointment, doctor, product, stock, user, role, and permission data remains available. *(SQ-005)*
- **FR-21** — The tool emits a value-audit report, and does not silently coerce or drop: every `MedicalService.Charge` string that cannot be parsed as a decimal (reported under the existing `NON_INTEGER_CHARGE` code), every `Patient.Gender` value outside `Male`/`Female`/`Others`, and every `StatusId` that does not map onto its owning entity's typed status. *(CQ-007, CQ-008, CQ-006)*
- **FR-22** — The tool runs reconciliation checks before cutover — per-entity row counts, and monetary totals for `Prescription` and `Payment` — and reports a non-success result when any check fails. It never reports success on a partial migration. *(SQ-005)*
- **FR-23** — The migration is re-runnable against a target database in a known state: either it is idempotent, or it refuses to run against a non-empty target with a clear message. It never half-applies silently.

### Non-Functional Requirements

- **NFR-01** — Indexes exist for the foreign keys and filter columns the backlog's later queries need (`Prescription.PatientId`, `Payment.PrescriptionId`, `PatientMedicalService.PrescriptionId`, `Inventory.ProductId`, `Appointment.DoctorId`, `Appointment.Date`), per SQ-010's "appropriate PostgreSQL indexes."
- **NFR-02** — All data-access entry points the data layer exposes are `async`. *(SQ-010)*
- **NFR-03** — Entity configuration supports projection-based reads so later items can avoid the legacy N+1 `.Last()` pattern; nothing in this item forces lazy loading. *(SQ-010, CQ-001)*
- **NFR-04** — Monetary columns use a fixed precision sufficient for the domain (`numeric(18,2)` unless the audit in FR-21 shows legacy data needs more), and never a floating-point type.
- **NFR-05** — Migration and reconciliation output is structured and machine-readable enough to attach to a verify report, not only human-readable console text. *(SQ-011)*
- **NFR-06** — The schema stays single-clinic: no tenant identifier, no tenant-scoped query filter. *(CQ-005)*
- **NFR-07** — Tests run against a real PostgreSQL instance, not an in-memory provider — cascade behaviour, unique indexes, and `numeric` precision are exactly the things an in-memory provider does not reproduce.

## Acceptance Criteria

Each criterion must pass for the change to be considered complete.

- **AC-01** — A clean clone restores and builds the solution with no manual steps. *(FR-01)*
- **AC-02** — The solution contains exactly one `DbContext` and one migrations folder; a search finds no second context and no second migration assembly. *(FR-02)*
- **AC-03** — Applying the migration chain to an empty PostgreSQL database succeeds in one pass, verified by an automated test that creates a fresh database, migrates it, and asserts every expected table exists. *(FR-16 — the direct guard against the legacy defect)*
- **AC-04** — The same test asserts no migration step creates an index that an earlier step already created, and the chain contains no `CreateIndex` on `Patient.Code` after `IX_Code` already exists. *(FR-16)*
- **AC-05** — All sixteen entities are present with the documented required flags, string lengths, and unique indexes; a test asserts each unique index rejects a duplicate at the database level. *(FR-04, FR-09)*
- **AC-06** — `Gender` accepts only the three enum values, and the column rejects any other value. *(FR-06)*
- **AC-07** — `TotalCharge` for `Charge = 10.50m, Quantity = 3` equals `31.50m` exactly — no truncation, no exception. *(FR-07, CQ-008)*
- **AC-08** — Every monetary column is `numeric` with the declared precision; a test asserts a value with two decimal places round-trips unchanged. *(FR-07, NFR-04)*
- **AC-09** — No `Status` table exists in the migrated schema, and each of the four typed status concepts rejects a value belonging to a different entity's set. *(FR-08)*
- **AC-10** — **GM-012 replays MATCH**: deleting a patient with 2 prescriptions, 1 `PatientMedicalService` and 1 `Payment` each leaves `prescriptions_remaining_count`, `patient_medical_services_remaining_count`, and `payments_remaining_count` all `0`. *(FR-10)*
- **AC-11** — **GM-024 replays MATCH**: deleting a product with 3 `Inventory` rows leaves `inventory_rows_remaining_count = 0`. *(FR-10)*
- **AC-12** — **GM-019 replays MATCH**: deleting a patient with 2 tagged `PatientMedicalInfo` rows leaves `patient_medical_info_rows_remaining_count = 2`, still referencing the deleted patient id. *(FR-10, A5)*
- **AC-13** — **GM-041 replays MATCH**: `TotalDiscountAmount` for `(0,0)`, `(10.5,5)`, `(0,-5)` returns `0`, `15.5`, `-5`. *(FR-11)*
- **AC-14** — **GM-039 replays MATCH**: seeding a fresh install creates one `Permission` row per private `Resource`, every row for SystemAdmin, and no other role holds any permission. *(FR-17)*
- **AC-15** — **GM-040 replays MATCH**: running the seeder again when any `Permission` row exists creates `0` rows. *(FR-17)*
- **AC-16** — The seeded `Doctor` can be fetched by the exact id the seeder assigned, and an `Appointment` referencing that id inserts without an FK violation. *(FR-18)*
- **AC-17** — Exactly one user with exactly one primary role can be created; assigning a second role to the same user is rejected. *(FR-14)*
- **AC-18** — A search of the repository finds no hardcoded password in any non-development seed path, and the production bootstrap path fails with a clear message when its environment credentials are absent rather than falling back to a default. *(FR-19)*
- **AC-19** — Running the migration tool against a populated synthetic legacy database (or the real export, if supplied) migrates every entity, and the reconciliation report shows matching per-entity row counts and matching `Prescription`/`Payment` monetary totals. *(FR-20, FR-22, A4)*
- **AC-20** — Given a synthetic legacy row with `Charge = "abc"`, one with `Charge = "10.50"`, a `Gender` of `"Unknown"`, and an `Appointment.StatusId` of `3`, the audit report names all four: the unparsable `"abc"` under `NON_INTEGER_CHARGE`, the out-of-set gender, and the unmappable status — while `"10.50"` migrates successfully as `10.50`. *(FR-21)*
- **AC-21** — Seeding a deliberate discrepancy (a deleted target row, an altered payment amount) makes the reconciliation check report failure. A reconciliation that cannot fail is not a check. *(FR-22)*
- **AC-22** — Running the migration tool against a non-empty target either produces the same result as the first run or refuses with a clear message; it never leaves the target half-migrated. *(FR-23)*
- **AC-23** — Every test in AC-03 through AC-22 runs against a real PostgreSQL instance in CI-reproducible fashion. *(NFR-07)*

## Edge Cases

- **Empty target database** — the case the legacy chain fails on. Explicitly tested (AC-03), not assumed.
- **`Charge` values that are neither integer nor decimal** — `"abc"`, empty string, `null`, currency symbols, thousands separators, locale decimal commas. Each must be reported, not coerced to zero.
- **`Charge = "10.50"`** — succeeds in the rebuild where legacy threw `FormatException`. The one deliberate, CQ-008-sanctioned behaviour change this item causes.
- **Negative `FixedDiscount`** — GM-041 pins `-5`. Adding a validation guard here would break a captured fixture; the guard, if wanted, needs its own CQ.
- **`Gender` values outside the known set**, including `null`, empty, and differently-cased `"male"`. Casing policy must be explicit: report or normalize, not both silently.
- **A legacy `StatusId` on the wrong entity** — the exact integrity hole CQ-006 closes. Nothing partitioned the legacy table, so real data may contain such rows.
- **Duplicate `Patient.Code` in legacy data** — GM-002 shows the legacy app could produce a collision that the unique index rejected while still returning `200 OK`. The unique index means such rows cannot both migrate; they must be reported, not dropped.
- **Legacy rows with `null` in a now-required column** — reported as an audit finding, never defaulted silently.
- **Orphaned `PatientMedicalInfo` rows in legacy data** — GM-019 proves legacy produced them. They must migrate as-is; a "cleanup" would destroy data outside this item's mandate.
- **Legacy `Created`/`LastUpdate` outside PostgreSQL's representable range**, or `null`. Reported.
- **Migration interrupted midway** — AC-22's concern. Partial state must be detectable.
- **Re-running the seeder on an already-seeded database** — GM-040 pins the no-op for permissions; the same must hold for roles, resources, and the doctor.

## Dependencies

**Backlog:** none. BL-001 is dependency rank 0 — *"the foundational item every other item in this backlog implicitly requires (a working, migratable schema)."*

**Decisions this spec rests on:** CQ-002, CQ-005, CQ-006, CQ-007, CQ-008, CQ-015, CQ-016, CQ-017, SQ-001, SQ-002, SQ-003, SQ-004, SQ-005, SQ-010, SQ-011, SQ-012. All are decided in `.specclaw/analysis/decisions.md`; none is open.

**Human-supplied inputs BL-001 names:**
1. A full legacy database export (schema + data) to validate reconciliation against — **not present in the repository.** A4 covers proceeding without it.
2. Human confirmation that the from-scratch migration chain builds and seeds cleanly end to end — AC-03/AC-04 automate the check, but BL-001 asks for human confirmation specifically because the defect was found by attempting exactly this.

**Runtime:** a real PostgreSQL instance for tests (NFR-07), and read access to a legacy SQL Server database (or the synthetic stand-in of A4) for the migration tool.

## Notes

- **Deliberately out of scope**, each owned elsewhere: API controllers and DTOs (every feature item); the React/MUI frontend and theme (SQ-006, BL-002 onward); token issuance, login/logout, Remember Me (BL-002, CQ-025); server-side rule enforcement (BL-007, CQ-011, CQ-013); `DM.Core` retirement (BL-009, CQ-024); the patient-list N+1 fix (CQ-001, BL-021); DR-020's fixed-window removal (CQ-010, BL-016); CI/CD, logging, monitoring, health checks, backup/restore (SQ-011) beyond the configuration the data layer needs; executing the production cutover.
- **UI fidelity:** none. BL-001 renders no screen and carries no `**UI fidelity:**` field in the backlog.
- **Open item surfaced, not resolved here:** `error-map.md`'s `NON_INTEGER_CHARGE` is defined as *"could not convert its `Charge` string to a whole number of currency units"* with **"Rebuild source: not yet mapped."** After CQ-008 the rebuild's condition is "not a valid decimal," which is narrower — `"10.50"` is no longer an error. FR-21 reuses the existing code because inventing one is not this item's call; renaming it (e.g. `UNPARSABLE_CHARGE`) and filling in its rebuild source belongs to `/specclaw:bf-baseline`.
- **Open item surfaced, not resolved here:** seams.md flags the missing injectable clock as *"a genuine open item for a future clarify pass"* that no PQ or CQ covers. FR-13 adopts the abstraction because BL-001 owns the timestamp writes, but the ADR seams.md suggests is still unwritten.
- **A5 is the one place this spec contradicts its own proposal**, on fixture evidence. If the FK is wanted, raise a CQ first.
