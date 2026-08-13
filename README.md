# Dental Management — Rebuild

A rebuild of a legacy AngularJS + ASP.NET Web API + SQL Server dental clinic
system, as React + TypeScript + MUI over an ASP.NET Core Web API and PostgreSQL
(SQ-001, SQ-002, SQ-006).

This repository currently contains **BL-001 — Data Layer Consolidation, Migration
& Schema Setup** only: the schema, its migration history, fresh-install seed data,
and the one-time tool that moves the legacy database into it. There are no feature
endpoints and no frontend yet — every later item in
`.specclaw/analysis/rebuild-backlog.md` builds on this.

## Layout

| Project | What it holds |
|---|---|
| `src/DentalManagement.Domain` | Entities, enums, `IClock`. No package references beyond the BCL — that constraint is what keeps persistence concerns out of the domain. |
| `src/DentalManagement.Infrastructure` | The single `DentalDbContext`, per-entity EF configurations, the migration history, seeders, `ApplicationUser`. |
| `src/DentalManagement.Api` | Host: DI, environment configuration, `/health`. No feature endpoints. |
| `src/DentalManagement.DataMigration` | One-time legacy SQL Server → PostgreSQL console tool. |
| `tests/DentalManagement.Infrastructure.Tests` | Schema, migration, captured-fixture replay, seeding. Real PostgreSQL. |
| `tests/DentalManagement.DataMigration.Tests` | The migration tool end to end. Real SQL Server + real PostgreSQL. |

## Prerequisites

- .NET SDK 10.0
- A reachable Docker daemon — the tests use Testcontainers to run real
  PostgreSQL 17 and SQL Server 2022. This is deliberate: the EF in-memory provider
  does not model cascade behaviour, unique-index rejection, check constraints, or
  `numeric` precision, and those are what most of this item's acceptance criteria
  turn on.
- `dotnet tool install --global dotnet-ef` for migration commands.

## Configuration

Nothing secret lives in `appsettings.json`. The connection string and the
production admin credentials come from environment configuration only, and the host
**fails at startup** rather than falling back to a default (spec FR-03, FR-19).

| Variable | Purpose |
|---|---|
| `ConnectionStrings__DentalManagement` | Target PostgreSQL. Required. |
| `DENTALMANAGEMENT_LEGACY_CONNECTIONSTRING` | Legacy SQL Server, for the migration tool. |
| `AdminBootstrap__UserName` / `AdminBootstrap__Password` | Production administrator. Both required unless demo accounts are enabled. |
| `AdminBootstrap__AllowDevelopmentDemoAccounts` | `true` seeds the known `superadmin`/`admin` demo accounts. **Local development only** — CQ-017 permits known credentials only in explicitly development seed data. |
| `Database__MigrateOnStartup` | `true` applies migrations and seeds on boot. Off by default so a rolling deployment cannot race itself. |

## Running

```bash
dotnet build DentalManagement.sln

# Fast tier — schema, fixture replay, seeding (~25s)
dotnet test tests/DentalManagement.Infrastructure.Tests/DentalManagement.Infrastructure.Tests.csproj

# Slow tier — the migration tool end to end (~35s; first run pulls a ~1.7GB SQL Server image)
dotnet test tests/DentalManagement.DataMigration.Tests/DentalManagement.DataMigration.Tests.csproj
```

```bash
# Apply the schema to a database by hand
dotnet ef database update --project src/DentalManagement.Infrastructure
```

## Migration runbook

**Order matters. Migrate first, seed second.**

The fresh-install seeder creates a default doctor (`DR001` / "Dental Doctor"), and
the legacy database supplies its own row with the same code and name. Seeding first
therefore leaves two identical-looking doctors — neither legacy nor the rebuild
indexes `Doctor.Code` uniquely, so nothing rejects it. A migration is not a fresh
install, so the seed belongs afterwards, where its own guards fill in only what
legacy did not supply. `Migrating_onto_an_already_seeded_target_succeeds_but_duplicates_the_default_doctor`
pins what the other order actually does.

1. **Create an empty PostgreSQL database** and apply the schema:
   ```bash
   dotnet ef database update --project src/DentalManagement.Infrastructure
   ```

2. **Dry-run the migration** to see the audit before touching anything:
   ```bash
   dotnet run --project src/DentalManagement.DataMigration -- \
     --source "<legacy-sql-server>" \
     --target "<postgres>" \
     --dry-run --report-dir ./migration-reports
   ```
   Read `migration-reports/audit-report.json`. Every legacy value the new schema
   cannot accept is listed rather than coerced — unparseable `Charge` strings
   (`NON_INTEGER_CHARGE`), genders outside Male/Female/Others, statuses belonging to
   another entity's set, duplicate `Patient.Code` values, nulls in now-required
   columns, and orphaned condition tags. **Findings marked blocking mean those rows
   will not migrate.** Resolve them in the legacy database, or accept the exclusions
   knowingly.

3. **Run the migration** for real:
   ```bash
   dotnet run --project src/DentalManagement.DataMigration -- \
     --source "<legacy-sql-server>" \
     --target "<postgres>" \
     --report-dir ./migration-reports
   ```
   The tool refuses a target that already holds domain data unless
   `--allow-non-empty` is passed, and writes inside one transaction, so a failure
   leaves the target exactly as it was. **Exit code 0 only when every reconciliation
   check agreed** — check `reconciliation-report.json`.

4. **Seed** the rebuild-only catalog entries (routes legacy never had, and the
   default doctor only if legacy supplied none):
   ```bash
   Database__MigrateOnStartup=true dotnet run --project src/DentalManagement.Api
   ```

5. **Before production cutover**, run steps 2–3 against a copy of the *real*
   production export and review both reports. See "Outstanding" below.

## Decisions worth knowing before you change this schema

Each of these looks like something to tidy up, and each is load-bearing. Every
intentional divergence from legacy behaviour has to be tied to a decided CQ
(SQ-012), so changing one without a decision breaks a captured golden-master
fixture.

- **`PatientMedicalInfo` has no foreign keys, on purpose.** GM-019 pins that
  deleting a patient leaves its rows orphaned. A cascading FK deletes them; a
  restricting FK makes the patient delete fail, which also breaks GM-012. Raise a
  CQ before adding one.
- **`Prescription.TotalDiscountAmount` has no floor at zero.** GM-041 pins `-5`.
- **`DiscountPercent` has no 0–100 range constraint.** GM-005 captures the server
  accepting `150` and `-25`. Server-side enforcement is CQ-011's work in a later
  item.
- **`Appointment.PatientNameOrId` is free text, not a `Patient` FK.** It exists so
  staff can book a slot for someone not yet registered.
- **`Created`/`LastUpdate` are `timestamp without time zone`, carried across
  verbatim.** Nothing on record says what timezone the clinic's legacy timestamps
  are in, so converting them would be a guess (spec A8).
- **`MedicalService.Charge` is now `decimal`, and that is a deliberate divergence.**
  GM-017 captured legacy rejecting `"10.50"` outright with a `FormatException`;
  CQ-008 decided that is a defect to fix. Expect GM-017 to diverge on replay — it
  is sanctioned.
- **No `Status` table.** CQ-006 replaced it with four typed per-entity enums that
  keep the legacy integer values, so migrated data keeps its meaning.

## Outstanding

- **A real production export has not been reconciled.** The migration tooling is
  validated against the synthetic legacy database in
  `tests/DentalManagement.DataMigration.Tests/SyntheticLegacy/`, which stands in for
  the export BL-001 names as a verification input but which this repository does not
  contain (spec A4). **Running steps 2–3 against a genuine export, and reviewing
  both reports, is a blocking pre-cutover gate that has not yet been done.**
- **`error-map.md`'s `NON_INTEGER_CHARGE` still reads "Rebuild source: not yet
  mapped",** and after CQ-008 its condition is narrower than legacy's — `"10.50"` is
  no longer an error. The audit reuses the existing code deliberately; renaming it
  belongs to `/specclaw:bf-baseline`.
- **No ADR records the injectable clock.** `seams.md` flagged it as a genuine open
  item that no PQ or CQ covers. `IClock` exists because this item owns the timestamp
  writes, but the decision is unwritten.

## Human sign-off — the live-build confirmation BL-001 requires

BL-001 names two verification inputs a human must supply. Record them here.

> "Confirmation from a human that the from-scratch EF Core migration chain actually
> builds and seeds cleanly end-to-end (the specific defect above was found by
> attempting exactly this, not by static reading, so the fix itself needs the same
> live-build verification, not just a fixture)."

`MigrationTests` automates the check — it creates a genuinely empty database,
applies the chain, and asserts no index is created twice, which is precisely how the
legacy chain failed. That is necessary but is not the human confirmation BL-001
asks for.

| Verification input | Status | Confirmed by | Date |
|---|---|---|---|
| From-scratch migration chain builds and seeds cleanly, run live against a real database | ☐ Awaiting human sign-off — live run performed, evidence below | | |
| Reconciliation validated against a full legacy production/staging export | ☐ Not confirmed — no export available (spec A4) | | |

**Evidence from the live run** (build, 2026-08-12). A PostgreSQL 17 container was
created empty, the API was started with `Database__MigrateOnStartup=true`, and the
resulting database was inspected directly:

| Observation | Result |
|---|---|
| `/health` | `Healthy` (HTTP 200) |
| Tables across `public` + `identity` | 21 |
| Roles seeded | 8 |
| Resources seeded | 18 |
| Permission grants | 16 — exactly the 16 private resources, i.e. DR-016 holds |
| Doctors | 1 |
| `Status` table present | 0 — CQ-006's typed enums replaced it |

This is the run BL-001 asks for, and it succeeded. The sign-off line is still
a human's to give, so it stays unticked until someone puts their name to it.
