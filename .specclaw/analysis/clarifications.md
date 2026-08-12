# Clarifications: App

**Date generated:** 2026-08-12
**Documents swept:** codebase-report.md,architecture.md,domain-model.md,functional-spec.md

<!--
  This file is drafted by /specclaw:bf-clarify (extract mode) and re-rendered
  on every subsequent run — never freehand-edited except for the fields a
  human fills in below each question.

  THREE question families, sharing one field structure and one --resolve
  pipeline, distinguished only by ID prefix and where their content comes
  from:

    CQ-NNN — Extracted from .specclaw/analysis/*.md by the clarify-
             extractor agent. Allocated per-repo, in extraction order.
    SQ-NNN — The standard bank (plugins/specclaw/references/clarify-
             standard-questions.md). IDs are FIXED by the bank file
             itself, not allocated per-repo — SQ-001 always means "target
             platform," in every project. Type/Blocking/Options/Proposed
             default are bash-owned, spliced in verbatim from the bank
             file on every render; only Finding/Why it matters/Source/
             Answer/Decided by/Date are ever agent- or human-authored for
             an SQ. Once an SQ has been rendered here (or listed under
             Not applicable below), it is never re-evaluated by a later
             run — a bank question doesn't flip-flop between applicable
             and not applicable across runs.
    UQ-NNN — Per-repo custom questions, ingested by bash (no agent
             involved) from .specclaw/analysis/custom-questions.md.
             Allocated per-repo, in file order, same permanence rules as
             CQ. De-duplicated by each question's original heading text,
             recorded in its Source field — editing an already-ingested
             heading in custom-questions.md later does NOT retroactively
             rewrite the rendered UQ; edit it here instead.

  Per-question block format — every question in every family follows this
  exact structure:

  ### <CQ|SQ|UQ>-NNN — <short title>

  - **Type:** DECISION | DATA | SCOPE | DEFECT | MECHANICAL | TARGET-GAP | CONFLICT
  - **Blocking:** yes — <what it blocks> | no
  - **Source:** <doc § section and/or file:line (CQ); "Standard bank vN" or
    a cited ADR/decision (SQ); custom-questions.md + the original heading
    text (UQ)>
  - **Finding:** <what was found and why it's uncertain>
  - **Why it matters:** <consequence of leaving this unresolved>
  - **Options:**
    1. <option>
    2. <option>
  - **Proposed default:** <an option number, or "adopt as-is">
  - **Answer:**
  - **Decided by:**
  - **Date:**

  Display ordering on every (re-)render: each family renders as its own
  section (Standard -> Custom -> Extracted, then Not applicable at the
  end), and WITHIN each family, blocking questions first, then grouped by
  Type in this fixed order — DECISION, DATA, SCOPE, DEFECT, MECHANICAL,
  TARGET-GAP, CONFLICT. IDs in all three families are permanent
  identifiers, not position — a re-run may move a block to a different
  place on the page but must never change its ID, and must never touch an
  Answer/Decided by/Date field a human has already filled in.

  A standard-bank question the agent judged inapplicable to this repo
  never disappears silently — it's listed one line each under "Not
  applicable" below, with the reason, for auditability.

  To answer a question: type your answer directly after "**Answer:**" (one
  line, or several up to the next "**Decided by:**" line), fill in
  "**Decided by:**" (your name) and "**Date:**" (YYYY-MM-DD). Then run
  `/specclaw:bf-clarify --resolve` to promote it into decisions.md.
-->

## Summary

**Total questions:** 39 (Extracted: 25, Standard bank: 12, Custom: 2)
**Blocking:** 22
**Unanswered:** 0
**By type:** DECISION: 15, DATA: 0, SCOPE: 11, DEFECT: 7, MECHANICAL: 2, TARGET-GAP: 4, CONFLICT: 0

## Standard Questions

### SQ-001 — Target platform

- **Type:** DECISION
- **Blocking:** yes
- **Source:** Standard bank v1 (references/clarify-standard-questions.md)
- **Finding:** The legacy system is a browser-based application only — a hand-written AngularJS 1.x SPA (`Client/app/scripts/**`, `ng-app="dentalApp"` per `codebase-report.md`'s Tech Stack) served over HTTP by an ASP.NET Web API 2 + OWIN backend (`DM.Server`), with no desktop or mobile client anywhere in the repository (`architecture.md`'s System Context: "browser client + Web API backend + SQL Server database"). Nothing in the analysis found any packaging for a native/mobile shell.
- **Why it matters:** The rebuild's target platform decision determines the entire technology stack (frontend framework choice, hosting model, and whether the AngularJS-era 20-screen `ui-inventory.md`/`screenshot-checklist.md` capture work is reusable as reference material) — this bank question exists precisely because no legacy code, browser-only or not, can force a "web" answer for the rebuild on its own.
- **Options:**
  1. Web application
  2. Desktop application
  3. Mobile application
  4. Hybrid / cross-platform
- **Proposed default:** unknown — no legacy-code signal determines this; ask explicitly.
- **Answer:** Option 1 — rebuild as a modern web application. Use React + TypeScript with Material UI (MUI) for the frontend, and ASP.NET Core Web API for the backend.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

### SQ-002 — Database engine and hosting

- **Type:** DECISION
- **Blocking:** yes
- **Source:** Standard bank v1 (references/clarify-standard-questions.md)
- **Finding:** The legacy app persists all data in SQL Server via Entity Framework 6, reached through two independently-migrated `DbContext`s (`DentalDbContext` and `ApplicationDbContext`) that both bind to the same named connection string `"DefaultConnection"` (`architecture.md` L2, `domain-model.md`'s Entities intro). The working-tree diff to `DM.Server/Web.config` (per `git status`) is mid-edit between `Data Source=.\SQLEXPRESS` and `Data Source=.` — a local dev-environment detail, not evidence about the production engine, which is SQL Server either way.
- **Why it matters:** Sixteen entities across five modules (`module-map.md`'s Coverage Check) and dozens of EF6 migrations going back to `201512281828016_InitialCreate.cs` (`codebase-report.md`) all assume SQL Server-specific behaviours (identity columns, unique indexes like `IX_Code`/`IX_Name`); keeping vs. migrating the engine changes how much of that migration history can be reused as-is versus needing a rewritten schema.
- **Options:**
  1. Keep the legacy database engine as-is.
  2. Migrate to a different engine sized for the target hosting model.
  3. Adopt a different persistence strategy entirely (state explicitly).
- **Proposed default:** unknown — no legacy-code signal determines this; ask explicitly.
- **Answer:** Option 2 — migrate from SQL Server to PostgreSQL. Use Entity Framework Core with the PostgreSQL provider. Create a clean PostgreSQL schema and new EF Core migrations rather than reusing the legacy EF6 SQL Server migration history directly.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

### SQ-003 — Hosting/deployment model

- **Type:** DECISION
- **Blocking:** yes
- **Source:** Standard bank v1 (references/clarify-standard-questions.md)
- **Finding:** The legacy app is deployed as IIS-hosted, precompiled artifacts — a `PrecompiledWeb/` tree and a `.pubxml` publish profile exist on disk, and `DentalManagement.sln` configures the `Client` website project's `AspNetCompiler` settings targeting `PrecompiledWeb\localhost_62262\` (`codebase-report.md` Tech Stack, `architecture.md` L2). `codebase-report.md`'s Domain section notes (low confidence) a hardcoded database name `Initial Catalog=MahmudaDentalDb` in `Web.config`, and no explicit multi-tenant/customer-selection mechanism was found anywhere opened — suggesting this may be a single-clinic, single-tenant on-prem install rather than SaaS.
- **Why it matters:** Whether the rebuild targets self-hosted/on-prem (matching the legacy deployment shape and its apparent single-tenant use) or a cloud multi-tenant model changes data isolation, connection-string management, and deployment-automation choices that the legacy app's IIS/precompiled-artifact deployment gives no signal on either way.
- **Options:**
  1. Self-hosted / on-prem, single-tenant.
  2. Cloud-hosted, single-tenant.
  3. Cloud-hosted, multi-tenant.
  4. Other (state explicitly).
- **Proposed default:** unknown — no legacy-code signal determines this; ask explicitly.
- **Answer:** Option 2 — cloud-hosted, single-tenant for the initial rebuild. Keep deployment configuration environment-based so the same solution can be self-hosted later if required. The application stack remains React + TypeScript + Material UI on the frontend, ASP.NET Core Web API on the backend, and PostgreSQL for persistence.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

### SQ-013 — UI fidelity policy

- **Type:** DECISION
- **Blocking:** yes
- **Source:** Standard bank v1 (references/clarify-standard-questions.md)
- **Finding:** `/specclaw:bf-ui` has already run in extract mode against this repo, producing `ui-inventory.md` (20 screens), `design-tokens.json` (4 token groups), and `screenshot-checklist.md` (37 capture rows across 18 capturable screens — SCR-019/About and SCR-020/Contact are listed under Not Capturable pending PQ-003), with 3 of those 37 rows already captured. This workstream is optional and only activates fully (per this bank entry's own Applicability note) if the human answers FAITHFUL or THEME-ONLY here — a REINTERPRET answer would leave the tokens/checklist work already invested as reference material only, not a grounding requirement for the rebuild.
- **Why it matters:** Capture work is already partially underway (3/37 rows) on the working assumption that visual fidelity matters to some degree; leaving this policy unanswered risks the team either continuing to invest in visual-parity capture that a REINTERPRET decision would make unnecessary, or abandoning it despite an eventual FAITHFUL/THEME-ONLY answer requiring exactly the SCR/TK grounding this checklist exists to produce.
- **Options:**
  1. FAITHFUL — reproduce the legacy layout structure and colour theme exactly (within the target platform's own rendering norms).
  2. THEME-ONLY — keep the colour palette / branding tokens; layout is reinterpreted for the target platform.
  3. REINTERPRET — new design; the legacy UI is reference material only.
- **Proposed default:** 3 (REINTERPRET — the least-work interpretation, and precisely why it must never be assumed silently: answer this one explicitly rather than letting a rebuild quietly discard a UI the users know).
- **Answer:** Option 2 — THEME-ONLY. Preserve the recognizable legacy colour palette, branding, terminology, and important visual cues, but rebuild the layouts as modern responsive React + Material UI components rather than reproducing the AngularJS/Bootstrap layout pixel-for-pixel.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

### SQ-005 — Existing production data

- **Type:** SCOPE
- **Blocking:** yes
- **Source:** Standard bank v1 (references/clarify-standard-questions.md)
- **Finding:** The legacy app has substantial real persisted business data across five modules — patients, per-visit bills ("Prescriptions"), billed service line items, payments, appointments, doctors, products, and stock movements (`domain-model.md` Entities, `module-map.md` Coverage Check: 16 entities). `codebase-report.md`'s Domain section flags (low confidence) that the app appears built for a single named clinic (`Initial Catalog=MahmudaDentalDb` in `Web.config`), which — if accurate — implies migration from one installation's live database rather than many, but this was not confirmed by any explicit multi-tenant/customer-selection code path.
- **Why it matters:** Sixteen entities' worth of real clinical/billing history is at stake — whether it migrates, and from how many installations, drives both the data-migration engineering effort and whether historical bills/payments remain auditable after the rebuild.
- **Options:**
  1. Migrate all existing production data.
  2. Start fresh with no data migration.
  3. Partially import (state which subset explicitly).
- **Proposed default:** unknown — no legacy-code signal determines this; ask explicitly.
- **Answer:** Option 1 — migrate all existing production data into PostgreSQL. Historical patient, billing, payment, appointment, doctor, product, stock, user, role, and permission data must remain available after migration. Include migration validation and reconciliation checks before production cutover.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

### SQ-004 — Authentication/authorization approach

- **Type:** TARGET-GAP
- **Blocking:** yes
- **Source:** Standard bank v1 (references/clarify-standard-questions.md)
- **Finding:** The legacy app has real authentication (ASP.NET Identity + OWIN OAuth bearer tokens against a `/Token` endpoint, 13-day expiry per `functional-spec.md`'s Login workflow) and a two-layer authorization model — role-based `[Authorize(Roles=...)]` on some controllers plus a separate `Resource`/`Permission` grant table checked only client-side on every AngularJS state transition (DR-015/DR-016, `domain-model.md`). However, `functional-spec.md` Named Gap #13 confirms **no server-side check ties the Resource/Permission model to the domain Web API controllers themselves** — `PatientController`, `ProductController`, etc. carry only the generic `[Authorize]` attribute, so a client bypassing the SPA and calling the API directly would not be blocked by DR-015/DR-016 at all. Seven OWIN social-login providers are referenced in `DM.Server.csproj` but every corresponding `app.UseXAuthentication(...)` call in `Startup.Auth.cs` is commented out (`architecture.md` System Context), and two seeded accounts ship with a shared hardcoded password `"123qwe"` (Named Gap #9).
- **Why it matters:** A rebuild that faithfully reproduces only the client-side permission gate would carry forward a real security gap (API-level authorization bypass) already present in the legacy app; the human needs to decide explicitly whether to preserve, fix, or re-architect auth/authz rather than have this decided implicitly by whichever layer happens to get rewritten first.
- **Options:**
  1. Preserve the legacy app's auth model as-is (including "none," if that's what it has).
  2. Add real authentication/authorization, sized to the target platform.
  3. Defer — ship without auth initially, add later (state the risk explicitly).
- **Proposed default:** unknown — no legacy-code signal determines this; ask explicitly.
- **Answer:** Option 2 — use ASP.NET Core Identity with secure token-based authentication suitable for the React SPA. Enforce role and Resource/Permission authorization on the server for every protected API operation. React route guards are only a UX feature and must not be the security boundary.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

### SQ-006 — UI framework / component library

- **Type:** DECISION
- **Blocking:** no
- **Source:** Standard bank v1 (references/clarify-standard-questions.md)
- **Finding:** The legacy UI layer is a hand-written AngularJS 1.x SPA with vendored (not package-managed) third-party libraries — `Client/Scripts/angular-ui/`, `Client/Scripts/angular-strap/`, and Bootstrap under `Client/Content/bootstrap/` (`architecture.md` L3, `codebase-report.md` Tech Stack) — built/minified via a Gulp pipeline rather than a modern npm/webpack toolchain. `functional-spec.md`'s UI Inventory documents 20 routed screens plus shared partials built entirely on this stack.
- **Why it matters:** AngularJS 1.x reached end-of-life years ago, and the whole client (391,101 JS LOC per `codebase-report.md`, much of it vendored) would need a real framework decision regardless of fidelity policy (see SQ-013) — this determines whether the 20 documented screens get rebuilt as a fresh SPA framework, a server-rendered app, or something else entirely.
- **Options:**
  1. Adopt a specific named framework/library (state which).
  2. Use the target platform's default/built-in components only.
  3. Undecided — defer to an implementation-time ADR.
- **Proposed default:** unknown — no legacy-code signal determines this; ask explicitly.
- **Answer:** Option 1 — use React with TypeScript and Material UI (MUI) as the component library. Map the legacy theme colours from design-tokens.json into a centralized MUI theme so the rebuilt UI keeps the recognizable legacy branding while using modern responsive components.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

### SQ-008 — Browser/device/OS support matrix

- **Type:** DECISION
- **Blocking:** no
- **Source:** Standard bank v1 (references/clarify-standard-questions.md)
- **Finding:** The legacy app is browser-only today — an AngularJS SPA whose layout is desktop-width-dependent (`screenshot-checklist.md`'s Setup Prerequisites explicitly calls out that capture requires "a desktop-width browser window" because Bootstrap's `col-lg-*`/`col-md-*` classes drive layout, and narrower widths trigger different, uncited responsive behavior) — with no evidence anywhere in the analysis of a defined browser/device support matrix or accessibility target. This question's own bank-defined applicability is keyed to the *rebuild's* target platform (SQ-001) being web/mobile/hybrid, which is not yet decided; it is surfaced here on the strength of the legacy app's own browser-only shape as the most likely continuation.
- **Why it matters:** The legacy Bootstrap-based layout was evidently tuned for desktop widths only, with no documented responsive/mobile behavior — if the rebuild's target platform (once SQ-001 is answered) is web or mobile, the human needs to decide explicitly whether to extend support to smaller viewports/accessibility levels the legacy app never targeted, rather than silently inheriting its desktop-only assumption.
- **Options:**
  1. Modern evergreen browsers only, no legacy support, standard accessibility (WCAG AA).
  2. Broader browser/device matrix (state explicitly).
  3. Not yet decided — defer to an implementation-time ADR.
- **Proposed default:** unknown — no legacy-code signal determines this; ask explicitly.
- **Answer:** Option 1 — support current evergreen Chrome, Edge, Firefox, and Safari. The React/MUI application should be responsive for normal desktop and tablet widths and usable on smaller screens where practical. Target WCAG AA accessibility for newly built UI.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

### SQ-010 — Non-functional targets

- **Type:** DECISION
- **Blocking:** no
- **Source:** Standard bank v1 (references/clarify-standard-questions.md)
- **Finding:** No data-volume, user-count, or performance target is documented or implied anywhere in the analysis — seed data is minimal (two staff accounts, one seeded doctor per `domain-model.md`'s Doctor entity and `screenshot-checklist.md`'s Setup Prerequisites), and the app appears built for a single small clinic (`codebase-report.md`'s low-confidence single-tenant inference). The `PatientController.Get()`/`Search()` N+1 query pattern (`architecture.md` L4, one extra query per patient) would only become a real performance risk at a patient-count scale the legacy app was never observed to reach.
- **Why it matters:** Whether the rebuild needs to design in paging/caching/indexing from day one — versus being safe to defer at single-clinic scale — changes both the urgency of fixing findings like the N+1 pattern above and the overall architecture (e.g. whether `GetGridList`'s current unbounded-then-`Take(100)` pattern remains adequate).
- **Options:**
  1. Small scale, no special performance work needed (state the rough numbers).
  2. Meaningful scale — paging/caching/indexing must be designed in from the start.
  3. Not yet known — defer to a later capacity-planning pass.
- **Proposed default:** unknown — no legacy-code signal determines this; ask explicitly.
- **Answer:** Design for a small-to-medium clinic workload but include normal production safeguards from the start: server-side paging/filtering, appropriate PostgreSQL indexes, avoidance of N+1 queries, async API/database operations, and basic response-time monitoring. Heavy distributed caching or high-scale infrastructure is not required initially.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

### SQ-012 — Fidelity default

- **Type:** DECISION
- **Blocking:** no
- **Source:** Standard bank v1 (references/clarify-standard-questions.md)
- **Finding:** The analysis already surfaced multiple legacy behaviours that are candidates for a fidelity-vs-improvement call once specific CQs are raised — e.g. the `PatientController` N+1/`.Last()` crash risk (`architecture.md` L4, promoted as a CQ from PQ-001), client-only business-rule enforcement with no server-side mirror (`functional-spec.md` Named Gap #2: DR-004/DR-005/DR-006/DR-008), and the API-level authorization gap (Named Gap #13). None of these has been decided yet — `.specclaw/analysis/decisions.md` does not exist for this repo.
- **Why it matters:** With this many open DEFECT/TARGET-GAP-flavoured findings already surfaced, a blanket fidelity default determines how each gets resolved if the team runs out of time to decide every one individually — the bank's proposed default (faithful reproduction unless a specific CQ overrides it) is the safer starting point for a fidelity-focused rebuild of a system with this many undocumented behavioural quirks.
- **Options:**
  1. Default to faithful reproduction of legacy behaviour unless a specific CQ says otherwise.
  2. Default to the "obviously better" behaviour unless a specific CQ says otherwise.
  3. No blanket default — decide case by case as each CQ/DEFECT question arises.
- **Proposed default:** 1 (adopt as-is by default — the safer default for a fidelity-focused rebuild; a specific DEFECT/CQ can always override it for one behaviour at a time).
- **Answer:** Option 3 — decide case by case. Preserve valid business behaviour by default, but do not automatically preserve confirmed security gaps, crashes, invalid-data behaviour, dead UI, or implementation accidents. Every intentional divergence from legacy behaviour must be tied to a decided CQ.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

### SQ-009 — Reporting/printing/export behaviours

- **Type:** SCOPE
- **Blocking:** no
- **Source:** Standard bank v1 (references/clarify-standard-questions.md)
- **Finding:** The legacy app has multiple genuine print/report capabilities: a payment-receipt print modal, an appointment-copy print modal, a Patient Payment Report with its own print modal, a Stock Report, and an Inventory Report with its own print modal (`functional-spec.md` capabilities #11, #12, #19, #23, and its Workflows section referencing `patientPaymentModal.html`, `patientAppointmentModal.html`, `patientReportModal.html`, `inventoryHistoryReportModal.html`, `inventoryReportModal.html`, all confirmed opened per the UI Inventory table). All five use `window.print()` against an inline HTML template rather than a dedicated PDF/export engine.
- **Why it matters:** Five distinct print/report surfaces built on a browser-print mechanism (not a structured export format like PDF/CSV) need an explicit call on whether the rebuild reproduces browser-print behaviour exactly, replaces it with a modern export format, or drops any of the five — silently reproducing raw `window.print()` in a new stack may not behave identically across browsers/platforms.
- **Options:**
  1. Reproduce the legacy behaviour exactly.
  2. Replace with a modern equivalent (state what).
  3. Drop entirely (state why it's safe to drop).
- **Proposed default:** unknown — no legacy-code signal determines this; ask explicitly.
- **Answer:** Option 2 — keep all existing report and receipt capabilities, but implement them as modern print-friendly React/MUI views. Where useful, provide PDF export for printable reports/receipts and CSV export for tabular reports. Exact legacy window.print() implementation is not required.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

### SQ-011 — Operational requirements

- **Type:** SCOPE
- **Blocking:** no
- **Source:** Standard bank v1 (references/clarify-standard-questions.md)
- **Finding:** No backup strategy, logging/monitoring framework, or CI/CD deployment pipeline was found anywhere in the analysis — `codebase-report.md`'s Dependencies section lists no logging library (no log4net/NLog/Serilog among the pinned NuGet packages), and the only deployment artifacts found are the manual IIS-precompilation shape (`PrecompiledWeb/`, a `.pubxml` publish profile) rather than an automated pipeline. `codebase-report.md`'s Risks section separately notes test coverage is effectively nil (`DM.Server.Tests/UnitTest1.cs` is an empty stub), reinforcing the picture of minimal operational tooling of any kind.
- **Why it matters:** A rebuild that just faithfully reproduces legacy behaviour would, by default, also reproduce its complete absence of backups/monitoring/CI-CD — the human needs to decide explicitly whether standard operational tooling gets added from day one or deliberately deferred, since nothing in the legacy code will surface this gap on its own.
- **Options:**
  1. Add standard backups/logging/monitoring/CI-CD from day one.
  2. Defer operational tooling to a later phase (state the risk explicitly).
  3. Not applicable — the target hosting environment already provides this (state which).
- **Proposed default:** unknown — no legacy-code signal determines this; ask explicitly.
- **Answer:** Option 1 — include structured application logging, centralized error monitoring, PostgreSQL backup/restore procedures, health checks, environment-based configuration/secrets, and automated CI/CD from the beginning of the rebuild.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

## Custom Questions

### UQ-002 — Do we need a mobile app eventually?

- **Type:** DECISION
- **Blocking:** no
- **Source:** .specclaw/analysis/custom-questions.md — "Do we need a mobile app eventually?"
- **Finding:** Author-defined question from custom-questions.md; not derived from analysis-doc extraction.
- **Why it matters:** Not stated by the author — the repo's maintainer flagged this as needing a decision.
- **Options:**
  - Yes — plan the architecture to support one later.
  - No — web-responsive is enough.
- **Proposed default:** unknown — not specified by the author
- **Answer:** No dedicated mobile application is required in the current scope. Build a responsive React + Material UI web application and keep the ASP.NET Core API cleanly separated so a mobile client can be added later without redesigning the backend.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

### UQ-001 — Should offline mode be supported?

- **Type:** SCOPE
- **Blocking:** no
- **Source:** .specclaw/analysis/custom-questions.md — "Should offline mode be supported?"
- **Finding:** Author-defined question from custom-questions.md; not derived from analysis-doc extraction.
- **Why it matters:** Not stated by the author — the repo's maintainer flagged this as needing a decision.
- **Options:**
  - Yes — add offline-first sync.
  - No — always-online is fine for this rebuild.
- **Proposed default:** No — no legacy behaviour requires offline support.
- **Answer:** No — the rebuild will be an online web application. Offline-first synchronization is outside the current scope.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

## Extracted Questions

### CQ-002 — Should a rebuild target consolidate the two independently-migrated EF DbContexts (DentalDbContext + ApplicationDbContext) sharing one physical database into a single schema/context, or preserve the split?

- **Type:** DECISION
- **Blocking:** yes — L2 "SQL Server Database" container and its two sub-schema nodes (architecture.md), L3 "Identity & Permission Subsystem" component, any future rebuild-backlog data-layer item
- **Source:** Promoted from PQ-002 (bf-architecture-analyst (architecture.md L2/L3), Trigger T5) — L2 "SQL Server Database" container and its two sub-schema nodes (architecture.md), L3 "Identity & Permission Subsystem" component, any future rebuild-backlog data-layer item
- **Finding:** `DM.Server/App_Start/UnityConfig.cs` registers both `container.RegisterType<DbContext, ApplicationDbContext>(...)` and `container.RegisterType<DbContext, DentalDbContext>(...)` in the same Unity container. `DM.Server/Models/ApplicationDbContext.cs` (`IdentityDbContext<ApplicationUser>`, connection name `"DefaultConnection"`) and `DM.Models/DentalDbContext.cs` (`base("name=DefaultConnection")`) both bind to the same named connection string. `DentalDbContext`'s static constructor calls `Database.SetInitializer<DentalDbContext>(null);` with the comment "The schema is owned by ApplicationDbContext's migrations, so skip EF's model-hash check for this context." Each context has its own independent `Migrations/` folder and history (`DM.Models/Migrations/*` vs `DM.Server/Migrations/*`).
- **Why it matters:** Until this is resolved, the L2 "SQL Server Database" container, its two sub-schema nodes, the L3 "Identity & Permission Subsystem" component, and any rebuild-backlog data-layer item derived from architecture.md must be held PROVISIONAL, since the target schema shape (one context vs. two) directly determines how that data layer is designed and migrated.
- **Options:**
  Could not determine: Whether this dual-context split was a deliberate design decision (e.g. to keep Identity/permission schema changes decoupled from domain schema changes) or accidental drift from scaffolding two separate Visual Studio templates (ASP.NET Identity template + a hand-rolled EF Code-First model) into one project — no comment or doc explaining the original intent was found beyond the workaround comment itself.
  1. Preserve the two-context split as-is in any rebuild target, since some modern stacks (e.g. a dedicated auth/identity service vs. a domain service) have a natural equivalent.
  2. Consolidate into a single DbContext/schema in the rebuild target, since both contexts already share one physical database and the split appears to be undocumented legacy structure rather than an intentional bounded-context separation.
  3. Split into two genuinely separate databases/services in the rebuild target, formalizing what is currently an implicit split.
- **Proposed default:** Consolidate into a single context/schema in the rebuild target (option 2) — the current split shares one physical database and has no documented rationale beyond a workaround comment disabling EF's own safety check, which suggests unintentional drift rather than a deliberate bounded-context boundary worth preserving.
- **Answer:** Option 2 — consolidate into one PostgreSQL database and one EF Core application DbContext/schema for the initial rebuild. Authentication/Identity tables and domain tables may remain logically separated by configuration/naming, but they should share one controlled migration history instead of two independent migration pipelines.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

### CQ-004 — Which navbar background color is actually rendered: style.css's `.navbar` rule or nav.html's own inline `.navbar-default` rule?

- **Type:** DECISION
- **Blocking:** yes — TK-003 in design-tokens.json (global navbar background token group); the "Layout structure"/top-nav-band bullet on every SCR-### screen entry in ui-inventory.md (the shared nav chrome is present on all 20 screens)
- **Source:** Promoted from PQ-004 (/specclaw:bf-ui (bf-ui-analyst), Trigger T6) — TK-003 in design-tokens.json (global navbar background token group); the "Layout structure"/top-nav-band bullet on every SCR-### screen entry in ui-inventory.md (the shared nav chrome is present on all 20 screens)
- **Finding:** `Client/app/styles/style.css:16-20` defines `.navbar { background: #006A4E; ... }`, loaded via a `<link>` in `Client/index-dev.html:16` (parsed at initial page load, inside `<head>`). `Client/app/views/nav.html:1-4` defines its own inline `<style>.navbar-default { background-color: #218283 !important; }</style>` immediately followed by `<nav class="nav navbar navbar-fixed-top navbar-default" ...>` (`nav.html:20`) — this `<style>` tag is injected into the DOM only when `index-dev.html:23`'s `<header ng-include="'app/views/nav.html'"></header>` resolves at runtime (after initial page load), and additionally uses `!important`, which by CSS cascade rules always wins over a non-`!important` declaration of equal or lower specificity regardless of source order. Both `.navbar` and `.navbar-default` are present as classes on the same `<nav>` element.
- **Why it matters:** Until this is resolved, TK-003 (the global navbar background token group in design-tokens.json) and the top-nav-band bullet on every one of the 20 SCR-### screen entries in ui-inventory.md must stay PROVISIONAL, since the shared nav chrome's effective color is a value every screen's visual baseline depends on.
- **Options:**
  Could not determine: Whether `.navbar-default`'s `!important` declaration is genuinely rendered by a browser reliably in every case (it should, per CSS spec, since `!important` beats a non-`!important` rule outright, making this actually resolvable rather than a true tie) — this was flagged as uncertain rather than asserted outright because the running application was not visually observed to confirm no other stylesheet (e.g. a browser extension, a later-loaded vendor CSS, or an `!important` override not found elsewhere in `Content/bootstrap/` or `Content/less/`) contests it.
  1. `.navbar-default`'s `#218283` wins (the `!important` declaration, per CSS cascade rules, should make this the effective color).
  2. `.navbar`'s `#006A4E` wins (if some other mechanism not found overrides or strips the `!important`).
  3. Record both as candidate values and let a human confirm visually via a screenshot rather than asserting either.
- **Proposed default:** Candidate 1, `#218283` — CSS's `!important` mechanism is a very strong, well-defined tiebreaker that should make `nav.html`'s own rule win over `style.css`'s `.navbar` rule regardless of load order, but a human should confirm this visually against an actual screenshot before it is treated as settled, since this document's own hard constraint is to never assert a computed/rendered color without that confirmation.
- **Answer:** Option 1 — use #218283 as the effective legacy navbar colour and map it into the React/MUI theme. The captured legacy screenshots remain the final visual reference if any contradiction is observed.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

### CQ-006 — Should the shared `Status` lookup table become four separate typed enumerations in the rebuild?

- **Type:** DECISION
- **Blocking:** yes — domain-model.md's Status entity/Enumerations section; module-map.md's "Unassigned: Status" entry referenced by MOD-001/MOD-003/MOD-004
- **Source:** domain-model.md § Enumerations, item 2 ("Status lookup table") — "this one shared table is reused as four *separate* enumerations depending on which entity's `StatusId` points at it... there is no code-level partition of the `Status` table by entity, so a future insert could in principle assign an 'In Stock'-flavoured status to a `Prescription` row with nothing to stop it."
- **Finding:** `Prescription` (5=Active/6=Closed), `Product`/`Inventory` (1=In Stock/2=Out Of Stock, 3=Received/4=Shipped), and `Appointment` (7=Appointed/8=Visited) all share one `Status` table/FK with no schema-level partition — the grouping is inferred purely from which numeric literals appear in which controllers, and nothing prevents an invalid cross-entity status assignment today.
- **Why it matters:** A rebuild that faithfully copies this single shared table forward inherits the same unguarded cross-assignment risk; a rebuild that instead models four separate, type-safe enumerations changes the schema/migration shape significantly and is a one-way design fork that legacy code cannot answer for us.
- **Options:**
  1. Preserve a single shared status/lookup concept, mirroring legacy structure as-is.
  2. Split into four separate, type-safe enumerations (one per consuming entity), each independently validated.
  3. Split into separate enumerations only for the entities most at risk of cross-assignment bugs, leaving the rest shared.
- **Proposed default:** 2
- **Answer:** Option 2 — replace the shared Status lookup with separate typed status concepts for Prescription/Bill, Product, Inventory Movement, and Appointment. Preserve the existing semantic values during migration but enforce valid status values per entity in the .NET domain/service layer.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

### CQ-007 — Should `Patient.Gender` become a real typed enum in the rebuild, or stay a free-form string?

- **Type:** DECISION
- **Blocking:** yes — domain-model.md's Patient entity and Enumerations §1 (`Gender`); ui-inventory.md SCR-003/SCR-004 Gender select widgets
- **Source:** domain-model.md § Enumerations, item 1 — "`Patient.Gender` is declared as a plain `string`, not this `Gender` enum type — the enum exists in the same file but is never actually used as a property type anywhere I opened; the AngularJS form... independently hardcodes the option list `[\"Male\",\"Female\",\"Others\"]` as plain strings... nothing in the code ties them together."
- **Finding:** A `Gender` enum (`Male=1, Female=2, Others=3`) is declared in `DM.Models/Patient.cs` but never used — `Patient.Gender` is a plain string, and the UI hardcodes its own matching option list independently. The two happen to agree today only by coincidence, not by any enforced contract.
- **Why it matters:** If the rebuild adopts a genuine enum-backed `Gender` field, any legacy row with a value outside `{"Male","Female","Others"}` (a typo, blank, or free-text value never blocked server-side) would need explicit migration handling; if it stays a free string, the rebuild inherits the same lack of validation and drift risk between UI dropdown options and stored data.
- **Options:**
  1. Formalize `Gender` as a real enum/lookup type in the rebuild, matching the existing (currently unused) enum's member names, with a data-migration step to normalize any non-conforming legacy values.
  2. Keep `Gender` a free-form string field, preserving current flexibility and validation behavior exactly.
  3. Use an enum for the UI-offered options but keep the persisted column a nullable/free string as an escape hatch for legacy data that doesn't match.
- **Proposed default:** 1
- **Answer:** Option 1 — formalize Gender as a typed enum/lookup in the rebuild and migrate the existing Male, Female, and Others values explicitly. Before migration, report any legacy values outside the known set so they can be reviewed rather than silently discarded.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

### CQ-015 — Should the rebuild support assigning multiple roles per user, matching the domain model's `ApplicationUser.Roles` collection, or preserve the UI's single-role-per-user constraint?

- **Type:** DECISION
- **Blocking:** yes — domain-model.md ApplicationUser entity; ui-inventory.md Widget Cross-Reference Finding item 3; module-map.md MOD-005
- **Source:** ui-inventory.md § Widget Cross-Reference Findings item 3 — "domain-model.md documents `Roles` as a collection (via `IdentityUserRole`), implying a user can hold more than one role, but the Manage Users screen's Role field is a single `<select>` bound to the singular `model.RoleId` — the UI can only ever assign exactly one role per user."
- **Finding:** ASP.NET Identity's underlying model (and `domain-model.md`'s documentation of it) supports a many-to-many User↔Role relationship, but the only UI for assigning roles (`user.tpl.html`) is a single-select dropdown, so every real user in this system holds exactly one role by construction of the UI, never by a model-level constraint.
- **Why it matters:** If the rebuild's rules (e.g. DR-014's SystemAdmin-hiding logic, or `AppService.nextRoute()`'s `user.RoleNames[0]` branching) implicitly assume "one role per user" throughout, genuinely enabling multi-role assignment could break those assumptions elsewhere unless audited; conversely, preserving the single-role UI constraint means the underlying collection-typed model is carrying unused capability the rebuild doesn't need to replicate.
- **Options:**
  1. Preserve single-role-per-user as an enforced constraint in the rebuild (simplify the model to match actual usage).
  2. Support genuine multi-role assignment in the rebuild UI, auditing every role-dependent code path (DR-014, `nextRoute()`, etc.) for a "first role only" assumption that would need to change.
  3. Keep the underlying model capable of multiple roles (for future flexibility) but keep the UI single-select for now, exactly as legacy does.
- **Proposed default:** 1
- **Answer:** Option 1 — preserve and explicitly enforce one primary role per user in this rebuild because that matches the observed legacy UI behaviour. Fine-grained Resource/Permission grants remain separate from the user's primary role.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

### CQ-019 — Which binding actually determines the persisted `Resource.IsPublic` value on the Manage Resources screen — the radio's native `value` attribute or its Angular `ng-value`?

- **Type:** DECISION
- **Blocking:** yes — functional-spec.md Named Gaps item 8; ui-inventory.md SCR-015 and Named Gaps item 10; domain-model.md Resource entity
- **Source:** functional-spec.md § Named Gaps item 8 — "`resource.tpl.html`'s 'Public' radio group binds both a plain `value` attribute ('0'/'1') and an Angular `ng-value` (`isPublicEnum.False`/`isPublicEnum.True`) to the same `ng-model=\"model.IsPublic\"`. The widget itself... is unambiguous, but which binding actually determines the persisted boolean was not fully verified from static markup alone."; ui-inventory.md § Named Gaps item 10 repeats the same finding.
- **Finding:** The Public Yes/No radio pair on the Manage Resources screen double-binds two different value sources (a plain HTML `value` attribute and an Angular `ng-value` expression) to the same model — which one AngularJS actually honors at runtime for this directive/version combination was not determinable from static reading alone.
- **Why it matters:** `Resource.IsPublic` directly controls DR-015's authorization gate (a public resource bypasses the permission check entirely) — if the "wrong" binding has been winning in production, some resources may have been marked public/private opposite to what an admin intended via this screen, a live security-relevant ambiguity worth confirming before the rebuild's equivalent form is built.
- **Options:**
  1. Confirm the actual rendered/persisted behavior by exercising the running legacy app (toggle the radio, save, inspect the persisted `IsPublic` value) before deciding what the rebuild's equivalent screen should do.
  2. Assume `ng-value` wins (the more specific/modern AngularJS binding) and build the rebuild's form accordingly, without further legacy verification.
  3. Rebuild the form cleanly with a single unambiguous binding (dropping the double-binding entirely), independent of which one legacy actually used.
- **Proposed default:** 1
- **Answer:** Option 3 — the React rebuild will use one explicit boolean IsPublic field with a single unambiguous Material UI control and server-side validation. Do not reproduce the AngularJS double-binding defect. Existing persisted boolean values will be migrated as stored.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

### CQ-003 — Should the About/Contact screens' broken controller wiring be fixed (implement `AboutController`/`ContactController`), or are these screens out of scope for a rebuild?

- **Type:** SCOPE
- **Blocking:** yes — SCR-019 (About), SCR-020 (Contact) in ui-inventory.md; the "View the About page" / "View the Contact page" capabilities in functional-spec.md (items 34–35) and its Named Gaps #5/#6
- **Source:** Promoted from PQ-003 (/specclaw:bf-ui (bf-ui-analyst), Trigger T4) — SCR-019 (About), SCR-020 (Contact) in ui-inventory.md; the "View the About page" / "View the Contact page" capabilities in functional-spec.md (items 34–35) and its Named Gaps #5/#6
- **Finding:** `Client/app/scripts/app.config.js:72-89` registers UI-Router states `root.about` (template `app/views/about/about.tpl.html`, controller `AboutController`) and `root.contact` (template `app/views/contact/contact.tpl.html`, controller `ContactController`). `Client/app/scripts/about/about.controller.js` contains only the comment `// code goes here` (no `angular.module(...).controller("AboutController", ...)` registration anywhere in the repo — confirmed by `grep -rn "AboutController"` returning only the `app.config.js` reference). No `ContactController` definition exists anywhere under `Client/app/scripts/` (same grep result — only the `app.config.js` reference). `Client/index-dev.html`'s `<script>` list (lines 58-94) does not include `about.config.js`, `about.controller.js`, `about.service.js`, or any contact script at all — these files are only ever picked up by the Gulp "scripts" task's glob (`Gulpfile.js:100-111`, `./app/scripts/**/*.controller.js` etc.) for the production bundle, but even then `about.controller.js`'s content never registers the controller, and no `contact.controller.js` file exists to be globbed at all. `Client/app/views/about/about.tpl.html` and `Client/app/views/contact/contact.tpl.html` are each a single literal word ("about" / "Contact") with no bindings or controls.
- **Why it matters:** Until this is resolved, SCR-019/SCR-020 in ui-inventory.md and the corresponding "View the About page"/"View the Contact page" capabilities and Named Gaps #5/#6 in functional-spec.md must stay PROVISIONAL, since whether these screens belong in the rebuild at all (and, if so, whether they need real controller logic) is unresolved.
- **Options:**
  Could not determine: Whether navigating to `root.about`/`root.contact` in a running instance throws an AngularJS dependency-injection error that blocks the view from rendering at all, or whether the plain-text template still becomes visible despite a console error — the application was not run to observe this, and static reading of the AngularJS/ui-router source alone does not settle it for certain across the ui-router version in use.
  1. Treat as a DEFECT — implement real `AboutController`/`ContactController` scripts (even trivial ones) and wire them into the script/bundle list so both routes render without a DI error.
  2. Treat as a TARGET-GAP/SCOPE call — these are placeholder marketing-style pages with no business logic, and a rebuild may reasonably drop them entirely or replace them with static content with no dedicated controller.
  3. Leave as-is on the assumption these routes are never exercised in practice (no nav link anywhere points at either state).
- **Proposed default:** Treat as SCOPE/TARGET-GAP (option 2) — both templates contain zero real content or controls today, no in-app navigation link reaches either state (confirmed by `grep` across `Client/app/views/**` for `root.about`/`root.contact` `ui-sref` usage returning no results beyond `app.config.js`'s own route registration), and the wiring bug has evidently gone unnoticed in production, suggesting these are vestigial placeholder pages rather than a live user-facing gap worth preserving pixel-for-pixel.
- **Answer:** Option 2 — exclude the broken legacy About and Contact routes from the rebuild scope. They contain no meaningful business functionality and are not reachable from the live navigation. If an About/Help page is requested later, implement it as a simple static React page as a new requirement.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

### CQ-005 — Is the rebuild target expected to remain single-clinic, or must it support multi-tenancy?

- **Type:** SCOPE
- **Blocking:** yes — architecture.md's System Context/L1 framing ("a single-clinic dental-practice management application") and every entity in domain-model.md (none carries a clinic/tenant identifier); any rebuild-backlog item that assumes a single organization-wide dataset
- **Source:** codebase-report.md § Domain — "Inference (low confidence): The product is operated by (or was built for) a single named clinic rather than sold as multi-tenant software — the uncommitted `DM.Server/Web.config` diff still references a hardcoded database name `Initial Catalog=MahmudaDentalDb`... I did not find an explicit multi-tenant/customer-selection mechanism in the files I opened."; architecture.md § System Context repeats the same single-clinic framing.
- **Finding:** Every analysis document treats this as a single-clinic system (one Patient table, one Doctor roster, one Product/Inventory catalog, no clinic/tenant identifier on any entity), but this is explicitly flagged as a low-confidence inference — the only supporting evidence is a hardcoded database name (`MahmudaDentalDb`) in the working-tree `Web.config` diff and an implicit README reference; no explicit multi-tenant mechanism, and no explicit single-tenant confirmation, was found anywhere.
- **Why it matters:** If the rebuild must support multiple independent clinics/tenants, every entity (Patient, Doctor, Product, ApplicationUser, etc.) would need a tenant/organization identifier added, and every query/permission check would need tenant-scoping — a foundational decision that is very costly to retrofit later. If single-clinic is confirmed, none of that scoping work is needed.
- **Options:**
  1. Preserve single-clinic scope — no tenant/organization concept in the rebuild, matching the legacy data model exactly.
  2. Add multi-tenant support as a rebuild requirement, with tenant-scoping designed in from the start.
  3. Single-clinic for launch, but design the data model to make adding tenant-scoping later straightforward (e.g. reserve a nullable OrganizationId column).
- **Proposed default:** 1
- **Answer:** Option 1 — preserve the current single-clinic scope. Do not add tenant identifiers or multi-tenant query complexity to the initial rebuild without a real business requirement.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

### CQ-018 — Are the orphaned, unrouted template files (`patient-report-2.tpl.html`, `denied.tpl.html`) abandoned drafts to drop, or unfinished features to complete in the rebuild?

- **Type:** SCOPE
- **Blocking:** yes — functional-spec.md Named Gaps item 7; ui-inventory.md Named Gaps item 1; module-map.md Unassigned section
- **Source:** functional-spec.md § Named Gaps item 7 — "`patient-report-2.tpl.html` exists on disk but is not referenced by any route config I opened — likely an abandoned alternate version of the patient-report screen"; ui-inventory.md § Named Gaps item 1 — "`Client/app/views/auth/denied.tpl.html` is an orphaned, unrouted template, distinct from the actually-wired `access-denied.tpl.html` (SCR-018). It references a function `backToDefaultRoute()` that is defined nowhere in the codebase... and no `$stateProvider.state(...)` registration anywhere references `denied.tpl.html`."
- **Finding:** Two template files exist on disk with no route wiring them up at all — `patient-report-2.tpl.html` (a likely abandoned alternate Patient Payment Report layout) and `denied.tpl.html` (a likely superseded predecessor to the actually-used `access-denied.tpl.html`, referencing an undefined function). Neither is reachable by any user today.
- **Why it matters:** If either represents an abandoned draft of a feature that was actually intended to replace its "live" sibling (e.g. `patient-report-2` was meant to supersede `patient-report.tpl.html` but the route switch was never finished), the rebuild might be missing an intended improvement rather than dropping true dead code. Without a human decision, an agent can't distinguish "delete this" from "finish this."
- **Options:**
  1. Treat both as abandoned dead code — drop them entirely, rebuilding only the actually-wired screens (`patient-report.tpl.html`, `access-denied.tpl.html`).
  2. Review each template's content for a genuinely different/improved design before dropping, in case either represents unfinished-but-intended work.
  3. Drop `denied.tpl.html` (references an undefined function, clearly broken) but review `patient-report-2.tpl.html` specifically, since it's a fuller alternate report layout rather than an obviously broken file.
- **Proposed default:** 1
- **Answer:** Option 1 — treat both templates as dead/unreachable legacy code and do not rebuild them. Only the actually wired Patient Report and Access Denied screens are in scope.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

### CQ-001 — Is the PatientController.Get()/Search() N+1 `.Last()` crash-on-empty-sequence pattern intentional legacy behavior to preserve, or a defect to fix in the rebuild?

- **Type:** DEFECT
- **Blocking:** yes — L3/L4 "Domain API Controllers" component (architecture.md), any future rebuild-backlog item covering the Patients list/search endpoint
- **Source:** Promoted from PQ-001 (bf-architecture-analyst (architecture.md L4), Trigger T4) — L3/L4 "Domain API Controllers" component (architecture.md), any future rebuild-backlog item covering the Patients list/search endpoint
- **Finding:** `DM.Server/Controllers/PatientController.cs` — both `Get()` and `Search(string request)` call `List<Patient> patients = _patientCreateService.GetAll();` then, inside a `foreach (Patient patient in patients)`, call `_prescriptionService.GetPatientCurrentPrescription(patient.Id).Last();` — one extra query per patient (N+1), and `.Last()` throws `InvalidOperationException` on an empty sequence (e.g. a patient with zero prescriptions). No try/catch or null-guard surrounds either call.
- **Why it matters:** Until this is resolved, the Patients list/search endpoint's crash risk stays an open question rather than a confirmed defect or confirmed-safe behavior, and any rebuild-backlog item covering it (plus the L3/L4 "Domain API Controllers" component) must be held PROVISIONAL rather than finalized.
- **Options:**
  Could not determine: Whether every `Patient` row in the live database is guaranteed (by an invariant enforced elsewhere, e.g. at patient-creation time) to always have at least one `Prescription`, which would make the crash theoretical rather than a live defect — I did not open `DM.Repository/PatientCreateRepository.cs`'s creation path or `DM.Service/PatientCreateService.cs` to confirm such an invariant is enforced.
  1. Treat as a known DEFECT to fix in the rebuild (add a null/empty check, batch-load prescriptions instead of N+1).
  2. Treat as intentional/acceptable because a create-patient flow always seeds one prescription record, making this dead-code risk rather than a live bug.
  3. Preserve the exact current behavior (including the crash) for parity/replay-testing purposes until proven safe to change.
- **Proposed default:** Treat as a DEFECT to fix in the rebuild (option 1) — an unhandled `InvalidOperationException` on a plausible empty-collection input is a stronger presumption than an unverified creation-time invariant, and fixing it (empty-check + batched prescription lookup) is unlikely to break any current caller since the failure mode today is a hard 500, not a meaningful business rule.
- **Answer:** Option 1 — treat it as a defect. The rebuild must safely handle patients with no prescription and avoid N+1 database queries by loading/projecting the required prescription information efficiently through EF Core.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

### CQ-008 — Should `MedicalService.Charge`/`TotalCharge`'s string-to-int currency truncation (DR-019) be preserved or fixed in the rebuild?

- **Type:** DEFECT
- **Blocking:** yes — domain-model.md DR-019, MedicalService entity; module-map.md MOD-002 business rules
- **Source:** domain-model.md § Entities item 4 and § Business Rules DR-019 — `MedicalService.Charge` is `[DataType(DataType.Currency)] string`, and `[NotMapped] TotalCharge` is computed as `Convert.ToInt32(Charge) * Quantity`, "which truncates any fractional currency amount and throws for a non-integer string... no comment explains the choice of `int` over a decimal type."
- **Finding:** A monetary field (`Charge`) is stored as a raw string rather than a numeric currency type, and its derived total truncates any fractional value and throws `FormatException` on a non-integer string. This is not flagged as a deliberate design in any comment — it reads as an oversight, not an intentional constraint.
- **Why it matters:** If any existing catalog `Charge` value contains a fractional amount (e.g. "45.50"), the legacy app already silently truncates it when computing `TotalCharge` on the Add-Services screen — a rebuild that "faithfully reproduces" this would carry forward silent revenue-calculation errors; a rebuild that fixes it (proper `decimal` type, no truncation) changes historical totals compared to what legacy computed for the same input.
- **Options:**
  1. Treat as a defect to fix in the rebuild — store `Charge` as a proper decimal/currency type and compute `TotalCharge` without truncation.
  2. Preserve the exact truncating/throwing legacy behavior for parity, since no complaint or workaround was found suggesting anyone hit the throw case in production.
  3. Fix the storage type but explicitly document/test that truncation behavior is intentionally dropped, requiring a data audit of existing `Charge` values first.
- **Proposed default:** 1
- **Answer:** Option 1 — fix the defect. Use a proper decimal money type in .NET and a fixed-precision numeric/decimal column in PostgreSQL. TotalCharge must retain fractional currency values and must not truncate to integer values. Audit legacy Charge strings during migration and explicitly report values that cannot be parsed.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

### CQ-011 — Should DR-004/DR-005/DR-006/DR-008's client-side-only business rules gain server-side enforcement in the rebuild?

- **Type:** DEFECT
- **Blocking:** yes — domain-model.md DR-004, DR-005, DR-006, DR-008; functional-spec.md Named Gaps item 2
- **Source:** functional-spec.md § Named Gaps item 2 — "Several client-side-only business rules have no server-side mirror: discount-percent range (DR-004), payment-not-exceeding-due (DR-005), bill-close-blocked-while-due (DR-006, though 'Force' exists as an intentional override), and shipment-not-exceeding-on-hand (DR-008) are all enforced only in AngularJS controllers. A rebuild that faithfully reimplements the Web API controllers but omits the equivalent Angular logic would silently accept data the legacy app blocks."
- **Finding:** Four distinct business rules — discount 0–100% bounds, payment not exceeding due balance, bill-close blocked while due>0 (unless forced), and shipment not exceeding on-hand — are enforced purely in the AngularJS layer today; the corresponding Web API controllers (`PrescriptionController.Put`, `PaymentController.Post`, `InventoryController.Post`) perform no equivalent check, so any direct API call bypasses all four.
- **Why it matters:** This is a live defect today (a non-SPA client could already violate these rules against the legacy database), and it's exactly the kind of gap a naive rebuild ("port the controllers, forget the Angular logic") would silently reproduce or even worsen if the rebuild's client layer is restructured differently.
- **Options:**
  1. Treat as a defect — add equivalent server-side validation for all four rules in the rebuild, matching the client-side thresholds/messages.
  2. Preserve exactly as-is (client-only enforcement) for behavioral parity with legacy, deferring server-side hardening to a later phase.
  3. Add server-side enforcement for some but not all four, prioritized by risk (e.g. payment-overage and shipment-overage first, discount-range and bill-close-guard later).
- **Proposed default:** 1
- **Answer:** Option 1 — enforce all four rules on the ASP.NET Core backend as authoritative business rules and mirror them in React for immediate UX feedback. Direct API calls must never be able to bypass these validations.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

### CQ-012 — Should DR-018's "replace scoped to the first submitted item's PrescriptionId" behavior be made explicit/enforced in the rebuild?

- **Type:** DEFECT
- **Blocking:** yes — domain-model.md DR-018; functional-spec.md Named Gaps item 3
- **Source:** domain-model.md § Business Rules DR-018 — "`DM.Repository/PatientMedicalServiceRepository.cs`'s `AddList` deletes every existing `PatientMedicalService` row for the **first** submitted item's `PrescriptionId` (the `foreach` loop `break`s after processing one item), then inserts every item in the new list. This is correct only because its one caller... always submits a list scoped to a single `PrescriptionId` — nothing in the type system enforces that assumption."; functional-spec.md § Named Gaps item 3 repeats the same finding.
- **Finding:** The replace-then-insert logic for a bill's service line items silently assumes every item in the submitted batch shares one `PrescriptionId` (taken from the first item only) — a correct assumption today only because the one caller happens to always submit such a batch; nothing in the API contract or type system enforces it.
- **Why it matters:** If the rebuild's API shape ever allows (even accidentally) a mixed-`PrescriptionId` batch — e.g. a future bulk-edit feature, or a client bug — the current logic would silently delete the wrong bill's line items rather than erroring. A rebuild has to decide whether to keep this implicit assumption or make it an explicit, enforced contract.
- **Options:**
  1. Preserve as-is — keep the "replace scoped to first item's PrescriptionId" behavior unenforced, since the current single caller never violates it.
  2. Add explicit validation that rejects a submitted list containing more than one distinct PrescriptionId.
  3. Redesign the endpoint contract so it takes a single PrescriptionId parameter plus a list of line items, removing the ambiguity from the API shape itself.
- **Proposed default:** 2
- **Answer:** Option 3 — redesign the API contract so PrescriptionId is supplied once as part of the route/request and the body contains only the service line items. The backend must reject any inconsistent identifiers instead of inferring the scope from the first submitted item.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

### CQ-013 — Should the rebuild add server-side enforcement of the Resource/Permission authorization model (DR-015/DR-016) directly on domain API controllers?

- **Type:** DEFECT
- **Blocking:** yes — domain-model.md DR-015, DR-016; functional-spec.md Named Gaps item 13 and the "Screen Access Authorization Check" workflow; module-map.md MOD-005's "Depends on" edges from every other module
- **Source:** functional-spec.md § Named Gaps item 13 — "No server-side authorization check tied to the Resource/Permission model was found on the domain Web API controllers themselves — `PatientController`, `ProductController`, etc. carry only the generic `[Authorize]` attribute (any authenticated user, any role), while the fine-grained per-route Permission check (DR-015) is enforced exclusively client-side in `app.config.js`'s `$stateChangeStart` hook. A user who could reach the API directly (bypassing the SPA) would not be blocked by DR-015/DR-016 at all."
- **Finding:** The fine-grained screen-level permission model (which roles can access which screens, per DR-015/016) is enforced only in the AngularJS routing layer; every domain API controller carries just a generic `[Authorize]` attribute, so any authenticated user (regardless of role/permission grants) can call any domain endpoint directly.
- **Why it matters:** This is a genuine security gap in the legacy system today — a user who obtains a valid bearer token but no `Permission` grant for, say, the Users screen could still call `api/User/*` endpoints directly, bypassing the SPA's gate entirely. A rebuild that faithfully copies "generic `[Authorize]` only" forward inherits this exposure; deciding to add server-side permission enforcement is an architectural change with real implementation cost.
- **Options:**
  1. Treat as a defect to fix — add a server-side permission-check filter/attribute (mirroring DR-015/016) to every domain API controller in the rebuild.
  2. Preserve exactly as-is (client-only enforcement) for legacy-behavior parity, accepting the same exposure.
  3. Add server-side enforcement only for the highest-risk controllers (Identity/Permission subsystem) first, deferring the rest.
- **Proposed default:** 1
- **Answer:** Option 1 — fix the security gap. Every protected ASP.NET Core endpoint must enforce the appropriate permission/role policy server-side. React route protection is supplementary UI behaviour only.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

### CQ-014 — Should the rebuild add real multi-doctor support (Doctor management UI + an Appointment doctor picker), or preserve the single-hardcoded-doctor limitation?

- **Type:** TARGET-GAP
- **Blocking:** yes — domain-model.md Doctor/Appointment entities; functional-spec.md Named Gaps item 1, capability #24; ui-inventory.md Widget Cross-Reference Findings items 1/2; module-map.md MOD-004
- **Source:** functional-spec.md § Named Gaps item 1 — "Doctor selection is not exposed in the Appointment UI... `patient-appointment.controller.js`'s `init()` hardcodes `DoctorId: \"9b6ba3ad-c9be-e511-9bf4-402cf40f4b2f\"` — the exact GUID of the single doctor seeded... There is no doctor picker anywhere in the appointment form, and `DoctorController` exposes no create/update/delete endpoint. Whether multi-doctor support is a planned-but-unbuilt feature or intentionally out of scope was not answered by any code path I opened."; ui-inventory.md § Widget Cross-Reference Findings items 1–2.
- **Finding:** The data model supports many doctors (`Doctor` entity, `Appointment.DoctorId` FK), but the seed data inserts exactly one doctor, the appointment form hardcodes that doctor's GUID client-side with no picker, and there is no Doctor create/update/delete UI or endpoint at all (`DoctorController` exposes only `GetAll`/`GetById`).
- **Why it matters:** If the clinic genuinely operates with one doctor, faithfully reproducing this hardcoded-single-doctor behavior is correct and low-cost. If the rebuild is meant to support a growing/multi-doctor practice, this is a real feature gap (doctor management screen + appointment doctor-picker) that needs to be scoped and built, not silently carried forward as a hardcoded value.
- **Options:**
  1. Preserve single-hardcoded-doctor behavior exactly, matching legacy scope.
  2. Build genuine multi-doctor support — a Doctor management screen (create/update/delete) and a doctor-picker on the Appointment form.
  3. Add a doctor picker to the Appointment form only (using the existing seed data), without building full Doctor CRUD management.
- **Proposed default:** 1
- **Answer:** Option 2 — implement proper multi-doctor support. Add Doctor CRUD/management and require appointment creation/editing to select a doctor instead of using a hardcoded GUID.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

### CQ-023 — What spacing scale should the rebuild's design system adopt, given the legacy app defines none?

- **Type:** TARGET-GAP
- **Blocking:** yes — design-tokens.json's `omitted[]` entry "Spacing scale"; every SCR-### screen's Layout structure section (ad hoc spacing throughout)
- **Source:** design-tokens.json § omitted — "Spacing scale — No first-party spacing-scale variables (LESS/SCSS variables, CSS custom properties, or a documented grid unit) were found anywhere in `Client/app/styles/style.css` or any other first-party source — every spacing value in the templates is an ad hoc inline style or a Bootstrap grid class, not a defined scale."
- **Finding:** The legacy application has no first-party spacing scale of any kind — no design tokens, no LESS/SCSS variables, no CSS custom properties. All spacing is either an ad hoc inline style or comes implicitly from the vendored Bootstrap 3 grid classes.
- **Why it matters:** A modern rebuild's design system (component library, CSS framework, or design tokens) will need a concrete spacing scale to be consistent — since the legacy app provides zero guidance here, this value has to come from somewhere else entirely (a chosen target framework's defaults, a new design system spec, or a fresh human decision), and pretending otherwise risks an agent inventing an unfounded scale.
- **Options:**
  1. Adopt the target UI framework/component library's own default spacing scale (e.g. Tailwind's, Material's, Bootstrap 5's) rather than inventing a bespoke one.
  2. Have a designer/stakeholder define a bespoke spacing scale specifically for this rebuild.
  3. Reverse-engineer an approximate scale from the ad hoc pixel values actually observed across the legacy templates, to visually mirror legacy spacing as closely as possible.
- **Proposed default:** 1
- **Answer:** Option 1 — use Material UI's standard spacing system consistently throughout the React application. Legacy screenshots remain a visual reference, but ad hoc legacy pixel spacing does not need to be reproduced exactly under THEME-ONLY fidelity.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

### CQ-016 — Should the rebuild carry forward any social-login (Facebook/Google/Twitter/Microsoft) integration points, or drop them entirely?

- **Type:** SCOPE
- **Blocking:** no
- **Source:** codebase-report.md § Risks/Tech-Debt — "seven separate OWIN social-login providers... inherited from the original ASP.NET Identity template... this suggests template cruft"; architecture.md § System Context — "`DM.Server/App_Start/Startup.Auth.cs` references OWIN social-login packages... but every corresponding `app.UseFacebookAuthentication(...)`, `app.UseGoogleAuthentication(...)`, `app.UseTwitterAuthentication(...)`, and Microsoft-Account call in `Startup.Auth.cs`'s `ConfigureAuth(IAppBuilder app)` is commented out."
- **Finding:** The project references seven OWIN social-login provider packages (Facebook, Google, Twitter, Microsoft Account, plus Cookies/OAuth), but every actual provider-registration call in `Startup.Auth.cs` is commented out — none is active. This is confirmed template cruft from the original ASP.NET Identity scaffold, not merely unconfirmed dead code.
- **Why it matters:** These references add package-update burden and attack surface without corresponding functionality today. A rebuild has to decide whether social login was ever a real intended feature (worth reviving) or pure scaffold noise (safe to drop entirely) — the legacy code alone doesn't say which.
- **Options:**
  1. Drop all social-login provider references entirely — they were never activated and represent unused template cruft.
  2. Implement one or more social-login providers as a genuine new feature in the rebuild, since the intent may have been there but never finished.
  3. Keep the reference footprint present but still disabled, preserving optionality without committing engineering effort now.
- **Proposed default:** 1
- **Answer:** Option 1 — drop all legacy social-login provider dependencies. They are inactive template cruft and are not part of the rebuild requirements. Add external identity providers later only through a separate explicit requirement.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

### CQ-022 — Should the rebuild add role-differentiated landing pages, or remove the currently-dead role-branching logic in post-login routing?

- **Type:** SCOPE
- **Blocking:** no
- **Source:** functional-spec.md § Workflows, "Login & Post-Auth Routing" — "Every one of the seven seeded roles currently routes to the same `root.patient` landing screen regardless of role — there is no role-differentiated landing page despite the branch existing in code (Named Gap)."; functional-spec.md § Named Gaps item 11.
- **Finding:** `AppService.nextRoute()` branches on seven distinct role names, but every branch currently resolves to the same `root.patient` screen — the branching logic exists but produces no observable behavioral difference today, and only two of the seven roles (`SystemAdmin`, `Admin`) have any seeded account to exercise a branch at all.
- **Why it matters:** If role-specific landing pages (e.g. an Inventory-clerk-focused dashboard, a Doctor-focused appointment view) were planned but never finished, the rebuild is a natural opportunity to complete that design; if the branching was speculative and never needed, carrying it forward as dead code (or reimplementing it) is wasted effort with no user-visible benefit.
- **Options:**
  1. Remove the role-branching logic entirely in the rebuild — route every role to the same landing screen, matching actual current behavior.
  2. Design and build genuine role-differentiated landing experiences for the rebuild, using the seven existing role names as the starting point.
  3. Preserve the branching code structure as-is (all branches still resolving to one screen) in case role-specific landings are added later, without investing in them now.
- **Proposed default:** 1
- **Answer:** Option 1 — remove the dead branching and route authenticated users to one standard landing/dashboard screen. Authorization determines what actions/navigation are visible. Role-specific dashboards can be introduced later as a separate requirement.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

### CQ-024 — Should `DM.Core` be retained in the rebuild despite no confirmed consumer being found?

- **Type:** SCOPE
- **Blocking:** no
- **Source:** architecture.md § Components (L3) — "`DM.Core` (`AppConstants.cs`, `AppSettingsDto.cs`, `AppSettingsKey.cs`) is referenced by the `DM.Server` project per `dependency_graph`..., but I did not open any file that imports it, so no inbound edge from a specific component is drawn — the node is included for completeness, but its consumer within `DM.Server` is unconfirmed."; module-map.md § Unassigned.
- **Finding:** `DM.Core` is a real, referenced project (`AppConstants.cs`, `AppSettingsDto.cs`, `AppSettingsKey.cs`), but no file actually opened during architecture/module analysis was found to import or use it — its status as live code vs. unused legacy is unconfirmed on current evidence, not confirmed either way.
- **Why it matters:** Before a rebuild decides to drop `DM.Core` as dead weight, its actual usage should be confirmed (a targeted search for `AppConstants`/`AppSettingsDto`/`AppSettingsKey` across the codebase would settle this quickly) — dropping a project that turns out to hold a load-bearing app-setting constant would be a silent regression.
- **Options:**
  1. Keep `DM.Core`'s contents in scope for the rebuild by default, pending a targeted verification pass (adopt as-is, low risk).
  2. Confirm actual usage via a full-codebase search before the rebuild plan is finalized, then decide keep/drop based on what's found.
  3. Drop it now, treating the lack of a confirmed consumer as sufficient evidence it's unused.
- **Proposed default:** 1
- **Answer:** Option 2 — perform a targeted full-repository usage search before finalizing the rebuild plan. Do not recreate DM.Core as a separate project automatically. Move genuinely used constants/settings into the appropriate modern ASP.NET Core configuration/domain location and drop genuinely unused code.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

### CQ-025 — Should the Login screen's dead "Remember me" checkbox be implemented as a real feature, or removed?

- **Type:** SCOPE
- **Blocking:** no
- **Source:** ui-inventory.md § Widget Cross-Reference Findings item 4 — "'Remember me' (Login screen). `login.tpl.html:42` binds a checkbox to `ng-model=\"isRemebered\"`, but no entity in `domain-model.md` has a corresponding field, and `isRemebered` is never read anywhere else in `Client/app/scripts/auth/**` (confirmed by grep) — an apparently dead/no-op widget."
- **Finding:** The Login screen's "Remember me" checkbox is bound to a model variable (`isRemebered`, itself a typo) that is never read anywhere else in the codebase — checking or unchecking it has no observable effect on session persistence, token expiry, or anything else.
- **Why it matters:** Users may reasonably expect this checkbox to do something (it's a standard, expected login-form convention); silently carrying it forward as a no-op widget in the rebuild would preserve a small but confusing piece of dead UI, while implementing real remember-me behavior (e.g. extending token expiry, or persisting credentials) is a small feature addition beyond faithful legacy reproduction.
- **Options:**
  1. Drop the widget entirely in the rebuild — it does nothing today and there's no evidence it was ever functional.
  2. Implement genuine remember-me behavior (e.g. a longer-lived refresh token when checked) as a small new feature.
  3. Keep the checkbox present but still non-functional, purely for visual/UX parity with the legacy screen.
- **Proposed default:** 1
- **Answer:** Option 2 — implement real Remember Me behaviour using a secure longer-lived refresh/session mechanism when selected and a shorter normal session when not selected. Never store user passwords in browser storage.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

### CQ-017 — Should the rebuild's seed/demo accounts avoid a shared hardcoded password, unlike the legacy `"123qwe"` seed?

- **Type:** DEFECT
- **Blocking:** no
- **Source:** functional-spec.md § Named Gaps item 9 — "Two seed user accounts ship with a shared, hardcoded password '123qwe' (`DM.Server/Migrations/Configuration.cs`'s `AddUsers()`, users `superadmin`/`admin`). If this seed data reaches a real deployment un-rotated, it is a default-credential exposure; no code path I saw forces a password change on first login."
- **Finding:** The `superadmin` and `admin` seed accounts both ship with the same hardcoded password (`"123qwe"`), and nothing in the code forces a password change on first login.
- **Why it matters:** If this seed pattern is carried forward unchanged into a rebuild's production deployment process, it reproduces a known default-credential security exposure. This is a low-effort, high-value fix if flagged now, but it does affect the demo/seeding developer experience the team may be relying on.
- **Options:**
  1. Treat as a defect — generate random per-deployment seed passwords (or force a password reset on first login) in the rebuild, dropping the shared hardcoded value.
  2. Preserve the convenience of a known, shared demo password for local/dev environments only, explicitly excluded from any production seeding path.
  3. Preserve exactly as-is for parity, relying on ops process to rotate credentials post-deployment (not enforced in code).
- **Proposed default:** 1
- **Answer:** Option 2 — allow simple known demo credentials only in explicitly local/development seed data. Production deployments must never create accounts with shared hardcoded passwords; production admin bootstrap must use secure environment-specific credentials or a forced first-login password reset.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

### CQ-020 — What is the actual default-selected Stock Type (Received/Shipped) when adding a new stock movement, and should the rebuild reproduce or fix it?

- **Type:** DEFECT
- **Blocking:** no
- **Source:** ui-inventory.md § Named Gaps item 8 — "`stock.tpl.html:55-56`'s 'Stock Type' radio pair carries a static `checked=\"checked\"` attribute on the 'Shipped' option, but `stock.controller.js:8`'s `init()` sets the bound model (`stock.StatusId`) to `0`, which matches neither radio's `value` (`3`/`4`). Under AngularJS's `ng-model` binding, the live rendered default-checked state... depends on Angular's own directive-priority/render-order behavior, which was not verified against a running instance."
- **Finding:** The static HTML markup marks "Shipped" as checked by default, but the Angular controller initializes the underlying model to a value (`0`) that matches neither radio option's value (3=Received/4=Shipped) — so which option (if any) actually appears pre-selected to a user opening the Add Stock form is not determinable from static code alone.
- **Why it matters:** If a user relies on the visual default and doesn't consciously pick a radio option, an ambiguous or misleading default could cause a stock movement to be recorded with the wrong direction (received vs. shipped) — a real data-quality risk in a live app, not merely a cosmetic quirk.
- **Options:**
  1. Confirm the actual rendered default by exercising the running legacy app, then decide whether the rebuild should reproduce that exact default or pick a safer explicit default (e.g. no option pre-selected, forcing a conscious choice).
  2. Skip verification and simply design the rebuild's equivalent form with no default selection (forcing an explicit choice), sidestepping the ambiguity entirely.
  3. Skip verification and default to "Received" as the safer/more common case.
- **Proposed default:** 2
- **Answer:** Option 2 — the rebuild will have no default stock movement type. The user must explicitly choose Received or Shipped before submission, and the ASP.NET Core backend must reject requests without a valid movement type.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

### CQ-009 — Why does the appointment-by-date list exclude "Visited" appointments (DR-010)?

- **Type:** MECHANICAL
- **Blocking:** no
- **Source:** domain-model.md § Business Rules DR-010 — "mechanical, reason not evident — `AppointmentRepository.GetByDate` filters to `x.StatusId == 7` (\"Appointed\") only, excluding \"Visited\" (8) appointments from the by-date list; no comment explains why visited appointments are hidden from this particular query."
- **Finding:** The by-date appointment query hides already-visited appointments with no stated rationale — mechanical, reason not evident, per domain-model.md's own labeling.
- **Why it matters:** If the rebuild's by-date appointment view should show a full day's schedule (including who has already been seen), silently carrying this filter forward would hide historical same-day visits from staff without anyone having decided that's desired.
- **Options:**
  1. Adopt as-is — preserve the "Appointed only" filter exactly, since it may reflect an intentional "today's remaining queue" view.
  2. Change the rebuild's by-date view to include both "Appointed" and "Visited" statuses, showing the full day's schedule.
- **Proposed default:** adopt as-is
- **Answer:** Option 2 — show both Appointed and Visited appointments in the date-based schedule so staff can see the full day's activity. Visited appointments should remain visually distinguishable by status.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

### CQ-010 — Why does the inventory on-hand report use a fixed one-month lookback/lookahead (DR-020)?

- **Type:** MECHANICAL
- **Blocking:** no
- **Source:** domain-model.md § Business Rules DR-020 — "mechanical, reason not evident — `DM.Server/Controllers/InventoryReportController.cs`'s private `GetOnHand` helper, used when a product has zero movements inside the requested report window, looks first at the movement nearest one month before the window start, then the movement nearest one month after the window end, and only falls back to the product's live `OnHand` if neither exists. No comment explains why a fixed one-month lookback/lookahead was chosen over, e.g., the single nearest movement regardless of distance."
- **Finding:** A hardcoded one-month window (rather than "nearest movement regardless of distance," or a configurable window) governs on-hand estimation for products with no in-window movement — no rationale is stated anywhere for this specific choice.
- **Why it matters:** A rebuild that silently copies this fixed one-month constant forward would reproduce inaccurate on-hand estimates for any product whose nearest movement happens to fall just outside that window (e.g. 32 days away), without anyone having confirmed that's the intended behavior rather than an arbitrary legacy default.
- **Options:**
  1. Adopt as-is — keep the fixed one-month lookback/lookahead exactly.
  2. Replace with "nearest movement regardless of distance" as the fallback rule.
  3. Make the lookback/lookahead window a configurable setting rather than a hardcoded one month.
- **Proposed default:** adopt as-is
- **Answer:** Option 2 — remove the arbitrary one-month cutoff and use the nearest relevant inventory movement needed to determine the historical on-hand value. The calculation must be deterministic and covered by tests.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

### CQ-021 — Should the Stock Report's Product/Status filters (present in the controller but commented out in the template) be restored in the rebuild?

- **Type:** TARGET-GAP
- **Blocking:** no
- **Source:** ui-inventory.md § SCR-012 — "the template itself has the Product/Status selects commented out (`stock-report.tpl.html:30-40`) even though the controller still defines their option data (`stock-report.controller.js:8-19`)"; ui-inventory.md § Named Gaps item 9.
- **Finding:** The Stock Report screen's controller still builds and maintains Product-name and Status filter option data, but the corresponding `<select>` elements in the template are commented out — the filtering UI these options were meant to power is not currently rendered to the user at all.
- **Why it matters:** This looks like an intentionally-disabled-but-not-removed feature (someone commented it out rather than deleting the controller logic too) — if the rebuild silently drops this filtering capability, it may be removing a feature users actually want; if the rebuild silently restores it without confirmation, it may be reviving something that was deliberately disabled for a reason not visible in the code.
- **Options:**
  1. Restore the Product/Status filter selects in the rebuild's equivalent Stock Report screen, completing what looks like an unfinished feature.
  2. Leave them out, matching the legacy app's actual (disabled) rendered behavior rather than its half-finished controller code.
  3. Ask clinic staff/stakeholders whether they currently miss this filtering capability before deciding either way.
- **Proposed default:** 1
- **Answer:** Option 1 — restore Product and Status filters in the rebuilt Stock Report. Implement filtering through explicit React/Material UI controls backed by server-side query parameters, with clear default/all values.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12

## Not Applicable

- **SQ-007** — Legacy app is already explicitly multi-user server software — ASP.NET Identity issues per-user login/roles (functional-spec.md Login workflow) and DM.Server/Migrations/Configuration.cs's AddUsers() seeds multiple named accounts (superadmin, admin) across eight roles, so multi-user is not a live fork the rebuild needs to introduce.
