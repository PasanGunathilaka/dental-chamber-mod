# Pending Questions

<!--
  Ask-don't-guess buffer for the brownfield analysis pipeline. Any analysis
  agent (bf-domain-analyst, bf-architecture-analyst, bf-rebuild-planner,
  bf-baseline-designer) that would otherwise have to silently assume an
  answer — see the T1-T6 trigger list below — appends a PQ-### entry here
  instead of guessing. /specclaw:bf-clarify ingests every OPEN entry,
  assigns it a real type (DECISION/DEFECT/SCOPE/TARGET-GAP) and a permanent
  CQ-###/SQ-###/UQ-### id, and rewrites that entry's Status line in place to
  PROMOTED → <that id>. clarifications.md/decisions.md remain the single
  source of truth for the question's content and its eventual answer — this
  file is only ever the handoff buffer between "an agent noticed something
  uncertain" and "a human got asked about it."

  APPEND-ONLY, LIKE clarifications.md's own source-of-truth invariant —
  NEVER archive-then-replace. An agent adding a new PQ must append it (e.g.
  via its own Bash tool: `cat >> .specclaw/analysis/pending-questions.md
  <<'PQEOF' ... PQEOF`) — never read the whole file and Write it back,
  which risks silently dropping another run's entry this agent never saw.
  The only in-place edit ever made to an existing entry is
  /specclaw:bf-clarify rewriting its own Status line on promotion — nothing
  else in this file is ever rewritten, reordered, or deleted. PQ-### ids
  are permanent, exactly like DR-NNN/BL-NNN/GM-NNN/CQ-NNN — never
  renumbered or reused, even for a WITHDRAWN entry.

  GATING IS SOFT-BLOCK: an OPEN PQ never stops any command from running. It
  marks whatever it Blocks as PROVISIONAL — a labelled provisional default
  that flows downstream (a rebuild-backlog.md item, a baseline
  scenario/fixture, a replay verdict) until a human answers it under
  ## Decisions in decisions.md. See CONTRACT.md and each analysis agent's
  own instructions for the exact PROVISIONAL marker convention per
  artifact.

  Uncertainty triggers (exhaustive — an agent asks on these, and only
  these; anything else uncited is dropped or flagged as today, never
  asked):
    T1 — Field rendering/widget type not evidenced in code (input type,
         component, file-handling logic all absent or ambiguous).
    T2 — Code behaviour contradicts comments, docs, or naming.
    T3 — Multiple plausible interpretations of a business rule, or of a
         module grouping, with no test, usage site, data constraint, or
         dependency edge disambiguating them. (The module case: an entity,
         rule, service or screen two MOD-###s could each plausibly own, or
         two prior modules that match one proposed module equally well
         during MOD-ID reconciliation. /specclaw:bf-clarify types these
         DECISION — an ownership fork — or SCOPE, when the real question is
         whether that module belongs in the rebuild at all.)
    T4 — Legacy behaviour that appears to be a defect (describe; clarify
         will type it DEFECT).
    T5 — A capability with no one-to-one mapping in the rebuild target
         (describe; clarify will type it TARGET-GAP).
    T6 — Ordering, formatting, or default-value behaviour that is
         observable to users but not pinned by any code path the agent
         can cite.

  Entry format:

  ### PQ-### — <one-line question>

  - **Status:** OPEN | PROMOTED → CQ-### | WITHDRAWN
  - **Source:** <command/agent that raised it>
  - **Trigger:** T1–T6
  - **Blocks:** <artifact item IDs: MOD-###, DR-###, BL-0##, GM-###, field
    path. Name every id the question actually blocks — for a contested
    module boundary that means BOTH candidate MOD-###s plus the item they
    are contesting, since a question naming only one of them reads as
    settled in that one's favour.>
  - **Evidence found:** <cited findings, file:line or quoted passage>
  - **Could not determine:** <the specific gap>
  - **Candidates considered:** <options>
  - **Proposed default (UNCONFIRMED):** <default + one-line reasoning>

  Every PQ needs a real Proposed default with reasoning — a PQ without one
  is malformed, not just incomplete. "No default is reasonable — needs a
  fresh human answer" is a valid one-line reasoning of last resort; leaving
  the field off entirely is not. Before appending a new PQ, check existing
  PQ entries here and CQ entries in clarifications.md (if present) for the
  same artifact item — if one already covers it, add a cross-reference to
  the existing id (e.g. "see PQ-002") in your own finding instead of
  drafting a duplicate.
-->

### PQ-001 — Is the PatientController.Get()/Search() N+1 `.Last()` crash-on-empty-sequence pattern intentional legacy behavior to preserve, or a defect to fix in the rebuild?

- **Status:** PROMOTED → CQ-001
- **Source:** bf-architecture-analyst (architecture.md L4)
- **Trigger:** T4
- **Blocks:** L3/L4 "Domain API Controllers" component (architecture.md), any future rebuild-backlog item covering the Patients list/search endpoint
- **Evidence found:** `DM.Server/Controllers/PatientController.cs` — both `Get()` and `Search(string request)` call `List<Patient> patients = _patientCreateService.GetAll();` then, inside a `foreach (Patient patient in patients)`, call `_prescriptionService.GetPatientCurrentPrescription(patient.Id).Last();` — one extra query per patient (N+1), and `.Last()` throws `InvalidOperationException` on an empty sequence (e.g. a patient with zero prescriptions). No try/catch or null-guard surrounds either call.
- **Could not determine:** Whether every `Patient` row in the live database is guaranteed (by an invariant enforced elsewhere, e.g. at patient-creation time) to always have at least one `Prescription`, which would make the crash theoretical rather than a live defect — I did not open `DM.Repository/PatientCreateRepository.cs`'s creation path or `DM.Service/PatientCreateService.cs` to confirm such an invariant is enforced.
- **Candidates considered:** (a) Treat as a known DEFECT to fix in the rebuild (add a null/empty check, batch-load prescriptions instead of N+1); (b) treat as intentional/acceptable because a create-patient flow always seeds one prescription record, making this dead-code risk rather than a live bug; (c) preserve the exact current behavior (including the crash) for parity/replay-testing purposes until proven safe to change.
- **Proposed default (UNCONFIRMED):** Treat as a DEFECT to fix in the rebuild (option a) — an unhandled `InvalidOperationException` on a plausible empty-collection input is a stronger presumption than an unverified creation-time invariant, and fixing it (empty-check + batched prescription lookup) is unlikely to break any current caller since the failure mode today is a hard 500, not a meaningful business rule.

### PQ-002 — Should a rebuild target consolidate the two independently-migrated EF DbContexts (DentalDbContext + ApplicationDbContext) sharing one physical database into a single schema/context, or preserve the split?

- **Status:** PROMOTED → CQ-002
- **Source:** bf-architecture-analyst (architecture.md L2/L3)
- **Trigger:** T5
- **Blocks:** L2 "SQL Server Database" container and its two sub-schema nodes (architecture.md), L3 "Identity & Permission Subsystem" component, any future rebuild-backlog data-layer item
- **Evidence found:** `DM.Server/App_Start/UnityConfig.cs` registers both `container.RegisterType<DbContext, ApplicationDbContext>(...)` and `container.RegisterType<DbContext, DentalDbContext>(...)` in the same Unity container. `DM.Server/Models/ApplicationDbContext.cs` (`IdentityDbContext<ApplicationUser>`, connection name `"DefaultConnection"`) and `DM.Models/DentalDbContext.cs` (`base("name=DefaultConnection")`) both bind to the same named connection string. `DentalDbContext`'s static constructor calls `Database.SetInitializer<DentalDbContext>(null);` with the comment "The schema is owned by ApplicationDbContext's migrations, so skip EF's model-hash check for this context." Each context has its own independent `Migrations/` folder and history (`DM.Models/Migrations/*` vs `DM.Server/Migrations/*`).
- **Could not determine:** Whether this dual-context split was a deliberate design decision (e.g. to keep Identity/permission schema changes decoupled from domain schema changes) or accidental drift from scaffolding two separate Visual Studio templates (ASP.NET Identity template + a hand-rolled EF Code-First model) into one project — I found no comment or doc explaining the original intent beyond the workaround comment itself.
- **Candidates considered:** (a) Preserve the two-context split as-is in any rebuild target, since some modern stacks (e.g. a dedicated auth/identity service vs. a domain service) have a natural equivalent; (b) consolidate into a single DbContext/schema in the rebuild target, since both contexts already share one physical database and the split appears to be undocumented legacy structure rather than an intentional bounded-context separation; (c) split into two genuinely separate databases/services in the rebuild target, formalizing what is currently an implicit split.
- **Proposed default (UNCONFIRMED):** Consolidate into a single context/schema in the rebuild target (option b) — the current split shares one physical database and has no documented rationale beyond a workaround comment disabling EF's own safety check, which suggests unintentional drift rather than a deliberate bounded-context boundary worth preserving.

### PQ-003 — Should the About/Contact screens' broken controller wiring be fixed (implement `AboutController`/`ContactController`), or are these screens out of scope for a rebuild?

- **Status:** PROMOTED → CQ-003
- **Source:** /specclaw:bf-ui (bf-ui-analyst)
- **Trigger:** T4
- **Blocks:** SCR-019 (About), SCR-020 (Contact) in ui-inventory.md; the "View the About page" / "View the Contact page" capabilities in functional-spec.md (items 34–35) and its Named Gaps #5/#6
- **Evidence found:** `Client/app/scripts/app.config.js:72-89` registers UI-Router states `root.about` (template `app/views/about/about.tpl.html`, controller `AboutController`) and `root.contact` (template `app/views/contact/contact.tpl.html`, controller `ContactController`). `Client/app/scripts/about/about.controller.js` contains only the comment `// code goes here` (no `angular.module(...).controller("AboutController", ...)` registration anywhere in the repo — confirmed by `grep -rn "AboutController"` returning only the `app.config.js` reference). No `ContactController` definition exists anywhere under `Client/app/scripts/` (same grep result — only the `app.config.js` reference). `Client/index-dev.html`'s `<script>` list (lines 58-94) does **not** include `about.config.js`, `about.controller.js`, `about.service.js`, or any contact script at all — these files are only ever picked up by the Gulp `"scripts"` task's glob (`Gulpfile.js:100-111`, `./app/scripts/**/*.controller.js` etc.) for the production bundle, but even then `about.controller.js`'s content never registers the controller, and no `contact.controller.js` file exists to be globbed at all. `Client/app/views/about/about.tpl.html` and `Client/app/views/contact/contact.tpl.html` are each a single literal word ("about" / "Contact") with no bindings or controls.
- **Could not determine:** Whether navigating to `root.about`/`root.contact` in a running instance throws an AngularJS dependency-injection error that blocks the view from rendering at all, or whether the plain-text template still becomes visible despite a console error — I did not run the application to observe this, and static reading of the AngularJS/ui-router source alone does not settle it for certain across the ui-router version in use.
- **Candidates considered:** (a) Treat as a DEFECT — implement real `AboutController`/`ContactController` scripts (even trivial ones) and wire them into the script/bundle list so both routes render without a DI error; (b) treat as a TARGET-GAP/SCOPE call — these are placeholder marketing-style pages with no business logic, and a rebuild may reasonably drop them entirely or replace them with static content with no dedicated controller; (c) leave as-is on the assumption these routes are never exercised in practice (no nav link anywhere points at either state).
- **Proposed default (UNCONFIRMED):** Treat as SCOPE/TARGET-GAP (option b) — both templates contain zero real content or controls today, no in-app navigation link reaches either state (confirmed by `grep` across `Client/app/views/**` for `root.about`/`root.contact` `ui-sref` usage returning no results beyond `app.config.js`'s own route registration), and the wiring bug has evidently gone unnoticed in production, suggesting these are vestigial placeholder pages rather than a live user-facing gap worth preserving pixel-for-pixel.

### PQ-004 — Which navbar background color is actually rendered: style.css's `.navbar` rule or nav.html's own inline `.navbar-default` rule?

- **Status:** PROMOTED → CQ-004
- **Source:** /specclaw:bf-ui (bf-ui-analyst)
- **Trigger:** T6
- **Blocks:** TK-003 in design-tokens.json (global navbar background token group); the "Layout structure"/top-nav-band bullet on every SCR-### screen entry in ui-inventory.md (the shared nav chrome is present on all 20 screens)
- **Evidence found:** `Client/app/styles/style.css:16-20` defines `.navbar { background: #006A4E; ... }`, loaded via a `<link>` in `Client/index-dev.html:16` (parsed at initial page load, inside `<head>`). `Client/app/views/nav.html:1-4` defines its own inline `<style>.navbar-default { background-color: #218283 !important; }</style>` immediately followed by `<nav class="nav navbar navbar-fixed-top navbar-default" ...>` (`nav.html:20`) — this `<style>` tag is injected into the DOM only when `index-dev.html:23`'s `<header ng-include="'app/views/nav.html'"></header>` resolves at runtime (after initial page load), and additionally uses `!important`, which by CSS cascade rules always wins over a non-`!important` declaration of equal or lower specificity regardless of source order. Both `.navbar` and `.navbar-default` are present as classes on the same `<nav>` element.
- **Could not determine:** Whether `.navbar-default`'s `!important` declaration is genuinely rendered by a browser reliably in every case (it should, per CSS spec, since `!important` beats a non-`!important` rule outright, making this actually resolvable rather than a true tie) — I flagged this as uncertain rather than asserting the winner outright because I have not visually observed the running application to confirm no other stylesheet (e.g. a browser extension, a later-loaded vendor CSS, or an `!important` override I did not find elsewhere in `Content/bootstrap/` or `Content/less/`) contests it, and the task instructions explicitly bar me from asserting a computed/effective rendered value without that kind of confirmation.
- **Candidates considered:** (a) `.navbar-default`'s `#218283` wins (the `!important` declaration, per CSS cascade rules, should make this the effective color); (b) `.navbar`'s `#006A4E` wins (if some other mechanism I have not found overrides or strips the `!important`); (c) record both as candidate values and let a human confirm visually via a screenshot rather than asserting either.
- **Proposed default (UNCONFIRMED):** Candidate (a), `#218283` — CSS's `!important` mechanism is a very strong, well-defined tiebreaker that should make `nav.html`'s own rule win over `style.css`'s `.navbar` rule regardless of load order, but a human should confirm this visually against an actual screenshot before it is treated as settled, since this document's own hard constraint is to never assert a computed/rendered color without that confirmation.

### PQ-005 — Does PatientCreateController.Post's silent ignoring of Add()'s failure result (returning 200 OK with patient.Id regardless of whether the insert actually succeeded) need a defect fix in the rebuild?

- **Status:** OPEN
- **Source:** bf-baseline-designer (baseline design, seam/scenario discovery)
- **Trigger:** T4
- **Blocks:** GM-002 (scenarios.md), DR-001 (domain-model.md), any future rebuild-backlog item covering patient creation
- **Evidence found:** `DM.Server/Controllers/PatientCreateController.cs:50-79`'s `Post(Patient patient)` computes `patient.Code` via `HelperRequestModel.GetThisPatientCode(...)`, calls `bool add = _patientCreateService.Add(patient);`, and, regardless of whether `add` is `true` or `false`, always ends with `return Ok(patient.Id);`. `DM.Service/BaseService.cs:40-55`'s `Add` catches `Exception` and returns `false` on any failure (including a `DbUpdateException` from `Patient.Code`'s unique index, `DM.Models/DentalDbContext.cs:50`, `HasIndex(p => p.Code).IsUnique()`). Because the auto-generated `Code` is derived from `_patientCreateService.GetPatientViewModel().Count() + 1` (`PatientCreateController.cs:52`) rather than from a value guaranteed unique against every possible edit, a prior patient whose `Code` was manually changed via `PUT api/PatientCreate/Update` (which performs no uniqueness pre-check) to a value the count-based formula will generate next causes the insert to fail, silently, since the controller never inspects `add`.
- **Could not determine:** Whether this was a deliberate simplification (an assumption that `Code` is never edited post-creation in practice) or an unnoticed defect. I found no comment, test, or client-side guard addressing this case.
- **Candidates considered:** (a) Treat as a DEFECT, the rebuild should have the create endpoint return an accurate success/failure result and never claim success for a row that was not persisted; (b) treat as intentional/low-risk since editing a Patient's own Code is not a common workflow in the UI (patient-detail.tpl.html's Patient tab does not expose Code as an editable field, per functional-spec.md capability 5); (c) preserve the exact legacy behaviour (silent false-positive success) for replay parity until a human decides.
- **Proposed default (UNCONFIRMED):** Treat as a DEFECT (option a). An API returning 200 OK for a write that did not happen is a stronger presumption of a bug than an unverified "Code is never edited" assumption, and GM-002 is designed to capture today's actual (defective) response shape as the golden master regardless of this default.

### PQ-006 — Is MedicalInfoService.SavePatientMedicalInfos's first-item-scoped replace pattern (mirroring DR-018) and its crash on an empty submitted list a defect to fix, or intentional/acceptable legacy behavior?

- **Status:** OPEN
- **Source:** bf-baseline-designer (baseline design, seam/scenario discovery)
- **Trigger:** T4
- **Blocks:** GM-018 (scenarios.md), MOD-002 (module-map.md), any future rebuild-backlog item covering the Medical Condition tab
- **Evidence found:** `DM.Service/MedicalInfoService.cs:40-60`'s `SavePatientMedicalInfos(List<PatientMedicalInfo> patientMedicalInfos)` calls `var patientId = patientMedicalInfos.First().PatientId;` before anything else. When a caller submits an empty list, which is the real, reachable result of a user unchecking every previously-tagged medical condition on patient-detail.tpl.html's Medical Condition tab and clicking Save (Client/app/scripts/patient/patient-detail.controller.js's savePatientMedicalInfo(), which posts whatever the checkbox-bound array currently contains), `.First()` throws `InvalidOperationException` on the empty sequence, and the existing tagged-conditions list for that patient is left completely untouched (neither cleared nor replaced), unlike DR-018's sibling pattern in `DM.Repository/PatientMedicalServiceRepository.cs:25-38` (whose foreach+break shape happens not to throw on an empty list, only to silently no-op).
- **Could not determine:** Whether this crash has ever been observed in production, or whether the client always keeps at least one item selected by convention. I found no such guarantee in patient-detail.controller.js.
- **Candidates considered:** (a) Treat as a DEFECT, guard the empty-list case and use an unambiguous PatientId parameter (mirroring CQ-012's decision for DR-018) rather than inferring it from the submitted list; (b) treat as acceptable given how rarely a user would untag every condition; (c) preserve the exact crash for replay parity until decided.
- **Proposed default (UNCONFIRMED):** Treat as a DEFECT (option a), consistent with CQ-012's decision for the sibling DR-018 pattern. GM-018 is designed to capture today's actual crash as the golden master regardless of this default.

### PQ-007 — Is PrescriptionController.Delete's no-op stub (parses and echoes the request Guid without deleting anything) a defect to fix, or dead/reserved API surface intentionally left unimplemented?

- **Status:** OPEN
- **Source:** bf-baseline-designer (baseline design, seam/scenario discovery)
- **Trigger:** T4
- **Blocks:** "No Legacy Behaviour Exists" (scenarios.md, Prescription-level cascade delete), MOD-001 (module-map.md)
- **Evidence found:** `DM.Server/Controllers/PrescriptionController.cs:64-69`: `[HttpDelete] [Route("Delete")] public IHttpActionResult Delete(string request) { return Ok(Guid.Parse(request)); }`, no call to `_prescriptionService.Delete` or any repository method. I confirmed no other code path calls `IPrescriptionService.Delete`/`IPrescriptionRepository.Delete` anywhere in DM.Repository/ or DM.Service/ (grep across both directories returns only the interface declaration and the base-class implementation, never a call site). No AngularJS controller (patient-detail.controller.js, patient-create.controller.js) issues a DELETE call to PrescriptionUrl. The endpoint is reachable (routed, [Authorize]-gated, callable) but functionally inert.
- **Could not determine:** Whether this was left deliberately unimplemented (e.g., business rule "bills are never deleted, only closed" per DR-007) or is an abandoned partial implementation.
- **Candidates considered:** (a) Treat as intentional, bills are audit records that should never be hard-deleted, and the stub is a safe no-op guarding against accidental data loss; (b) treat as a DEFECT, either implement real deletion (cascading per DM.Models/Prescription.cs's required FKs to PatientMedicalService/Payment) or remove the misleading route entirely; (c) leave as-is since nothing in the UI calls it.
- **Proposed default (UNCONFIRMED):** Candidate (a) is the more likely reading given DR-007's close-then-reopen pattern treats bills as permanent audit records, but this needs a human decision since "endpoint exists, named Delete, does nothing" is exactly the kind of API-contract gap that should be a deliberate choice, not an accident. Pending resolution, GM-012's Patient-cascade scenario is the only reachable evidence of Prescription-level cascade-delete behaviour; a hypothetical direct Prescription delete is filed under "No Legacy Behaviour Exists."

### PQ-008 — Which of the two independently-coexisting "patient's current bill" resolution mechanisms is authoritative when they can disagree?

- **Status:** OPEN
- **Source:** bf-baseline-designer (baseline design, seam/scenario discovery)
- **Trigger:** T3
- **Blocks:** GM-011 (scenarios.md), DR-002, DR-007 (domain-model.md, both describe the workflow that can leave a patient with no Active prescription), MOD-001 (module-map.md)
- **Evidence found:** `DM.Server/Controllers/PatientController.cs:36` (Get()) and `:70` (Search) both call `_prescriptionService.GetPatientCurrentPrescription(patient.Id).Last()`, where `DM.Service/PrescriptionService.cs:22-25`'s GetPatientCurrentPrescription returns every prescription for the patient (both Active StatusId=5 and Closed StatusId=6, DM.Repository/PrescriptionRepository.cs:22-25 applies no status filter at all) ordered by Code ascending, then takes the last one with no status check. `DM.Server/Controllers/PrescriptionController.cs:39-43`'s GetPatientCurrentPrescription action instead calls `.LastOrDefault(x => x.StatusId == 5)` on the same unfiltered list, an explicit Active-only filter. When a patient's most-recently-created prescription (by Code ordering) is not Active, the documented failure mode of DR-002/DR-007's un-transacted two-write workflow (functional-spec.md's "Close Bill / Open New Bill" workflow, and its own note that if the second call is omitted the patient is left with no active bill at all), the two mechanisms diverge: PatientController's version returns the last Closed bill's stale totals with no error, while PrescriptionController.GetPatientCurrentPrescription returns null.
- **Could not determine:** Whether this divergence has ever manifested in production, or which of the two behaviours (silently showing stale Closed-bill totals on the grid, vs. returning null) is the one a rebuild should standardize on.
- **Candidates considered:** (a) Standardize on the Active-only (StatusId==5) filter everywhere, since it is the more precise definition of "current bill" and matches DR-002/DR-007's own intent; (b) standardize on the "most recent by Code" definition, since it degrades more gracefully (never null) at the cost of precision; (c) preserve both mechanisms' exact current disagreement for replay parity until a human decides, since fixing one call site without the other could introduce a new, different inconsistency.
- **Proposed default (UNCONFIRMED):** Candidate (a). DR-002/DR-007's own business intent (current/active bill) is explicitly Active-status-scoped, so the StatusId==5 filter is the more defensible single definition. GM-011 is designed to capture today's actual disagreement as the golden master regardless of this default, since both call sites are live legacy behaviour today.

### PQ-009 — Does UserService.CreateUser's password/retype-mismatch guard need a defect fix, given it forwards null into ASP.NET Identity's CreateAsync rather than returning a graceful rejection?

- **Status:** OPEN
- **Source:** bf-baseline-designer (baseline design, seam/scenario discovery)
- **Trigger:** T4
- **Blocks:** GM-028 (scenarios.md), DR-012 (domain-model.md), MOD-005 (module-map.md)
- **Evidence found:** `DM.Server/Service/UserService.cs:71-90`'s CreateUser: `if (model.PasswordHash != model.RetypePassword) return _repository.CreateUser(null);`, `DM.Server/Repository/UserRepository.cs:57-60`'s CreateUser(ApplicationUser applicationUser) forwards directly to `_manager.CreateAsync(applicationUser)` (Microsoft.AspNet.Identity.UserManager<TUser>). ASP.NET Identity 2.x's UserManager.CreateAsync(TUser user) throws ArgumentNullException when user is null (its own null-guard, not a graceful IdentityResult.Failed(...)). I am relying on well-documented ASP.NET Identity 2.x framework behaviour here rather than a first-party comment, since I cannot execute the compiled framework code in this environment to observe it directly. domain-model.md's DR-012 describes this path only as "abort the write," which does not distinguish a graceful rejection from an unhandled framework exception.
- **Could not determine:** Whether the actual runtime exception type is precisely ArgumentNullException versus some other Identity-internal exception, without executing the code. This should be settled empirically by the harness capture itself (Mode B), not asserted here.
- **Candidates considered:** (a) Treat as a DEFECT, the mismatch guard should return a clean IdentityResult.Failed(...)-shaped rejection instead of forwarding null; (b) treat as acceptable since the client-side mirror (Client/app/scripts/user/user.controller.js, per DR-012's own citation) already prevents this from being reachable through the normal UI flow, making it a defence-in-depth-only code path; (c) preserve the exact current (crashing) behaviour for replay parity until decided.
- **Proposed default (UNCONFIRMED):** Treat as a DEFECT (option a), an unhandled framework exception is a weaker API contract than the same rule's Update-path sibling (DR-012's UpdateUser, which degrades to a no-op-ish call rather than crashing). GM-028 is designed to capture today's actual behaviour (whichever exception genuinely surfaces) as the golden master regardless of this default, and is marked PROVISIONAL until a human resolves this question.

### PQ-010 — Which module should own the "retire or relocate DM.Core shared constants" backlog item?

- **Status:** OPEN
- **Source:** bf-rebuild-planner (rebuild-plan drafting, first run)
- **Trigger:** T3
- **Blocks:** BL-009 (rebuild-backlog.md, once rendered)
- **Evidence found:** `module-map.md`'s own `## Unassigned` entry — "`DM.Core` project (`AppConstants.cs`, `AppSettingsDto.cs`, `AppSettingsKey.cs`) — referenced by `DM.Server/DM.Server.csproj` per the collected `dependency_graph`, but `architecture.md` itself notes no opened file confirmed an actual consumer within `DM.Server`... not assignable to any module on current evidence." `decisions.md`'s CQ-024 — "perform a targeted full-repository usage search before finalizing the rebuild plan. Do not recreate DM.Core as a separate project automatically."
- **Could not determine:** Which of the five business modules, if any, should carry the actual "search for usage / relocate constants / drop unused code" work — DM.Core is referenced only by the `DM.Server` project as a whole (the `Host Bootstrap` L3 component in `architecture.md`), not by any single business module's own controllers/services/repositories.
- **Candidates considered:** (a) MOD-005 (Identity, Roles & Permissions), since it is the dependency root and the only module whose own controllers/services are compiled directly inside `DM.Server` alongside Host Bootstrap; (b) leave genuinely unassigned pending the CQ-024 usage search itself settling which module (if any) actually consumes a DM.Core constant; (c) treat as a cross-cutting infra item outside the module hierarchy entirely.
- **Proposed default (UNCONFIRMED):** (a) MOD-005 — of the five modules, only MOD-005's controllers/services sit inside the same `DM.Server` project as Host Bootstrap per `architecture.md`'s own L3 diagram, making it the closest existing module boundary to attach this cleanup work to pending CQ-024's usage search settling the question for real.
