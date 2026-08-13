# Design: BL-001 — Data Layer Consolidation, Migration & Schema Setup

**Change:** bl-001-data-layer-consolidation-migration-schema
**Created:** 2026-08-12

## Technical Approach

Four layers, built in dependency order, each provable before the next starts:

1. **Solution skeleton** — a minimal ASP.NET Core solution whose only job initially is to host a `DbContext` and its tests. No controllers, no MVC surface beyond the host required to run migrations.
2. **Domain model** — plain entity classes and enums with no EF attributes. All persistence concerns live in `IEntityTypeConfiguration<T>` classes in the infrastructure project, so the domain layer stays free of provider knowledge and the configuration for each entity is one file you can read in full.
3. **Persistence** — one `DentalDbContext` deriving from `IdentityDbContext<ApplicationUser>`, so Identity and domain tables share one context and one migration history (CQ-002) while remaining separable by PostgreSQL schema. One initial migration, authored fresh.
4. **Data migration tool** — a standalone console project that reads the legacy SQL Server database over ADO.NET (raw SQL, no EF6 model to resurrect), writes through the new `DbContext`, and produces an audit + reconciliation report. It is a one-time tool, not part of the API's runtime.

The ordering is deliberate: the migration tool cannot be written before the target schema exists, and the target schema's delete behaviour must be pinned by the captured persistence fixtures before any data is moved through it.

**Why raw ADO.NET for the legacy read side.** Reconstructing the legacy EF6 model in the new solution would import the very defects this item exists to avoid — including the broken migration chain. Reading the legacy tables as data (`SELECT` per table, `SqlDataReader`) keeps the legacy schema as an input format rather than a code dependency, and makes the synthetic-legacy-database path of A4 trivially constructible from plain SQL.

**Why a real PostgreSQL instance in tests.** Every acceptance criterion that matters here — cascade chains, unique-index rejection, `numeric` precision, "migrations apply to an empty database" — is precisely what the EF Core in-memory provider does not model. Testing against it would produce green tests that prove nothing about AC-03 through AC-12.

## Architecture

```
src/
  DentalManagement.Domain/            entities, enums, IClock — no EF, no Npgsql
    Entities/                         Patient, Prescription, ..., ApplicationUser
    Enums/                            Gender, BillStatus, ProductStatus,
                                        InventoryMovementStatus, AppointmentStatus
    Abstractions/                     IClock
  DentalManagement.Infrastructure/    DbContext, configurations, migrations, seed
    Persistence/DentalDbContext.cs
    Persistence/Configurations/       one IEntityTypeConfiguration per entity
    Persistence/Migrations/           single, fresh migration history
    Persistence/Seeding/              DatabaseSeeder + Development/Production paths
    Time/SystemClock.cs
  DentalManagement.Api/               host — DI, config, migrate-on-start switch
  DentalManagement.DataMigration/     one-time legacy -> PostgreSQL console tool
    LegacyReaders/                    raw ADO.NET readers, one per legacy table
    Auditing/                         value-audit report
    Reconciliation/                   row-count and monetary-total checks
tests/
  DentalManagement.Infrastructure.Tests/   schema, migration, cascade, seed
  DentalManagement.DataMigration.Tests/    audit, reconciliation, synthetic legacy DB
```

**Container view.** One PostgreSQL database, one API host, one throwaway migration tool. The legacy SQL Server database is an external input to the tool and nothing else — no runtime dependency on it survives cutover.

**Schema separation without a second context.** Identity tables land in an `identity` PostgreSQL schema, domain tables in `public`, both configured on the single `DentalDbContext`. This satisfies CQ-002's *"may remain logically separated by configuration/naming, but they should share one controlled migration history"* literally: separation is a naming choice, the migration history is one.

**Status as enums.** Four independent enums replace one table (CQ-006), each persisted as an `int` carrying the legacy numeric value so migrated data keeps its meaning without a translation table:

| Enum | Values (legacy id preserved) |
|---|---|
| `ProductStatus` | `InStock = 1`, `OutOfStock = 2` |
| `InventoryMovementStatus` | `Received = 3`, `Shipped = 4` |
| `BillStatus` | `Active = 5`, `Closed = 6` |
| `AppointmentStatus` | `Appointed = 7`, `Visited = 8` |

Keeping the legacy integers as the enum values makes FR-21's "unmappable `StatusId`" check a simple set-membership test per entity, and makes the migration a straight copy of the integer rather than a lookup that could silently mismatch.

**Clock.** `IClock` in the domain, `SystemClock` registered in the API, a fixed test double in tests. `Created`/`LastUpdate` are set through it. This is what lets `/specclaw:bf-replay` pin time, and it is the abstraction seams.md named as missing.

## File Changes Map

| File | Action | Description |
|------|--------|-------------|
| `DentalManagement.sln` | Create | Solution referencing the four `src` and two `tests` projects |
| `Directory.Build.props` | Create | Shared TFM, nullable, warnings-as-errors, analysis level |
| `src/DentalManagement.Domain/*.csproj` | Create | No package references beyond the BCL |
| `src/DentalManagement.Domain/Entities/*.cs` | Create | 11 domain entities + `ApplicationUser`, `Resource`, `Permission` |
| `src/DentalManagement.Domain/Enums/*.cs` | Create | `Gender` + the four status enums above |
| `src/DentalManagement.Domain/Abstractions/IClock.cs` | Create | FR-13 |
| `src/DentalManagement.Infrastructure/Persistence/DentalDbContext.cs` | Create | Single context, `IdentityDbContext<ApplicationUser>` |
| `src/DentalManagement.Infrastructure/Persistence/Configurations/*.cs` | Create | One config per entity: keys, FKs, delete behaviour, indexes, precision |
| `src/DentalManagement.Infrastructure/Persistence/Migrations/*` | Create | One fresh initial migration (FR-16) |
| `src/DentalManagement.Infrastructure/Persistence/Seeding/DatabaseSeeder.cs` | Create | Roles, resources, SystemAdmin permissions, doctor (FR-17, FR-18) |
| `src/DentalManagement.Infrastructure/Persistence/Seeding/DevelopmentSeedData.cs` | Create | Demo accounts, development environments only (FR-19) |
| `src/DentalManagement.Infrastructure/Time/SystemClock.cs` | Create | `IClock` implementation |
| `src/DentalManagement.Infrastructure/DependencyInjection.cs` | Create | `AddInfrastructure(...)` — context, Identity, clock, seeder |
| `src/DentalManagement.Api/Program.cs` | Create | Host, config binding, DI; no endpoints beyond a health probe |
| `src/DentalManagement.Api/appsettings*.json` | Create | Structure only — connection string and secrets from environment |
| `src/DentalManagement.DataMigration/Program.cs` | Create | CLI: source, target, `--dry-run`, report path |
| `src/DentalManagement.DataMigration/LegacyReaders/*.cs` | Create | Raw ADO.NET readers, one per legacy table |
| `src/DentalManagement.DataMigration/Auditing/*.cs` | Create | Charge/Gender/Status audit findings + report writer (FR-21) |
| `src/DentalManagement.DataMigration/Reconciliation/*.cs` | Create | Row-count and monetary-total checks (FR-22) |
| `tests/DentalManagement.Infrastructure.Tests/*` | Create | AC-03 … AC-18 against real PostgreSQL |
| `tests/DentalManagement.DataMigration.Tests/*` | Create | AC-19 … AC-22, synthetic legacy database from SQL scripts |
| `tests/.../SyntheticLegacy/*.sql` | Create | Legacy-shaped SQL Server schema + seed data for A4 |
| `.specclaw/config.yaml` | Modify | Fill `build.test_command` / `verify` commands once the solution exists |
| `.gitignore` | Create | .NET artifacts (`bin/`, `obj/`, user secrets) |

No existing source file is modified — none exists.

## Data Model Changes

This item *is* the data model. Deltas from the legacy schema, each tied to a decision:

| Legacy | Rebuild | Basis |
|---|---|---|
| Two contexts, two migration pipelines, one database | One context, one migration history, schema-separated tables | CQ-002 |
| SQL Server | PostgreSQL via Npgsql, fresh migrations | SQ-002 |
| `MedicalService.Charge` `string`; `TotalCharge` = `Convert.ToInt32(Charge) * Quantity` | `decimal` over `numeric(18,2)`; `TotalCharge` computed in `decimal` | CQ-008 |
| `Payment.Amount` `double`; `Prescription` totals `double` | `decimal` over `numeric(18,2)` | CQ-008 (money-type intent), NFR-04 |
| `Patient.Gender` free `string` | `Gender` enum | CQ-007 |
| One `Status` table serving four entities | Four typed enums, legacy integers preserved, no table | CQ-006 |
| `Guid` PK with `DatabaseGenerated(Identity)` | `Guid` PK assigned in application code | A6 |
| Seeded `Doctor` GUID discarded by EF | Seeded id persisted as assigned | the harness-confirmed FK defect |
| Shared hardcoded seed password `"123qwe"` | Development-only demo credentials; environment-based production bootstrap | CQ-017 |
| `Created`/`LastUpdate` via `DateTime.Now` | Same wall-clock semantics, written through `IClock`, `timestamp without time zone` | FR-13, A8 |

**Deliberately unchanged**, because no decision sanctions changing them:

- `Appointment.PatientNameOrId` stays free text with no `Patient` FK. *"Appointments and patient records are entirely independent data."*
- `PatientMedicalInfo` keeps no database-level FK, so patient deletion orphans its rows — pinned by GM-019 (spec A5).
- `Prescription.TotalDiscountAmount` stays unguarded, negatives included — pinned by GM-041.
- No tenant column (CQ-005).

## API Changes

None. BL-001 exposes no endpoint. The API host exists only to own DI, configuration, and the migrate/seed entry point; a health probe is the sole route, and it is scaffolding for SQL-011's later work, not a feature of this item. Every controller belongs to a later backlog item.

## Key Decisions

- **D-1 — Configuration classes, not data annotations.** Keeps the domain project free of EF and puts each entity's keys, delete behaviour, indexes, and precision in one readable file. Directly serves FR-10, where delete behaviour must be auditable against three captured fixtures.
- **D-2 — `IdentityDbContext<ApplicationUser>` as the single context** rather than a separate identity context. The literal implementation of CQ-002; schema separation is achieved with `ToTable(..., schema: "identity")`.
- **D-3 — Enum-as-int preserving legacy numeric values.** Makes migration a copy rather than a translation, and makes the unmappable-status audit a set-membership test.
- **D-4 — Application-assigned `Guid` PKs.** Removes the mechanism behind the legacy `Doctor` seed defect: a seeder can assign an id, persist it, and reference it. Cost: no database-side uniqueness generation, which is irrelevant for `Guid`s.
- **D-5 — Raw ADO.NET for legacy reads.** Avoids importing the legacy EF6 model and its broken migration chain. Legacy schema is an input format, not a dependency.
- **D-6 — Migration tool refuses a non-empty target by default** (`--allow-non-empty` to override). AC-22 needs a defined behaviour; refusing is the safe default for a one-way operation.
- **D-7 — Testcontainers-style real PostgreSQL in tests**, not the in-memory provider. NFR-07's rationale; without it AC-03 through AC-12 are unverifiable.
- **D-8 — `timestamp without time zone`, legacy values preserved verbatim.** Converting legacy local timestamps to UTC needs the clinic's timezone, which no artifact records. Preserving the values makes the migration lossless and defers the interpretation to whoever can answer it. Npgsql's UTC-mapping default is overridden explicitly rather than left to provider version behaviour.
- **D-9 — Reconciliation is a first-class output with a failing exit code**, not console prose. NFR-05 and AC-21: a check that cannot fail is not a check.

## Risks & Mitigations

| # | Risk | Mitigation |
|---|---|---|
| **R-1** | Schema mistakes propagate into all 28 remaining backlog items | The six replayable fixtures (AC-10 … AC-15) are the gate; the schema is not "done" until each replays MATCH |
| **R-2** | Reconciliation validated only against synthetic data (A4) gives false confidence | AC-19 runs against synthetic data *and* the real export reconciliation is recorded as a named blocking pre-cutover gate, not quietly assumed complete |
| **R-3** | The money-type change makes GM-017 diverge, and a later replay reads it as a regression | Documented here and in the spec as CQ-008-sanctioned. `/specclaw:bf-replay` checks `decisions.md` for a sanctioning CQ; CQ-008's text explicitly covers it. GM-016 still matches |
| **R-4** | Adding an FK to `PatientMedicalInfo` "for correctness" silently breaks GM-019 | Spec A5 forbids it without a CQ. A test asserting the orphaning behaviour makes the breakage loud rather than silent |
| **R-5** | Legacy data violates constraints the new schema enforces (duplicate `Patient.Code` per GM-002, nulls in now-required columns) | FR-21's audit reports every such row; the tool reports failure rather than dropping data |
| **R-6** | Npgsql's `DateTime` UTC mapping silently rejects or shifts legacy timestamps | D-8 sets the mapping explicitly; a round-trip test pins it |
| **R-7** | `numeric(18,2)` proves too narrow for real legacy `Charge` strings | The FR-21 audit runs before cutover and surfaces the actual value range; precision is confirmed against the export, not assumed |
| **R-8** | Project renaming later (A2 unconfirmed) churns every subsequent item | Cheap to change now — flagged as an assumption awaiting a one-word answer |
| **R-9** | A test suite needing a real database is skipped in CI and rots | AC-23 requires the tests be CI-reproducible; `config.yaml`'s test command is wired as part of this item |

## Grounding sources

`specclaw-discover-context .specclaw list` returned nothing — the repository has no README, `CLAUDE.md`, or `docs/`, and `.specclaw/context.md` does not exist. Discovery contributed no docs, so this design is grounded in the `.specclaw/` artifacts read directly:

- **`.specclaw/analysis/rebuild-backlog.md` (BL-001, L179–193)** — scope, dependencies, and the harness-confirmed defect: *"`InitialCreate` creates `dbo.Patient`'s unique `IX_Code` ... and the later migration `202509030639057_Patient_Code_Unique` runs `CreateIndex` on the same column with no preceding `DropIndex`, so `Update()` against an empty database always fails."* → FR-16, AC-03, AC-04.
- **`.specclaw/analysis/rebuild-backlog.md` (L743)** — *"BL-001 (data layer/migration foundation) precedes everything else in the entire backlog — no other item's schema can exist before this one runs."* → sequencing.
- **`.specclaw/analysis/rebuild-backlog.md` (L512)** — the `Doctor` seed defect: *"EF discards the assigned GUID and SQL Server generates a different one at seed time ... booking an appointment from the legacy UI fails the `FK_dbo.Appointment_dbo.Doctor_DoctorId` constraint outright."* → FR-18, D-4, AC-16.
- **`.specclaw/analysis/decisions.md`** — CQ-002 (*"one PostgreSQL database and one EF Core application DbContext/schema ... share one controlled migration history instead of two independent migration pipelines"*), CQ-006, CQ-007, CQ-008 (*"Use a proper decimal money type in .NET and a fixed-precision numeric/decimal column in PostgreSQL"*), CQ-015, CQ-016, CQ-017, SQ-002, SQ-004, SQ-005, SQ-010, SQ-011, SQ-012 (*"Every intentional divergence from legacy behaviour must be tied to a decided CQ"* → R-3, R-4, spec A5).
- **`.specclaw/analysis/domain-model.md`** — entity fields and constraints (FR-04), the ER diagram (FR-05), and the two confirmed absences: *"`Appointment.PatientNameOrId` is a free-text `string`, not a `Guid` FK to `Patient`"* and the identity/domain schemas being *"linked only by sharing one physical database connection string ... not by any relational reference."*
- **`.specclaw/baseline/scenarios.md` + `fixtures/`** — GM-012, GM-019, GM-024 (persistence-layer delete behaviour), GM-039, GM-040 (seed permissions), GM-041 (`0`, `15.5`, `-5`), GM-016/GM-017 (`"10.50"` → `NON_INTEGER_CHARGE`) → AC-10 … AC-15, R-3, R-4. All 41 fixtures are captured at legacy commit `5ff87d3a`.
- **`.specclaw/baseline/seams.md` (L65)** — *"every one of these unguarded writes implies the rebuild needs an injectable clock ... none of `pending-questions.md`'s existing entries or `decisions.md`'s existing CQs/SQs cover this specifically"* → FR-13, D-8.
- **`.specclaw/baseline/error-map.md` (`NON_INTEGER_CHARGE`)** — *"Rebuild source: not yet mapped"* → FR-21 reuses the code; renaming is left to `/specclaw:bf-baseline`.
- **`.specclaw/analysis/module-map.md` (L132)** — the 16-entity roster and `Status` being unassigned to any module → FR-04, A7.
