# Verify Report: BL-020 — Register New Patient & Auto-Provision Bill

**Change:** bl-020-register-patient-auto-provision-bill
**Verified:** 2026-08-13

**Verdict:** PARTIAL

16 of 20 acceptance criteria met, 4 partially met. **Every partial is a defect in the spec's own
wording, not a gap in the implementation** — three ACs promise a `/specclaw:bf-replay` artifact that
was never produced, and one asks for an assertion that is unobservable by construction.

## Acceptance Criteria

| AC | Label | Status | Evidence |
|---|---|---|---|
| AC-01 | `[real]` | **Met** | `PatientRegistrationTests.cs::Register_into_an_empty_database_replays_GM_003` — exactly one `Patient` (`Code = "P000001"`), exactly one `Prescription` (`Status = Active`, `TotalDue = 0m`). |
| AC-02 | `[real]` | **Partially met** | Same test asserts `Assert.Single(prescriptions)`, `BillStatus.Active = 5` (`Enums/BillStatus.cs:16`), bill code exactly `"BILL001-P000001"`, `total_due: 0`. Values proven; the AC's "**GM-003 replays MATCH**" names a bf-replay artifact that does not exist — Finding 2. |
| AC-03 | `[real]` | **Partially met** | `CodeFormatterTests.cs::Patient_code_formatter_replays_GM_001` — 4 theory cases matching GM-001 exactly (`1→"P000001"` len 7, `999999→"P999999"` len 7, `9999999→"P9999999"` len 8, `99999999→"P99999999"` len 9). Same caveat. |
| AC-04 | `[real]` | **Partially met** | `CodeFormatterTests.cs::Bill_code_formatter_replays_GM_004` — 3 cases matching exactly. Same caveat. |
| AC-05 | `[real]` | **Met** | `Failing_prescription_insert_leaves_no_new_patient_committed` — forces a real `IX_Prescription_Code` violation, asserts the attempted code is absent and no new patient committed. Real PostgreSQL. |
| AC-06 | `[real]` | **Met** | `Twenty_concurrent_registrations_produce_twenty_distinct_codes` — 20 parallel calls, 20 distinct patient codes, 20 distinct bill codes, 20 rows each. |
| AC-07 | `[real]` | **Met** | `RegisterPatientRequest.cs` has no `Code`/`Id` property at all. `Client_supplied_code_in_the_request_body_is_ignored` posts `code: "HACKED-CODE"`, asserts the response code is `"P000001"`. |
| AC-08 | `[real]` | **Met** | `Failing_prescription_insert_reports_failure_not_success` — `IsSuccess == false` with a non-empty `FailureReason` under zero persisted rows. |
| AC-09 | `[stub: ST-002, ST-003]` | **Met** (stub-strength) | `Valid_registration_returns_201_and_persists_patient_and_prescription` — 201 with `Id`/`Code`/`BillCode`; rows confirmed via a fresh read context. |
| AC-10 | `[stub: ST-002, ST-003]` | **Met** (stub-strength) | `Gender_outside_the_named_set_returns_400_and_creates_no_patient` — 400 naming `gender`, 0 patients. Rejected at deserialization, so `CK_Patient_Gender` is never reached. |
| AC-11 | `[stub: ST-002, ST-003]` | **Met** (stub-strength) | Four tests: 31-char Name → 400/name; 101-char Email → 400/email; missing Name → 400/name; **3-char Name → 201** (`Three_character_name_is_accepted`). |
| AC-12 | `[stub: ST-002, ST-003]` | **Met** (stub-strength, wording gap) | `PatientsController.cs` carries `[Authorize] [Permission("root.patient-create")]`; `Request_with_no_successful_authentication_fails_rather_than_succeeds` asserts 401/403. Proxied via `ConfigureTestServices` rather than literally removing the registration — Finding 7. |
| AC-13 | `[real]` | **Partially met** | `StubScopingTests.cs` covers the non-Development throw naming BL-002/BL-007 (including the flag-set-true case) and the Development contrast case. The third clause is unwritable as specified — Finding 1. |
| AC-14 | `[real]` | **Met** | `module-stubs.md` cites `StubCurrentUser.cs:24`, `StubAuthenticationHandler.cs:23` (claim `:36`), `StubPermissionChecker.cs:16` (`:18`), `Program.cs:50/54/60/61/65` — every line verified against the files. ST-001 reads `n/a — no stub code; nothing split out of BL-020`. |
| AC-15 | `[real]` | **Met** | `npm run build` (`tsc -b && vite build`) — 0 TypeScript errors, `dist/` produced. Independently rerun. |
| AC-16 | `[real]` | **Met** | `client/src/theme.test.ts` asserts `#EBEBEB`, `#333`, `#006a4e`, `#2e8b57`, `#f5f5f5`, font family/size, and `#218283` off the theme object. |
| AC-17 | `[stub: ST-002, ST-003]` | **Met** | `RegisterPatientPage.test.tsx` — seven fields plus Save render; submit posts the expected body and renders `P000001` / `BILL001-P000001`. |
| AC-18 | `[stub: ST-002, ST-003]` | **Met** | Same file — a mocked 400 renders the server's own field messages, with no generic banner. |
| AC-19 | `[real]` | **Met** | `aria-invalid="true"` plus `toHaveAccessibleDescription` for Name/Email on error; `toHaveAccessibleName` for all seven fields. |
| AC-20 | `[real]` | **Met** | `dotnet build DentalManagement.sln` — 0 warnings, 0 errors with `TreatWarningsAsErrors=true` (`Directory.Build.props:8`). `DentalManagement.Domain.csproj` carries no `PackageReference` and is unchanged across every BL-020 commit. |

## Gate Results

| Gate | Result | In the automated gate? |
|---|---|---|
| .NET build | PASS — 0 warnings, 0 errors, `TreatWarningsAsErrors=true` | yes |
| Lint | **No evidence** — `lint_command` is empty; no analyzer or ESLint/Oxlint pass ran | n/a |
| `Infrastructure.Tests` | 84/84 | yes (`test_command`) |
| `Api.Tests` | 12/12 | **no** — manual run |
| Frontend Vitest | 3 files / 9 tests | **no** — manual run |
| `DataMigration.Tests` (e2e) | 17/17, `e2e_state: passed` | yes (`e2e_command`) |

**122 tests total, all passing — but only 84 (69%) are exercised by the configured automated gate.**
The verifier independently reran the Infrastructure, Api, and frontend suites; it did not rerun the
e2e tier (that one is BL-001's migration tool, not code this item added).

`systemd-run` is unusable on this Windows host, so the e2e memory cap was not applied.
`e2e_memory_limited` is `false` and the suite passed, so nothing was killed — but no cap was in force.

## Non-Functional Requirements

| NFR | Status | Evidence |
|---|---|---|
| NFR-01 | Met | Testcontainers PostgreSQL 17 throughout; never the EF in-memory provider. |
| NFR-02 | Met | AC-06's 20-way concurrency test. |
| NFR-03 | Met | `DentalManagement.Domain.csproj` has zero `PackageReference` and is unchanged across all BL-020 commits. |
| NFR-04 | Met | 0 warnings under `TreatWarningsAsErrors`; frontend build has 0 TypeScript errors. |
| NFR-05 | Met | `appsettings.json` has empty `ConnectionStrings` and no `DevelopmentAuth` section; the dev flags live only in `appsettings.Development.json`. |
| NFR-06 | **Partially met** | The ARIA/labelling half is strongly tested. The "responsive at desktop and tablet widths" half rests only on MUI breakpoint props in `RegisterPatientPage.tsx` — no automated test exercises viewport behaviour. |

## Findings

1. **AC-13's third clause is unwritable as specified (spec defect).** It asks a test to assert the dev
   stub types are "absent from the service collection" in a non-Development boot. The gate in
   `Program.cs` throws before `builder.Build()`, so no `IServiceProvider` is ever constructed on that
   path to inspect. `Non_development_boot_never_produces_a_service_provider_at_all` supplies the
   strongest available substitute. Against the spec, not the implementation. (Learning L12.)
2. **AC-02/03/04 promise a replay artifact that was never produced (spec defect).** They are worded
   "GM-00X **replays MATCH**", naming a `/specclaw:bf-replay` output. That command has never run for
   this change — `tasks.md` wrongly scheduled it inside the build (L13). The underlying claims are
   true and directly tested; the wording promises a process step that did not happen.
3. **No golden-master replay verdict exists at all (caps the ceiling).** Per spec Note N-3, when it
   runs it is expected to land at `PASS-PENDING-DECISIONS`, not a clean PASS: PQ-005 and PQ-008 are
   confirmed still OPEN in `pending-questions.md`, leaving GM-002 and GM-011 PROVISIONAL.
4. **`build.test_command` misses the tests this change shipped (config gap).** It names only
   `Infrastructure.Tests`, so the 12 `Api.Tests` and 9 frontend tests added by this very item sit
   outside CI's reach. Fix `config.yaml` before the next change. (Learning L14.)
5. **No lint pass exists** — neither a .NET style/analyzer pass beyond warnings-as-errors nor
   ESLint/Oxlint over roughly 1,700 new lines.
6. **A4's sequence-source change has no sanctioning CQ (process).** Moving from legacy's `Count() + 1`
   to a PostgreSQL sequence is a real behavioural divergence. SQ-012 requires every intentional
   divergence to be tied to a decided CQ; this one is not. Spec Note N-1 already recommends raising it
   as a pending question. The implementation built exactly what A4 specified — the gap is procedural.
7. **AC-12's test is a functional proxy (minor).** It swaps in an always-unauthenticated scheme via
   `ConfigureTestServices` rather than literally removing the `Program.cs` registration — which would
   instead trip AC-13's boot-time throw. Sound, but not literally what AC-12 describes.
8. **Two disclosed deviations, both already resolved.** `client/src/App.tsx` was edited without being
   declared in T8's file list (necessary route wiring, L11). `PatientCodeSequence.cs`'s missing
   `AS "Value"` alias — which made every sequence call throw — was found and fixed by T5 though it
   belonged to T3 (L9); the correct alias is confirmed in place.

## What this verdict does not cover

- **No golden-master replay.** The GM-001/GM-003/GM-004 "MATCH" claims rest on hand-written tests
  asserting the same pinned values, never on `/specclaw:bf-replay`. GM-002 and GM-011 are out of scope
  by spec design (A6) and remain PROVISIONAL on PQ-005/PQ-008.
- **All three stubs are ACTIVE; none retired.** Every `[stub: ST-002, ST-003]` criterion (AC-09–AC-12,
  AC-17, AC-18) proves the write path and endpoint plumbing work *when authentication and permission
  checking always succeed*. It proves nothing about how BL-002's real Identity session or BL-007's real
  `Resource`/`Permission` authorization will integrate — those do not exist yet, so their integration is
  unverified by construction. Per Note N-2, once replay runs even the `[real]` fixtures will be stamped
  stub-tainted at item level.
- **NFR-06's responsive-layout half** is unverified by any automated test.
- **No static analysis** beyond compiler warnings-as-errors.
- **The e2e tier was not independently rerun** by the verifier; it rests on the orchestrator's run.
- **BL-001's human sign-off table** in `README.md` remains unticked — a carryover, outside BL-020's ACs,
  but still open.

---

**Code Review:** skipped — `workflow.code_review` is `false`.
