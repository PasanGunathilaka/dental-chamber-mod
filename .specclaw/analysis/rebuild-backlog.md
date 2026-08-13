# Rebuild Backlog: App

**Path analyzed:** .
**Date generated:** 2026-08-12
**Source documents:** codebase-report.md, architecture.md, domain-model.md, functional-spec.md, module-map.md

<!--
  NOTE ON THIS COMMENT: never write a literal double-brace placeholder
  token inside this comment's own prose (not even to describe it) — the
  render step's template substitution is a dumb global string replace, and
  a token mentioned here would get overwritten by that token's rendered
  value along with the real placeholder below, corrupting this comment.
  Refer to placeholders by section name instead (e.g. "the status block
  below", "the Backlog section").

  The status block right after this comment is bash-computed, never
  agent-drafted — date, which optional inputs (decisions.md,
  clarifications.md, baseline/manifest.json, baseline/scenarios.md) were
  consumed vs. missing (with the command that produces each),
  Gate/Verification counts, and the single recommended next item to
  propose. This block, and every item's Gate:/Verification: field below,
  is recomputed from scratch on every run — never hand-maintained.

  MODULE GROUPING. The Backlog section below is two levels deep: one
  "## MOD-### — <Module Name>" heading per module from module-map.md, with
  that module's "### BL-0##" items beneath it. Modules are ordered by their
  own dependency rank from the map (foundations first), computed in bash by
  the same fixed-point pass that ranks items; a declared cycle is reported
  and no rank number is printed, because the number would be an artifact of
  the iteration cap rather than a dependency depth.

  A module is a MIGRATION/ACCEPTANCE unit — the "one flow at a time" slice a
  large legacy system is rebuilt and signed off in. BL items remain the
  BUILD units: the hierarchy is MOD-### -> BL-0## -> DR-### -> GM-###, and a
  module is NEVER collapsed into one giant BL item. Item granularity rules
  are exactly as they were — a module only groups items that already exist
  at capability-bullet granularity.

  Item order WITHIN a module is unchanged from before modules existed
  (dependency rank first — a hard constraint — then within the same rank:
  CLEAR+VERIFIABLE, then CLEAR+PENDING CAPTURE/NO BASELINE DATA/
  UNVERIFIABLE, then OPEN QUESTIONS, then BLOCKED). Two further top-level
  groups may appear after the modules: "## Unassigned — no module declared"
  (items with no **Module:** field, or one naming a MOD-### the map does not
  define — never folded into a real module by guesswork) and "## Struck"
  (tombstones, which belong to no module).

  Expected per-item sub-structure inside each module group — one entry per
  backlog item:

  ### BL-NNN — <Feature Title>

  **Module:** <the MOD-### from module-map.md this item belongs to. Declared
    by the planner agent, read mechanically by bash, and NEVER derived from
    the item's DR-### rules — deriving one would be a silent assignment, and
    a disagreement between this field and the map's own rule ownership is
    exactly what /specclaw:bf-baseline record reports as a WARN at record
    time. An item with no such field is rendered under "## Unassigned",
    never guessed into a module.>
  **Maps to capability:** <functional-spec.md capability name/quote>
  **Depends on:** <earlier items' BL-NNN IDs, or "None">
  **Acceptance basis (domain-model.md):**
  - <entity/business-rule/enumeration reference, quoted — cite a business
    rule's DR-NNN ID (from domain-model.md) directly wherever the
    acceptance basis rests on a numbered rule, e.g. "DR-007: ..."; this is
    the join key /specclaw:bf-clarify and /specclaw:bf-baseline key their own
    CQ-NNN/GM-NNN citations against, so the ID itself must be textually
    present, not just implied by the quoted prose>

  **Verification inputs needed:**
  - <golden-master capture, external-format/DLL/COM semantics, or other
    human-supplied input this item's fidelity check will need — never
    leave this field blank; if genuinely nothing beyond the acceptance
    criteria above applies, say so explicitly rather than omitting it>

  **Gate:** <bash-computed: BLOCKED — blocked by <CQ-NNN + one-line title,
    ...> | OPEN QUESTIONS — risk from unanswered, non-blocking: <CQ-NNN,
    ...> | CLEAR>
  **Verification:** <bash-computed: VERIFIABLE — fixtures: <GM-NNN (legacy
    commit sha), ...> | PENDING CAPTURE — scenarios designed, no recorded
    fixture yet: <GM-NNN, ...> | UNVERIFIABLE — acceptance must come from a
    stakeholder decision, not fixture comparison (see CQ-NNN) | NO BASELINE
    DATA — baseline not run (or not designed) for these rules>
  **UI fidelity:** <bash-computed, and present ONLY when this item renders a
    screen AND the UI fidelity policy (SQ-013, read mechanically from
    decisions.md) is decided FAITHFUL/THEME-ONLY or is undecided. Renders as:
    FAITHFUL — reproduce the layout structure and token values of: <SCR-###,
    ...>; token groups: <TK-###, ...> | THEME-ONLY — reproduce the token
    values of: <TK-###, ...>; screens for reference only: <SCR-###, ...> |
    ⚠ UI GROUNDING MISSING — <the decided policy, plus which .specclaw/ui/
    artifacts are absent, or the fact that this item cites no SCR-### at all>
    | UNDECIDED — <SQ-013 has no recorded decision>. The last two also
    contribute an OPEN QUESTIONS state to the Gate line above, naming SQ-013.
    Under a decided REINTERPRET policy this field never appears on any item
    and no warning is emitted anywhere — the zero-extra-work path for a
    project that does not need visual fidelity. Which items render a screen
    is the planner agent's judgment, delivered as a SCREEN-BEARING: directive
    and applied mechanically here; SCR-###/TK-### content itself belongs to
    /specclaw:bf-ui, never to this document. A cited SCR-### never implies
    visual equivalence has been proven — that is established by a named human
    signing ui-review.md against recorded screenshots, never by this backlog
    and never by fixture replay.>
  **Settled constraints (from decisions):** <optional — only present when a
    mechanical-adopt decision applies to this item; omit the field entirely
    otherwise, never render it empty>

  **Status notes (human-added):** <optional — anything a human types under
    this exact heading (e.g. "built and merged, PR #12") survives every
    future /specclaw:bf-rebuild-plan --refresh verbatim, byte for byte. Nothing
    else in this document offers that guarantee — this is the one place a
    human note is safe to leave.>

  If two or more functional-spec capabilities are merged into a single
  backlog item, the item must state why in a "Merge rationale:" line —
  merging is a judgment call, never silent. A revised item (its acceptance
  basis rewritten because a decision changed its shape) states so inline,
  e.g. a line reading "⟲ revised per CQ-005, 2026-08-01" placed right after
  the heading.

  PROVISIONAL marker: an item touched by an open pending question — either
  a direct DR-NNN/BL-NNN join to a CQ-NNN promoted from a PQ-NNN (bash-
  computed), or a prose-level match the planner agent found and directed
  via a PROVISIONAL: line (agent-judged, mechanically re-verified by bash
  the same way an UNVERIFIABLE: directive is) — carries its own line right
  after the heading: "⚠ PROVISIONAL — pending PQ-NNN/CQ-NNN (proposed
  default: <x>)". This is soft-block: the item is still fully drafted,
  sequenced, and gated/verified exactly as any other; the marker rides
  alongside Gate/Verification, not instead of them, and both this line and
  Gate/Verification are recomputed from scratch on every run — it clears
  automatically once decisions.md answers the underlying question, no
  manual cleanup.

  BL-NNN IDs are permanent identifiers, not position — assigned once in
  dependency order on the first-ever run and never renumbered afterward.
  A later /specclaw:bf-rebuild-plan --refresh may append a genuinely new item
  (next free BL-NNN, dependency-placed correctly) or strike/defer an
  existing one, but an already-assigned ID is never reused, renumbered, or
  silently deleted — a struck item stays in the Backlog section as a
  one-line tombstone ("### BL-NNN — STRUCK — <reason>, <date>"); a deferred
  item moves in full to the Deferred section, out of the ready ordering.
  "Depends on:" always cites BL-NNN IDs, never bare position, for exactly
  this reason.
-->

**Date:** 2026-08-12
**Inputs consumed:**
- decisions.md: present
- clarifications.md: present
- baseline/manifest.json: present
- baseline/scenarios.md: present

**Module map:** CONFIRMED by Pasan Gunathilaka, 2026-08-12

> ⚠ **The module map is not confirmed.** Its `**Status:**` line reads `PROPOSED — awaiting human confirmation`, so the grouping and sequencing below rest on a proposal no human has signed off. Review `.specclaw/analysis/module-map.md` and set its Status to `CONFIRMED by <name>, <date>`. Nothing here is blocked by this — the backlog is complete and usable — but a module boundary nobody checked becomes the shape of the whole migration.

**Recommended next module to build:** MOD-005 — Identity, Roles & Permissions
- **Why:** dependency rank 0; depends on none; 9 active item(s) — 9 CLEAR, 0 OPEN QUESTIONS, 0 BLOCKED, 0 PROVISIONAL; every module it depends on is likewise free of BLOCKED and PROVISIONAL items.
- **Readiness, not completion:** specclaw records no "built" state for a backlog item, so this is the next module whose work can *start*, not a claim that anything it depends on is finished.

**UI fidelity policy:** THEME-ONLY (SQ-013)
- .specclaw/ui/ui-inventory.md: present
- .specclaw/ui/design-tokens.json: present
- .specclaw/ui/screens/: present
- .specclaw/ui/ui-manifest.json: present
- Screen-bearing items: 27, of which 1 lack UI grounding

**Gate counts:** CLEAR: 28, OPEN QUESTIONS: 1, BLOCKED: 0 (of 29 active items; 0 struck, 0 deferred)
**Verification counts:** VERIFIABLE: 19, PENDING CAPTURE: 0, UNVERIFIABLE: 0, NO BASELINE DATA: 10
**Provisional (pending a decision):** 0 item(s) — independent of Gate/Verification; see each item's own marker

**Recommended next item to propose:** BL-001 — Data Layer Consolidation, Migration & Schema Setup

## Backlog

## MOD-005 — Identity, Roles & Permissions

_Depends on: none. Module dependency rank 0. 9 active item(s)._

### BL-001 — Data Layer Consolidation, Migration & Schema Setup


**Module:** MOD-005
**Maps to capability:** No functional-spec capability bullet covers this directly — it is decision-implied foundational work per rubric step 6 (CQ-002, SQ-002, SQ-005), plus a legacy defect confirmed by running the harness this session.
**Depends on:** None — this is the foundational item every other item in this backlog implicitly requires (a working, migratable schema).
**Acceptance basis (domain-model.md):**
- Per CQ-002 ("consolidate into one PostgreSQL database and one EF Core application DbContext/schema for the initial rebuild... Authentication/Identity tables and domain tables may remain logically separated by configuration/naming, but they should share one controlled migration history instead of two independent migration pipelines"), the rebuild's EF Core migration history must be single and coherent across every entity this backlog touches, including all four MOD-005-owned entities (ApplicationUser, IdentityRole, Resource, Permission — domain-model.md's Identity/permission schema) and every domain-schema entity DR-001 through DR-020 govern.
- Per SQ-002 ("migrate from SQL Server to PostgreSQL. Use Entity Framework Core with the PostgreSQL provider. Create a clean PostgreSQL schema and new EF Core migrations rather than reusing the legacy EF6 SQL Server migration history directly") and SQ-005 ("migrate all existing production data into PostgreSQL... Include migration validation and reconciliation checks before production cutover"), this item covers the actual schema/migration build plus the one-time data-migration/reconciliation tooling for all sixteen legacy entities.
- **Legacy defect confirmed by running the harness this session (not yet in any analysis document):** the EF6 migration chain cannot build a fresh database — `InitialCreate` creates `dbo.Patient`'s unique `IX_Code` (DR-001: "`Code`... enforced unique via `DentalDbContext.OnModelCreating`'s `HasIndex(p => p.Code).IsUnique()`"), and the later migration `202509030639057_Patient_Code_Unique` runs `CreateIndex` on the same column with no preceding `DropIndex`, so `Update()` against an empty database always fails. The rebuild's own from-scratch EF Core migration history (built fresh per SQ-002, not reusing the legacy EF6 history) must not reproduce this specific defect, and the data-migration tooling must be validated against a genuinely fresh target schema, not assumed to work because the legacy chain "eventually" reaches a working state on an already-seeded database.
**Verification inputs needed:**
- A full legacy-database export (schema + data) to validate the SQ-005 reconciliation tooling against — this is a golden-master input only a human with production/staging database access can supply; no fixture captures it.
- Confirmation from a human that the from-scratch EF Core migration chain actually builds and seeds cleanly end-to-end (the specific defect above was found by attempting exactly this, not by static reading, so the fix itself needs the same live-build verification, not just a fixture).
**Gate:** CLEAR
**Verification:** VERIFIABLE — fixtures: GM-001 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-002 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-022 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-023 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93)

---

### BL-002 — Authenticate & End Session (Login / Logout)


**Maps to capability:** "25. **Log in** — Username (text) / Password (password) — ... `LoginController.login()`. See workflow **"Login & Post-Auth Routing."**"; "26. **Log out** — clears local token/user/role storage and returns to the login screen (`AppControlller.logout()` / `ProfileController`'s own `logout()`)."
**Merge rationale:** Both bullets describe the two halves of one session lifecycle (start/end), share the same underlying OAuth bearer-token mechanism, and have no independent acceptance criteria of their own beyond that shared mechanism.
**Module:** MOD-005
**Depends on:** BL-001
**Acceptance basis (domain-model.md):**
- Per CQ-022 ("remove the dead branching and route authenticated users to one standard landing/dashboard screen. Authorization determines what actions/navigation are visible") — the "Login & Post-Auth Routing" workflow's seven-way role branch (functional-spec.md: "Every one of the seven seeded roles currently routes to the same `root.patient` landing screen... there is no role-differentiated landing page despite the branch existing in code") is replaced with a single landing route in the rebuild; this is a deliberate divergence from legacy per CQ-022, not a fidelity gap.
- Per CQ-025 ("implement real Remember Me behaviour using a secure longer-lived refresh/session mechanism when selected and a shorter normal session when not selected. Never store user passwords in browser storage") — the legacy "Remember me" checkbox (documented dead/no-op in ui-inventory.md's Widget Cross-Reference Findings item 4: "`isRemebered` is never read anywhere else in `Client/app/scripts/auth/**`") becomes a real feature in the rebuild.
- Per CQ-016 ("drop all legacy social-login provider dependencies. They are inactive template cruft") — no Facebook/Google/Twitter/Microsoft OAuth provider wiring is carried forward, per architecture.md's own finding that every corresponding `app.Use*Authentication(...)` call is already commented out in legacy `Startup.Auth.cs`.
- Per SQ-004 ("use ASP.NET Core Identity with secure token-based authentication suitable for the React SPA") — the OAuth resource-owner-password-grant mechanism the "Login & Post-Auth Routing" workflow documents (`POST token, grant_type=password` → bearer token, 13-day expiry) is rebuilt on ASP.NET Core Identity's own token issuance, not reproduced byte-for-byte.
- SCR-001 (Login): its layout is a centered single-column "Sign In" panel with Username, Password, "Remember me", and a Sign In button (ui-inventory.md). Token groups TK-001 (global-colors: body-background `#EBEBEB`, dropdown-menu-background `#006a4e`), TK-002 (global-typography), TK-003 (navbar-background, `#218283` per CQ-004's decision) apply per THEME-ONLY policy — reproduce these token values, not the legacy pixel layout.
**Verification inputs needed:**
- No golden-master fixture exists for the login/logout seam itself (`manifest.json`'s 41 fixtures cover DR-001 through DR-020 and cascade/computed-property findings only, none targeting `AccountController`/OAuth token issuance) — this item's acceptance rests on the decisions above (CQ-022, CQ-025, CQ-016, SQ-004), not on a captured legacy fixture; a human must confirm the new token-issuance flow against a manual login/logout smoke test.
**Gate:** CLEAR
**Verification:** VERIFIABLE — fixtures: GM-001 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-002 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-022 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-023 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93)
**UI fidelity:** THEME-ONLY — reproduce the token values of: TK-001, TK-002, TK-003; screens for reference only: SCR-001 (layout reinterpreted for the target platform)

---

### BL-009 — Retire or Relocate DM.Core Shared Constants


⚠ PROVISIONAL — pending PQ-010 (proposed default: MOD-005)
**Maps to capability:** No functional-spec capability bullet covers this — decision-implied work per rubric step 6 (CQ-024).
**Module:** MOD-005
**Depends on:** BL-001
**Acceptance basis (domain-model.md):**
- Per CQ-024 ("perform a targeted full-repository usage search before finalizing the rebuild plan. Do not recreate DM.Core as a separate project automatically. Move genuinely used constants/settings into the appropriate modern ASP.NET Core configuration/domain location and drop genuinely unused code"), grounded in architecture.md's own finding that `DM.Core` (`AppConstants.cs`, `AppSettingsDto.cs`, `AppSettingsKey.cs`) is referenced by `DM.Server/DM.Server.csproj` per the collected dependency graph, but no opened file confirmed an actual consumer within `DM.Server` — this item is the CQ-024 usage search itself, plus the resulting relocate-or-drop work.
**Verification inputs needed:**
- No fixture exists or is expected for this item — its acceptance criterion is CQ-024's decision text plus a human-run repository-wide usage search, not a behavioral replay.
**Gate:** CLEAR
**Verification:** NO BASELINE DATA — baseline has been run, but no scenario in scenarios.md cites this item's rules

---

### BL-003 — Manage Roles


**Maps to capability:** "30. **Manage roles** (Create/Update/Delete `IdentityRole`) — Name (text) — `role.tpl.html` (not opened this run), `RoleController.js` (Angular) → `api/Role`."
**Module:** MOD-005
**Depends on:** BL-002
**Acceptance basis (domain-model.md):**
- DR-014: "SystemAdmin is hidden from non-SystemAdmin viewers — `DM.Server/Service/RoleService.cs`'s `GetAll()` removes the "SystemAdmin" role from the list... whenever the caller... is not itself in the SystemAdmin role."
- SCR-014 (Manage Roles): a one-row create/edit form (Name + Save/Update/Cancel) above a roles table with edit/delete icons (ui-inventory.md). Token groups TK-001, TK-002, TK-003 per THEME-ONLY policy.
**Verification inputs needed:**
- GM-033 (RoleService.GetAll hides SystemAdmin from a non-SystemAdmin caller — VERIFIABLE, `manifest.json`) and GM-035 (both list-filtering calls show everything for a SystemAdmin caller — VERIFIABLE) are the captured fixtures for DR-014 as it applies to this item; no further human input is needed beyond replaying them against the rebuilt endpoint.
**Gate:** CLEAR
**Verification:** VERIFIABLE — fixtures: GM-033 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-034 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-035 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93)
**UI fidelity:** THEME-ONLY — reproduce the token values of: TK-001, TK-002, TK-003; screens for reference only: SCR-014 (layout reinterpreted for the target platform)

---

### BL-008 — User Profile & Change Password


**Maps to capability:** "27. **View/update own profile** — First Name, Last Name, Email, Phone (all text) — `profile.tpl.html`... `ProfileController.updateProfile()` → `POST api/Profile/UpdateProfile`."; "28. **Change own password** — Current Password, New Password, Retype Password (all password inputs, inferred...) → `POST api/Profile/UpdatePassword`; enforces DR-013. Forces a logout on success."
**Merge rationale:** Both bullets are two panels of the single `root.profile` screen (SCR-017), share the same `ProfileController`, and neither has any acceptance criterion independent of the other's screen context.
**Module:** MOD-005
**Depends on:** BL-002
**Acceptance basis (domain-model.md):**
- DR-013: "Changing your own password requires the current password — `ProfileService.cs`'s `UpdatePassword` verifies... against the supplied `CurrentPassword` before accepting the change, and separately requires `NewPassword == RetypePassword`."
- SCR-017 (User Profile & Change Password): two side-by-side panels — User Profile (Username disabled, First/Last Name, Email, Phone, Update) and Change Password (Current/New/Retype Password, Update Password) — with a `demo-user-restricted` state hiding both Update buttons for the demo account (ui-inventory.md). Token groups TK-001, TK-002, TK-003 per THEME-ONLY policy.
**Verification inputs needed:**
- GM-030 (wrong current password rejected), GM-031 (mismatched new/retype rejected before current-password check), GM-032 (happy path) — all three VERIFIABLE for DR-013 — are captured and directly replayable.
**Gate:** CLEAR
**Verification:** VERIFIABLE — fixtures: GM-030 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-031 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-032 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93)
**UI fidelity:** THEME-ONLY — reproduce the token values of: TK-001, TK-002, TK-003; screens for reference only: SCR-017 (layout reinterpreted for the target platform)

---

### BL-005 — Manage Resources (Protected Screens Catalog)


**Maps to capability:** "31. **Manage the resource/screen catalog** (Create/Update/Delete `SecurityModels.Resource`) — Name (text), Route (text), Public (radio pair Yes/No) — `Client/app/views/auth/resource.tpl.html`, `ResourceController.js` (Angular) → `api/Resource`. The Public radio group binds both a plain `value` attribute and an Angular `ng-value` to the same `ng-model` — see Named Gaps."
**Module:** MOD-005
**Depends on:** BL-002
**Acceptance basis (domain-model.md):**
- domain-model.md's SecurityModels.Resource entity: "`Id` (string PK), `Name`, `Route` (required — matches an AngularJS UI-Router state name...), `IsPublic` (required bool)."
- Per CQ-019 ("the React rebuild will use one explicit boolean IsPublic field with a single unambiguous Material UI control and server-side validation. Do not reproduce the AngularJS double-binding defect. Existing persisted boolean values will be migrated as stored") — the legacy double-binding ambiguity (functional-spec.md Named Gap #8) is deliberately not reproduced; this is a decided divergence, not a fidelity gap.
- SCR-015 (Manage Resources): a create/edit form (Name, Route, Public radio pair) above a resources table with edit/delete icons (ui-inventory.md). Token groups TK-001, TK-002, TK-003 per THEME-ONLY policy.
**Verification inputs needed:**
- No golden-master fixture in `manifest.json` targets `ResourceController` directly (its only documented behavior is the ambiguous double-binding CQ-019 already resolved by decision, not by replay) — acceptance rests on CQ-019's decision text, verified by a human confirming the new single-field control persists `IsPublic` correctly, not by fixture comparison.
**Gate:** CLEAR
**Verification:** NO BASELINE DATA — baseline has been run, but no scenario in scenarios.md cites this item's rules
**UI fidelity:** THEME-ONLY — reproduce the token values of: TK-001, TK-002, TK-003; screens for reference only: SCR-015 (layout reinterpreted for the target platform)

---

### BL-004 — Manage Staff User Accounts


**Maps to capability:** "29. **Manage users** (Create/Update/Delete `ApplicationUser`) — First/Last Name, Email, Phone, Username (all text), Role (select, options from `api/Role`), a "Change Password" checkbox that reveals New Password / Retype Password (password inputs) only when checked — `Client/app/views/user/user.tpl.html`, `UserController.js` → `api/User/{CreateUser,UpdateUser,DeleteUser}`. Enforces DR-011 (no self-delete) and DR-012 (password confirmation match)."
**Module:** MOD-005
**Depends on:** BL-002, BL-003
**Acceptance basis (domain-model.md):**
- DR-011: "A user cannot delete their own account — `UserController.cs`'s `DeleteUser` returns `BadRequest()` when `HttpContext.Current.User.Identity.GetUserId() == id`."
- DR-012: "New/updated user passwords must be confirmed — `UserService.cs`'s `CreateUser`/`UpdateUser` compare `model.PasswordHash` to `model.RetypePassword` and abort the write if they differ."
- DR-014: "SystemAdmin is hidden from non-SystemAdmin viewers... `UserService.cs`'s `GetUsers()` removes users holding that role... whenever the caller... is not itself in the SystemAdmin role."
- Per CQ-015 ("preserve and explicitly enforce one primary role per user in this rebuild because that matches the observed legacy UI behaviour") — the single-`<select>` Role field (ui-inventory.md Widget Cross-Reference Findings item 3: "the Manage Users screen's Role field is a single `<select>` bound to the singular `model.RoleId` — the UI can only ever assign exactly one role per user," despite `ApplicationUser.Roles` being a collection) is preserved as a deliberate one-role-per-user model, not "fixed" to expose multi-role assignment.
- Per CQ-017 ("allow simple known demo credentials only in explicitly local/development seed data. Production deployments must never create accounts with shared hardcoded passwords") — the legacy `AddUsers()` seed's shared hardcoded `"123qwe"` password for `superadmin`/`admin` (functional-spec.md Named Gap #9) is not carried into any production seed path.
- SCR-013 (Manage Users): a create/edit form (First/Last Name, Email, Phone, Username, Role, conditional Change-Password fields) above a users table with edit/delete icons (ui-inventory.md). Token groups TK-001, TK-002, TK-003 per THEME-ONLY policy.
**Verification inputs needed:**
- GM-026/GM-027 (DR-011 self-delete block and its contrast case — both VERIFIABLE) and GM-029 (DR-012's password-mismatch-discards-all-fields finding — VERIFIABLE) are captured. GM-028 (DR-012's CreateUser password-mismatch path) is marked `PROVISIONAL` in `manifest.json` pending **PQ-009** (no CQ id has been assigned to it yet — it is still OPEN in `pending-questions.md`), which asks whether `UserService.CreateUser` forwarding `null` into ASP.NET Identity's `CreateAsync` on a password/retype mismatch (rather than a graceful rejection) should be a defect fix; this item's acceptance for that specific sub-case cannot be finalized until PQ-009 is answered. GM-034 (DR-014's UserService.GetUsers finding) itself carries an unresolved open question in scenarios.md about whether EF6's reference-equality removal genuinely works — the rebuild's own EF Core equivalent should be verified fresh rather than assumed equivalent.
**Gate:** CLEAR
**Verification:** VERIFIABLE — fixtures: GM-026 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-027 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-028 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-029 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-033 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-034 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-035 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93)
**UI fidelity:** THEME-ONLY — reproduce the token values of: TK-001, TK-002, TK-003; screens for reference only: SCR-013 (layout reinterpreted for the target platform)

---

### BL-006 — Grant/Revoke Role Permissions


**Maps to capability:** "32. **Grant/revoke role permissions** — select a Role from a read-only list, then check/uncheck individual Resources (checkbox list wired via the `checklist-model` directive) or use "Check All"/"Uncheck All", then Save — `Client/app/views/auth/permission.tpl.html`, `PermissionController.js` (Angular) → `POST api/Permission/AddList`, which fully replaces that role's permission set (`AddPermissions` seed pattern mirrored at runtime by `PermissionController.CheckPermission` reads)."
**Module:** MOD-005
**Depends on:** BL-003, BL-005
**Acceptance basis (domain-model.md):**
- DR-016: "Fresh installs grant permissions only to SystemAdmin — `AddPermissions()` seeds `Permission` rows only for the `SystemAdmin` role against every private `Resource`; every other seeded role... starts with zero granted resources until a SystemAdmin explicitly grants them via the Permission screen."
- SCR-016 (Manage Permissions): a roles list panel beside a resources panel with per-row checkboxes and Check All/Uncheck All bulk actions, Save fully replacing the selected role's permission set (ui-inventory.md). Token groups TK-001, TK-002, TK-003 per THEME-ONLY policy.
**Verification inputs needed:**
- GM-039/GM-040 (DR-016's seed-only-grants-SystemAdmin finding and its no-op-on-rerun contrast case — both VERIFIABLE) are captured and directly replayable against the rebuilt seeding logic.
**Gate:** CLEAR
**Verification:** VERIFIABLE — fixtures: GM-039 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-040 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93)
**UI fidelity:** THEME-ONLY — reproduce the token values of: TK-001, TK-002, TK-003; screens for reference only: SCR-016 (layout reinterpreted for the target platform)

---

### BL-007 — Server-Side & Screen-Level Authorization Enforcement


**Maps to capability:** "33. **Screen-level access gate on every navigation** — not a user-initiated action but a capability the system enforces on all 32 above: every AngularJS state transition is checked before it renders — see workflow **"Screen Access Authorization Check."**"
**Module:** MOD-005
**Depends on:** BL-006
**Acceptance basis (domain-model.md):**
- DR-015: "Screen/route access requires a public resource or an explicit permission — `PermissionService.cs`'s `CheckPermission`: if the matched `Resource.IsPublic` is true, access is granted unconditionally; otherwise, access is granted only if a `Permission` row exists for the caller's role + that resource."
- The "Screen Access Authorization Check" workflow (functional-spec.md): every state transition calls `AuthService.authorize` → `PermissionService.CheckPermission`, redirecting to `root.access-denied` on denial. This is a client-orchestrated check with no server-side mirror documented on the domain controllers today (functional-spec.md Named Gap #13: "No server-side authorization check tied to the Resource/Permission model was found on the domain Web API controllers themselves... A user who could reach the API directly (bypassing the SPA) would not be blocked by DR-015/DR-016 at all").
- Per CQ-013 ("fix the security gap. Every protected ASP.NET Core endpoint must enforce the appropriate permission/role policy server-side. React route protection is supplementary UI behaviour only") — this item's core acceptance criterion is that the rebuild closes exactly this gap: DR-015/DR-016 must be enforced by the ASP.NET Core backend itself on every protected endpoint, not only by the React client's route guard. **Because the legacy client-orchestrated check is the only mechanism the legacy app has today, and no `DR-###` describes a server-side equivalent that already exists to fixture against, this item's server-side half has no legacy fixture to inherit — its acceptance is the CQ-013 decision text itself, not a golden-master replay.**
- SCR-018 (Access Denied): a centered "Access Denied" message with a "Back To Home" button, shown on authorization failure (ui-inventory.md). Token groups TK-001, TK-002, TK-003 per THEME-ONLY policy.
**Verification inputs needed:**
- GM-036/GM-037/GM-038 (DR-015's public-resource-grants-unconditionally, private-resource-denies-without-a-grant, and private-resource-grants-with-a-matching-Permission-row cases — all three VERIFIABLE) capture the legacy `CheckPermission` truth table and remain valid fixtures for whichever layer (client guard or new server-side policy) ends up calling the equivalent logic in the rebuild. **They do not, however, exercise the new server-side enforcement CQ-013 requires** — those fixtures pin `PermissionService.CheckPermission`'s own decision logic, not "does a direct API call bypassing the SPA get rejected," which is exactly the omission CQ-013 exists to close and which no legacy fixture can attest to, since the legacy app never enforced it. A human must add and run a new server-side authorization test suite (calling protected endpoints directly, without going through the SPA) once the rebuild's policy middleware exists; no golden-master capture can substitute for it.
**Gate:** CLEAR
**Verification:** VERIFIABLE — fixtures: GM-036 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-037 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-038 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-039 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-040 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93)
**UI fidelity:** THEME-ONLY — reproduce the token values of: TK-001, TK-002, TK-003; screens for reference only: SCR-018 (layout reinterpreted for the target platform)

## MOD-002 — Service & Medical-Info Catalog

_Depends on: MOD-005. Module dependency rank 1. 3 active item(s)._

### BL-010 — Manage Dental Service Price Catalog


**Maps to capability:** "13. **Manage the dental-service price catalog** (Create/Edit/Delete `MedicalService`) — Name (text, capitalized), Charge (text) — `patient-service.tpl.html` (not opened this run), `PatientServiceControlller` → `api/MedicalServices/{GetAll,Create,Update,Delete}`."
**Module:** MOD-002
**Depends on:** BL-002, BL-007
**Acceptance basis (domain-model.md):**
- DR-017: "Catalog names must be unique — `MedicalService.Name` and `MedicalInfo.Name` both carry `[Required][StringLength(50,MinimumLength=2)]` plus a unique index (`IX_Name`)."
- DR-019: "mechanical, reason not evident — `MedicalService.TotalCharge` (`[NotMapped]`) is computed as `Convert.ToInt32(Charge) * Quantity`, even though `Charge` is declared `[DataType(DataType.Currency)] string`. This truncates any fractional currency value and throws `FormatException` for a non-integer `Charge` string."
- Per CQ-008 ("fix the defect. Use a proper decimal money type in .NET and a fixed-precision numeric/decimal column in PostgreSQL. TotalCharge must retain fractional currency values and must not truncate to integer values. Audit legacy Charge strings during migration and explicitly report values that cannot be parsed") — DR-019's truncation/crash behavior is a defect the rebuild fixes, not preserves; the migration tooling (BL-001) must audit and report any legacy `Charge` value that cannot be parsed as a decimal.
- SCR-006 (Service Catalog): a one-row create/edit form (Name, Charge) above a catalog table with edit/delete icons (ui-inventory.md). Token groups TK-001, TK-002, TK-003 per THEME-ONLY policy.
**Verification inputs needed:**
- GM-013/GM-014 (duplicate-name rejection at the service and persistence layers — both VERIFIABLE) cover DR-017. GM-016 (integer Charge boundary values — VERIFIABLE) and GM-017 (non-integer Charge strings — VERIFIABLE, but scenarios.md itself notes the exact outcome, truncate-vs-throw, "let the harness's actual capture... settle which behaviour is real") are captured for DR-019's *legacy* behavior; because CQ-008 changes this behavior going forward, GM-016/GM-017 verify the migration/audit tooling's handling of legacy data, not the rebuilt `TotalCharge` computation itself, which must instead be verified against CQ-008's decision text (decimal type, no truncation).
**Gate:** CLEAR
**Verification:** VERIFIABLE — fixtures: GM-013 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-014 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-015 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-016 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-017 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93)
**UI fidelity:** THEME-ONLY — reproduce the token values of: TK-001, TK-002, TK-003; screens for reference only: SCR-006 (layout reinterpreted for the target platform)

---

### BL-011 — Manage Medical Condition Master List


**Maps to capability:** "14. **Manage the medical-condition master list** (Create/Edit/Delete `MedicalInfo`) — Name (text, capitalized) — `patient-info.tpl.html` (not opened this run), `PatientInfoControlller` → `api/MedicalInfo/{GetAll,Create,Update,Delete}`."
**Module:** MOD-002
**Depends on:** BL-002, BL-007
**Acceptance basis (domain-model.md):**
- DR-017: "Catalog names must be unique — `MedicalService.Name` and `MedicalInfo.Name` both carry `[Required][StringLength(50,MinimumLength=2)]` plus a unique index (`IX_Name`)."
- SCR-005 (Medical Condition Catalog): a one-row create/edit form (Name) above a catalog table with edit/delete icons (ui-inventory.md). Token groups TK-001, TK-002, TK-003 per THEME-ONLY policy.
**Verification inputs needed:**
- GM-015 (duplicate `MedicalInfo.Name` rejected at the service layer — VERIFIABLE) is captured and directly replayable for DR-017 as it applies to this entity.
**Gate:** CLEAR
**Verification:** VERIFIABLE — fixtures: GM-013 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-014 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-015 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93)
**UI fidelity:** THEME-ONLY — reproduce the token values of: TK-001, TK-002, TK-003; screens for reference only: SCR-005 (layout reinterpreted for the target platform)

---

### BL-012 — Tag/Untag Patient Medical Conditions


**Maps to capability:** "6. **Tag/untag a patient's medical conditions** — checkbox list bound to the catalog `MedicalInfo` list — `patient-detail.tpl.html`'s "Medical Condition" tab, `savePatientMedicalInfo()` → `POST api/MedicalInfo/SavePatientMedicalInfos`."
**Module:** MOD-002
**Depends on:** BL-002, BL-007, BL-011
**Acceptance basis (domain-model.md):**
- domain-model.md's PatientMedicalInfo entity: "join entity: `PatientId`, `MedicalInfoId` (both plain `Guid`, no `[ForeignKey]`/navigation declared, unlike the other join entities)." This item, owned by MOD-002 per module-map.md (which owns `PatientMedicalInfo` even though the screen itself lives on MOD-001's Patient Detail), is placed here because it is primarily about this entity's own save-logic defect (below), not about the Patient record it tags.
- **No numbered rule** — a sibling defect pattern to DR-018, found by `bf-baseline-designer`: `MedicalInfoService.cs`'s `SavePatientMedicalInfos` calls `.First().PatientId` before anything else, so submitting an empty list (the real, reachable result of unchecking every condition and clicking Save) throws `InvalidOperationException` and leaves the prior tagged-conditions list completely untouched. ⚠ This finding is `PROVISIONAL — pending PQ-006` (still OPEN in `pending-questions.md`, no `CQ-###` has been assigned to it yet, so no `PROVISIONAL:` directive is issued for it here per this run's own instructions) — PQ-006's proposed default is to treat it as a DEFECT, consistent with CQ-012's already-decided fix for the sibling DR-018 pattern, but that is not yet a human-confirmed decision.
- Per CQ-012's already-decided sibling fix for DR-018 ("redesign the API contract so `PrescriptionId`/equivalent identifying id is supplied once as part of the route/request and the body contains only the line items. The backend must reject any inconsistent identifiers instead of inferring the scope from the first submitted item") — this item's rebuilt API contract should supply `PatientId` explicitly rather than inferring it from `.First()`, by analogy, pending PQ-006's own resolution.
- SCR-004 (Patient Detail, Medical Condition tab): a checkbox list of every catalog condition beside a read-only list of the patient's currently-tagged conditions (ui-inventory.md). Token groups TK-001, TK-002, TK-003 per THEME-ONLY policy.
**Verification inputs needed:**
- GM-018 (empty medical-conditions list crashes instead of clearing prior tags) is marked `PROVISIONAL` in `manifest.json` pending **PQ-006** (OPEN, no CQ id yet) — this item's acceptance for the empty-list case cannot be finalized until a human answers PQ-006. GM-019 (deleting a Patient does not cascade-clean tagged conditions, orphaning `PatientMedicalInfo` rows — VERIFIABLE) is a separate, already-decided-context finding (module-map.md explicitly tags this scenario to both MOD-001 and MOD-002 since no `DR-###` exists to derive ownership from) that this item's acceptance should also account for when the rebuild decides whether to add a real FK/cascade.
**Gate:** CLEAR
**Verification:** VERIFIABLE — fixtures: GM-009 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-010 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-018 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93)
**UI fidelity:** THEME-ONLY — reproduce the token values of: TK-001, TK-002, TK-003; screens for reference only: SCR-004 (layout reinterpreted for the target platform)

## MOD-003 — Inventory & Products

_Depends on: MOD-005. Module dependency rank 1. 4 active item(s)._

### BL-013 — Manage Product Catalog


**Maps to capability:** "15. **Manage the product catalog** (Create/Update/Delete `Product`) — Code (text, uppercased), Name (text, capitalized), Starting Inventory (text), Minimum Required (text), Unit Price (text), Sale Price (text) — `Client/app/views/product/product.tpl.html`, `ProductController.js` → `api/Products/{Create,Update,Delete}`. A new product's `Received` and `OnHand` are both seeded from `StartingInventory` client-side before the POST."
**Module:** MOD-003
**Depends on:** BL-002, BL-007
**Acceptance basis (domain-model.md):**
- domain-model.md's Product entity: "`Code`, `Name` (required, unique, 1–40 chars), `StartingInventory`, `Received`, `Shipped`, `OnHand`, `MinimumRequired`, `UnitPrice`, `SalePrice`... `StatusId` (FK → Status)... derived from `OnHand`'s sign by client code..., not enforced server-side."
- Per CQ-006 ("replace the shared Status lookup with separate typed status concepts for Prescription/Bill, Product, Inventory Movement, and Appointment. Preserve the existing semantic values during migration but enforce valid status values per entity in the .NET domain/service layer") — `Product.StatusId`'s In-Stock/Out-Of-Stock values become a typed per-entity status in the rebuild, not the shared `Status` lookup table.
- SCR-010 (Product Catalog): a create/edit form panel beside a search+table panel with edit/delete icons (ui-inventory.md). Token groups TK-001, TK-002, TK-003 per THEME-ONLY policy.
**Verification inputs needed:**
- GM-024 (deleting a Product cascades to delete its Inventory rows — VERIFIABLE) is the only captured fixture directly touching this entity's own lifecycle; the Code-uniqueness/field-validation rules this capability describes (`[Required][StringLength]`) have no dedicated `DR-###`/`GM-###` beyond what GM-024 exercises incidentally — a human should confirm the rebuild's field-level validation (Name uniqueness, 1–40 chars) matches `DM.Models/Product.cs`'s declared attributes, since no fixture pins this specifically.
**Gate:** CLEAR
**Verification:** NO BASELINE DATA — baseline has been run, but no scenario in scenarios.md cites this item's rules
**UI fidelity:** THEME-ONLY — reproduce the token values of: TK-001, TK-002, TK-003; screens for reference only: SCR-010 (layout reinterpreted for the target platform)

---

### BL-014 — Search & Browse Products (Product List + Dashboard)


**Maps to capability:** "16. **Search/browse products** — by Code/Name text key or In-Stock/Out-Of-Stock filter — `product.tpl.html` and `dashboard.tpl.html`, → `api/Products/{SearchProduct,Search,Filter,GetProductsIncludeStatus}`."; "36. **Dashboard** — product search/filter (mirrors capability 16) plus quick-navigation buttons to Inventory, Product, Report, and Patient screens — `dashboard.tpl.html`, `DashboardController.js`."
**Merge rationale:** functional-spec.md itself states capability 36 "mirrors capability 16" — both are the same product search/filter behavior surfaced on two screens, with the Dashboard's own acceptance criterion being only the additional quick-navigation buttons.
**Module:** MOD-003
**Depends on:** BL-013
**Acceptance basis (domain-model.md):**
- domain-model.md's Product entity (searched/filtered fields: Code, Name, StatusId) — no numbered `DR-###` governs search/filter itself; this capability's acceptance rests on the field set functional-spec.md documents, not a business rule.
- functional-spec.md's Named Gap #4: "`DashboardController.js`'s `$scope.detail` function is confirmed dead code... unreachable from the UI as currently wired." This dead code is explicitly excluded from this item's acceptance basis — it must not be reproduced in the rebuild.
- SCR-010 (Product Catalog) and SCR-009 (Dashboard): both carry a search box and a Code/Name/Status filter above a product grid/table (ui-inventory.md). Token groups TK-001, TK-002, TK-003 apply to both screens; TK-004 (dashboard-onhand-grid-cell, `#5cb85c` per design-tokens.json) additionally applies to SCR-009's On-Hand column specifically.
**Verification inputs needed:**
- No `DR-###`/`GM-###` targets search/filter directly (`manifest.json`'s fixtures cover DR-008/DR-009/DR-020 on this module, none of which is the search/filter endpoint family) — acceptance rests on functional-spec.md's own field/endpoint documentation, with a human confirming the rebuilt search behaves equivalently since no golden-master fixture exists to replay.
**Gate:** CLEAR
**Verification:** VERIFIABLE — fixtures: GM-020 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-021 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-022 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-023 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93)
**UI fidelity:** THEME-ONLY — reproduce the token values of: TK-001, TK-002, TK-003, TK-004; screens for reference only: SCR-010, SCR-009 (layout reinterpreted for the target platform)

---

### BL-015 — Record & Review Stock Movements


**Maps to capability:** "17. **Record a stock movement (receive or ship)** — Date (`type="datetime"`), Product (select dropdown, options from `GetProductsName`), Cash Memo No (text, uppercased), On Hand (disabled/read-only text), Received/Shipped Quantity (number), Stock Type (radio pair: Received=3 / Shipped=4) — `Client/app/views/stock/stock.tpl.html`. Triggers a two-call backend sequence — see workflow **"Record Stock Movement & Update Product Levels."**"; "18. **View stock-movement history for a product** — date-range... and a preset days filter..., with running Received/Shipped totals and a print modal — `stock.tpl.html`, `getProductInventoryHistory()` → `GET api/Inventories/GetProductHistory`."
**Merge rationale:** Both bullets render on the single `root.stock` screen (SCR-011), share the same `StockController`, and the history table is the direct downstream read-view of the movements this item's own create action writes.
**Module:** MOD-003
**Depends on:** BL-013, BL-002, BL-007
**Acceptance basis (domain-model.md):**
- DR-008: "A "Shipped" stock movement cannot exceed the product's current on-hand quantity — enforced only in `stock.controller.js`'s `save()`... `InventoryController.Post` performs no equivalent server-side check."
- DR-009: "Recording a stock movement updates the product's running totals — see Workflows, "Record Stock Movement & Update Product Levels."" The workflow itself documents the two-call sequence (`POST api/Inventories/Create` then `PUT api/Products/Update`) and that the second call is entirely client-driven with no server-side recomputation.
- Per CQ-011 ("enforce all four rules on the ASP.NET Core backend as authoritative business rules and mirror them in React for immediate UX feedback. Direct API calls must never be able to bypass these validations") — DR-008's shipment-exceeds-on-hand guard gains authoritative server-side enforcement in the rebuild, closing the exact gap functional-spec.md's Named Gap #2 documents.
- Per CQ-006 (typed per-entity status) — `Product.StatusId`/`Inventory.StatusId`'s In-Stock/Out-Of-Stock and Received/Shipped values become typed statuses, not the shared `Status` lookup.
- Per CQ-020 ("the rebuild will have no default stock movement type. The user must explicitly choose Received or Shipped before submission, and the ASP.NET Core backend must reject requests without a valid movement type") — the legacy Stock Type radio pair's unresolved default-checked state (ui-inventory.md Named Gap #8: the template's static `checked="checked"` on "Shipped" disagrees with the controller's own `init()` setting `stock.StatusId = 0`, matching neither radio value) is closed by requiring an explicit user choice, with server-side rejection of an unset/invalid movement type.
- **This item covers a client-orchestrated two-call workflow (`POST .../Create` then `PUT .../Update`) that no single `DR-###` enforces as one atomic unit** — DR-009 documents the intended effect, but functional-spec.md's own workflow text warns: "If the second call (`updateProduct`) is omitted, the `Inventory` movement row is persisted but the parent `Product.OnHand`/`Received`/`Shipped`/`StatusId` never change." The rebuild's acceptance criterion for this specific risk is that the sequence becomes a single authoritative server-side transaction, not two independently-failable client calls — this is new server-side design work per CQ-011, not something any legacy fixture can attest to.
- SCR-011 (Stock / Inventory Movement): a movement-entry form panel beside a history-table panel with date-range/preset filters and a print modal (ui-inventory.md). Token groups TK-001, TK-002, TK-003 per THEME-ONLY policy.
**Verification inputs needed:**
- GM-020 (a shipment exceeding on-hand is accepted server-side today — VERIFIABLE) and GM-021 (Product totals persisted exactly as sent, no server-side recomputation — VERIFIABLE) capture DR-008/DR-009's *legacy* (defective) behavior; because CQ-011/this item's own design change these behaviors going forward, GM-020/GM-021 verify what the rebuild must **stop** doing, not what it should replicate — a human must additionally verify the new atomic server-side transaction against a live test, since no fixture captures the rebuild's own target behavior (it did not exist in the legacy app to capture).
**Gate:** CLEAR
**Verification:** VERIFIABLE — fixtures: GM-020 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-021 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93)
**UI fidelity:** THEME-ONLY — reproduce the token values of: TK-001, TK-002, TK-003; screens for reference only: SCR-011 (layout reinterpreted for the target platform)

---

### BL-016 — Inventory On-Hand Report


**Maps to capability:** "19. **View the inventory report** — per-product Received/Shipped/On-Hand across a date range, with a Product filter (select) and Status filter (select: All/In Stock/Out Of Stock) — `stock-report.tpl.html` (not opened this run), `StockReportController` → `GET api/InventoryReports/GetReport`, with its own print modal."
**Module:** MOD-003
**Depends on:** BL-015
**Acceptance basis (domain-model.md):**
- DR-020: "mechanical, reason not evident — `InventoryReportController.cs`'s private `GetOnHand` helper, used when a product has zero movements inside the requested report window, looks first at the movement nearest one month before the window start, then the movement nearest one month after the window end, and only falls back to the product's live `OnHand` if neither exists."
- Per CQ-010 ("remove the arbitrary one-month cutoff and use the nearest relevant inventory movement needed to determine the historical on-hand value. The calculation must be deterministic and covered by tests") — DR-020's fixed one-month lookback/lookahead is replaced with a deterministic nearest-movement lookup in the rebuild.
- Per CQ-021 ("restore Product and Status filters in the rebuilt Stock Report. Implement filtering through explicit React/Material UI controls backed by server-side query parameters") — the Product/Status filters this capability bullet documents, which ui-inventory.md confirms are commented out in the legacy template despite the controller still maintaining their option data (Named Gap #9), are restored as real, working filters in the rebuild.
- SCR-012 (Stock Report): a filter row (From/To date, Search, Print) above a Received/Shipped/On-Hand report table (ui-inventory.md). Token groups TK-001, TK-002, TK-003 per THEME-ONLY policy.
**Verification inputs needed:**
- GM-022 (zero movements in window, fallback to the "next month" movement — VERIFIABLE) and GM-023 (zero movements ever, fallback to live OnHand — VERIFIABLE) capture DR-020's legacy behavior, including scenarios.md's own note that the "previous month" branch is provably dead code given its only caller's precondition (filed under "No Legacy Behaviour Exists," not a new pending question, since CQ-010 already covers its removal). Because CQ-010 replaces this logic entirely, GM-022/GM-023 verify what the legacy app did, not what the rebuild's deterministic nearest-movement lookup must do — a human must define and verify new test cases for the replacement algorithm, since it has no legacy precedent to fixture against.
**Gate:** CLEAR
**Verification:** VERIFIABLE — fixtures: GM-022 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-023 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93)
**UI fidelity:** THEME-ONLY — reproduce the token values of: TK-001, TK-002, TK-003; screens for reference only: SCR-012 (layout reinterpreted for the target platform)

## MOD-004 — Appointments & Doctors

_Depends on: MOD-005. Module dependency rank 1. 3 active item(s)._

### BL-017 — Doctor Management (CRUD)


**Maps to capability:** "24. **View the doctor list** — read-only; no create/update/delete UI or endpoint exists (`DoctorController` exposes only `GetAll`/`GetById`)."
**Module:** MOD-004
**Depends on:** BL-002, BL-007
**Acceptance basis (domain-model.md):**
- domain-model.md's Doctor entity: "`Code`, `Name`, `Phone`, `Created`, `LastUpdate`, `Appointments` (collection). Inference:... No create/update/delete endpoint exists on `DoctorController`... the seed data... inserts exactly **one** doctor... confirming this system was built/seeded for a **single-doctor** clinic despite the data model supporting many doctors."
- Per CQ-014 ("implement proper multi-doctor support. Add Doctor CRUD/management and require appointment creation/editing to select a doctor instead of using a hardcoded GUID") — this item is the admin-side half of CQ-014's decision: a genuinely new Doctor CRUD screen/endpoint set that has **no one-to-one legacy equivalent** (a `TARGET-GAP`, per functional-spec.md Named Gap #1's own framing of the question CQ-014 answers).
- **No legacy screen exists for this capability** — ui-inventory.md's 20 captured screens include no Doctor-management screen; `DoctorController` today exposes only `GetAll`/`GetById`. This item's acceptance basis is therefore CQ-014's decision text alone, not any existing UI/business-rule quote.
**Verification inputs needed:**
- No golden-master fixture exists or is expected for Doctor CRUD — it is new functionality with no legacy behavior to capture. Acceptance is verified against CQ-014's decision text and ordinary new-feature testing, not fixture replay.
**Gate:** OPEN QUESTIONS — UI fidelity: SQ-013 decided THEME-ONLY, no SCR-### cited by this item
**Verification:** NO BASELINE DATA — baseline has been run, but no scenario in scenarios.md cites this item's rules
**UI fidelity:** ⚠ UI GROUNDING MISSING — THEME-ONLY decided (SQ-013) and this item renders a screen, but its acceptance basis cites no SCR-### entry from ui-inventory.md

---

### BL-018 — Schedule Appointments with Doctor Selection


**Maps to capability:** "20. **Schedule/manage appointments** — Patient Name/Id (free text, capitalized), Age (number), Phone (text), Date (`uib-datepicker-popup` date-picker with min/max bounds and a calendar-icon trigger button), Time (`uib-timepicker` hour/minute stepper with AM/PM toggle) — `Client/app/views/patient/patient-appointment.tpl.html`, `PatientAppointmentController.save()` → `POST api/Appointments/Create`. The Doctor for the appointment is **not** an exposed form field — see Named Gaps."
**Module:** MOD-004
**Depends on:** BL-017
**Acceptance basis (domain-model.md):**
- domain-model.md's Appointment entity: "`DoctorId` (FK → Doctor)... `PatientNameOrId` (free-text string, required, 2–40 chars — **not** a `Patient` FK)... Inference: a scheduled visit slot, deliberately decoupled from the `Patient` entity."
- functional-spec.md Named Gap #1: "Doctor selection is not exposed in the Appointment UI. `Appointment.DoctorId` is a required FK, but `patient-appointment.controller.js`'s `init()` hardcodes `DoctorId: "9b6ba3ad-c9be-e511-9bf4-402cf40f4b2f"`... There is no doctor picker anywhere in the appointment form."
- Per CQ-014 ("require appointment creation/editing to select a doctor instead of using a hardcoded GUID") — this item is the appointment-side half of CQ-014's decision: a real doctor picker sourced from BL-017's Doctor CRUD, replacing the hardcoded GUID.
- **Legacy defect confirmed by running the harness this session (not yet in any analysis document):** `Configuration.Seed`'s `AddDoctor` assigns the literal GUID `9b6ba3ad-c9be-e511-9bf4-402cf40f4b2f` (the same value `patient-appointment.controller.js:10` hardcodes), but `BaseModel.Id` is `[DatabaseGenerated(DatabaseGeneratedOption.Identity)]`, so EF discards the assigned GUID and SQL Server generates a different one at seed time — meaning on any freshly migrated database, booking an appointment from the legacy UI fails the `FK_dbo.Appointment_dbo.Doctor_DoctorId` constraint outright, since the client's hardcoded GUID never actually matches the seeded Doctor row's real id. The rebuild's own doctor-picker (this item) and Doctor seed (BL-017/BL-001) must not reproduce this specific defect — a real picker sourced from a live Doctor list, not a hardcoded id, structurally avoids it.
- SCR-008 (Appointments): a create/edit form (Patient Name, Age, Phone, Date, Time) above a filter row and appointments table (ui-inventory.md); this item adds a Doctor field to that form, which has no legacy screenshot to ground since it never existed in the legacy UI. Token groups TK-001, TK-002, TK-003 per THEME-ONLY policy for the rest of the screen's existing chrome.
**Verification inputs needed:**
- GM-025 (appointment-by-date lookup excludes Visited appointments — VERIFIABLE, DR-010) exercises this module's list query but not doctor selection itself. No fixture exists for the doctor-picker addition (new functionality) or for the harness-found seed/FK-mismatch defect above (found by attempting a live migration+seed this session, not by a captured scenario) — a human must verify both by running the rebuild's own fresh migration+seed and confirming a booked appointment's `DoctorId` actually resolves to a real, persisted Doctor row.
**Gate:** CLEAR
**Verification:** VERIFIABLE — fixtures: GM-025 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93)
**UI fidelity:** THEME-ONLY — reproduce the token values of: TK-001, TK-002, TK-003; screens for reference only: SCR-008 (layout reinterpreted for the target platform)

---

### BL-019 — Appointment Status, Listing & Print Actions


**Maps to capability:** "21. **Mark an appointment as visited** — confirm dialog then `PUT api/Appointments/Update` setting `StatusId = 8`."; "22. **Search/filter appointments** — by date (defaults to today) or a free-text key against Code/PatientNameOrId."; "23. **Print an appointment copy** — modal (`patientAppointmentModal.html` inline template) + `window.print()`."
**Merge rationale:** All three are small, tightly-coupled actions on the single Appointments screen (SCR-008), each a one-button/one-modal affordance with no independent acceptance criterion beyond the shared appointment list/detail state BL-018 establishes.
**Module:** MOD-004
**Depends on:** BL-018
**Acceptance basis (domain-model.md):**
- DR-010: "mechanical, reason not evident — `AppointmentRepository.GetByDate` filters to `x.StatusId == 7` ("Appointed") only, excluding "Visited" (8) appointments from the by-date list; no comment explains why visited appointments are hidden from this particular query."
- Per CQ-009 ("show both Appointed and Visited appointments in the date-based schedule so staff can see the full day's activity. Visited appointments should remain visually distinguishable by status") — DR-010's exclusion of Visited appointments from the by-date list is a defect the rebuild fixes, not preserves; both statuses are shown, visually distinguished.
- Per SQ-009 ("keep all existing report and receipt capabilities, but implement them as modern print-friendly React/MUI views... Exact legacy `window.print()` implementation is not required") — the print-appointment-copy capability is rebuilt as a modern print-friendly view, not a pixel-identical reproduction of the inline modal template.
- SCR-008 (Appointments): the same create/edit-form-plus-table screen BL-018 covers; this item's actions (mark-visited icon, search/date filter, print icon) are all rows/controls on that same table (ui-inventory.md). Token groups TK-001, TK-002, TK-003 per THEME-ONLY policy.
**Verification inputs needed:**
- GM-025 (appointment-by-date lookup excludes Visited appointments — VERIFIABLE) captures DR-010's *legacy* (to-be-fixed) behavior; because CQ-009 changes it, GM-025 verifies what the rebuild must stop doing, not its target state — a human must define and verify a new fixture (both statuses shown, visually distinguished) since the fixed behavior has no legacy precedent to capture.
**Gate:** CLEAR
**Verification:** VERIFIABLE — fixtures: GM-025 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93)
**UI fidelity:** THEME-ONLY — reproduce the token values of: TK-001, TK-002, TK-003; screens for reference only: SCR-008 (layout reinterpreted for the target platform)

## MOD-001 — Patient & Billing

_Depends on: MOD-002. Module dependency rank 2. 10 active item(s)._

### BL-020 — Register New Patient & Auto-Provision Bill


**Maps to capability:** "3. **Register a new patient** — form fields: Name (text, capitalized on submit), Age (text), Gender (select dropdown, hardcoded options `Male/Female/Others`), Phone (text), Email (text), Address (textarea), Note (textarea) — `Client/app/views/patient/patient-create.tpl.html`, submitted via `PatientCreateController.addPatient()` → `POST api/PatientCreate/Create`. This single action also auto-opens the patient's first bill — see workflow **"New Patient Registration & Bill Auto-Provisioning."**"
**Module:** MOD-001
**Depends on:** BL-010, BL-002, BL-007
**Acceptance basis (domain-model.md):**
- DR-001: "Patient Code is auto-generated, unique, human-facing — `Code` is computed server-side... never client-supplied... and enforced unique via... `HasIndex(p => p.Code).IsUnique()` plus `[StringLength(8,MinimumLength=7)]`."
- DR-002: "New patient is auto-provisioned with an initial bill — `PatientCreateController.Post` creates the `Patient`, then, if no `Prescription` yet exists for that `PatientId`, creates one with `StatusId = 5` ("Active"). Not wrapped in a database transaction — if the second write fails, the patient exists with zero bills."
- DR-003: "Bill code format — a Prescription's `Code` is generated as `"BILL" + zero-padded sequence + "-" + PatientCode`."
- **This item covers a client-orchestrated, two-write single-request workflow** ("New Patient Registration & Bill Auto-Provisioning" in functional-spec.md) that DR-002 documents but does not itself enforce as atomic: "Both writes happen inside one HTTP call with **no database transaction** wrapping them... If the second insert fails after the first succeeds, the patient exists with zero bills." Per SQ-002/SQ-010's general production-safeguard direction, this item's acceptance criterion is that the rebuild wraps both writes in one real database transaction, closing this specific gap — new server-side design work, not something a legacy fixture can attest to since the legacy app never had a transaction to verify.
- Per CQ-007 ("formalize Gender as a typed enum/lookup in the rebuild and migrate the existing Male, Female, and Others values explicitly. Before migration, report any legacy values outside the known set so they can be reviewed rather than silently discarded") — the Gender field, plain `string` in legacy despite an unused `Gender` enum in the same file (domain-model.md Enumerations item 1), becomes a real typed enum in the rebuild, with the migration tooling (BL-001) auditing legacy values.
- ⚠ **PQ-005 (OPEN, no CQ id assigned yet):** "Does `PatientCreateController.Post`'s silent ignoring of `Add()`'s failure result (returning 200 OK with `patient.Id` regardless of whether the insert actually succeeded) need a defect fix in the rebuild?" — GM-002's fixture (below) is marked `PROVISIONAL` in `manifest.json` pending this question; per this run's own instructions, no `PROVISIONAL:` directive is issued since no `CQ-###` exists yet to cite.
- SCR-003 (Register Patient / Add Services to Bill, "new-patient" state): a centered panel form (Name, Age, Gender, Phone, Email, Address, Note, Save) (ui-inventory.md). Token groups TK-001, TK-002, TK-003 per THEME-ONLY policy.
**Verification inputs needed:**
- GM-001 (patient-code zero-pad boundary — VERIFIABLE), GM-003 (new patient auto-provisioned with an Active bill — VERIFIABLE), GM-004 (bill-code zero-pad boundary — VERIFIABLE) are captured for DR-001/DR-002/DR-003. GM-002 (patient creation silently "succeeds" over a Code collision — marked `PROVISIONAL` pending PQ-005) cannot be finalized until PQ-005 is answered. GM-011 (the two independently-coexisting "current bill" mechanisms disagree once a patient has no Active prescription — marked `PROVISIONAL` in `manifest.json` pending **PQ-008**, OPEN, no CQ id yet) is directly relevant to this item's auto-provisioning guarantee (it is the concrete failure mode DR-002's own un-transacted workflow can produce) — this item's acceptance for the "always has ≥1 Active bill" invariant cannot be finalized until PQ-008 is answered, and is cross-referenced again at BL-027 (Close Bill / Open New Bill), which shares the same underlying mechanism.
**Gate:** CLEAR
**Verification:** VERIFIABLE — fixtures: GM-001 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-002 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-003 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-004 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-011 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93)
**UI fidelity:** THEME-ONLY — reproduce the token values of: TK-001, TK-002, TK-003; screens for reference only: SCR-003 (layout reinterpreted for the target platform)

---

### BL-022 — Add Treatment Services to Bill


**Maps to capability:** "4. **Add treatment services to a patient's current bill** — checkbox list of catalog `MedicalService` rows (one checkbox per service), a numeric Quantity input per selected row, plus text inputs for Discount % and Fixed Discount — same `patient-create.tpl.html`, "Add Services" panel. This action triggers a two-call backend sequence — see workflow **"Add Treatment Services to Bill."**"
**Module:** MOD-001
**Depends on:** BL-020, BL-010
**Acceptance basis (domain-model.md):**
- DR-018: "Saving a bill's service list replaces rather than merges — `PatientMedicalServiceRepository.cs`'s `AddList` deletes every existing `PatientMedicalService` row for the **first** submitted item's `PrescriptionId`... This is correct only because its one caller... always submits a list scoped to a single `PrescriptionId` — nothing in the type system enforces that assumption."
- DR-004: "Discount percent must be 0–100 — enforced only in `patient-create.controller.js`'s `calculateDiscount()`... `Prescription.cs`'s `DiscountPercent` carries no server-side `[Range]` annotation."
- Per CQ-012 ("redesign the API contract so PrescriptionId is supplied once as part of the route/request and the body contains only the service line items. The backend must reject any inconsistent identifiers instead of inferring the scope from the first submitted item") — DR-018's first-item-scoped replace pattern is redesigned, not preserved, per functional-spec.md Named Gap #3's own framing of the risk.
- Per CQ-011 ("enforce all four rules on the ASP.NET Core backend as authoritative business rules... Direct API calls must never be able to bypass these validations") — DR-004's 0–100 discount-percent range gains authoritative server-side enforcement.
- **This item covers a client-orchestrated two-call workflow** ("Add Treatment Services to Bill" in functional-spec.md: `POST .../CreateList` then `PUT .../Update` pushing client-computed totals) **that no single `DR-###` enforces as one atomic unit** — "If step 2 is omitted (or fails silently), the bill's stored totals... never reflect the services just added." This item's acceptance criterion is that the rebuild computes and persists totals server-side as part of one authoritative operation, per CQ-011's general direction, not as two independently-failable client calls.
- SCR-003 (Register Patient / Add Services to Bill, "add-services" state): a full-width panel with a services checklist table, discount inputs, running totals, and Save (ui-inventory.md). Token groups TK-001, TK-002, TK-003 per THEME-ONLY policy.
**Verification inputs needed:**
- GM-009 (replace scoped only to the first submitted item's PrescriptionId — VERIFIABLE), GM-010 (empty list is a silent no-op, not a clear — VERIFIABLE) capture DR-018's legacy behavior, which CQ-012 redesigns; these verify what the rebuild's new API contract must **not** reproduce (inferring scope from `.First()`), not the new contract's own target shape, which needs fresh human-defined test cases. GM-005 (discount percent outside 0–100 accepted server-side — VERIFIABLE) captures DR-004's legacy gap that CQ-011 closes; same caveat applies. **The two-call workflow's own atomicity has no legacy fixture at all** — both GM-009/GM-010 and GM-005 exercise individual backend steps in isolation, not whether the client's second call (`updatePrescription`) actually ran; this is exactly the omission rubric step 4(c) and the Fidelity Discipline note warn about, and a human must verify the rebuilt end-to-end flow directly.
**Gate:** CLEAR
**Verification:** VERIFIABLE — fixtures: GM-005 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-009 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-010 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-018 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93)
**UI fidelity:** THEME-ONLY — reproduce the token values of: TK-001, TK-002, TK-003; screens for reference only: SCR-003 (layout reinterpreted for the target platform)

---

### BL-021 — View & Search Patient List


**Maps to capability:** "1. **View patient list** — grid of all patients with Code/Name/Phone/Age/Gender/LastVisitingDate/Payable/Paid/Due columns (`Client/app/scripts/patient/patient.controller.js`'s `loadPatientGridData()` → `GET api/Patients/GetGridList`; screen `root.patient` / `patient.tpl.html`)."; "2. **Search/filter patients** — by Code/Name/Phone text key, combined with a Due/Payment-Complete/All filter (`patient.controller.js`'s `search()` → `GET api/Patients/Search`)."
**Merge rationale:** Both bullets share the same grid, the same per-patient view-model construction, and the same underlying N+1/`.Last()` defect this item's acceptance basis centers on — they are not independently testable slices.
**Module:** MOD-001
**Depends on:** BL-020
**Acceptance basis (domain-model.md):**
- Per CQ-001 ("treat it as a defect. The rebuild must safely handle patients with no prescription and avoid N+1 database queries by loading/projecting the required prescription information efficiently through EF Core") — architecture.md's L4 finding that `PatientController.Get()`/`Search()` both perform an N+1 per-patient prescription lookup via `.Last()`, which throws `InvalidOperationException` for a patient with zero prescriptions, is a defect the rebuild fixes with a batched/projected query, not preserved.
- domain-model.md's Patient entity fields (Code, Name, Age, Phone, Gender, plus Prescription's TotalPayable/TotalPaid/TotalDue) are the grid's own column set per functional-spec.md capability 1.
- SCR-002 (Patient List): a toolbar (search box, Due/Payment-Complete/All filter, navigation buttons) above a data grid (Code/Name/Phone/Age/Gender/LastVisitingDate/Payable/Paid/Due) (ui-inventory.md). Token groups TK-001, TK-002, TK-003 per THEME-ONLY policy.
**Verification inputs needed:**
- No `DR-###`/`GM-###` fixture directly targets `PatientController.Get()`/`Search()`'s own N+1/`.Last()` defect (it is architecture.md's own L4 finding, not a numbered domain-model business rule) — acceptance rests on CQ-001's decision text; a human must verify the rebuilt query performs a single batched/projected read and does not throw for a zero-prescription patient, since no golden-master fixture captures this specific endpoint's crash behavior. GM-011 (cross-referenced from BL-020/BL-027) is also relevant here since `PatientController.Get()` is one of the two "current bill" mechanisms GM-011 documents as disagreeing.
**Gate:** CLEAR
**Verification:** NO BASELINE DATA — baseline has been run, but no scenario in scenarios.md cites this item's rules
**UI fidelity:** THEME-ONLY — reproduce the token values of: TK-001, TK-002, TK-003; screens for reference only: SCR-002 (layout reinterpreted for the target platform)

---

### BL-023 — View/Edit Patient Personal Info


**Maps to capability:** "5. **View/edit a patient's personal info** — Patient Id (disabled text), Name (text), Age (text), Gender (select), Phone (text), Email (text), Address (textarea), Note (textarea) — `patient-detail.tpl.html`'s "Patient" tab, `PatientDetailControlller.update()` → `PUT api/PatientCreate/Update`."
**Module:** MOD-001
**Depends on:** BL-020
**Acceptance basis (domain-model.md):**
- domain-model.md's Patient entity: "`Code`..., `Name` (required, 3–30 chars), `Age` (required int), `Phone`, `Email`, `Address`, `Gender` (plain `string`...), `Note`."
- Per CQ-007 (Gender becomes a typed enum, migrated explicitly) — this tab's Gender field is rebuilt against the new typed enum, consistent with BL-020's registration-side decision.
- SCR-004 (Patient Detail, Patient tab): a two-column edit form (Id disabled, Name, Age, Gender, Phone, Email, Address, Note, Update) (ui-inventory.md). Token groups TK-001, TK-002, TK-003 per THEME-ONLY policy.
**Verification inputs needed:**
- No dedicated `DR-###`/`GM-###` targets `PatientDetailControlller.update()`'s own field-level validation beyond the entity attributes quoted above — a human should confirm the rebuilt edit form enforces the same field constraints (Name 3–30 chars, required Age), since no fixture pins this specific endpoint.
**Gate:** CLEAR
**Verification:** NO BASELINE DATA — baseline has been run, but no scenario in scenarios.md cites this item's rules
**UI fidelity:** THEME-ONLY — reproduce the token values of: TK-001, TK-002, TK-003; screens for reference only: SCR-004 (layout reinterpreted for the target platform)

---

### BL-024 — Record Payment Against Bill


**Maps to capability:** "7. **Record a payment against the patient's current bill** — Date (`type="datetime"` text input), Amount (number input), Comment/"Paid for Service Taken" (single-row textarea) — `patient-detail.tpl.html`'s "Payment" tab. Triggers a two-call sequence — see workflow **"Record Payment Against Bill."**"
**Module:** MOD-001
**Depends on:** BL-022
**Acceptance basis (domain-model.md):**
- DR-005: "A payment cannot exceed the bill's current due amount — enforced only in `patient-detail.controller.js`'s `savePayment()`... `PaymentController.Post` performs no equivalent server-side check."
- Per CQ-011 ("enforce all four rules on the ASP.NET Core backend as authoritative business rules... Direct API calls must never be able to bypass these validations") — DR-005's overpayment guard gains authoritative server-side enforcement.
- **This item covers a client-orchestrated two-call workflow** ("Record Payment Against Bill" in functional-spec.md: client-side overpayment guard, then `POST .../Create`, then client recomputes totals and issues `PUT .../Update`) — "If the second call... is skipped, the `Payment` row is persisted but the `Prescription.TotalPaid`/`TotalDue` snapshot silently diverges... nothing recomputes those fields from the `Payments` collection server-side." This item's acceptance criterion is that the rebuild recomputes and persists `TotalPaid`/`TotalDue` server-side as part of one authoritative payment-recording operation, per CQ-011's general direction.
- SCR-004 (Patient Detail, Payment tab): a payment-entry form (Date, Amount, Comment, Add/Update) above a payment-history table and running totals (ui-inventory.md). Token groups TK-001, TK-002, TK-003 per THEME-ONLY policy.
**Verification inputs needed:**
- GM-006 (a payment exceeding due is accepted server-side, and totals are not recomputed server-side — VERIFIABLE) captures DR-005's legacy gap that CQ-011 closes; it exercises the individual backend step (`PaymentController.Post`) but **not** whether the client's follow-up recomputation call ran — a human must verify the rebuilt server-side recomputation directly, since no fixture exercises the end-to-end client sequence.
**Gate:** CLEAR
**Verification:** VERIFIABLE — fixtures: GM-006 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93)
**UI fidelity:** THEME-ONLY — reproduce the token values of: TK-001, TK-002, TK-003; screens for reference only: SCR-004 (layout reinterpreted for the target platform)

---

### BL-027 — Close Bill / Open New Bill


**Maps to capability:** "10. **Close the current bill / open a new one** — "New Bill" and "Force New Bill" buttons — see workflow **"Close Bill / Open New Bill."**"
**Module:** MOD-001
**Depends on:** BL-024
**Acceptance basis (domain-model.md):**
- DR-006: "A bill cannot be closed to open a new one while due > 0, unless forced — `patient-detail.controller.js`'s `newBill()` blocks (toast) when `patientPrescription.TotalDue > 0`; `forceNewBill()` is an explicit escape hatch... Client-side only."
- DR-007: "Closing a bill immediately opens a new one — see Workflows, "Close Bill / Open New Bill"."
- Per CQ-011 ("enforce all four rules on the ASP.NET Core backend as authoritative business rules... Direct API calls must never be able to bypass these validations") — DR-006's due-balance guard (and the "Force" override remaining a deliberate, distinct escape hatch) gains authoritative server-side enforcement.
- **This item covers a client-orchestrated two-call workflow** ("Close Bill / Open New Bill": `PUT .../Update` closing the old bill, then `POST .../Create` opening a zeroed new one) — "If the second call is omitted, the patient is left with **no active bill at all**." This is the exact mechanism that produces the divergence GM-011 documents. This item's acceptance criterion is that both writes happen as one authoritative server-side transaction (mirroring BL-020's own transactional requirement for the original bill-provisioning write).
- ⚠ **PQ-008 (OPEN, no CQ id assigned yet, cross-referenced from BL-020/BL-021):** "Which of the two independently-coexisting "patient's current bill" resolution mechanisms is authoritative when they can disagree?" — this item's own close/reopen workflow is the documented cause of that disagreement (DR-002's/DR-007's un-transacted two-write pattern); this item's acceptance for "which lookup wins" cannot be finalized until PQ-008 is answered.
- SCR-004 (Patient Detail, Payment tab): the New Bill / Force New Bill buttons on the same tab BL-024 covers (ui-inventory.md). Token groups TK-001, TK-002, TK-003 per THEME-ONLY policy.
**Verification inputs needed:**
- GM-007 (a bill can be closed and reopened while due > 0, with no "Force" distinction server-side — VERIFIABLE) and GM-008 (happy-path close+reopen — VERIFIABLE) capture DR-006/DR-007's legacy behavior. GM-011 (the two "current bill" mechanisms disagree — `PROVISIONAL` pending PQ-008) is the single most important fixture for this item's own atomicity fix and cannot be finalized until PQ-008 is answered; this item's server-side transaction design should be validated against GM-011's own documented disagreement once the underlying question is resolved.
**Gate:** CLEAR
**Verification:** VERIFIABLE — fixtures: GM-003 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-007 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-008 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93), GM-011 (5ff87d3a0adf7cbab09099bf65d4afc909b78b93)
**UI fidelity:** THEME-ONLY — reproduce the token values of: TK-001, TK-002, TK-003; screens for reference only: SCR-004 (layout reinterpreted for the target platform)

---

### BL-025 — Delete Payment & Reverse Bill Totals


**Maps to capability:** "8. **Delete a payment** — confirm dialog (`confirm(...)`) then removal; also reverses the bill's running totals — see workflow **"Delete Payment & Reverse Bill Totals."**"
**Module:** MOD-001
**Depends on:** BL-024
**Acceptance basis (domain-model.md):**
- domain-model.md's Payment entity: "`PrescriptionId` (FK), `Amount` (double), `Comment`, ..., navigation to `Prescription`."
- **This item covers a client-orchestrated two-call workflow** ("Delete Payment & Reverse Bill Totals": `DELETE .../Delete` then client-computed reversal via `PUT .../Update`) with the same "totals silently diverge if the second call is skipped" risk functional-spec.md explicitly calls out as shared with the payment-creation workflow (BL-024). No numbered `DR-###` governs this specific reversal; its acceptance criterion, by the same CQ-011-style reasoning applied to BL-024, is that the rebuild performs the delete-and-reverse as one authoritative server-side operation.
- SCR-004 (Patient Detail, Payment tab): the delete-payment icon action on the same payment-history table BL-024 covers (ui-inventory.md). Token groups TK-001, TK-002, TK-003 per THEME-ONLY policy.
**Verification inputs needed:**
- GM-012 (deleting a Patient cascades to delete every Prescription/PatientMedicalService/Payment row — VERIFIABLE) confirms the cascade chain a Payment row participates in, but no fixture targets `PaymentController.Delete` in isolation, nor the client's reversal recomputation — a human must verify both the delete action itself and the server-side total-reversal directly, since the end-to-end flow (like BL-024's) has no fixture that exercises it beyond individual backend steps.
**Gate:** CLEAR
**Verification:** NO BASELINE DATA — baseline has been run, but no scenario in scenarios.md cites this item's rules
**UI fidelity:** THEME-ONLY — reproduce the token values of: TK-001, TK-002, TK-003; screens for reference only: SCR-004 (layout reinterpreted for the target platform)

---

### BL-026 — View Bill/Service/Payment History


**Maps to capability:** "9. **View bill/service/payment history** — read-only table per past bill (Bill No, Created, charges, discounts, paid/due, status, service line items, payments) — `patient-detail.tpl.html`'s "History" tab, `GetPatientHistory()` → `GET api/Prescriptions/GetPatientHistory`."
**Module:** MOD-001
**Depends on:** BL-022, BL-024
**Acceptance basis (domain-model.md):**
- domain-model.md's Prescription entity: "`TotalCharge`, `DiscountPercent`, `DiscountAmount`, `FixedDiscount`... `TotalDiscountAmount` (computed property, `DiscountAmount + FixedDiscount`)... `TotalPayable`, `TotalPaid`, `TotalDue`... `StatusId`..." — this read-only history view surfaces exactly these fields per past bill.
- SCR-004 (Patient Detail, History tab): a single wide read-only table of every past bill (ui-inventory.md). Token groups TK-001, TK-002, TK-003 per THEME-ONLY policy.
**Verification inputs needed:**
- GM-041 (`Prescription.TotalDiscountAmount` computed-property boundary values, including an unguarded negative `FixedDiscount` producing a negative total — VERIFIABLE) is the one captured fixture directly relevant to a field this history view displays; no fixture targets `GetPatientHistory()` itself — a human should confirm the rebuilt history query surfaces the same field set functional-spec.md documents.
**Gate:** CLEAR
**Verification:** NO BASELINE DATA — baseline has been run, but no scenario in scenarios.md cites this item's rules
**UI fidelity:** THEME-ONLY — reproduce the token values of: TK-001, TK-002, TK-003; screens for reference only: SCR-004 (layout reinterpreted for the target platform)

---

### BL-028 — Print Payment Receipt


**Maps to capability:** "11. **Print a payment receipt** — modal (`patientPaymentModal.html` inline template) rendering clinic letterhead, bill totals, and the single payment being printed, then `window.print()`."
**Module:** MOD-001
**Depends on:** BL-024
**Acceptance basis (domain-model.md):**
- domain-model.md's Payment and Prescription entities — the receipt surfaces `Payment.Amount`/`Comment`/`Created` alongside the parent `Prescription`'s totals, per functional-spec.md's own description of the modal's content.
- Per SQ-009 ("keep all existing report and receipt capabilities, but implement them as modern print-friendly React/MUI views. Where useful, provide PDF export for printable reports/receipts... Exact legacy `window.print()` implementation is not required") — this capability is rebuilt as a modern print-friendly/PDF-exportable view, not a pixel-identical reproduction of the legacy inline modal.
- SCR-004 (Patient Detail): the embedded `patientPaymentModal.html` print-preview modal rendering clinic letterhead, bill totals, and the single payment (ui-inventory.md). Token groups TK-001, TK-002, TK-003 per THEME-ONLY policy (SQ-009 explicitly relaxes exact `window.print()` fidelity, but the THEME-ONLY token values still apply to whatever screen/modal the rebuild renders).
**Verification inputs needed:**
- No fixture targets the print/receipt rendering itself (it is presentation-only, not a business-rule seam `manifest.json` was designed to capture) — acceptance rests on SQ-009's decision text and a human visually confirming the rebuilt print/PDF view carries the same content (letterhead, totals, payment) the legacy modal does.
**Gate:** CLEAR
**Verification:** NO BASELINE DATA — baseline has been run, but no scenario in scenarios.md cites this item's rules
**UI fidelity:** THEME-ONLY — reproduce the token values of: TK-001, TK-002, TK-003; screens for reference only: SCR-004 (layout reinterpreted for the target platform)

---

### BL-029 — Patient Payments Report by Date Range


**Maps to capability:** "12. **View a patient payments report by date range** — `patient-report.tpl.html` (not opened this run), `PatientReportController.loadPatientPaymentsReports()` → `GET api/PatientReports/GetPatientPaymentReport`, with its own print modal."
**Module:** MOD-001
**Depends on:** BL-024
**Acceptance basis (domain-model.md):**
- domain-model.md's Payment entity — the report is a date-ranged view over `Payment` rows for one patient, per functional-spec.md's own description.
- Per SQ-009 (report/print capabilities kept but rebuilt as modern print-friendly/exportable views) — this report is rebuilt with PDF/CSV export per SQ-009's own text, not a pixel-identical reproduction of the legacy modal.
- SCR-007 (Patient Payment Report): a From/To date filter row above a printable report table (Date/Amount/Service Comment, total row), with its own embedded print-preview modal (ui-inventory.md). Token groups TK-001, TK-002, TK-003 per THEME-ONLY policy.
**Verification inputs needed:**
- No fixture targets `PatientReportController` (report/read-only endpoint, outside `manifest.json`'s 41 captured business-rule scenarios) — acceptance rests on functional-spec.md's own endpoint/field documentation and SQ-009's decision text; a human should confirm the rebuilt report's date-range filtering and totals match the legacy report's arithmetic since no golden-master fixture exists to replay.
**Gate:** CLEAR
**Verification:** NO BASELINE DATA — baseline has been run, but no scenario in scenarios.md cites this item's rules
**UI fidelity:** THEME-ONLY — reproduce the token values of: TK-001, TK-002, TK-003; screens for reference only: SCR-007 (layout reinterpreted for the target platform)



## Deferred

None.

## Sequencing Rationale

Modules are ordered exactly as `module-map.md`'s own dependency graph states it: **MOD-005** (Identity, Roles & Permissions) is the dependency root with no outgoing edges ("MOD-005... sits at the bottom of every dependency chain: all four other modules require its authorization gate before any of their screens render"); **MOD-002/MOD-003/MOD-004** each depend only on MOD-005; **MOD-001** depends on both MOD-002 and (transitively, through every other module) MOD-005.

Within **MOD-005**, BL-001 (data layer/migration foundation) precedes everything else in the entire backlog — no other item's schema can exist before this one runs, per its own citation of the harness-found migration-chain defect. BL-002 (login/logout) precedes every other MOD-005 screen because every one of them sits behind the same OAuth session BL-002 establishes. BL-003 (Manage Roles) precedes BL-004 (Manage Users) because the Users screen's own Role `<select>` (ui-inventory.md) is populated from `api/Role` — the role list must exist before a user can be assigned one. BL-005 (Manage Resources) is independent of roles/users but still depends on BL-002's session. BL-006 (Grant/Revoke Permissions) depends on both BL-003 (roles to select) and BL-005 (resources to grant), since `PermissionController.AddList` operates on exactly those two catalogs. BL-007 (server-side/screen-level authorization enforcement) depends on BL-006 because DR-015/DR-016's enforcement logic reads the `Permission` rows BL-006's screen writes — enforcing a permission model with nothing yet granted would have no meaningful acceptance criterion to test against. BL-008 (profile/password) depends only on BL-002's session, not on anything role/permission-related, so it is sequenced after the identity-catalog items but does not block or get blocked by them. BL-009 (DM.Core retirement) depends only on BL-001, since it is a repository-wide cleanup task unrelated to any specific identity screen.

Within **MOD-002**, BL-011 (Medical Condition catalog) is sequenced before BL-012 (tag/untag) because the tagging checkbox list is populated from this catalog — the catalog must exist first. BL-010 (Service catalog) has no dependency on BL-011/BL-012 (a separate catalog entirely) and is sequenced first among the three simply because module-map.md lists it first among MOD-002's owned entities; all three depend on MOD-005's BL-002/BL-007 per the module-level edge.

Within **MOD-003**, BL-013 (Product catalog) precedes BL-014 (search/browse) because search/browse operates over the same Product rows BL-013's CRUD creates — there is nothing to search before products exist. BL-013 also precedes BL-015 (stock movements) for the same reason (a movement is always against a Product). BL-016 (inventory report) depends on BL-015 because the report reads the very `Inventory` movement rows BL-015 creates.

Within **MOD-004**, BL-017 (Doctor management) is sequenced **before** BL-018 (appointment scheduling) even though functional-spec.md lists "Schedule/manage appointments" (capability 20) before "View the doctor list" (capability 24) — this reverses the source document's own bullet order deliberately, because CQ-014's decision requires appointment creation to select a doctor from a real, existing Doctor list; building the picker before the thing it picks from would leave BL-018 with nothing to point at. BL-019 (status/listing/print actions) depends on BL-018 because it operates on the same appointment records BL-018's create/edit action produces.

Within **MOD-001**, BL-020 (register patient + auto-bill) is the module's own foundational item — every other MOD-001 item operates on a Patient/Prescription pair this item creates — and it additionally depends on MOD-002's BL-010 (the service catalog BL-022 will later bill against, sequenced here for the module-level edge, not because BL-020 itself calls the catalog). BL-021 (patient list) is sequenced right after BL-020 because the grid it renders has nothing to list before a patient exists. BL-022 (add treatment services) depends on BL-020 (a bill to add services to) and BL-010 (the catalog of services to select from). BL-023 (edit patient info) depends only on BL-020. BL-024 (record payment) depends on BL-022 because a payment is recorded against the bill state BL-022 establishes (services + totals); BL-025 (delete payment), BL-026 (history), BL-027 (close/reopen bill), BL-028 (print receipt), and BL-029 (payments report) all depend on BL-024 because each reads or reverses the payment records that item creates.

## Coverage Check

<!--
  Capability-bullet coverage, authored by the planner agent (bash never
  writes prose it cannot verify against the source documents) and carried
  by bash: this run's draft wins, otherwise the prior file's section is
  preserved verbatim, otherwise a line saying plainly that it is absent.

  Each bullet is accounted for on its own line, in this countable form so
  that the per-module rollup below can be computed mechanically rather than
  asserted:

    - **MOD-002** — "<capability bullet, quoted>" -> BL-014
    - **MOD-002** — "<capability bullet, quoted>" -> EXCLUDED: <reason>
    - **MOD-002** — "<capability bullet, quoted>" -> ORPHAN

  Granularity is unchanged — this is still one line per individual
  capability bullet (and per distinct clause of a compound bullet), never
  one line per module. The "### Module Coverage Rollup" subsection is
  bash-computed by counting these lines per module and is re-derived from
  scratch every run; a prior run's copy is dropped before the new one is
  appended, exactly as the UI Screen Coverage subsection is. When no line
  matches the countable form, the rollup says it is not computable rather
  than reporting 0/0 — which would read as "nothing to cover."

  Under a --module scoped run, only the scoped module's lines are replaced;
  every other module's accounting is preserved by line-level surgery.
-->

- **MOD-005** — "25. **Log in**..." → BL-002
- **MOD-005** — "26. **Log out**..." → BL-002
- **MOD-005** — "27. **View/update own profile**..." → BL-008
- **MOD-005** — "28. **Change own password**..." → BL-008
- **MOD-005** — "29. **Manage users**..." → BL-004
- **MOD-005** — "30. **Manage roles**..." → BL-003
- **MOD-005** — "31. **Manage the resource/screen catalog**..." → BL-005
- **MOD-005** — "32. **Grant/revoke role permissions**..." → BL-006
- **MOD-005** — "33. **Screen-level access gate on every navigation**..." → BL-007
- **MOD-005** — Workflow "Login & Post-Auth Routing" → BL-002
- **MOD-005** — Workflow "Screen Access Authorization Check" → BL-007
- **MOD-002** — "13. **Manage the dental-service price catalog**..." → BL-010
- **MOD-002** — "14. **Manage the medical-condition master list**..." → BL-011
- **MOD-002** — "6. **Tag/untag a patient's medical conditions**..." → BL-012
- **MOD-003** — "15. **Manage the product catalog**..." → BL-013
- **MOD-003** — "16. **Search/browse products**..." → BL-014
- **MOD-003** — "17. **Record a stock movement (receive or ship)**..." → BL-015
- **MOD-003** — "18. **View stock-movement history for a product**..." → BL-015
- **MOD-003** — "19. **View the inventory report**..." → BL-016
- **MOD-003** — Workflow "Record Stock Movement & Update Product Levels" → BL-015
- **MOD-003** — "36. **Dashboard**..." → BL-014 (merge rationale stated on BL-014: functional-spec.md itself says this bullet "mirrors capability 16")
- **MOD-004** — "20. **Schedule/manage appointments**..." → BL-018
- **MOD-004** — "21. **Mark an appointment as visited**..." → BL-019
- **MOD-004** — "22. **Search/filter appointments**..." → BL-019
- **MOD-004** — "23. **Print an appointment copy**..." → BL-019
- **MOD-004** — "24. **View the doctor list**..." → BL-017
- **MOD-001** — "1. **View patient list**..." → BL-021
- **MOD-001** — "2. **Search/filter patients**..." → BL-021
- **MOD-001** — "3. **Register a new patient**..." → BL-020
- **MOD-001** — "4. **Add treatment services to a patient's current bill**..." → BL-022
- **MOD-001** — "5. **View/edit a patient's personal info**..." → BL-023
- **MOD-001** — "7. **Record a payment against the patient's current bill**..." → BL-024
- **MOD-001** — "8. **Delete a payment**..." → BL-025
- **MOD-001** — "9. **View bill/service/payment history**..." → BL-026
- **MOD-001** — "10. **Close the current bill / open a new one**..." → BL-027
- **MOD-001** — "11. **Print a payment receipt**..." → BL-028
- **MOD-001** — "12. **View a patient payments report by date range**..." → BL-029
- **MOD-001** — Workflow "New Patient Registration & Bill Auto-Provisioning" → BL-020
- **MOD-001** — Workflow "Add Treatment Services to Bill" → BL-022
- **MOD-001** — Workflow "Record Payment Against Bill" → BL-024
- **MOD-001** — Workflow "Delete Payment & Reverse Bill Totals" → BL-025
- **MOD-001** — Workflow "Close Bill / Open New Bill" → BL-027
- **(Static/Informational, no module)** — "34. **View the About page**..." → EXCLUDED: CQ-003 decided to exclude the broken legacy About route from the rebuild ("no meaningful business functionality and... not reachable from the live navigation").
- **(Static/Informational, no module)** — "35. **View the Contact page**..." → EXCLUDED: CQ-003 decided to exclude the broken legacy Contact route from the rebuild (its controller does not exist at all in the legacy codebase).
- **(Cross-cutting, no capability bullet)** — DM.Core usage search/relocation (CQ-024) → BL-009 (⚠ PROVISIONAL — pending PQ-010 for module placement)
- **(Cross-cutting, no capability bullet)** — EF Core data-layer consolidation & migration (CQ-002/SQ-002/SQ-005) → BL-001

**Orphaned:** none.

### Open Questions Blocking Readiness

- **PQ-005** (OPEN, no CQ assigned) — "Does `PatientCreateController.Post`'s silent ignoring of `Add()`'s failure result... need a defect fix?" — blocks **BL-020**'s acceptance for the Code-collision sub-case.
- **PQ-006** (OPEN, no CQ assigned) — "Is `MedicalInfoService.SavePatientMedicalInfos`'s first-item-scoped replace pattern and its crash on an empty submitted list a defect to fix?" — blocks **BL-012**'s acceptance for the empty-list sub-case.
- **PQ-008** (OPEN, no CQ assigned) — "Which of the two independently-coexisting "patient's current bill" resolution mechanisms is authoritative when they can disagree?" — blocks **BL-020**, **BL-021**, and **BL-027**'s acceptance for the "always has exactly one current bill" guarantee.
- **PQ-009** (OPEN, no CQ assigned) — "Does `UserService.CreateUser`'s password/retype-mismatch guard need a defect fix, given it forwards `null` into ASP.NET Identity's `CreateAsync`?" — blocks **BL-004**'s acceptance for the Create-user mismatch sub-case.
- **PQ-010** (OPEN, no CQ assigned, raised by this run) — "Which module should own the "retire or relocate DM.Core shared constants" backlog item?" — blocks **BL-009**'s module placement.
- **SQ-013**'s underlying UI-fidelity artifacts are all present and the policy (THEME-ONLY) is decided, so no item in this backlog is blocked on UI-fidelity grounding itself; every screen-bearing item above already cites its SCR-###/TK-### tokens directly.

Every other `CQ-###`/`SQ-###`/`UQ-###` this backlog's acceptance bases cite (CQ-001 through CQ-025, SQ-001 through SQ-013, UQ-001, UQ-002) is already answered per `decisions.md`'s own "Outstanding Questions: All questions in clarifications.md have been answered."

### Module Coverage Rollup

_Bash-computed by counting the per-module capability-coverage lines above. Bullet granularity is unchanged — every bullet is still accounted for individually; this only rolls those counts up per module._

- **MOD-005 — Identity, Roles & Permissions:** 11/11 capability bullets covered; 0 excluded; 0 orphaned
- **MOD-002 — Service & Medical-Info Catalog:** 3/3 capability bullets covered; 0 excluded; 0 orphaned
- **MOD-003 — Inventory & Products:** 7/7 capability bullets covered; 0 excluded; 0 orphaned
- **MOD-004 — Appointments & Doctors:** 5/5 capability bullets covered; 0 excluded; 0 orphaned
- **MOD-001 — Patient & Billing:** 16/16 capability bullets covered; 0 excluded; 0 orphaned


### UI Screen Coverage (SCR)

_Bash-computed from .specclaw/ui/ui-inventory.md against every active item's own SCR-### citations (plus this run's SCR-OUT-OF-SCOPE directives). 20 screen(s) under UI fidelity policy THEME-ONLY._

- **SCR-001** — Login → BL-002
- **SCR-002** — Patient List → BL-021
- **SCR-003** — Register Patient / Add Services to Bill → BL-020, BL-022
- **SCR-004** — Patient Detail → BL-012, BL-023, BL-024, BL-027, BL-025, BL-026, BL-028
- **SCR-005** — Medical Condition Catalog → BL-011
- **SCR-006** — Service Catalog → BL-010
- **SCR-007** — Patient Payment Report → BL-029
- **SCR-008** — Appointments → BL-018, BL-019
- **SCR-009** — Dashboard (Product/Stock Hub) → BL-014
- **SCR-010** — Product Catalog → BL-013, BL-014
- **SCR-011** — Stock / Inventory Movement → BL-015
- **SCR-012** — Stock Report → BL-016
- **SCR-013** — Manage Users → BL-004
- **SCR-014** — Manage Roles → BL-003
- **SCR-015** — Manage Resources → BL-005
- **SCR-016** — Manage Permissions → BL-006
- **SCR-017** — User Profile & Change Password → BL-008
- **SCR-018** — Access Denied → BL-007
- **SCR-019** — About → out of scope: CQ-003 decided to exclude the legacy About screen (empty placeholder controller, unreachable from live navigation) from the rebuild.
- **SCR-020** — Contact → out of scope: CQ-003 decided to exclude the legacy Contact screen (missing controller entirely, unreachable from live navigation) from the rebuild.

**Unmapped:** none

## Change Report

<!--
  Populated only by /specclaw:bf-rebuild-plan --refresh — bash-computed by
  diffing this run's fresh Gate/Verification against the prior file's own
  stored Gate:/Verification: lines, never agent-narrated. On a first-ever
  run this section reads "Not applicable."
-->

Not applicable — this is the first-ever run.
