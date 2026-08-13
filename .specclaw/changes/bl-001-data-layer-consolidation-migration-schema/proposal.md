# Proposal: BL-001 — Data Layer Consolidation, Migration & Schema Setup

**Created:** 2026-08-12
**Status:** 🟡 Draft

**Backlog item:** BL-001 (`.specclaw/analysis/rebuild-backlog.md` — the source of truth for this proposal's scope, acceptance basis, dependencies, and verification requirements)
**Module:** MOD-005 — Identity, Roles & Permissions (dependency rank 0)
**Depends on:** None — foundational; every other item in the backlog implicitly requires a working, migratable schema
**Gate:** CLEAR · **Verification:** VERIFIABLE (fixtures GM-001, GM-002, GM-022, GM-023 — see Open Question 3)

## Problem

_What problem are we solving? Why does it matter?_

**This repository contains no application source code yet.** All 198 tracked files live under `.specclaw/` — the analysis, baseline, and UI artifacts generated from the legacy app. There is no solution, no project, no schema. BL-001 is the item the whole backlog rests on: per the backlog's own sequencing note, *"BL-001 (data layer/migration foundation) precedes everything else in the entire backlog — no other item's schema can exist before this one runs."*

Three distinct problems have to be solved together, because they all land on the same schema:

1. **The legacy data layer is structurally split with no reason recorded.** Two independently-migrated EF6 contexts (`DentalDbContext` + `ApplicationDbContext`) share one physical SQL Server database with two independent migration pipelines. CQ-002 decided to consolidate; nothing exists yet to consolidate *into*.

2. **The legacy EF6 migration chain cannot build a fresh database at all** — confirmed by running the harness, and recorded in BL-001 as a defect not present in any earlier analysis document. `InitialCreate` creates `dbo.Patient`'s unique `IX_Code` (DR-001), and the later `202509030639057_Patient_Code_Unique` runs `CreateIndex` on the same column with no preceding `DropIndex`, so `Update()` against an empty database always fails. Any migration tooling validated only against an already-seeded database would inherit that blind spot. The rebuild's from-scratch EF Core history must not reproduce the defect, and must be proven against a genuinely empty target.

3. **All existing production data must survive the move to PostgreSQL, and three data-shape defects must be audited rather than silently carried across** (SQ-002, SQ-005): `MedicalService.Charge` is a `string` truncated through `Convert.ToInt32` (DR-019 / CQ-008), `Patient.Gender` is a free-form `string` despite an unused `Gender` enum in the same file (CQ-007), and one shared `Status` lookup table is reused as four unrelated enumerations with nothing partitioning it (CQ-006). A migration that quietly drops or coerces these values loses clinical and billing history.

## Proposed Solution

_What are we building? High-level approach._

A greenfield ASP.NET Core + EF Core data foundation for PostgreSQL, plus the one-time migration and reconciliation tooling that moves the legacy database into it.

1. **Backend solution scaffolding** — the minimum ASP.NET Core solution structure needed to host a data layer and its tests (SQ-001, SQ-003). No backlog item covers scaffolding, so it necessarily lands here; see Open Questions 1–2.

2. **One EF Core `DbContext`, one migration history** (CQ-002) — covering all sixteen legacy entities *and* the Identity/permission entities. Identity and domain tables may be separated by PostgreSQL schema or table naming, but share a single controlled migration history rather than two pipelines.

3. **A from-scratch EF Core migration chain** targeting PostgreSQL via the Npgsql provider (SQ-002) — authored fresh, never ported from the EF6 history, and proven to apply cleanly to an empty database (this is the direct guard against problem 2 above).

4. **Corrected data shapes at the schema level, each tied to a decided CQ:**
   - `Charge`/`TotalCharge` as a .NET `decimal` over a fixed-precision `numeric` column; `TotalCharge` retains fractional currency and never truncates (CQ-008).
   - `Patient.Gender` as a typed enum — `Male`/`Female`/`Others` (CQ-007).
   - The shared `Status` lookup replaced by four separate typed status concepts (Prescription/Bill, Product, Inventory Movement, Appointment), with valid values enforced per entity in the domain layer, preserving existing semantic values through migration (CQ-006).

5. **Identity/permission schema on ASP.NET Core Identity** (SQ-004) — `ApplicationUser` (with `FirstName`/`LastName`), roles with one primary role per user explicitly enforced (CQ-015), and the `Resource`/`Permission` entities that DR-015/DR-016 read. No social-login provider wiring is carried forward (CQ-016). Token issuance and the login flow itself belong to BL-002, not here.

6. **Seed data for a fresh install** — the eight status/semantic values, the eight role names, the `Resource` route catalog, and `Permission` rows for SystemAdmin only (DR-016). Two specific legacy defects are *not* reproduced: the seeded `Doctor` must not depend on a hardcoded GUID that EF discards (`BaseModel.Id` is `DatabaseGenerated.Identity`, so the legacy seed's literal GUID never matches the row actually created — booking an appointment on a freshly migrated legacy database fails `FK_dbo.Appointment_dbo.Doctor_DoctorId` outright), and demo credentials exist only in explicitly local/development seed data, with production admin bootstrap using environment-specific credentials or a forced first-login reset (CQ-017).

7. **One-time data migration and reconciliation tooling** (SQ-005) — moves all sixteen entities' data from the legacy SQL Server database into PostgreSQL, and emits an explicit audit report rather than coercing silently: every `Charge` value that cannot be parsed as a decimal (mapping to the existing `NON_INTEGER_CHARGE` code in `.specclaw/baseline/error-map.md`), every `Gender` value outside the known set, every `StatusId` that does not map onto its entity's typed status, plus row-count and monetary-total reconciliation checks that must pass before cutover.

## Scope

### In Scope

- ASP.NET Core solution/project skeleton sufficient to host the data layer, its migrations, and its tests (see Open Questions 1–2)
- A single EF Core `DbContext` covering all sixteen legacy entities (Patient, Prescription, PatientMedicalService, MedicalService, MedicalInfo, PatientMedicalInfo, Payment, Product, Inventory, Doctor, Appointment, Status-as-typed-concepts) plus the four MOD-005 identity entities (ApplicationUser, IdentityRole, Resource, Permission)
- Entity configuration: PKs, FKs, cascade behaviour, unique indexes (`Patient.Code` `IX_Code`, `MedicalService.Name`, `MedicalInfo.Name` per DR-017), string-length constraints, and required fields as documented in `domain-model.md`
- A fresh EF Core migration chain for PostgreSQL, applying cleanly from empty
- Typed `Gender` enum (CQ-007), four typed status concepts (CQ-006), decimal money type for `Charge`/`TotalCharge` (CQ-008)
- Fresh-install seed data: statuses, roles, resources, SystemAdmin-only permissions, doctor, dev-only demo accounts
- One-time legacy → PostgreSQL data migration tooling for all sixteen entities
- Migration validation, reconciliation checks, and the value-audit report for unparseable/out-of-set legacy values
- Environment-based connection-string and secret configuration for the data layer
- Integration tests proving the migration chain builds an empty database and the reconciliation checks detect seeded discrepancies

### Out of Scope

- **Any API controller, endpoint, or DTO** — every feature item (BL-002 onward) brings its own
- **The React + TypeScript + MUI frontend** entirely (SQ-006), including the MUI theme from `design-tokens.json`; BL-001 renders no screen and carries no UI fidelity obligation
- **Authentication token issuance, login/logout, and Remember Me** — BL-002 (CQ-025, SQ-004)
- **Server-side enforcement of the Resource/Permission model and of DR-004/005/006/008** — BL-007, CQ-011, CQ-013. BL-001 creates the tables those rules read; it does not enforce them
- **Retiring or relocating `DM.Core`** — BL-009 (CQ-024)
- **The N+1 / `.Last()` crash fix in the patients list** — CQ-001, belongs to the BL-021 patient-list item
- **Removing DR-020's fixed one-month lookback** — CQ-010, belongs to BL-016
- **CI/CD, structured logging, centralized error monitoring, health checks, and backup/restore procedures** (SQ-011) beyond the environment-based configuration the data layer itself needs
- **Executing the production cutover** — this item delivers and validates the tooling; running it against production is an operational event
- **Multi-tenancy** — explicitly preserved as single-clinic (CQ-005)
- **Normalizing `Appointment.PatientNameOrId` into a real `Patient` FK** — no decision sanctions it; see Open Question 5

## Impact

- **Files affected:** ~45–70 new files (estimated) — greenfield: solution/project files, ~16 entity classes, entity configurations, `DbContext`, initial migration, seed data, migration/reconciliation tooling, and tests. No existing source file is touched, because none exists.
- **Complexity:** large
- **Risk:** high — every subsequent backlog item builds on this schema; a wrong shape here propagates into all 28 remaining items. The one-time production data migration is additionally irreversible in practice, which is why SQ-005's reconciliation checks are in scope rather than deferred.

## Open Questions

1. **Does BL-001 own the backend solution scaffolding?** No backlog item covers "create the solution" — BL-001 is the first item and the only one whose work cannot start without it. **Proposed:** yes, BL-001 creates the minimum backend solution/project skeleton, and the frontend scaffolding stays with BL-002 (the first item that renders a screen). Confirm before planning.

2. **Repository layout and project naming for the new solution.** No ADR or decision records it. **Proposed:** a `src/`-rooted solution — `DentalManagement.Domain` (entities, enums), `DentalManagement.Infrastructure` (DbContext, configurations, migrations, seed), `DentalManagement.Api` (host), `DentalManagement.DataMigration` (the one-time tooling), plus `tests/`. Say so now if a different convention is expected; renaming later touches every item.

3. **BL-001's four cited fixtures cannot replay at this item's completion.** GM-001 is a pure-function seam on `HelperRequestModel.GetThisPatientCode`, GM-002 a service seam on `PatientCreateController.Post` (both DR-001, MOD-001), and GM-022/GM-023 service seams on `InventoryReportController.GetReport` (DR-020, MOD-003) — all four need application code that BL-020 and BL-016 deliver, not a schema. The join is mechanical: BL-001's acceptance basis quotes DR-001's unique-index text, so bash matched it. **Proposed:** BL-001's real acceptance rests on its own two stated verification inputs — a human-confirmed clean from-scratch migration build/seed, and reconciliation validated against a legacy export — with those four fixtures replaying under BL-016/BL-020/BL-021 where their seams actually exist. Confirm, so `/specclaw:verify` and `/specclaw:bf-replay` are not scoped against fixtures this item structurally cannot satisfy.

4. **Is a full legacy database export (schema + data) available now?** BL-001 names it as a verification input only a human with production/staging access can supply, and no fixture captures it. **Proposed, if it is not available yet:** build the migration and reconciliation tooling in full, validate it against a synthetic legacy database generated from `domain-model.md`'s documented shapes, and record the real-export reconciliation run as a named, blocking pre-cutover gate rather than silently treating the synthetic run as sufficient. If the export *is* available, we validate against it directly and this question closes.

5. **Two legacy schema oddities have no decision covering them.** `Appointment.PatientNameOrId` is free text rather than a `Patient` FK, and `PatientMedicalInfo` holds two plain `Guid`s with no `[ForeignKey]`/navigation, unlike every other join entity. **Proposed** (per SQ-012's case-by-case default, which preserves valid business behaviour absent a decided CQ): preserve `PatientNameOrId` as-is — normalizing it would change appointment-booking behaviour with no sanctioning decision — but *do* declare proper FK/navigation on `PatientMedicalInfo`, since that is an implementation accident with no behavioural consequence. Flagging both rather than deciding silently.

6. **Primary-key generation strategy.** Legacy `BaseModel.Id` is a `Guid` marked `[DatabaseGenerated(DatabaseGeneratedOption.Identity)]`, which is precisely what makes the legacy `Doctor` seed defect possible (EF discards the assigned GUID; the client's hardcoded id never matches). **Proposed:** keep `Guid` PKs but generate them in application code rather than the database, so seed data can assign stable, known ids and the whole class of defect disappears. Confirm, since it affects every entity.

7. **Does the `Status` table survive as a table?** CQ-006 replaces the shared lookup with typed per-entity concepts and says to "preserve the existing semantic values during migration"; the module map leaves `Status` unassigned to any module. **Proposed:** no physical `Status` table in the rebuild — the eight seeded values (`1=In Stock … 8=Visited`) become typed enums per entity, and the migration maps each legacy `StatusId` onto its owning entity's enum, reporting any value that does not map. Confirm, since a surviving lookup table would change the shape of four entities.

---

**To proceed:** Review this proposal and approve to begin planning.
