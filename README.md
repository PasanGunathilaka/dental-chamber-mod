# Dental Management — Rebuild

A rebuild of a legacy AngularJS + ASP.NET Web API + SQL Server dental clinic
system, as React + TypeScript + MUI over an ASP.NET Core Web API and PostgreSQL
(SQ-001, SQ-002, SQ-006).

This repository contains two backlog items so far:

- **BL-001 — Data Layer Consolidation, Migration & Schema Setup**: the schema, its
  migration history, fresh-install seed data, and the one-time tool that moves the
  legacy database into it.
- **BL-020 — Register New Patient & Auto-Provision Bill**: the first feature slice
  — one transactional registration operation, `POST /api/patients`, and the first
  frontend.

Every later item in `.specclaw/analysis/rebuild-backlog.md` builds on these.

**The API does not boot outside Development yet.** BL-020 was built ahead of its
authentication and authorization dependencies, so those are dev-only stubs and the
host deliberately refuses to start without them. See "Dependency bypasses" below.

## Layout

| Project | What it holds |
|---|---|
| `src/DentalManagement.Domain` | Entities, enums, `IClock`, the registration contract, the pure code formatters. No package references beyond the BCL — that constraint is what keeps persistence concerns out of the domain. |
| `src/DentalManagement.Infrastructure` | The single `DentalDbContext`, per-entity EF configurations, the migration history, seeders, `ApplicationUser`, the patient-registration service and its code sequence. |
| `src/DentalManagement.Api` | Host: DI, environment configuration, `/health`, `POST /api/patients`, the permission-policy plumbing, and the dev-only auth stubs. |
| `src/DentalManagement.DataMigration` | One-time legacy SQL Server → PostgreSQL console tool. |
| `client` | React 19 + TypeScript + MUI v7 (Vite). The MUI theme carrying the legacy tokens, and the patient-registration screen. |
| `tests/DentalManagement.Infrastructure.Tests` | Schema, migration, captured-fixture replay, seeding, patient registration. Real PostgreSQL. |
| `tests/DentalManagement.Api.Tests` | Endpoint behaviour and the stub-scoping gate. Real PostgreSQL. |
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
| `DevelopmentAuth__AllowDevelopmentAuthenticationStub` | `true` registers the dev-only authentication and permission stubs. **Only honoured when the environment is Development**, and set only in `appsettings.Development.json`. See "Dependency bypasses". |

## Running

```bash
dotnet build DentalManagement.sln

# Fast tier — schema, fixture replay, seeding, patient registration (~25s)
dotnet test tests/DentalManagement.Infrastructure.Tests/DentalManagement.Infrastructure.Tests.csproj

# Fast tier — endpoint behaviour and the stub-scoping gate
dotnet test tests/DentalManagement.Api.Tests/DentalManagement.Api.Tests.csproj

# Slow tier — the migration tool end to end (~35s; first run pulls a ~1.7GB SQL Server image)
dotnet test tests/DentalManagement.DataMigration.Tests/DentalManagement.DataMigration.Tests.csproj
```

```bash
# Apply the schema to a database by hand
dotnet ef database update --project src/DentalManagement.Infrastructure
```

### The frontend

```bash
cd client
npm install
npm run dev     # http://localhost:5173 — the origin the API's CORS policy allows
npm run build   # tsc -b && vite build
npm test        # vitest
```

The registration screen is at `/patients/new`. It calls `POST /api/patients` at
`VITE_API_BASE_URL`, defaulting to `http://localhost:5000`.

## Dependency bypasses

BL-020 was built ahead of BL-002 (login/logout) and BL-007 (server-side
authorization) by explicit human decision, recorded in
`.specclaw/analysis/module-stubs.md` as `ST-002` and `ST-003`. Both are
`stub-interface` bypasses: `ICurrentUser` returns a fixed `admin@dev.local`/`Admin`
identity, and `IPermissionChecker` grants every request.

**Both are registered only inside one gate** in `src/DentalManagement.Api/Program.cs`:

```csharp
if (builder.Environment.IsDevelopment() && developmentAuthOptions.AllowDevelopmentAuthenticationStub)
```

Every other boot — including one where the flag is set but the environment is not
Development — throws at startup naming BL-002 and BL-007, *before* `builder.Build()`
runs. The host cannot start unprotected; it refuses to start at all. That is
deliberate, and `tests/DentalManagement.Api.Tests/StubScopingTests.cs` gates it.

The flag lives only in `appsettings.Development.json`. This mirrors the mechanism
already used for `AdminBootstrap__AllowDevelopmentDemoAccounts` rather than
inventing a second one.

**Consequence for verification:** every golden-master fixture verifying BL-020 is
stamped stub-tainted until `ST-002` and `ST-003` retire, even the ones whose seams
never touch a stub. Taint is stamped per backlog item, not per seam.

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

### From BL-020

- **Patient codes now come from a PostgreSQL sequence, not a row count, and no
  decided CQ sanctions that.** Legacy computed the code as
  `GetPatientViewModel().Count() + 1`, which is the exact mechanism that produced
  GM-002's duplicate-code defect. No captured fixture observes the *source* —
  GM-001/GM-004 take the sequence as an input and GM-003 only requires `P000001`
  first — so nothing breaks on replay. But it does change behaviour after a patient
  is deleted: legacy would reissue a code, a sequence will not. PQ-005 covers the
  *response* to a failed insert, not the *source* of the number. **SQ-012 requires
  every intentional divergence to be tied to a decided CQ; this one should be raised
  as a pending question rather than left resting on the spec** (spec Note N-1).
- **PQ-005 and PQ-008 are still OPEN,** so GM-002 and GM-011 remain PROVISIONAL and
  BL-020 cannot reach a clean replay PASS — expect `PASS-PENDING-DECISIONS`.
- **`/specclaw:bf-replay` has not been run for BL-020.** It belongs after
  `/specclaw:verify`, not inside the build. GM-001, GM-003 and GM-004 are the three
  fixtures expected to replay; GM-002 and GM-011 are out of this item's scope
  (spec A6).

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
