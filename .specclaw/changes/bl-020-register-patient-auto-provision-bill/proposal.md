# Proposal: BL-020 — Register New Patient & Auto-Provision Bill

**Created:** 2026-08-13
**Status:** 🟡 Draft

## Problem

_What problem are we solving? Why does it matter?_

The rebuild has a schema but no behaviour. BL-001 landed the EF Core model, the `InitialCreate`
migration, seeding, and an API host whose only endpoint is `/health` — `src/DentalManagement.Api/Program.cs`
says so in its own comment: *"BL-001 exposes no feature endpoint... controllers belong to later
backlog items."* `Patient` and `Prescription` exist as types; nothing in the solution can create one.

BL-020 is **MOD-001's foundational item**. Per `rebuild-backlog.md`'s own sequencing rationale,
"every other MOD-001 item operates on a Patient/Prescription pair this item creates" — BL-021 (patient
list) has nothing to list, BL-022 (add services) has no bill to add to, BL-023/BL-024/BL-026/BL-027/BL-028
all read state that starts here. Nothing else in the Patient & Billing module can begin until this ships.

The legacy behaviour this item rebuilds carries three specific defects the analysis already decided
to close rather than reproduce:

- **The two writes are not atomic (DR-002).** `PatientCreateController.Post` creates the `Patient`,
  then creates a `Prescription` with `StatusId = 5` — with no transaction wrapping them. If the second
  write fails, *"the patient exists with zero bills."* `GM-011` pins the concrete downstream symptom:
  `PatientController.Get()` resolves "current bill" via `.Last()` (returns the stale Closed bill,
  `status_id: 6`), while `PrescriptionController.GetPatientCurrentPrescription` uses
  `.LastOrDefault(x => x.StatusId == 5)` (returns null) — the two mechanisms disagree, silently.
- **Patient code generation is count-based and its failure is swallowed (DR-001).** The code comes from
  `GetPatientViewModel().Count() + 1`, and the controller never inspects `Add()`'s boolean result —
  it returns `Ok(patient.Id)` unconditionally. `GM-002` captures a duplicate `P000002` landing with
  `http_status: 200` and `patient_b_persisted: true`.
- **Gender was a free-form string (CQ-007).** BL-001 already typed it —
  `src/DentalManagement.Domain/Enums/Gender.cs` plus the `CK_Patient_Gender` check constraint in
  `PatientConfiguration.cs`. This item is the first write path that has to map user input onto it.

## Proposed Solution

_What are we building? High-level approach._

One transactional registration operation, its HTTP endpoint, and the screen that drives it.

1. **A single atomic registration write path** in Domain/Infrastructure that generates the patient
   code, inserts the `Patient`, generates the bill code, and inserts a `Prescription` at
   `BillStatus.Active` with zeroed money fields — **all inside one EF Core transaction**. This is the
   acceptance criterion BL-020 states explicitly: *"the rebuild wraps both writes in one real database
   transaction, closing this specific gap — new server-side design work, not something a legacy
   fixture can attest to."*

2. **Code generation that matches the captured fixtures exactly.**
   - `GM-001` pins patient codes: `"P"` + the sequence zero-padded to 6 digits — `"P000001"`,
     `"P999999"` (7 chars), `"P9999999"` (8 chars, at `Patient.Code`'s `HasMaxLength(8)` ceiling),
     `"P99999999"` (9 chars, over it). The pad is a minimum width, not a truncating format.
   - `GM-004` pins bill codes: `"BILL"` + sequence zero-padded to 3 + `"-"` + patient code —
     `"BILL001-P000001"`, `"BILL999-P000001"`, `"BILL1000-P000001"`.
   - The **sequence source** changes: legacy's row count is what produces `GM-002`'s collision.
     The rebuild needs a collision-safe source (design decides between a database sequence, a
     retry-on-unique-violation loop, or an allocation table) — the *format* is fixture-pinned, the
     *source* is not.

3. **The registration endpoint** (`POST` under the patients route), carrying its real `[Authorize]`
   attribute and its real permission-policy attribute from day one, resolved through the two dev
   stubs recorded below. Response shape must accurately report whether the write happened —
   PQ-005's proposed default, flagged as an open question rather than assumed.

4. **The registration screen** — SCR-003's "new-patient" state: a centered panel with Name, Age,
   Gender, Phone, Email, Address, Note, Save. THEME-ONLY fidelity against TK-001 (body background
   `#EBEBEB`, dropdown-menu background `#006a4e`), TK-002 (typography), TK-003 (navbar `#218283` per
   CQ-004). Per SQ-006/CQ-023 this is React + TypeScript + Material UI — **and no frontend project
   exists in this repository yet**, so this item stands up the SPA shell as well (see Open Question 1).

5. **Verification** runs `/specclaw:bf-replay` against `GM-001`, `GM-003`, `GM-004`.
   `GM-002` and `GM-011` are marked PROVISIONAL in `manifest.json` and will report with a
   `-PROVISIONAL` suffix, holding the verdict at `PASS-PENDING-DECISIONS` until PQ-005 and PQ-008
   are answered. Three ACTIVE stubs additionally mark every fixture this item verifies as
   stub-tainted until they retire.

## Scope

### In Scope

- Transactional patient-registration operation: `Patient` insert + auto-provisioned `Prescription`
  at `BillStatus.Active`, `TotalDue` 0, one transaction, one failure boundary.
- DR-001 patient code generation (`GM-001` format) on a collision-safe sequence source.
- DR-003 bill code generation (`GM-004` format).
- CQ-007 `Gender` enum mapping on the request contract, including rejection of values outside
  `Male`/`Female`/`Others`.
- Registration HTTP endpoint with an accurate success/failure result, real `[Authorize]` and
  permission-policy attributes, request validation mirroring the column constraints already in
  `PatientConfiguration.cs` (Name ≤ 30, Phone ≤ 30, Email ≤ 100, Address ≤ 200, Note ≤ 500).
- Dev-only `ICurrentUser` + dev authentication handler (ST-002) and dev-only `IPermissionChecker`
  (ST-003) — the seams BL-002 and BL-007 replace.
- React + TypeScript + MUI application shell: Vite project, MUI theme carrying TK-001/TK-002/TK-003,
  routing, API client, and the SCR-003 registration screen.
- Integration tests over the registration path, including the transaction-rollback case and the
  code-format boundaries `GM-001`/`GM-004` pin.

### Out of Scope

- **BL-010's service catalog** — BL-020 does not consume it (ST-001; see Dependency Bypass).
  Catalog consumption first arrives at BL-022.
- **Real authentication (BL-002)** and **real server-side authorization (BL-007)** — stubbed here,
  built in their own items. No login screen, no token issuance, no `Resource`/`Permission` evaluation.
- **Patient list / search (BL-021)**, **add treatment services (BL-022)**, **edit patient (BL-023)**,
  payments, history, close/reopen bill, receipts, reports — each its own MOD-001 item.
- The Medical Condition tab (MOD-002 backend serving a MOD-001 screen) — BL-011/BL-012.
- Resolving PQ-005 and PQ-008. This item builds against their *proposed defaults* and reports
  `PASS-PENDING-DECISIONS`; it does not decide them.
- Full application navigation shell (nav, footer, every route) beyond what the registration screen
  needs to render and be reached.
- Reproducing SCR-003's legacy pixel layout — THEME-ONLY per CQ-023 means token values, not geometry.

## Impact

- **Files affected:** ~35 (estimated) — ~12 backend (registration service, code generators, endpoint,
  DTOs, stub abstractions + dev implementations, DI), ~15 frontend (new Vite/React project scaffold,
  theme, screen, form, API client), ~8 test files
- **Complexity:** large (small / medium / large) — the backend write path alone is medium; standing up
  the SPA shell for the first time is what pushes it to large
- **Risk:** medium (low / medium / high) — the transactional write and code formats are tightly pinned
  by fixtures and cheap to verify; the risk sits in the sequence-source design (a wrong choice
  reintroduces `GM-002`'s collision under concurrency) and in three ACTIVE stubs tainting this item's
  fixtures until BL-002 and BL-007 land

## Open Questions

1. **Does BL-020 stand up the React SPA, or is the screen deferred?** No frontend project exists in
   this repository — the solution is four .NET projects and two test projects. BL-020's UI fidelity
   line requires SCR-003 at THEME-ONLY, which means an app shell has to exist first. **Assumed for
   this proposal: yes, BL-020 scaffolds a minimal React + TS + MUI app** (theme + routing + API client
   + one screen). The alternative is to ship the backend half only and let the first UI item scaffold
   it — which would leave BL-020 partially delivered against its own acceptance basis. Please confirm.

2. **PQ-005 (OPEN) — must the create endpoint report an honest success/failure?** Proposed default is
   "treat as a DEFECT": never return 200 for a row that did not persist. `GM-002` stays PROVISIONAL
   either way. Building to the default is the assumption here; confirming it at plan time would let
   the fixture be finalized.

3. **PQ-008 (OPEN) — which "current bill" mechanism is authoritative?** Proposed default is the
   `Status == Active` filter everywhere. BL-020 only *creates* the Active bill, so the choice affects
   this item's "always has exactly one current bill" invariant and its test assertions, not its write
   path. `GM-011` stays PROVISIONAL.

4. **What is the collision-safe sequence source for patient and bill codes?** A PostgreSQL sequence,
   an insert-retry on unique-violation, or a dedicated allocation table each satisfy `GM-001`/`GM-004`'s
   formats. Note that `BaseEntity.Id` is `ValueGeneratedNever()` — ids are app-generated — so the code
   sequence cannot ride on an identity column. Design decides; flagging it because it is the one place
   a wrong choice recreates the defect being fixed.

5. **Endpoint route and request contract.** Legacy was `POST api/PatientCreate/Create` taking the
   `Patient` entity directly. CQ-012 pushes (for BL-022) toward explicit DTOs and route-supplied ids.
   Proposed: `POST /api/patients` with a `RegisterPatientRequest` DTO. Confirm at plan time.

## Dependency Bypass

BL-020 declares `Depends on: BL-010, BL-002, BL-007` — all three unmet, all three cross-module, all
three bypassed by explicit choice on 2026-08-13. The registry
(`.specclaw/analysis/module-stubs.md`) is the source of truth for each entry.

- **BL-010 (MOD-002 — Service & Medical-Info Catalog)** → `ST-001`, strategy `item-split`.
  Chosen by Pasan Gunathilaka, 2026-08-13.
  Stands in with: nothing — a no-op split. BL-020's scope already excludes the `MedicalService`
  catalog entirely, and `rebuild-backlog.md`'s own sequencing prose states this edge is a module-level
  artifact, *"not because BL-020 itself calls the catalog."* SCR-003's "new-patient" state has no
  service fields. Nothing is faked, nothing is deferred out of BL-020, and there is no fake to retire —
  catalog consumption first arrives at BL-022.

- **BL-002 (MOD-005 — Identity, Roles & Permissions)** → `ST-002`, strategy `stub-interface`.
  Chosen by Pasan Gunathilaka, 2026-08-13.
  Stands in with: a dev-only `ICurrentUser` abstraction returning a fixed seeded identity
  (`admin@dev.local`, role `Admin`), backed by a dev-only authentication handler that authenticates
  every request as that user. The real `[Authorize]` attribute goes on the registration endpoint from
  day one; BL-002 replaces the handler with ASP.NET Core Identity token issuance per SQ-004 and the
  abstraction survives unchanged. Identity is already registered in
  `src/DentalManagement.Infrastructure/DependencyInjection.cs` (`AddIdentityCore<ApplicationUser>`)
  and `AdminAccountSeeder` already seeds an admin, so the stub has real rows to point at.

- **BL-007 (MOD-005 — Identity, Roles & Permissions)** → `ST-003`, strategy `stub-interface`.
  Chosen by Pasan Gunathilaka, 2026-08-13.
  Stands in with: an `IPermissionChecker.CheckAsync(role, resourceRoute)` port whose dev-only
  implementation grants every request. The registration endpoint carries its real permission policy
  attribute from day one; BL-007 substitutes the `Resource`/`Permission`-backed implementation
  enforcing DR-015/DR-016 server-side per CQ-013, with no change to the endpoint's own declaration.

All three entries are `ACTIVE`, which means **every fixture verifying BL-020 reports as stub-tainted
until they are retired** — `/specclaw:bf-replay` will say so, and that is the intended signal, not a
failure.

---

**To proceed:** Review this proposal and approve to begin planning.
