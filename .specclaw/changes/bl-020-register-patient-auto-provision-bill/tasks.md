# Tasks: BL-020 — Register New Patient & Auto-Provision Bill

**Change:** bl-020-register-patient-auto-provision-bill
**Created:** 2026-08-13
**Total Tasks:** 9

## Summary

Nine tasks across four waves. Wave 1 is four independent foundations — the two bypass stubs, the pure
code formatters, the sequence migration, and the frontend scaffold — none of which depend on each
other. Wave 2 builds the registration path on top. Wave 3 verifies it and puts a screen on it. Wave 4
closes the item out: replay, the stub registry, and the docs.

**T1 is the stub-implementation task the bypass requires, and it lands before anything that consumes
it.** Every task touching the endpoint depends on it, because the endpoint cannot be exercised at all
without ST-002 authenticating and ST-003 granting.

The fixture-replaying tests (T3, T5) deliberately call the Domain formatters and the registration
service **directly, never over HTTP** — GM-001/GM-004 were captured at the `pure-function` layer and
GM-003 at the `service` layer, and `/specclaw:bf-replay` refuses a fixture replayed at any other layer.

## Tasks

### Wave 1 — Foundations (independent; may run in parallel)

- [x] `T1` — Bypass seams: `ICurrentUser`, `IPermissionChecker`, their dev-only implementations, and the scoping gate
  - Files: `src/DentalManagement.Domain/Abstractions/ICurrentUser.cs`, `src/DentalManagement.Domain/Abstractions/IPermissionChecker.cs`, `src/DentalManagement.Api/DevelopmentOnly/{DevelopmentAuthOptions,StubCurrentUser,StubAuthenticationHandler,StubPermissionChecker}.cs`, `src/DentalManagement.Api/Authorization/{PermissionRequirement,PermissionAuthorizationHandler,PermissionAttribute}.cs`, `src/DentalManagement.Api/Program.cs`, `src/DentalManagement.Api/appsettings.Development.json`
  - Estimate: medium
  - Kind: impl
  - Depends: —
  - Notes: **ST-002 and ST-003.** The two interfaces stay plain BCL types in Domain (NFR-03, learning L1) — everything ASP.NET Core lives in Api. Registration happens only inside `if (builder.Environment.IsDevelopment() && options.AllowDevelopmentAuthenticationStub)`; the flag defaults to `false`, appears only in `appsettings.Development.json`, and a boot with it set outside Development throws `InvalidOperationException` naming BL-002 and BL-007 (design D-4, D-5). Stub identity is `admin@dev.local` / `Admin` exactly as chosen at propose time, plus a `stub=ST-002` claim so it is self-documenting in a log (spec N-4). Do not change either strategy — if the implementation shows one is wrong, stop and say so.

- [x] `T2` — Pure code formatters
  - Files: `src/DentalManagement.Domain/Patients/PatientCodeFormatter.cs`, `src/DentalManagement.Domain/Patients/BillCodeFormatter.cs`
  - Estimate: small
  - Kind: impl
  - Depends: —
  - Notes: `"P" + sequence.ToString("D6")` and `"BILL" + sequence.ToString("D3") + "-" + patientCode`. `D6`/`D3` are minimum widths — `9999999.ToString("D6")` is `"9999999"`, which is exactly why GM-001's 8- and 9-character cases need no special handling. No database, no clock, no ambient state (FR-07, design D-2).

- [x] `T3` — Sequence + migration
  - Files: `src/DentalManagement.Infrastructure/Persistence/DentalDbContext.cs`, `src/DentalManagement.Infrastructure/Patients/PatientCodeSequence.cs`, `src/DentalManagement.Infrastructure/Persistence/Migrations/*_AddPatientCodeSequence.cs`
  - Estimate: small
  - Kind: migration
  - Depends: —
  - Notes: `HasSequence<long>("patient_code_seq")` starting at 1. **Additive only** — the migration creates a sequence and touches no existing table, column, or index, so BL-001's AC-04 (no index created twice) still holds. `nextval` is what makes concurrent registration collision-safe without blocking.

- [x] `T4` — Frontend scaffold and theme
  - Files: `client/**` (Vite + React + TypeScript + MUI project), `client/src/theme.ts`, `.gitignore`
  - Estimate: medium
  - Kind: config
  - Depends: —
  - Notes: Scope is capped by FR-16/FR-17 — project, theme, routing, nothing else. Theme carries TK-001 (`#EBEBEB` body, `#333` text, `#006a4e` dropdown, `#f5f5f5` form control), TK-002 (`"Helvetica Neue", Helvetica, Arial, sans-serif` 14px), TK-003 (`#218283` navbar, per CQ-004 resolving the contested value). No navigation shell, no login screen, no route guards. Add `client/node_modules` and `client/dist` to `.gitignore`.

### Wave 2 — The registration path

- [x] `T5` — Registration service: the transaction, and the tests that replay GM-001/GM-003/GM-004
  - Files: `src/DentalManagement.Domain/Abstractions/IPatientRegistrationService.cs`, `src/DentalManagement.Infrastructure/Patients/PatientRegistrationService.cs`, `src/DentalManagement.Infrastructure/DependencyInjection.cs`, `tests/DentalManagement.Infrastructure.Tests/CodeFormatterTests.cs`, `tests/DentalManagement.Infrastructure.Tests/PatientRegistrationTests.cs`
  - Estimate: large
  - Kind: impl
  - Depends: T2, T3
  - Notes: One explicit `BeginTransactionAsync` covering the sequence read and both inserts (design D-3). `Prescription` at `BillStatus.Active` with all seven monetary fields at `0`; bill sequence is always `1` here, since BL-020 only creates a patient's first bill — do not invent a general per-patient numbering rule, that is BL-027's. Timestamps via the existing `IClock`. Returns an explicit success/failure result and never reports success for a write that did not persist (FR-03). Tests call the service and the formatters **directly, not over HTTP** (design D-1, D-2). The rollback test pre-inserts a `Prescription` whose `Code` matches the one about to be generated, so the real unique index fails the second insert inside the transaction (design D-7) — then asserts zero `Patient` rows. Concurrency test: 20 simultaneous registrations, 20 distinct codes. Real PostgreSQL via the existing `PostgresContainerFixture`.

- [x] `T6` — Endpoint, request contract, and validation
  - Files: `src/DentalManagement.Api/Controllers/PatientsController.cs`, `src/DentalManagement.Api/Contracts/{RegisterPatientRequest,RegisterPatientResponse}.cs`, `src/DentalManagement.Api/Program.cs`
  - Estimate: medium
  - Kind: impl
  - Depends: T1, T5
  - Notes: `POST /api/patients`, `[ApiController]` for automatic field-scoped `ProblemDetails`. Carries `[Authorize]` and `[Permission("root.patient-create")]` — a route already seeded as a non-public `Resource` in `SeedCatalog.Resources`, so BL-007 substitutes its implementation without touching this declaration. `Code` and `Id` are absent from the request type entirely (DR-001 enforced by the contract, not a check). Validation mirrors `PatientConfiguration.cs`: Name required ≤ 30, Phone ≤ 30, Email ≤ 100, Address ≤ 200, Note ≤ 500 — and **not** legacy's minimum lengths, which BL-001 deliberately kept out of the database. `Gender` outside Male/Female/Others is a 400 before the write. Also wire controllers, ProblemDetails, and CORS for the Vite dev origin.

### Wave 3 — Verification and the screen

- [x] `T7` — API test project: endpoint behaviour and the stub-scoping gate
  - Files: `tests/DentalManagement.Api.Tests/**`, `DentalManagement.sln`
  - Estimate: large
  - Kind: test
  - Depends: T6
  - Notes: New xUnit project, `WebApplicationFactory` over a Testcontainers PostgreSQL, reusing `PostgresContainerFixture`'s pattern (design D-9). Covers AC-09 through AC-12 — 201 with id/code/billCode, `Gender: "Unknown"` → 400 with no row created, the length boundaries (including a 3-character Name that must be **accepted**), and a client-supplied `Code` being ignored. **AC-13 is the stub-discipline criterion and belongs here:** a boot with `ASPNETCORE_ENVIRONMENT` set to anything but `Development` throws at startup naming BL-002 and BL-007, the same boot with the opt-in flag set also throws, and the dev stub types are absent from the service collection. PostgreSQL only — no SQL Server, so this joins the fast tier.

- [x] `T8` — SCR-003 registration screen and API client
  - Files: `client/src/api/patients.ts`, `client/src/features/patients/RegisterPatientPage.tsx`, `client/src/features/patients/*.test.tsx`
  - Estimate: medium
  - Kind: impl
  - Depends: T4, T6
  - Notes: Centered "New Patient" panel — Name, Age, Gender (select: Male/Female/Others), Phone, Email, Address (textarea), Note (textarea), Save. THEME-ONLY: reproduce the token values, reinterpret the Bootstrap grid geometry as responsive MUI (CQ-023, CQ-020). Success shows the returned patient code and bill code; a server 400 renders the server's own field messages against the offending inputs, never a generic banner (FR-19). Every input labelled, every error programmatically associated with its field (NFR-06). Component tests via Vitest + React Testing Library; no browser E2E in this item (spec A8).

### Wave 4 — Close-out

- [x] `T9` — Replay, stub registry, and docs
  - Files: `.specclaw/analysis/module-stubs.md`, `README.md`
  - Estimate: medium
  - Kind: docs
  - Depends: T7, T8
  - Notes: Run `/specclaw:bf-replay bl-020-register-patient-auto-provision-bill` and record the result. Expect **GM-001, GM-003, GM-004 to replay**; GM-002 and GM-011 are out of this item's scope (spec A6) and remain PROVISIONAL on PQ-005/PQ-008, so the overall verdict is expected to be `PASS-PENDING-DECISIONS`, and every fixture will be stamped stub-tainted at item level even where the replayed seam never touches a stub (spec N-2). Complete `ST-002` and `ST-003` in `.specclaw/analysis/module-stubs.md` **in place** — `Fakes` and `Implementation` each get a real `file:line` plus the concrete scoping mechanism; edit field by field, never rewrite the file. `ST-001` gets `n/a — no stub code; nothing split out of BL-020`. Update `README.md`'s layout table, add how to run the frontend, and note the stub gate and that the API will not boot outside Development until BL-002/BL-007 land.

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

The optional `Kind` hint is consumed by `build.dynamic_agents` (when enabled) to
synthesize a specialized subagent per task. Omit it and build classifies
heuristically, defaulting to `impl`.
