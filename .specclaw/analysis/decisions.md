# Decisions: App

**Date generated:** 2026-08-12
**Source:** .specclaw/analysis/clarifications.md

<!--
  This is the clean, pinnable decision record /specclaw:bf-clarify --resolve
  produces from clarifications.md's answered questions, swept across all
  three question families (CQ-NNN/SQ-NNN/UQ-NNN). Every entry below is a
  mechanical transcription of an already-answered question — the
  Answer/Decided by/Date fields are transcribed, never reinterpreted. Each
  entry carries a **Family:** line (Extracted | Standard bank | Custom
  (per-repo)) derived mechanically from the question's ID prefix, so a
  reader can tell at a glance whether a decision came from this repo's own
  code, the plugin's standard bank, or a per-repo custom question.
  Re-running --resolve is idempotent: it always reflects the current state
  of clarifications.md's answered blocks, replacing this file's prior
  content wholesale (the prior version is archived, never lost).

  Pin this file — add `.specclaw/analysis/decisions.md` to config.yaml's
  `context.pin` (raise `max_lines` accordingly) and `git add` it, so every
  downstream /specclaw:propose, /specclaw:plan, and /specclaw:build cites
  these decisions as grounding instead of re-deriving them. Discovery
  enumerates via `git ls-files` — an untracked file is invisible to it.
-->

## Decisions

### CQ-001 — Is the PatientController.Get()/Search() N+1 `.Last()` crash-on-empty-sequence pattern intentional legacy behavior to preserve, or a defect to fix in the rebuild?

- **Type:** DEFECT
- **Family:** Extracted
- **Decision:** Option 1 — treat it as a defect. The rebuild must safely handle patients with no prescription and avoid N+1 database queries by loading/projecting the required prescription information efficiently through EF Core.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** Promoted from PQ-001 (bf-architecture-analyst (architecture.md L4), Trigger T4) — L3/L4 "Domain API Controllers" component (architecture.md), any future rebuild-backlog item covering the Patients list/search endpoint

### CQ-002 — Should a rebuild target consolidate the two independently-migrated EF DbContexts (DentalDbContext + ApplicationDbContext) sharing one physical database into a single schema/context, or preserve the split?

- **Type:** DECISION
- **Family:** Extracted
- **Decision:** Option 2 — consolidate into one PostgreSQL database and one EF Core application DbContext/schema for the initial rebuild. Authentication/Identity tables and domain tables may remain logically separated by configuration/naming, but they should share one controlled migration history instead of two independent migration pipelines.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** Promoted from PQ-002 (bf-architecture-analyst (architecture.md L2/L3), Trigger T5) — L2 "SQL Server Database" container and its two sub-schema nodes (architecture.md), L3 "Identity & Permission Subsystem" component, any future rebuild-backlog data-layer item

### CQ-003 — Should the About/Contact screens' broken controller wiring be fixed (implement `AboutController`/`ContactController`), or are these screens out of scope for a rebuild?

- **Type:** SCOPE
- **Family:** Extracted
- **Decision:** Option 2 — exclude the broken legacy About and Contact routes from the rebuild scope. They contain no meaningful business functionality and are not reachable from the live navigation. If an About/Help page is requested later, implement it as a simple static React page as a new requirement.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** Promoted from PQ-003 (/specclaw:bf-ui (bf-ui-analyst), Trigger T4) — SCR-019 (About), SCR-020 (Contact) in ui-inventory.md; the "View the About page" / "View the Contact page" capabilities in functional-spec.md (items 34–35) and its Named Gaps #5/#6

### CQ-004 — Which navbar background color is actually rendered: style.css's `.navbar` rule or nav.html's own inline `.navbar-default` rule?

- **Type:** DECISION
- **Family:** Extracted
- **Decision:** Option 1 — use #218283 as the effective legacy navbar colour and map it into the React/MUI theme. The captured legacy screenshots remain the final visual reference if any contradiction is observed.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** Promoted from PQ-004 (/specclaw:bf-ui (bf-ui-analyst), Trigger T6) — TK-003 in design-tokens.json (global navbar background token group); the "Layout structure"/top-nav-band bullet on every SCR-### screen entry in ui-inventory.md (the shared nav chrome is present on all 20 screens)

### CQ-005 — Is the rebuild target expected to remain single-clinic, or must it support multi-tenancy?

- **Type:** SCOPE
- **Family:** Extracted
- **Decision:** Option 1 — preserve the current single-clinic scope. Do not add tenant identifiers or multi-tenant query complexity to the initial rebuild without a real business requirement.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** codebase-report.md § Domain — "Inference (low confidence): The product is operated by (or was built for) a single named clinic rather than sold as multi-tenant software — the uncommitted `DM.Server/Web.config` diff still references a hardcoded database name `Initial Catalog=MahmudaDentalDb`... I did not find an explicit multi-tenant/customer-selection mechanism in the files I opened."; architecture.md § System Context repeats the same single-clinic framing.

### CQ-006 — Should the shared `Status` lookup table become four separate typed enumerations in the rebuild?

- **Type:** DECISION
- **Family:** Extracted
- **Decision:** Option 2 — replace the shared Status lookup with separate typed status concepts for Prescription/Bill, Product, Inventory Movement, and Appointment. Preserve the existing semantic values during migration but enforce valid status values per entity in the .NET domain/service layer.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** domain-model.md § Enumerations, item 2 ("Status lookup table") — "this one shared table is reused as four *separate* enumerations depending on which entity's `StatusId` points at it... there is no code-level partition of the `Status` table by entity, so a future insert could in principle assign an 'In Stock'-flavoured status to a `Prescription` row with nothing to stop it."

### CQ-007 — Should `Patient.Gender` become a real typed enum in the rebuild, or stay a free-form string?

- **Type:** DECISION
- **Family:** Extracted
- **Decision:** Option 1 — formalize Gender as a typed enum/lookup in the rebuild and migrate the existing Male, Female, and Others values explicitly. Before migration, report any legacy values outside the known set so they can be reviewed rather than silently discarded.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** domain-model.md § Enumerations, item 1 — "`Patient.Gender` is declared as a plain `string`, not this `Gender` enum type — the enum exists in the same file but is never actually used as a property type anywhere I opened; the AngularJS form... independently hardcodes the option list `[\"Male\",\"Female\",\"Others\"]` as plain strings... nothing in the code ties them together."

### CQ-008 — Should `MedicalService.Charge`/`TotalCharge`'s string-to-int currency truncation (DR-019) be preserved or fixed in the rebuild?

- **Type:** DEFECT
- **Family:** Extracted
- **Decision:** Option 1 — fix the defect. Use a proper decimal money type in .NET and a fixed-precision numeric/decimal column in PostgreSQL. TotalCharge must retain fractional currency values and must not truncate to integer values. Audit legacy Charge strings during migration and explicitly report values that cannot be parsed.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** domain-model.md § Entities item 4 and § Business Rules DR-019 — `MedicalService.Charge` is `[DataType(DataType.Currency)] string`, and `[NotMapped] TotalCharge` is computed as `Convert.ToInt32(Charge) * Quantity`, "which truncates any fractional currency amount and throws for a non-integer string... no comment explains the choice of `int` over a decimal type."

### CQ-009 — Why does the appointment-by-date list exclude "Visited" appointments (DR-010)?

- **Type:** MECHANICAL
- **Family:** Extracted
- **Decision:** Option 2 — show both Appointed and Visited appointments in the date-based schedule so staff can see the full day's activity. Visited appointments should remain visually distinguishable by status.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** domain-model.md § Business Rules DR-010 — "mechanical, reason not evident — `AppointmentRepository.GetByDate` filters to `x.StatusId == 7` (\"Appointed\") only, excluding \"Visited\" (8) appointments from the by-date list; no comment explains why visited appointments are hidden from this particular query."

### CQ-010 — Why does the inventory on-hand report use a fixed one-month lookback/lookahead (DR-020)?

- **Type:** MECHANICAL
- **Family:** Extracted
- **Decision:** Option 2 — remove the arbitrary one-month cutoff and use the nearest relevant inventory movement needed to determine the historical on-hand value. The calculation must be deterministic and covered by tests.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** domain-model.md § Business Rules DR-020 — "mechanical, reason not evident — `DM.Server/Controllers/InventoryReportController.cs`'s private `GetOnHand` helper, used when a product has zero movements inside the requested report window, looks first at the movement nearest one month before the window start, then the movement nearest one month after the window end, and only falls back to the product's live `OnHand` if neither exists. No comment explains why a fixed one-month lookback/lookahead was chosen over, e.g., the single nearest movement regardless of distance."

### CQ-011 — Should DR-004/DR-005/DR-006/DR-008's client-side-only business rules gain server-side enforcement in the rebuild?

- **Type:** DEFECT
- **Family:** Extracted
- **Decision:** Option 1 — enforce all four rules on the ASP.NET Core backend as authoritative business rules and mirror them in React for immediate UX feedback. Direct API calls must never be able to bypass these validations.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** functional-spec.md § Named Gaps item 2 — "Several client-side-only business rules have no server-side mirror: discount-percent range (DR-004), payment-not-exceeding-due (DR-005), bill-close-blocked-while-due (DR-006, though 'Force' exists as an intentional override), and shipment-not-exceeding-on-hand (DR-008) are all enforced only in AngularJS controllers. A rebuild that faithfully reimplements the Web API controllers but omits the equivalent Angular logic would silently accept data the legacy app blocks."

### CQ-012 — Should DR-018's "replace scoped to the first submitted item's PrescriptionId" behavior be made explicit/enforced in the rebuild?

- **Type:** DEFECT
- **Family:** Extracted
- **Decision:** Option 3 — redesign the API contract so PrescriptionId is supplied once as part of the route/request and the body contains only the service line items. The backend must reject any inconsistent identifiers instead of inferring the scope from the first submitted item.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** domain-model.md § Business Rules DR-018 — "`DM.Repository/PatientMedicalServiceRepository.cs`'s `AddList` deletes every existing `PatientMedicalService` row for the **first** submitted item's `PrescriptionId` (the `foreach` loop `break`s after processing one item), then inserts every item in the new list. This is correct only because its one caller... always submits a list scoped to a single `PrescriptionId` — nothing in the type system enforces that assumption."; functional-spec.md § Named Gaps item 3 repeats the same finding.

### CQ-013 — Should the rebuild add server-side enforcement of the Resource/Permission authorization model (DR-015/DR-016) directly on domain API controllers?

- **Type:** DEFECT
- **Family:** Extracted
- **Decision:** Option 1 — fix the security gap. Every protected ASP.NET Core endpoint must enforce the appropriate permission/role policy server-side. React route protection is supplementary UI behaviour only.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** functional-spec.md § Named Gaps item 13 — "No server-side authorization check tied to the Resource/Permission model was found on the domain Web API controllers themselves — `PatientController`, `ProductController`, etc. carry only the generic `[Authorize]` attribute (any authenticated user, any role), while the fine-grained per-route Permission check (DR-015) is enforced exclusively client-side in `app.config.js`'s `$stateChangeStart` hook. A user who could reach the API directly (bypassing the SPA) would not be blocked by DR-015/DR-016 at all."

### CQ-014 — Should the rebuild add real multi-doctor support (Doctor management UI + an Appointment doctor picker), or preserve the single-hardcoded-doctor limitation?

- **Type:** TARGET-GAP
- **Family:** Extracted
- **Decision:** Option 2 — implement proper multi-doctor support. Add Doctor CRUD/management and require appointment creation/editing to select a doctor instead of using a hardcoded GUID.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** functional-spec.md § Named Gaps item 1 — "Doctor selection is not exposed in the Appointment UI... `patient-appointment.controller.js`'s `init()` hardcodes `DoctorId: \"9b6ba3ad-c9be-e511-9bf4-402cf40f4b2f\"` — the exact GUID of the single doctor seeded... There is no doctor picker anywhere in the appointment form, and `DoctorController` exposes no create/update/delete endpoint. Whether multi-doctor support is a planned-but-unbuilt feature or intentionally out of scope was not answered by any code path I opened."; ui-inventory.md § Widget Cross-Reference Findings items 1–2.

### CQ-015 — Should the rebuild support assigning multiple roles per user, matching the domain model's `ApplicationUser.Roles` collection, or preserve the UI's single-role-per-user constraint?

- **Type:** DECISION
- **Family:** Extracted
- **Decision:** Option 1 — preserve and explicitly enforce one primary role per user in this rebuild because that matches the observed legacy UI behaviour. Fine-grained Resource/Permission grants remain separate from the user's primary role.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** ui-inventory.md § Widget Cross-Reference Findings item 3 — "domain-model.md documents `Roles` as a collection (via `IdentityUserRole`), implying a user can hold more than one role, but the Manage Users screen's Role field is a single `<select>` bound to the singular `model.RoleId` — the UI can only ever assign exactly one role per user."

### CQ-016 — Should the rebuild carry forward any social-login (Facebook/Google/Twitter/Microsoft) integration points, or drop them entirely?

- **Type:** SCOPE
- **Family:** Extracted
- **Decision:** Option 1 — drop all legacy social-login provider dependencies. They are inactive template cruft and are not part of the rebuild requirements. Add external identity providers later only through a separate explicit requirement.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** codebase-report.md § Risks/Tech-Debt — "seven separate OWIN social-login providers... inherited from the original ASP.NET Identity template... this suggests template cruft"; architecture.md § System Context — "`DM.Server/App_Start/Startup.Auth.cs` references OWIN social-login packages... but every corresponding `app.UseFacebookAuthentication(...)`, `app.UseGoogleAuthentication(...)`, `app.UseTwitterAuthentication(...)`, and Microsoft-Account call in `Startup.Auth.cs`'s `ConfigureAuth(IAppBuilder app)` is commented out."

### CQ-017 — Should the rebuild's seed/demo accounts avoid a shared hardcoded password, unlike the legacy `"123qwe"` seed?

- **Type:** DEFECT
- **Family:** Extracted
- **Decision:** Option 2 — allow simple known demo credentials only in explicitly local/development seed data. Production deployments must never create accounts with shared hardcoded passwords; production admin bootstrap must use secure environment-specific credentials or a forced first-login password reset.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** functional-spec.md § Named Gaps item 9 — "Two seed user accounts ship with a shared, hardcoded password '123qwe' (`DM.Server/Migrations/Configuration.cs`'s `AddUsers()`, users `superadmin`/`admin`). If this seed data reaches a real deployment un-rotated, it is a default-credential exposure; no code path I saw forces a password change on first login."

### CQ-018 — Are the orphaned, unrouted template files (`patient-report-2.tpl.html`, `denied.tpl.html`) abandoned drafts to drop, or unfinished features to complete in the rebuild?

- **Type:** SCOPE
- **Family:** Extracted
- **Decision:** Option 1 — treat both templates as dead/unreachable legacy code and do not rebuild them. Only the actually wired Patient Report and Access Denied screens are in scope.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** functional-spec.md § Named Gaps item 7 — "`patient-report-2.tpl.html` exists on disk but is not referenced by any route config I opened — likely an abandoned alternate version of the patient-report screen"; ui-inventory.md § Named Gaps item 1 — "`Client/app/views/auth/denied.tpl.html` is an orphaned, unrouted template, distinct from the actually-wired `access-denied.tpl.html` (SCR-018). It references a function `backToDefaultRoute()` that is defined nowhere in the codebase... and no `$stateProvider.state(...)` registration anywhere references `denied.tpl.html`."

### CQ-019 — Which binding actually determines the persisted `Resource.IsPublic` value on the Manage Resources screen — the radio's native `value` attribute or its Angular `ng-value`?

- **Type:** DECISION
- **Family:** Extracted
- **Decision:** Option 3 — the React rebuild will use one explicit boolean IsPublic field with a single unambiguous Material UI control and server-side validation. Do not reproduce the AngularJS double-binding defect. Existing persisted boolean values will be migrated as stored.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** functional-spec.md § Named Gaps item 8 — "`resource.tpl.html`'s 'Public' radio group binds both a plain `value` attribute ('0'/'1') and an Angular `ng-value` (`isPublicEnum.False`/`isPublicEnum.True`) to the same `ng-model=\"model.IsPublic\"`. The widget itself... is unambiguous, but which binding actually determines the persisted boolean was not fully verified from static markup alone."; ui-inventory.md § Named Gaps item 10 repeats the same finding.

### CQ-020 — What is the actual default-selected Stock Type (Received/Shipped) when adding a new stock movement, and should the rebuild reproduce or fix it?

- **Type:** DEFECT
- **Family:** Extracted
- **Decision:** Option 2 — the rebuild will have no default stock movement type. The user must explicitly choose Received or Shipped before submission, and the ASP.NET Core backend must reject requests without a valid movement type.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** ui-inventory.md § Named Gaps item 8 — "`stock.tpl.html:55-56`'s 'Stock Type' radio pair carries a static `checked=\"checked\"` attribute on the 'Shipped' option, but `stock.controller.js:8`'s `init()` sets the bound model (`stock.StatusId`) to `0`, which matches neither radio's `value` (`3`/`4`). Under AngularJS's `ng-model` binding, the live rendered default-checked state... depends on Angular's own directive-priority/render-order behavior, which was not verified against a running instance."

### CQ-021 — Should the Stock Report's Product/Status filters (present in the controller but commented out in the template) be restored in the rebuild?

- **Type:** TARGET-GAP
- **Family:** Extracted
- **Decision:** Option 1 — restore Product and Status filters in the rebuilt Stock Report. Implement filtering through explicit React/Material UI controls backed by server-side query parameters, with clear default/all values.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** ui-inventory.md § SCR-012 — "the template itself has the Product/Status selects commented out (`stock-report.tpl.html:30-40`) even though the controller still defines their option data (`stock-report.controller.js:8-19`)"; ui-inventory.md § Named Gaps item 9.

### CQ-022 — Should the rebuild add role-differentiated landing pages, or remove the currently-dead role-branching logic in post-login routing?

- **Type:** SCOPE
- **Family:** Extracted
- **Decision:** Option 1 — remove the dead branching and route authenticated users to one standard landing/dashboard screen. Authorization determines what actions/navigation are visible. Role-specific dashboards can be introduced later as a separate requirement.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** functional-spec.md § Workflows, "Login & Post-Auth Routing" — "Every one of the seven seeded roles currently routes to the same `root.patient` landing screen regardless of role — there is no role-differentiated landing page despite the branch existing in code (Named Gap)."; functional-spec.md § Named Gaps item 11.

### CQ-023 — What spacing scale should the rebuild's design system adopt, given the legacy app defines none?

- **Type:** TARGET-GAP
- **Family:** Extracted
- **Decision:** Option 1 — use Material UI's standard spacing system consistently throughout the React application. Legacy screenshots remain a visual reference, but ad hoc legacy pixel spacing does not need to be reproduced exactly under THEME-ONLY fidelity.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** design-tokens.json § omitted — "Spacing scale — No first-party spacing-scale variables (LESS/SCSS variables, CSS custom properties, or a documented grid unit) were found anywhere in `Client/app/styles/style.css` or any other first-party source — every spacing value in the templates is an ad hoc inline style or a Bootstrap grid class, not a defined scale."

### CQ-024 — Should `DM.Core` be retained in the rebuild despite no confirmed consumer being found?

- **Type:** SCOPE
- **Family:** Extracted
- **Decision:** Option 2 — perform a targeted full-repository usage search before finalizing the rebuild plan. Do not recreate DM.Core as a separate project automatically. Move genuinely used constants/settings into the appropriate modern ASP.NET Core configuration/domain location and drop genuinely unused code.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** architecture.md § Components (L3) — "`DM.Core` (`AppConstants.cs`, `AppSettingsDto.cs`, `AppSettingsKey.cs`) is referenced by the `DM.Server` project per `dependency_graph`..., but I did not open any file that imports it, so no inbound edge from a specific component is drawn — the node is included for completeness, but its consumer within `DM.Server` is unconfirmed."; module-map.md § Unassigned.

### CQ-025 — Should the Login screen's dead "Remember me" checkbox be implemented as a real feature, or removed?

- **Type:** SCOPE
- **Family:** Extracted
- **Decision:** Option 2 — implement real Remember Me behaviour using a secure longer-lived refresh/session mechanism when selected and a shorter normal session when not selected. Never store user passwords in browser storage.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** ui-inventory.md § Widget Cross-Reference Findings item 4 — "'Remember me' (Login screen). `login.tpl.html:42` binds a checkbox to `ng-model=\"isRemebered\"`, but no entity in `domain-model.md` has a corresponding field, and `isRemebered` is never read anywhere else in `Client/app/scripts/auth/**` (confirmed by grep) — an apparently dead/no-op widget."

### SQ-001 — Target platform

- **Type:** DECISION
- **Family:** Standard bank
- **Decision:** Option 1 — rebuild as a modern web application. Use React + TypeScript with Material UI (MUI) for the frontend, and ASP.NET Core Web API for the backend.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** Standard bank v1 (references/clarify-standard-questions.md)

### SQ-002 — Database engine and hosting

- **Type:** DECISION
- **Family:** Standard bank
- **Decision:** Option 2 — migrate from SQL Server to PostgreSQL. Use Entity Framework Core with the PostgreSQL provider. Create a clean PostgreSQL schema and new EF Core migrations rather than reusing the legacy EF6 SQL Server migration history directly.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** Standard bank v1 (references/clarify-standard-questions.md)

### SQ-003 — Hosting/deployment model

- **Type:** DECISION
- **Family:** Standard bank
- **Decision:** Option 2 — cloud-hosted, single-tenant for the initial rebuild. Keep deployment configuration environment-based so the same solution can be self-hosted later if required. The application stack remains React + TypeScript + Material UI on the frontend, ASP.NET Core Web API on the backend, and PostgreSQL for persistence.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** Standard bank v1 (references/clarify-standard-questions.md)

### SQ-004 — Authentication/authorization approach

- **Type:** TARGET-GAP
- **Family:** Standard bank
- **Decision:** Option 2 — use ASP.NET Core Identity with secure token-based authentication suitable for the React SPA. Enforce role and Resource/Permission authorization on the server for every protected API operation. React route guards are only a UX feature and must not be the security boundary.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** Standard bank v1 (references/clarify-standard-questions.md)

### SQ-005 — Existing production data

- **Type:** SCOPE
- **Family:** Standard bank
- **Decision:** Option 1 — migrate all existing production data into PostgreSQL. Historical patient, billing, payment, appointment, doctor, product, stock, user, role, and permission data must remain available after migration. Include migration validation and reconciliation checks before production cutover.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** Standard bank v1 (references/clarify-standard-questions.md)

### SQ-006 — UI framework / component library

- **Type:** DECISION
- **Family:** Standard bank
- **Decision:** Option 1 — use React with TypeScript and Material UI (MUI) as the component library. Map the legacy theme colours from design-tokens.json into a centralized MUI theme so the rebuilt UI keeps the recognizable legacy branding while using modern responsive components.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** Standard bank v1 (references/clarify-standard-questions.md)

### SQ-008 — Browser/device/OS support matrix

- **Type:** DECISION
- **Family:** Standard bank
- **Decision:** Option 1 — support current evergreen Chrome, Edge, Firefox, and Safari. The React/MUI application should be responsive for normal desktop and tablet widths and usable on smaller screens where practical. Target WCAG AA accessibility for newly built UI.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** Standard bank v1 (references/clarify-standard-questions.md)

### SQ-009 — Reporting/printing/export behaviours

- **Type:** SCOPE
- **Family:** Standard bank
- **Decision:** Option 2 — keep all existing report and receipt capabilities, but implement them as modern print-friendly React/MUI views. Where useful, provide PDF export for printable reports/receipts and CSV export for tabular reports. Exact legacy window.print() implementation is not required.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** Standard bank v1 (references/clarify-standard-questions.md)

### SQ-010 — Non-functional targets

- **Type:** DECISION
- **Family:** Standard bank
- **Decision:** Design for a small-to-medium clinic workload but include normal production safeguards from the start: server-side paging/filtering, appropriate PostgreSQL indexes, avoidance of N+1 queries, async API/database operations, and basic response-time monitoring. Heavy distributed caching or high-scale infrastructure is not required initially.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** Standard bank v1 (references/clarify-standard-questions.md)

### SQ-011 — Operational requirements

- **Type:** SCOPE
- **Family:** Standard bank
- **Decision:** Option 1 — include structured application logging, centralized error monitoring, PostgreSQL backup/restore procedures, health checks, environment-based configuration/secrets, and automated CI/CD from the beginning of the rebuild.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** Standard bank v1 (references/clarify-standard-questions.md)

### SQ-012 — Fidelity default

- **Type:** DECISION
- **Family:** Standard bank
- **Decision:** Option 3 — decide case by case. Preserve valid business behaviour by default, but do not automatically preserve confirmed security gaps, crashes, invalid-data behaviour, dead UI, or implementation accidents. Every intentional divergence from legacy behaviour must be tied to a decided CQ.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** Standard bank v1 (references/clarify-standard-questions.md)

### SQ-013 — UI fidelity policy

- **Type:** DECISION
- **Family:** Standard bank
- **Decision:** Option 2 — THEME-ONLY. Preserve the recognizable legacy colour palette, branding, terminology, and important visual cues, but rebuild the layouts as modern responsive React + Material UI components rather than reproducing the AngularJS/Bootstrap layout pixel-for-pixel.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** Standard bank v1 (references/clarify-standard-questions.md)

### UQ-001 — Should offline mode be supported?

- **Type:** SCOPE
- **Family:** Custom (per-repo)
- **Decision:** No — the rebuild will be an online web application. Offline-first synchronization is outside the current scope.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** .specclaw/analysis/custom-questions.md — "Should offline mode be supported?"

### UQ-002 — Do we need a mobile app eventually?

- **Type:** DECISION
- **Family:** Custom (per-repo)
- **Decision:** No dedicated mobile application is required in the current scope. Build a responsive React + Material UI web application and keep the ASP.NET Core API cleanly separated so a mobile client can be added later without redesigning the backend.
- **Decided by:** Pasan Gunathilaka
- **Date:** 2026-08-12
- **Source:** .specclaw/analysis/custom-questions.md — "Do we need a mobile app eventually?"

## ADR Promotion Candidates

- **SQ-001** — Target Platform: Web Application (React + ASP.NET Core) — Foundational platform choice with no legacy-code signal; must be documented for every downstream architecture decision.
- **SQ-002** — Database Migration: SQL Server to PostgreSQL with EF Core — Major architectural fork changing schema, migrations, and persistence strategy across all sixteen legacy entities.
- **SQ-003** — Hosting Model: Cloud-Hosted, Single-Tenant — Determines deployment, connection-string, and data-isolation architecture with no legacy precedent to follow.
- **SQ-013** — UI Fidelity Policy: Theme-Only Reproduction — Governs the whole rebuild's visual-parity approach and the already-invested UI-capture workstream.
- **SQ-005** — Data Migration Strategy: Full Production Data Migration — Costly, one-way decision covering migration and reconciliation of all legacy clinical and billing data.
- **SQ-004** — Authentication and Authorization Architecture Overhaul — Closes a confirmed legacy API-level authorization bypass and sets the backend security architecture.
- **SQ-006** — Frontend Stack: React, TypeScript, and Material UI — Foundational frontend technology choice replacing a 391K-LOC AngularJS 1.x codebase.
- **SQ-012** — Fidelity Default Policy: Case-by-Case Legacy Behaviour Decisions — Sets the precedent governing how every future legacy-behaviour fork gets judged across the whole rebuild.
- **SQ-011** — Operational Tooling from Day One: Logging, Monitoring, Backups, CI/CD — Establishes baseline operational architecture the legacy app never had, affecting infra setup project-wide.
- **CQ-002** — Consolidate Dual EF DbContexts into a Single Schema — Resolves an undocumented legacy structural split and directly shapes the new data-layer and migration design.
- **CQ-006** — Split Shared Status Lookup into Typed Per-Entity Enumerations — Changes schema and migration shape significantly and closes a cross-entity data-integrity risk.
- **CQ-015** — Enforce a Single Primary Role per User — Shapes the authorization/role model and every role-dependent code path across the application.
- **CQ-005** — Preserve Single-Clinic Scope, No Multi-Tenancy — Foundational scope decision affecting the data model and every query/permission check in the app.
- **CQ-011** — Enforce Client-Side Business Rules Server-Side — Establishes the validation-architecture principle that business rules must be authoritative on the backend across modules.
- **CQ-013** — Enforce the Resource/Permission Model on API Controllers — Closes a confirmed cross-cutting security gap and sets the authorization architecture for every domain controller.
- **CQ-014** — Build Genuine Multi-Doctor Support — New feature scope requiring Doctor CRUD and data-model/UI changes across the Appointment module.


## Outstanding Questions

All questions in clarifications.md have been answered.
