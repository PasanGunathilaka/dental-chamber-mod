# Tasks: BL-001 — Data Layer Consolidation, Migration & Schema Setup

**Change:** bl-001-data-layer-consolidation-migration-schema
**Created:** 2026-08-12
**Total Tasks:** 16

## Summary

Five waves, ordered so each is provable before the next depends on it: solution skeleton → schema and its captured-fixture gates → seed data and its gates → the one-time migration tool → wiring and the human confirmation BL-001 explicitly asks for.

The hinge is **Wave 2**. Three captured persistence fixtures (GM-012, GM-019, GM-024) and one pure-function fixture (GM-041) pin the schema's delete and value semantics. Until those replay MATCH, no later wave should be trusted — a migration tool that moves data through a wrong schema moves it wrongly.

Waves 3 and 4 are independent of each other and can run in parallel once Wave 2 is green.

## Tasks

### Wave 1 — Solution skeleton and domain model

- [x] `T1` — Create the solution, shared build properties, and .NET ignore rules
  - Files: `DentalManagement.sln`, `Directory.Build.props`, `.gitignore`, `src/*/​*.csproj`, `tests/*/​*.csproj`
  - Estimate: small
  - Kind: config
  - Notes: Four `src` projects and two `tests` projects per design A2/architecture. Current .NET LTS, nullable enabled, warnings-as-errors. `DentalManagement.Domain` takes no package reference beyond the BCL — that constraint is what keeps FR-02's separation honest. Satisfies AC-01.

- [x] `T2` — Implement domain entities, enums, and the clock abstraction
  - Files: `src/DentalManagement.Domain/Entities/*.cs`, `Enums/*.cs`, `Abstractions/IClock.cs`
  - Estimate: medium
  - Kind: impl
  - Depends: T1
  - Notes: All 11 domain entities plus `ApplicationUser`/`Resource`/`Permission`, fields exactly as `domain-model.md` documents them (FR-04). No EF attributes — persistence config is T3. `Gender` enum (FR-06); four status enums carrying the legacy integers `1..8` (FR-08, design D-3). Money as `decimal` throughout, including `Payment.Amount` and `Prescription`'s totals (FR-07). `Prescription.TotalDiscountAmount` is unmapped `DiscountAmount + FixedDiscount` with **no** floor at zero and **no** negative guard — GM-041 pins `-5` (FR-11). `Appointment.PatientNameOrId` stays a free-text string with no `Patient` navigation (FR-05).

### Wave 2 — Persistence, migration chain, and the fixture gates

- [x] `T3` — Implement the single DbContext and per-entity EF configurations
  - Files: `src/DentalManagement.Infrastructure/Persistence/DentalDbContext.cs`, `Persistence/Configurations/*.cs`, `Time/SystemClock.cs`
  - Estimate: large
  - Kind: impl
  - Depends: T2
  - Notes: One context deriving `IdentityDbContext<ApplicationUser>` — no second context, no second migration assembly (FR-02, AC-02). Identity tables to an `identity` schema, domain tables to `public` (design D-2). One `IEntityTypeConfiguration<T>` per entity (D-1) declaring: application-assigned `Guid` keys with generation turned off (FR-12, A6); unique indexes on `Patient.Code`, `Prescription.Code`, `MedicalService.Code`/`.Name`, `MedicalInfo.Name` (FR-09); FK indexes per NFR-01; `numeric(18,2)` on every monetary column (NFR-04); `timestamp without time zone` with Npgsql's UTC mapping explicitly overridden (A8, D-8, R-6). **Delete behaviour is the load-bearing part** (FR-10): cascade `Patient`→`Prescription`→`PatientMedicalService`/`Payment` and `Product`→`Inventory`; `PatientMedicalInfo` gets **no database-level FK at all** — navigation properties only if the constraint stays out of the database. See spec A5 before changing that: adding the FK breaks captured fixture GM-019 with no CQ to sanction it.

- [x] `T4` — Author the initial migration and stand up the real-PostgreSQL test harness
  - Files: `src/DentalManagement.Infrastructure/Persistence/Migrations/*`, `tests/DentalManagement.Infrastructure.Tests/` fixture/base classes
  - Estimate: medium
  - Kind: migration
  - Depends: T3
  - Notes: One fresh migration, authored from the new model — never ported from the legacy EF6 history (FR-16). The test harness provisions a real PostgreSQL database per run and must be CI-reproducible (NFR-07, D-7, AC-23); the in-memory provider cannot verify cascades, unique indexes, or `numeric` precision, so it is not an option here. First test: create an empty database, apply the chain, assert every expected table exists and no step re-creates an existing index (AC-03, AC-04) — this is the direct guard against the harness-confirmed legacy defect.

- [x] `T5` — Test schema constraints and value semantics
  - Files: `tests/DentalManagement.Infrastructure.Tests/SchemaTests.cs`, `ValueSemanticsTests.cs`
  - Estimate: medium
  - Kind: test
  - Depends: T4
  - Notes: AC-05 (each unique index rejects a duplicate at the database level), AC-06 (`Gender` rejects out-of-set values), AC-08 (two-decimal round-trip unchanged), AC-09 (no `Status` table exists; each status enum rejects another entity's value), AC-13 (`TotalDiscountAmount` → `0`, `15.5`, `-5`), and AC-07 (`Charge = 10.50m × 3 = 31.50m` exactly, no truncation, no exception — the CQ-008 fix).

- [x] `T6` — Prove the three captured delete-behaviour fixtures replay MATCH
  - Files: `tests/DentalManagement.Infrastructure.Tests/DeleteBehaviourTests.cs`
  - Estimate: medium
  - Kind: test
  - Depends: T4
  - Notes: The gate for this whole change. GM-012 — patient with 2 prescriptions each holding 1 `PatientMedicalService` and 1 `Payment`; after delete all three remaining counts are `0` (AC-10). GM-024 — product with 3 `Inventory` rows; after delete `0` remain (AC-11). GM-019 — patient with 2 tagged `PatientMedicalInfo` rows; after delete **2 remain**, still referencing the deleted patient id (AC-12). Arrange through the persistence path, matching each fixture's captured seam layer. A failure here means T3's delete configuration is wrong, not that the fixture is wrong.

### Wave 3 — Seed data

- [x] `T7` — Implement the database seeder and environment-split credential paths
  - Files: `src/DentalManagement.Infrastructure/Persistence/Seeding/DatabaseSeeder.cs`, `DevelopmentSeedData.cs`, `DependencyInjection.cs`
  - Estimate: medium
  - Kind: impl
  - Depends: T4
  - Notes: Seeds the eight role names, the `Resource` route catalog, and `Permission` rows for SystemAdmin only against every private `Resource`, guarded so a re-run creates zero additional rows (FR-17, DR-016, GM-039/GM-040). Seeded ids are application-assigned and must actually persist — the seeded `Doctor` has to be retrievable by the id the seeder chose (FR-18); this is the legacy defect where EF discarded the literal GUID and appointment booking then failed its FK. Demo credentials live only in the development path; production bootstrap reads admin credentials from environment configuration and **fails with a clear message when they are absent** rather than falling back to a default (FR-19, CQ-017). One primary role per user enforced (FR-14, CQ-015).

- [x] `T8` — Test seeding, idempotency, and credential handling
  - Files: `tests/DentalManagement.Infrastructure.Tests/SeedingTests.cs`
  - Estimate: medium
  - Kind: test
  - Depends: T7
  - Notes: AC-14 (GM-039 — one permission row per private resource, all SystemAdmin, no other role holds any), AC-15 (GM-040 — re-run creates `0`), AC-16 (seeded doctor fetchable by its assigned id; an appointment referencing it inserts without FK violation), AC-17 (a second role on the same user is rejected), AC-18 (no hardcoded password outside the development path; production bootstrap fails clearly without its environment credentials). Also assert the roles/resources/doctor seeds are themselves re-runnable.

### Wave 4 — One-time legacy data migration tool

- [x] `T9` — Build the synthetic legacy database scripts
  - Files: `tests/DentalManagement.DataMigration.Tests/SyntheticLegacy/*.sql`
  - Estimate: medium
  - Kind: test
  - Depends: T1
  - Notes: Legacy-shaped SQL Server schema and seed data for all 16 tables, standing in for the export BL-001 names but that the repository does not contain (spec A4). Must include the edge rows the audit has to catch: `Charge` of `"abc"` and `"10.50"`, a `Gender` of `"Unknown"`, an `Appointment.StatusId` of `3`, a duplicate `Patient.Code` pair (per GM-002's collision), a `null` in a now-required column, and orphaned `PatientMedicalInfo` rows (which GM-019 proves legacy produced). Independent of Waves 2–3, so it can start immediately after T1.

- [x] `T10` — Implement the legacy readers
  - Files: `src/DentalManagement.DataMigration/LegacyReaders/*.cs`, `Program.cs` (CLI skeleton)
  - Estimate: medium
  - Kind: impl
  - Depends: T9
  - Notes: Raw ADO.NET reads, one reader per legacy table (design D-5) — the legacy EF6 model is deliberately not reconstructed, since importing it would import the broken migration chain this item exists to avoid. CLI takes source connection, target connection, `--dry-run`, and a report path.

- [x] `T11` — Implement the value-audit report
  - Files: `src/DentalManagement.DataMigration/Auditing/*.cs`
  - Estimate: medium
  - Kind: impl
  - Depends: T10
  - Notes: FR-21 — report, never coerce: `Charge` strings that will not parse as `decimal` (under the existing `NON_INTEGER_CHARGE` code from `error-map.md`), `Gender` values outside the three known ones, `StatusId` values that do not belong to the owning entity's enum, plus duplicate `Patient.Code` and nulls in now-required columns (R-5). `"10.50"` is **not** a finding — it migrates as `10.50`, which is exactly CQ-008's fix. Output is structured and machine-readable (NFR-05). Casing policy for `Gender` must be an explicit choice, stated in the report legend.

- [x] `T12` — Implement reconciliation checks
  - Files: `src/DentalManagement.DataMigration/Reconciliation/*.cs`
  - Estimate: medium
  - Kind: impl
  - Depends: T10
  - Notes: FR-22 — per-entity row counts plus `Prescription`/`Payment` monetary totals, source versus target. Returns a failing result and a non-zero exit code on any mismatch (design D-9). Also record the observed `Charge` value range so `numeric(18,2)` is confirmed against real data rather than assumed (R-7).

- [x] `T13` — Implement the migration orchestrator and non-empty-target guard
  - Files: `src/DentalManagement.DataMigration/Program.cs`, orchestration/writer classes
  - Estimate: large
  - Kind: migration
  - Depends: T11, T12, T7
  - Notes: FR-20 — move all 16 entities, preserving primary keys and relationships, writing through the new `DbContext` in FK-safe order. Refuses a non-empty target unless `--allow-non-empty` is passed, and never half-applies silently (FR-23, D-6). Runs the audit and reconciliation as part of the run, not as a separate optional step. Depends on T7 because a migrated database must land on top of the seeded roles/resources rather than duplicating them.

- [x] `T14` — Test the migration tool end to end
  - Files: `tests/DentalManagement.DataMigration.Tests/*`
  - Estimate: large
  - Kind: test
  - Depends: T13, T9
  - Notes: AC-19 (full migration from the synthetic legacy database; reconciliation reports matching counts and monetary totals), AC-20 (all four planted bad values named in the audit report, while `"10.50"` migrates as `10.50`), AC-21 (**a deliberately planted discrepancy makes reconciliation fail** — a check that cannot fail is not a check), AC-22 (re-run against a non-empty target either matches the first result or refuses clearly; never half-applied).

### Wave 5 — Host wiring and the human confirmation gate

- [x] `T15` — Wire the API host: DI, environment configuration, health probe
  - Files: `src/DentalManagement.Api/Program.cs`, `appsettings*.json`, `src/DentalManagement.Infrastructure/DependencyInjection.cs`
  - Estimate: small
  - Kind: impl
  - Depends: T7
  - Notes: FR-03 — connection string and every secret from environment-based configuration; `appsettings*.json` carries structure only, no credential. Registers context, Identity, `IClock`, seeder. A health probe is the only route — no feature endpoint belongs to this item.

- [x] `T16` — Wire the verify commands and record the human confirmation BL-001 requires
  - Files: `.specclaw/config.yaml`, `README.md` (setup + migration runbook)
  - Estimate: small
  - Kind: docs
  - Depends: T14, T15
  - Notes: Fill `build.test_command` and the verify commands now that a solution exists, including how the real-PostgreSQL test database is provisioned in CI (AC-23, R-9). Then capture the second verification input BL-001 names explicitly: **a human confirming the from-scratch migration chain builds and seeds cleanly end to end** — *"the specific defect above was found by attempting exactly this, not by static reading, so the fix itself needs the same live-build verification, not just a fixture."* AC-03/AC-04 automate the check; this task records the human sign-off alongside it. Also record the real-export reconciliation run as a named blocking pre-cutover gate, still outstanding under spec A4 (R-2).

---

## Legend

- `[ ]` Pending
- `[~]` In Progress
- `[x]` Complete
- `[!]` Failed

**Task format:**
```
  - [ ] `T<n>` — <title>
    - Files: <files to create/modify>
    - Estimate: small | medium | large
    - Kind: docs | test | config | refactor | impl | migration   (optional; hints the build subagent's role, tools, and model)
    - Depends: <task ids> (if any)
    - Notes: <additional context>
```
