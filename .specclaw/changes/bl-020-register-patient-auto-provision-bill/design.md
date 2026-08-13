# Design: BL-020 — Register New Patient & Auto-Provision Bill

**Change:** bl-020-register-patient-auto-provision-bill
**Created:** 2026-08-13

## Technical Approach

Three layers, added to a repository that currently has a schema and nothing above it.

**1. A registration service at the seam GM-003 was captured at.** GM-003's seam layer is `service`, and
`/specclaw:bf-replay` *"refuses to replay a fixture at any seam layer other than the one it was captured
at."* So the whole registration decision — generate code, insert patient, generate bill code, insert
bill, commit — lives in one injectable service that a test can call **without** an HTTP request. The
controller is a thin translation layer over it. Putting this logic in the controller would make GM-003
unreplayable, which is the single strongest constraint on this item's shape.

**2. Pure code formatters in the Domain project.** GM-001 and GM-004 were captured against
`HelperRequestModel.GetThisPatientCode(string)` and `GenerateBillCode(string, string)` — pure functions
at the `pure-function` layer. The rebuild's equivalents are static methods on a Domain type with no
dependencies, so those two fixtures replay against Domain alone. This also keeps the sequence *source*
(a database concern) cleanly separated from the code *format* (a fixture-pinned concern).

**3. Two abstractions and their throwaway implementations.** `ICurrentUser` and `IPermissionChecker`
are the seams BL-002 and BL-007 will fill. They ship here as interfaces plus dev-only implementations
registered behind the same environment-and-flag gate the repository already uses to keep known demo
credentials out of production.

The frontend is a separate Vite/React/TypeScript/MUI application under `client/`, deliberately minimal:
a theme carrying the legacy tokens, one route, one form, one API call.

## Architecture

```
client/                                  ← new. Vite + React 19 + TS + MUI
  src/theme.ts                             TK-001/TK-002/TK-003 → MUI theme
  src/api/patients.ts                      POST /api/patients
  src/features/patients/RegisterPatient…   SCR-003 new-patient panel

src/DentalManagement.Domain               ← BCL-only. NFR-03 holds.
  Abstractions/IPatientRegistrationService  contract + result records
  Patients/PatientCodeFormatter             pure — GM-001 replays here
  Patients/BillCodeFormatter                pure — GM-004 replays here

src/DentalManagement.Infrastructure
  Patients/PatientRegistrationService       the transaction — GM-003 replays here
  Patients/PatientCodeSequence              the collision-safe sequence source
  Persistence/Migrations/…AddPatientCodeSequence

src/DentalManagement.Api
  Controllers/PatientsController            POST /api/patients
  Contracts/RegisterPatientRequest|Response
  Authorization/PermissionRequirement       + handler → IPermissionChecker
  DevelopmentOnly/DevelopmentAuthOptions    the gate
  DevelopmentOnly/StubCurrentUser           ST-002
  DevelopmentOnly/StubAuthenticationHandler ST-002
  DevelopmentOnly/StubPermissionChecker     ST-003

tests/DentalManagement.Api.Tests           ← new. WebApplicationFactory + Testcontainers
```

Request path in Development:

```
POST /api/patients
  → StubAuthenticationHandler        (ST-002 — authenticates everyone as admin@dev.local/Admin)
  → PermissionAuthorizationHandler   → IPermissionChecker.CheckAsync("Admin", "root.patient-create")
                                       (ST-003 — grants)
  → PatientsController.Register      model validation → ProblemDetails on failure
  → IPatientRegistrationService      ┐
      BEGIN TRANSACTION              │  ← the seam GM-003 replays against
        nextval(patient_code_seq)    │
        PatientCodeFormatter.Format  │  ← GM-001
        INSERT Patient               │
        BillCodeFormatter.Format     │  ← GM-004
        INSERT Prescription (Active) │
      COMMIT                         ┘
  → 201 { id, code, billCode }
```

In any non-Development boot the first two steps do not exist: the `DevelopmentOnly` registrations are
never executed, and startup throws rather than serving an unauthenticated endpoint.

## File Changes Map

| File | Action | Description |
|------|--------|-------------|
| `src/DentalManagement.Domain/Patients/PatientCodeFormatter.cs` | Create | Pure `Format(long sequence)` → `"P" + sequence.ToString("D6")`. `D6` is a minimum width, matching GM-001's no-truncation behaviour exactly. |
| `src/DentalManagement.Domain/Patients/BillCodeFormatter.cs` | Create | Pure `Format(string patientCode, long sequence)` → `"BILL" + sequence.ToString("D3") + "-" + patientCode`. |
| `src/DentalManagement.Domain/Abstractions/IPatientRegistrationService.cs` | Create | `RegisterAsync(NewPatient, CancellationToken)` → `RegistrationResult` (success carries ids and codes; failure carries a reason). BCL types only. |
| `src/DentalManagement.Domain/Abstractions/ICurrentUser.cs` | Create | `UserName`, `Role`. ST-002's seam. |
| `src/DentalManagement.Domain/Abstractions/IPermissionChecker.cs` | Create | `CheckAsync(string role, string resourceRoute, CancellationToken)`. ST-003's seam. |
| `src/DentalManagement.Infrastructure/Patients/PatientRegistrationService.cs` | Create | The explicit transaction. Timestamps via `IClock`. |
| `src/DentalManagement.Infrastructure/Patients/PatientCodeSequence.cs` | Create | `NextAsync()` — reads the PostgreSQL sequence. |
| `src/DentalManagement.Infrastructure/Persistence/DentalDbContext.cs` | Modify | `HasSequence<long>("patient_code_seq")` starting at 1. |
| `src/DentalManagement.Infrastructure/Persistence/Migrations/*_AddPatientCodeSequence.cs` | Create | Additive migration. Creates the sequence only — touches no existing table or index. |
| `src/DentalManagement.Infrastructure/DependencyInjection.cs` | Modify | Register `IPatientRegistrationService`, `PatientCodeSequence`. |
| `src/DentalManagement.Api/Controllers/PatientsController.cs` | Create | `[Authorize]`, `[Permission("root.patient-create")]`, `POST`. |
| `src/DentalManagement.Api/Contracts/RegisterPatientRequest.cs` | Create | Name/Age/Gender/Phone/Email/Address/Note + validation attributes mirroring `PatientConfiguration`. No `Code`, no `Id`. |
| `src/DentalManagement.Api/Contracts/RegisterPatientResponse.cs` | Create | `id`, `code`, `billCode`. |
| `src/DentalManagement.Api/Authorization/PermissionRequirement.cs` + `PermissionAuthorizationHandler.cs` + `PermissionAttribute.cs` | Create | Policy plumbing that calls `IPermissionChecker`. Unchanged when BL-007 lands. |
| `src/DentalManagement.Api/DevelopmentOnly/DevelopmentAuthOptions.cs` | Create | `AllowDevelopmentAuthenticationStub`, default `false`, section `DevelopmentAuth`. |
| `src/DentalManagement.Api/DevelopmentOnly/StubCurrentUser.cs` | Create | **ST-002.** `admin@dev.local` / `Admin`. |
| `src/DentalManagement.Api/DevelopmentOnly/StubAuthenticationHandler.cs` | Create | **ST-002.** Authenticates every request; attaches a `stub=ST-002` claim. |
| `src/DentalManagement.Api/DevelopmentOnly/StubPermissionChecker.cs` | Create | **ST-003.** Grants unconditionally. |
| `src/DentalManagement.Api/Program.cs` | Modify | Controllers, ProblemDetails, CORS for the Vite dev origin, the environment-and-flag gate, the non-Development startup throw. |
| `src/DentalManagement.Api/appsettings.Development.json` | Modify | `DevelopmentAuth:AllowDevelopmentAuthenticationStub: true`. Development file only. |
| `tests/DentalManagement.Api.Tests/**` | Create | New xUnit project: `WebApplicationFactory` over a Testcontainers PostgreSQL, reusing `PostgresContainerFixture`'s pattern. |
| `tests/DentalManagement.Infrastructure.Tests/PatientRegistrationTests.cs` | Create | GM-003 replay, rollback, concurrency. Service seam — no HTTP. |
| `tests/DentalManagement.Infrastructure.Tests/CodeFormatterTests.cs` | Create | GM-001 and GM-004 replay. Pure — no database. |
| `client/**` | Create | Vite + React + TS + MUI app, theme, screen, API client, Vitest + RTL tests. |
| `DentalManagement.sln` | Modify | Add `DentalManagement.Api.Tests`. |
| `.gitignore` | Modify | `client/node_modules`, `client/dist`. |
| `README.md` | Modify | Layout table, how to run the frontend, and the stub-scoping note. |

## Data Model Changes

One additive migration, and no change to any existing table, column, index, or constraint.

- **`patient_code_seq`** — a PostgreSQL sequence, `START 1 INCREMENT 1`, declared via
  `modelBuilder.HasSequence<long>`. It is the collision-safe source spec A4/FR-08 requires. `nextval`
  is transactional-safe and non-blocking: two concurrent registrations get two different values without
  either waiting on the other, which is what AC-06 asserts.
- **No bill sequence.** BL-020 only ever creates a patient's *first* bill, so the bill sequence is
  always `1` and `BillCodeFormatter.Format(code, 1)` yields `"BILL001-P000001"`. Defining a general
  per-patient bill-numbering rule would be inventing BL-027's behaviour (its close/reopen workflow is
  what creates second bills). The formatter already takes the sequence as a parameter, so BL-027
  supplies its own source without touching this code.
- BL-001's AC-04 — *"no migration step creates an index that an earlier step already created"* — still
  holds: this migration creates a sequence and nothing else.

## API Changes

**`POST /api/patients`** — new. The only endpoint in this item.

```
Request   { name, age, gender?, phone?, email?, address?, note? }
201       { id, code, billCode }          e.g. { …, "P000001", "BILL001-P000001" }
400       ProblemDetails                  validation, field-scoped
401 / 403                                 no authenticated caller / permission denied
500       ProblemDetails                  the write did not persist — never a 2xx (FR-03)
```

`Code` and `Id` are absent from the request contract entirely, so DR-001's *"never client-supplied"*
is enforced by the type rather than by a check that could be removed.

Legacy's route was `POST api/PatientCreate/Create` taking the `Patient` entity. Both change (spec A5).

## Key Decisions

- **D-1 — The registration logic is a service in Infrastructure, not the controller.** Forced by
  GM-003's captured seam layer (`service`). A controller-only implementation would leave the fixture
  unreplayable and this item without its central golden-master check.
- **D-2 — Formatters are pure statics in Domain.** Same reasoning for GM-001/GM-004's `pure-function`
  layer, and it keeps the fixture-pinned format independent of where the number comes from. `"D6"`/`"D3"`
  are exactly minimum-width padding — `9999999.ToString("D6")` is `"9999999"`, which is why GM-001's
  8- and 9-character cases fall out without a special case.
- **D-3 — An explicit transaction, not EF's implicit per-`SaveChanges` one.** The sequence read and both
  inserts are one unit, and BL-022 will extend this same block. BL-020's acceptance basis asks for
  *"one real database transaction"* by name.
- **D-4 — The stub gate reuses the repository's existing mechanism.** `references/stub-discipline.md`:
  *"Whichever mechanism the repo already uses for this is the one to use. Inventing a new isolation
  mechanism for a stub is itself a smell."* `AdminBootstrapOptions.AllowDevelopmentDemoAccounts` is
  that mechanism — a flag defaulting to `false`, absent from `appsettings.json`, whose production path
  throws rather than falling back. `DevelopmentAuthOptions` is the same shape, additionally requiring
  `IsDevelopment()`, so the flag alone cannot open the door.
- **D-5 — A non-Development boot fails at startup rather than starting unprotected.** The alternative —
  registering a throwing implementation — starts the host and fails per request, which looks like a
  runtime bug instead of an unfinished dependency. The startup message names BL-002 and BL-007 so the
  cause is unambiguous.
- **D-6 — Controllers, not minimal APIs.** `[ApiController]` gives automatic `ProblemDetails` on model
  validation (FR-12) and attribute-based `[Authorize]`/`[Permission]` declarations (FR-13) that BL-007
  inherits untouched. The app will end up with roughly thirty endpoints across MOD-001..MOD-004.
- **D-7 — The rollback test forces a real unique-index violation.** Pre-inserting a `Prescription` whose
  `Code` equals the one the next registration will generate makes the second insert fail at the database,
  inside the transaction. That exercises the real constraint rather than a mock, consistent with NFR-01
  and with BL-001's reason for using Testcontainers at all.
- **D-8 — The frontend lives at `client/`, outside `src/`.** `src/` is the .NET tree governed by
  `Directory.Build.props` / `Directory.Packages.props`; a Node project there invites confusion. It also
  mirrors the legacy repository's own `Client/` naming.
- **D-9 — A third test project rather than folding API tests into the existing two.** The existing
  projects are scoped by what they boot (`Infrastructure.Tests` → DbContext; `DataMigration.Tests` →
  the migration tool). API tests boot a web host, and mixing them in would drag `WebApplicationFactory`
  into the schema suite the fast tier depends on.

## Risks & Mitigations

- **R-1 — A4's sequence source is a behavioural change with no sanctioning CQ.** SQ-012 requires
  divergences to be tied to a decided CQ, and PQ-005 covers the response to a failed insert, not where
  the number comes from. *Mitigation:* no captured fixture observes it (GM-001/GM-004 take the sequence
  as an input; GM-003 only needs `P000001` first), so nothing breaks on replay — but spec Note N-1
  recommends raising a pending question so the decision is recorded rather than inferred from this
  design.
- **R-2 — Frontend scaffolding is unbounded work if left unscoped.** *Mitigation:* FR-16–FR-19 cap it at
  theme, one route, one form, one API call. No navigation shell, no login screen, no route guards —
  those belong to BL-002/BL-007 and to the items that own their screens.
- **R-3 — A stub reaching production is the failure mode the whole bypass mechanism exists to prevent.**
  *Mitigation:* AC-13 makes it a gated, checkable criterion — a non-Development boot must throw, and a
  test asserts the stub types are absent from the service collection. Not an assertion in prose.
- **R-4 — Getting the seam layer wrong silently costs the item its main fixture.** *Mitigation:* D-1 and
  D-2, plus tests that call the service and the formatters directly, never through HTTP. If a test needs
  a web host to exercise registration, the seam is in the wrong place.
- **R-5 — A third Testcontainers project lengthens the build.** *Mitigation:* the API suite reuses
  PostgreSQL only (no SQL Server), so it joins the ~25s fast tier rather than the ~35s slow one.
- **R-6 — `Patient.Code`'s 8-character ceiling collides with FR-05's no-truncation rule at sequence
  100,000,000.** *Mitigation:* recorded as an edge case, surfaced honestly through FR-03 if ever hit,
  and not designed around — GM-001 pins the formatter's output, and legacy never reached it either.

## Grounding sources

Documents discovered by `specclaw-discover-context` and actually used, with the lines they rest on.

- **`README.md`** — establishes that this item is greenfield above the schema: *"There are no feature
  endpoints and no frontend yet — every later item in `.specclaw/analysis/rebuild-backlog.md` builds on
  this."* Used for the Overview and for A1.
- **`README.md`** (Prerequisites) — the reason NFR-01 forbids the in-memory provider: *"the EF in-memory
  provider does not model cascade behaviour, unique-index rejection, check constraints, or `numeric`
  precision, and those are what most of this item's acceptance criteria turn on."* Used for NFR-01, D-7,
  and R-5.
- **`README.md`** (Configuration) — the project's secret-handling rule, carried into NFR-05 and D-4:
  *"Nothing secret lives in `appsettings.json`. The connection string and the production admin
  credentials come from environment configuration only, and the host **fails at startup** rather than
  falling back to a default."*
- **`README.md`** (Outstanding / "load-bearing" list) — the constraint that stopped this design from
  tidying anything up in passing: *"Every intentional divergence from legacy behaviour has to be tied to
  a decided CQ (SQ-012), so changing one without a decision breaks a captured golden-master fixture."*
  Used for R-1 and Note N-1.
- **`.specclaw/learnings.md` [L1]** — *"When a clean-architecture design places a framework-derived type
  in a dependency-free project, check the base type's package requirement at design time."* Used for
  NFR-03 and for keeping `ICurrentUser`/`IPermissionChecker` plain interfaces in Domain while the
  authentication handler stays in Api.
