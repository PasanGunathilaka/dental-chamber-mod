# Spec: BL-020 — Register New Patient & Auto-Provision Bill

**Change:** bl-020-register-patient-auto-provision-bill
**Created:** 2026-08-13
**Status:** 🟡 Draft

## Overview

Build the first feature slice of the rebuild: registering a patient and, in the same authoritative
server-side operation, opening that patient's first bill. This is BL-020 in
`.specclaw/analysis/rebuild-backlog.md` (module MOD-001, Gate CLEAR) — MOD-001's foundational item,
which the backlog's own sequencing rationale describes as the one *"every other MOD-001 item operates
on a Patient/Prescription pair this item creates."*

The repository today holds BL-001's output only. `README.md` states it plainly: *"There are no feature
endpoints and no frontend yet — every later item in `.specclaw/analysis/rebuild-backlog.md` builds on
this."* `src/DentalManagement.Api/Program.cs` carries the same note in code. So this item delivers
three firsts: the first application service, the first HTTP endpoint, and the first frontend.

It also closes two decided defects and carries one already-made schema decision into its first write
path:

- **DR-002's missing transaction.** Legacy creates the `Patient`, then the `Prescription`, with nothing
  wrapping them — *"if the second write fails, the patient exists with zero bills."* BL-020's own
  acceptance basis makes atomicity the criterion: *"the rebuild wraps both writes in one real database
  transaction, closing this specific gap — new server-side design work, not something a legacy fixture
  can attest to."*
- **DR-001's swallowed failure.** The controller never inspects `Add()`'s result. `GM-002` captures a
  duplicate `P000002` persisting behind `http_status: 200`.
- **CQ-007's typed `Gender`.** BL-001 already delivered the enum and the `CK_Patient_Gender` check
  constraint; this is the first code path that must map user input onto it.

### Assumptions (Rule 1 — stated, not silently chosen)

The proposal raised five open questions and was approved with "continue" rather than answers, so each
is recorded here as a named assumption.

| # | Assumption | Basis | Cost if wrong |
|---|---|---|---|
| **A1** | BL-020 scaffolds the React + TypeScript + MUI frontend | Proposal OQ-1. BL-001's own spec assumed *"frontend scaffolding belongs to BL-002"* — but BL-002 is bypassed here (ST-002), so the first item needing a screen inherits it. BL-020's `UI fidelity: THEME-ONLY` line requires SCR-003 to exist. | Medium — the scaffold moves to another item; the backend half is unaffected |
| **A2** | PQ-005 is built to its proposed default: an accurate success/failure result, never 200 for a row that did not persist | `pending-questions.md` PQ-005 candidate (a). *"An API returning 200 OK for a write that did not happen is a stronger presumption of a bug than an unverified 'Code is never edited' assumption."* | Low — the alternative is preserving a defect; reversing means relaxing FR-03 |
| **A3** | PQ-008 is built to its proposed default: `Status == Active` is what "current bill" means | PQ-008 candidate (a). BL-020 only *creates* the Active bill, so this affects assertions and invariant wording, not the write path. | Low — no read path in this item |
| **A4** | Patient code sequence is a database sequence, not legacy's `Count() + 1` | `Count() + 1` is the mechanism that produces GM-002's collision. No fixture observes the *source* — GM-001/GM-004 are pure functions taking the sequence as an input, and GM-003 only requires the first patient in an empty database to be `P000001`. | Medium — see Note N-1; this is a behavioural change with no sanctioning CQ |
| **A5** | Endpoint is `POST /api/patients` taking a `RegisterPatientRequest` DTO, never the entity | Legacy was `POST api/PatientCreate/Create` taking `Patient` directly. CQ-012 pushes toward explicit contracts; DR-001 forbids a client-supplied `Code`. | Low — a route rename |
| **A6** | BL-020's replayable fixture surface is **GM-001, GM-003, GM-004**; GM-002 and GM-011 defer | See "Verification surface" below | Medium — wrong fixtures scoped into `/specclaw:bf-replay` |
| **A7** | The bill sequence is **per patient** (a patient's first bill is `BILL001-<code>`) | DR-003's `"BILL" + zero-padded sequence + "-" + PatientCode` reads as patient-scoped, and GM-003's own patient is `P000001` with a bill matching the pattern. GM-004 is a pure function over arbitrary inputs and pins no scope. | Low — no fixture pins it |
| **A8** | Frontend tests are component-level (Vitest + React Testing Library); no browser E2E harness in this item | No frontend test infrastructure exists to extend, and no backlog item names E2E | Low — E2E can be added later without reshaping the screen |

### Verification surface (why A6)

BL-020's `**Verification:**` line cites GM-001, GM-002, GM-003, GM-004, GM-011. That join is mechanical
— bash matched every fixture whose pinned rules appear in this item's acceptance basis. Three replay
against what this item delivers; two cannot, and for reasons specific to their own arrange steps:

| Fixture | Seam layer | What it pins | This item |
|---|---|---|---|
| GM-001 | pure-function | `"P000001"`, `"P999999"` (7), `"P9999999"` (8), `"P99999999"` (9) — the pad is a minimum width, not a truncating format | **Replayable** — FR-06 keeps the formatter a pure function |
| GM-004 | pure-function | `"BILL001-P000001"`, `"BILL999-P000001"`, `"BILL1000-P000001"` | **Replayable** — same |
| GM-003 | service | Patient `P000001` created, `prescription_created: true`, `status_id: 5`, bill code matches the pattern, `total_due: 0` | **Replayable** — FR-01's registration service is the equivalent seam at the same layer |
| GM-002 | service | Duplicate-code insert returns 200 with `patient_b_persisted: true` | **Deferred.** Its arrange step requires `PUT api/PatientCreate/Update` to edit a patient's `Code` — the patient-edit path, which is BL-023, not this item. There is no way to construct the collision through BL-020's own surface, since FR-08 never accepts a client-supplied `Code`. Also PROVISIONAL on PQ-005. |
| GM-011 | service | `PatientController.Get()`'s `.Last()` and `PrescriptionController.GetPatientCurrentPrescription`'s `.LastOrDefault(x => x.StatusId == 5)` disagree | **Deferred.** Both seams are read paths this item does not build — the first is BL-021's grid, the second a prescription read. Also PROVISIONAL on PQ-008. |

Consequence: `/specclaw:bf-replay` for this change is expected to report **PASS on GM-001, GM-003,
GM-004**, with GM-002 and GM-011 out of scope rather than failing. See Note N-2 on taint.

## Requirements

### Functional Requirements

**The registration operation**

- **FR-01** — One application service performs the whole registration: generate the patient code, insert
  the `Patient`, generate the bill code, insert the `Prescription`, **inside a single explicit EF Core
  transaction**. If either write fails, neither persists. *(DR-002, BL-020 acceptance basis)*
- **FR-02** — The auto-provisioned `Prescription` is created with `Status = BillStatus.Active`, its
  `PatientId` set to the new patient, and every monetary field — `TotalCharge`, `DiscountPercent`,
  `DiscountAmount`, `FixedDiscount`, `TotalPayable`, `TotalPaid`, `TotalDue` — at `0`. *(DR-002, GM-003)*
- **FR-03** — The operation returns an explicit result distinguishing success from failure. It never
  reports success for a write that did not persist, and the endpoint never returns 2xx for one.
  *(PQ-005/A2, closing GM-002's defect)*
- **FR-04** — `Created`/`LastUpdate` on both rows are written through the existing `IClock` abstraction
  (`src/DentalManagement.Domain/Abstractions/IClock.cs`), not `DateTime.Now`, so replay can pin time.
  *(BL-001 FR-13 precedent)*

**Code generation**

- **FR-05** — Patient code is `"P"` followed by the sequence value left-zero-padded to a **minimum** of
  6 digits. A value wider than 6 digits is not truncated: `1 → "P000001"`, `999999 → "P999999"`,
  `9999999 → "P9999999"`, `99999999 → "P99999999"`. *(DR-001, GM-001)*
- **FR-06** — Bill code is `"BILL"` + the bill sequence left-zero-padded to a **minimum** of 3 digits +
  `"-"` + the patient code, with the same no-truncation rule: `(P000001, 1) → "BILL001-P000001"`,
  `(P000001, 1000) → "BILL1000-P000001"`. *(DR-003, GM-004)*
- **FR-07** — Both formatters are **pure functions** whose only variable inputs are the sequence value
  (and, for the bill code, the patient code) — no database access, no clock, no ambient state. This is
  what lets GM-001 and GM-004 replay at the pure-function layer they were captured at.
- **FR-08** — The patient sequence source is collision-safe under concurrent registration and yields
  `1` for the first patient in an empty database. Codes are never client-supplied and never derived
  from a row count. *(DR-001, GM-003, A4)*

**Contract and validation**

- **FR-09** — `POST /api/patients` accepts a `RegisterPatientRequest` carrying only Name, Age, Gender,
  Phone, Email, Address, Note. `Code` and `Id` are not accepted from the client on any path; supplying
  them changes nothing. *(DR-001, A5)*
- **FR-10** — `Gender` is accepted as one of `Male`, `Female`, `Others`, or omitted. Any other value is
  rejected with 400 before the write — it never reaches the `CK_Patient_Gender` check constraint.
  *(CQ-007)*
- **FR-11** — Request validation mirrors the constraints already configured in
  `PatientConfiguration.cs`: Name required and ≤ 30, Phone ≤ 30, Email ≤ 100, Address ≤ 200, Note ≤ 500.
  Legacy's *minimum* lengths are not reintroduced — BL-001 deliberately left them out of the database,
  and adding them here would reject rows the migration carries.
- **FR-12** — Validation and failure responses are RFC 9457 `ProblemDetails`. A success response
  carries the created patient's `Id`, `Code`, and the bill's `Code`.
- **FR-13** — The endpoint declares its real `[Authorize]` attribute and its real permission-policy
  requirement naming resource route `root.patient-create` — already present in `SeedCatalog.Resources`
  as `IsPublic: false`. The declaration does not change when BL-002 and BL-007 land. *(CQ-013)*

**Bypass seams**

- **FR-14** — `ICurrentUser` and `IPermissionChecker` abstractions are defined in this item, shaped so
  BL-002 and BL-007 replace only their implementations. *(ST-002, ST-003)*
- **FR-15** — The dev-only implementations of both are registered **only** when the host environment is
  Development **and** an explicit configuration flag opts in. Any other boot fails at startup with a
  message naming BL-002 and BL-007. *(stub-discipline: dev/test scope only)*

**Frontend**

- **FR-16** — A React + TypeScript + Material UI application exists in the repository and builds from a
  clean clone with no manual steps. *(SQ-001, SQ-006, CQ-023, A1)*
- **FR-17** — A centralized MUI theme carries the token values of TK-001 (body background `#EBEBEB`,
  body text `#333`, dropdown-menu background `#006a4e`, form-control background `#f5f5f5`), TK-002
  (`"Helvetica Neue", Helvetica, Arial, sans-serif` at 14px), and TK-003 (navbar `#218283`, per CQ-004's
  decision resolving the contested value). *(CQ-023, CQ-004)*
- **FR-18** — SCR-003's `new-patient` state exists: a centered panel titled "New Patient" with a vertical
  form — Name, Age, Gender (select: Male/Female/Others), Phone, Email, Address (textarea), Note
  (textarea), Save. THEME-ONLY fidelity — the token values are reproduced, the Bootstrap grid geometry
  is not. *(CQ-023, ui-inventory.md SCR-003)*
- **FR-19** — The screen calls `POST /api/patients` and surfaces both outcomes: success shows the new
  patient code and bill code; a validation failure shows the server's own field messages rather than a
  generic error.

### Non-Functional Requirements

- **NFR-01** — Atomicity, unique-index rejection, and the check constraint are verified against a real
  PostgreSQL instance via Testcontainers, never the EF in-memory provider — the same discipline BL-001
  established and `README.md` justifies: *"the EF in-memory provider does not model cascade behaviour,
  unique-index rejection, check constraints, or `numeric` precision."*
- **NFR-02** — Concurrent registrations produce distinct patient codes. A test issuing N simultaneous
  registrations asserts N distinct codes and N patients.
- **NFR-03** — `DentalManagement.Domain` keeps no package reference beyond the BCL. Per learning L1,
  a framework-derived type placed there is a design error caught late — the stub abstractions are
  plain interfaces, and anything touching ASP.NET Core lives in Api or Infrastructure.
- **NFR-04** — `TreatWarningsAsErrors` stays on for the .NET build; the frontend build completes with no
  TypeScript errors.
- **NFR-05** — No credential, connection string, or stub configuration value is committed outside
  development configuration. The existing rule holds: *"Nothing secret lives in `appsettings.json`."*
- **NFR-06** — The frontend is responsive at desktop and tablet widths and targets WCAG AA for the new
  form — every input labelled, errors associated with their field. *(SQ-008)*

## Acceptance Criteria

Each criterion must pass for the change to be considered complete.

Labels: `[real]` — verified against real behaviour. `[stub: ST-###]` — verified only against what that
stub fakes. Criteria exercised through the HTTP endpoint are reachable **only because** the dev
authentication handler and the always-granting permission checker are in the tree, so they carry the
stub label even where the write path underneath them is real.

- [ ] **AC-01** `[real]` — Registering a patient against an empty database creates exactly one `Patient`
  with `Code = "P000001"` and exactly one `Prescription` for it with `Status = Active` and `TotalDue = 0`.
  *(FR-01, FR-02)*
- [ ] **AC-02** `[real]` — **GM-003 replays MATCH** at the service seam: `prescription_created: true`,
  `status_id: 5`, `code_format_matches_bill_pattern: true`, `total_due: 0`. *(FR-01, FR-02, A6)*
- [ ] **AC-03** `[real]` — **GM-001 replays MATCH**: the patient-code formatter returns `"P000001"`,
  `"P999999"`, `"P9999999"`, `"P99999999"` for `1`, `999999`, `9999999`, `99999999`, with lengths
  7, 7, 8, 9. *(FR-05, FR-07)*
- [ ] **AC-04** `[real]` — **GM-004 replays MATCH**: the bill-code formatter returns `"BILL001-P000001"`,
  `"BILL999-P000001"`, `"BILL1000-P000001"`. *(FR-06, FR-07)*
- [ ] **AC-05** `[real]` — Forcing the `Prescription` insert to fail leaves **zero** `Patient` rows
  committed. A test injects the failure (a deliberate constraint violation or a fault-injecting
  interceptor) and asserts the patient is absent afterwards — this is the DR-002 gap, and a test that
  cannot observe the rollback does not verify it. *(FR-01, NFR-01)*
- [ ] **AC-06** `[real]` — Twenty concurrent registrations produce twenty patients with twenty distinct
  codes and twenty bills. *(FR-08, NFR-02)*
- [ ] **AC-07** `[real]` — No code path accepts a client-supplied `Code`; a request body carrying one
  produces a patient whose code is still server-generated. *(FR-09, DR-001)*
- [ ] **AC-08** `[real]` — The registration service reports failure — not success — when the write does
  not persist, verified by asserting the failure result while no row exists. *(FR-03, A2)*
- [ ] **AC-09** `[stub: ST-002, ST-003]` — `POST /api/patients` with a valid body returns 201 with the
  new patient `Id`, `Code`, and bill `Code`, and the rows exist in a real PostgreSQL database.
  *(FR-09, FR-12, NFR-01)*
- [ ] **AC-10** `[stub: ST-002, ST-003]` — `Gender: "Unknown"` returns 400 `ProblemDetails` naming the
  field, and no `Patient` row is created. The `CK_Patient_Gender` constraint is never reached.
  *(FR-10)*
- [ ] **AC-11** `[stub: ST-002, ST-003]` — A 31-character Name, a 101-character Email, and a missing
  Name each return 400 with the offending field named; a 3-character Name is **accepted**, confirming
  legacy's minimum lengths were not reintroduced. *(FR-11)*
- [ ] **AC-12** `[stub: ST-002, ST-003]` — The endpoint carries `[Authorize]` and a permission-policy
  requirement naming `root.patient-create`; removing the dev authentication registration makes the
  request fail authentication rather than succeed. *(FR-13)*
- [ ] **AC-13** `[real]` — **Stub scoping.** Booting the API host with `ASPNETCORE_ENVIRONMENT` set to
  anything other than `Development` throws at startup with a message naming BL-002 and BL-007; the
  same boot with the opt-in flag set throws rather than registering the stubs. A test asserts the dev
  stub types are absent from the service collection in a non-Development boot. *(FR-15 — the
  stub-discipline criterion for ST-002 and ST-003)*
- [ ] **AC-14** `[real]` — **Registry completion.** `ST-002` and `ST-003` in
  `.specclaw/analysis/module-stubs.md` carry a real `file:line` in both `Fakes` and `Implementation`,
  each naming the concrete scoping mechanism. `ST-001` reads
  `n/a — no stub code; nothing split out of BL-020`. *(stub-discipline: what the build step owes the
  registry)*
- [ ] **AC-15** `[real]` — The frontend builds from a clean clone and its production bundle emits no
  TypeScript errors. *(FR-16, NFR-04)*
- [ ] **AC-16** `[real]` — The MUI theme's palette and typography resolve to the exact TK-001/TK-002
  values and `#218283` for the navbar; a test asserts the theme object's values rather than a rendered
  screenshot. *(FR-17)*
- [ ] **AC-17** `[stub: ST-002, ST-003]` — The registration screen renders all seven fields plus Save,
  submits to `POST /api/patients`, and displays the returned patient and bill codes on success.
  *(FR-18, FR-19)*
- [ ] **AC-18** `[stub: ST-002, ST-003]` — A server 400 renders the server's own field messages against
  the offending inputs, not a generic failure banner. *(FR-19)*
- [ ] **AC-19** `[real]` — Every new input has an associated label and every error message is
  programmatically associated with its field. *(NFR-06)*
- [ ] **AC-20** `[real]` — `dotnet build DentalManagement.sln` succeeds with `TreatWarningsAsErrors`
  on, and the Domain project's package references are unchanged. *(NFR-03, NFR-04)*

## Edge Cases

- **Sequence exceeds the column.** `Patient.Code` is `HasMaxLength(8)`. FR-05's no-truncation rule means
  sequence `99999999` produces a 9-character code the column rejects. GM-001 pins the string the
  formatter returns; it does not pin what happens next, and legacy never reached this. The insert
  fails, and per FR-03 that surfaces as a failure rather than a false success. Nine-character codes
  require ~100 million patients — recorded, not designed around.
- **Bill code width.** `Prescription.Code` is `HasMaxLength(18)`. `"BILL1000-P00000001"` is 18 — at the
  ceiling. Wider combinations exist in principle and behave as above.
- **Duplicate code from a manually edited row.** GM-002's scenario. Unreachable through this item's
  surface (FR-09 accepts no client `Code`, and no edit path ships here), but the unique index is the
  backstop and FR-03 means the caller is told the truth if it ever fires.
- **Gender omitted.** `Patient.Gender` is nullable and `CK_Patient_Gender` admits NULL. An omitted
  Gender is valid, not a 400.
- **Age.** `Patient.Age` is a non-nullable `int` and legacy's field was a free-text input. A missing or
  non-numeric Age is a 400; the spec sets no clinical upper bound, since neither legacy nor the schema
  has one.
- **Concurrent first registration on an empty database.** Two simultaneous requests must not both
  produce `P000001` — the case A4's sequence source exists to prevent, gated by AC-06.
- **Frontend submits while offline / the API is down.** The screen surfaces the failure; it never shows
  a success state it did not receive. The mirror of FR-03 on the client side.

## Dependencies

- **BL-001** — built. Supplies `Patient`, `Prescription`, `Gender`, `BillStatus`, `DentalDbContext`,
  the EF configurations this spec's validation mirrors, `IClock`, the `root.patient-create` seeded
  `Resource`, and the Testcontainers test harness pattern.
- **BL-010, BL-002, BL-007** — unmet. See below.

## Bypassed Dependencies

### ST-001 — BL-010's service catalog, for BL-020

- **Substitutes:** BL-010 (MOD-002 — Service & Medical-Info Catalog)
- **Strategy:** `item-split`
- **Stands in with:** nothing — a no-op split. BL-020's scope already excludes the `MedicalService`
  catalog, and `rebuild-backlog.md`'s sequencing rationale states the edge is a module-level artifact,
  *"not because BL-020 itself calls the catalog."* SCR-003's `new-patient` panel has no service fields;
  the catalog first appears in the `add-services` panel, which is BL-022.
- **Scoping mechanism:** n/a — there is no stub code to scope. Nothing was split out of BL-020 either,
  so no new `BL-0##` is created.
- **Criteria verified against this stub:** none. No criterion in this spec rests on ST-001.
- **Retires when:** BL-010 is built. There is nothing to remove first.

### ST-002 — BL-002's authenticated session, for BL-020

- **Substitutes:** BL-002 (MOD-005 — Identity, Roles & Permissions)
- **Strategy:** `stub-interface`
- **Stands in with:** a dev-only `ICurrentUser` returning a fixed seeded identity (`admin@dev.local`,
  role `Admin` — both already real values in `SeedCatalog.RoleNames`), backed by a dev-only
  authentication handler that authenticates every request as that user. BL-002 replaces the handler
  with ASP.NET Core Identity token issuance per SQ-004; the abstraction survives unchanged.
- **Scoping mechanism:** registration happens only inside
  `if (builder.Environment.IsDevelopment() && DevelopmentAuthOptions.AllowDevelopmentAuthenticationStub)`
  in `Program.cs`, with the flag defaulting to `false` and absent from `appsettings.json`. A boot with
  the flag set outside Development throws `InvalidOperationException` at startup. This mirrors the
  mechanism the repo already uses for exactly this problem —
  `AdminBootstrapOptions.AllowDevelopmentDemoAccounts` gating `AdminAccountSeeder`'s known demo
  credentials, whose production path *"fails loudly when the credentials are absent rather than falling
  back to a default."*
- **Criteria verified against this stub:** AC-09, AC-10, AC-11, AC-12, AC-17, AC-18. AC-13 and AC-14
  are the two mandatory stub criteria and are themselves `[real]`.
- **Retires when:** BL-002 is built and BL-020's fixtures re-replay clean.

### ST-003 — BL-007's server-side authorization, for BL-020

- **Substitutes:** BL-007 (MOD-005 — Identity, Roles & Permissions)
- **Strategy:** `stub-interface`
- **Stands in with:** an `IPermissionChecker.CheckAsync(role, resourceRoute)` port whose dev-only
  implementation grants every request. The endpoint's own policy declaration names
  `root.patient-create` — a route already seeded as a non-public `Resource` — so BL-007 substitutes the
  `Resource`/`Permission`-backed implementation enforcing DR-015/DR-016 per CQ-013 with no change to
  the endpoint.
- **Scoping mechanism:** the same gate as ST-002. Both dev implementations are registered by the same
  conditional block and are absent from every non-Development boot.
- **Criteria verified against this stub:** AC-09, AC-10, AC-11, AC-12, AC-17, AC-18.
- **Retires when:** BL-007 is built and BL-020's fixtures re-replay clean.

## Notes

- **N-1 — A4 is a behavioural change with no sanctioning CQ, and that is worth raising.** SQ-012
  requires every intentional divergence from legacy to be tied to a decided CQ. Moving the patient code
  from `Count() + 1` to a database sequence is unobservable to the captured corpus — GM-001 and GM-004
  take the sequence as an *input*, and GM-003 only requires `P000001` first — but it does change
  behaviour after a patient is deleted: legacy would reissue a code, a sequence will not. PQ-005 covers
  the *response* to a failed insert, not the *source* of the number. **Recommend raising this as a
  pending question** so the choice is on the record rather than inferred from this spec.
- **N-2 — every fixture in this run will report stub-tainted, including the `[real]` ones.** ST-001,
  ST-002 and ST-003 all name BL-020 in `Consumed by`, and `/specclaw:bf-replay` stamps taint per
  *item*, not per seam. GM-001, GM-003 and GM-004 replay at pure-function and service seams that never
  touch the dev authentication handler or the permission checker, so the label on those ACs is `[real]`
  on the merits — but the report will still mark them tainted. That is the join being conservative, not
  a hidden weakness, and it clears when ST-002 and ST-003 retire.
- **N-3 — the overall verdict cannot be a clean PASS.** GM-002 and GM-011 remain PROVISIONAL pending
  PQ-005 and PQ-008. Expect `PASS-PENDING-DECISIONS`.
- **N-4 — obviously-fake stub data.** `references/stub-discipline.md` prefers stub identities that are
  *"recognisable as fake on sight."* `admin@dev.local` with role `Admin` is what was chosen at propose
  time and is implemented as chosen; the stub additionally attaches an explicit `stub=ST-002` claim so
  it is self-documenting in a log without changing the human's decision.
