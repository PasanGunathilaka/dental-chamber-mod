# UI Inventory: Dental Management System (DentalManagement.sln)

**Path analyzed:** C:\Learnings\Projects\legacy\dental-chamber\Source\App
**Date analyzed:** 2026-08-12
**View technology identified:** AngularJS 1.x SPA (UI-Router states, `templateUrl` + `controller` pairs) — every screen is a `$stateProvider.state(...)` registration in `Client/app/scripts/**/*.config.js` (confirmed by opening `app.config.js`, `auth.config.js`, `patient.config.js`, `dashboard.config.js`, `product.config.js`, `stock.config.js`, `user.config.js` directly), rendering a `Client/app/views/**/*.tpl.html` template (hand-written Bootstrap-3-grid markup, not a component-framework template) via a controller in `Client/app/scripts/**/*.controller.js`. Styling comes from one first-party stylesheet, `Client/app/styles/style.css`, layered on top of the unmodified vendored Bootstrap 3 theme (`Client/Content/bootstrap/bootstrap.min.css`) and vendored `ng-grid`/`angular-busy`/`toaster` CSS — confirmed both by `Client/index-dev.html:11-16`'s `<link>` order and by `Client/Gulpfile.js`'s `getThemeCssSources`/`getVendorCssSources`/`"styles"` task (lines 142-153, 251-258, 322-328, 347-351), which package exactly these same three groups into `dist/theme/css/theme-style.min.css`, `dist/vendors/css/vendor-style.min.css`, and `dist/v1.1.0/styles/style.min.css` respectively for the production shell (`Client/index-prod.html:10-14`). **This agrees with `codebase-report.md`'s Tech Stack finding** ("Frontend is AngularJS 1.x... hand-written `*.config.js`/`*.controller.js`/`*.service.js` files") — no disagreement between the collector's extension histogram and `codebase-report.md` was found; `codebase-report.md` did not itself analyze the styling/theme layer in detail, so the theme-source identification above (style.css + vendored Bootstrap/ng-grid/toaster) is new grounding added in this run, not a correction of a prior claim. Of the histogram's 28 `.html`/51 `.css`/48 `.less`/1 `.scss` files, the authored application surface is: 27 `.tpl.html` + `nav.html`/`footer.html`/`confirm.modal.tpl.html` under `Client/app/views/**`, plus `index-dev.html`/`index-prod.html`; 1 first-party `.css` (`Client/app/styles/style.css`); the remaining ~50 `.css`/48 `.less`/1 `.scss` files are vendored `ng-grid`/`ui-grid`/Bootstrap/Materialize assets under `Client/Content/**` (Materialize is present on disk but not referenced by either `index-dev.html`, `index-prod.html`, or `Gulpfile.js` — confirmed inactive, see Named Gaps).
**Cross-referenced against:** `domain-model.md` (12 domain + 4 identity entities, DR-001–DR-020, 3 enumerations) and `functional-spec.md` (36 capabilities, 9 workflows, its own `## UI Inventory` roster of 20 routed states + shared layout/modal files, 13 Named Gaps) — both read in full before writing this document.

<!--
  NOTE ON THIS COMMENT: never write a literal double-brace placeholder
  token inside this comment's own prose (not even to describe it) — filling
  this template is a dumb global string replace, and a token mentioned here
  would get overwritten along with the real placeholder below, corrupting
  this comment. Refer to placeholders by section name instead.
-->

## Screens

### SCR-001 — Login

**Purpose:** Inference: an unauthenticated user signs in with a username/password to obtain a bearer token.
**Defined in:** `Client/app/scripts/auth/auth.config.js:9-17` (state `root.login`, template `app/views/auth/login.tpl.html`, controller `LoginController`)
**Functional-spec UI Inventory line:** "`root.login` | `login.tpl.html` | `LoginController` | Controller opened; template not opened this run | password field (inferred...)" — this run additionally opened `login.tpl.html` directly.
**Navigation in:** Any unauthenticated navigation attempt is redirected here — `Client/app/scripts/app.config.js:43-46` (`authnErrorCallback`/`$state.go("root.login")`); explicit logout from the nav dropdown or Profile screen also lands here — `Client/app/scripts/app.controller.js:17-21`, `Client/app/scripts/auth/auth.controller.js:143-148`.
**Navigation out:** Successful sign-in routes to `root.patient` for every valid seeded role — `Client/app/scripts/auth/auth.controller.js:9-48` → `Client/app/scripts/app.service.js:14-24`.

**Layout structure:**
- A centered, single-column panel roughly a third of the viewport wide (`col-xs-8 col-sm-6 col-md-4 col-lg-4`, horizontally offset to center) holding one "Sign In" panel — `Client/app/views/auth/login.tpl.html:22-57`.
  - Panel heading band with the title "Sign In" — `login.tpl.html:26-29`.
  - Panel body holding the login form (Username, Password, a "Remember me" checkbox, and a Sign In button, stacked vertically) — `login.tpl.html:31-47`.

**Widgets:**

| Field / control | Widget type | Domain-model ref | Citation |
|---|---|---|---|
| Username | text input | ApplicationUser.UserName (indirect — via OAuth `/token` credentials, not a direct entity binding) | `login.tpl.html:35` |
| Password | text input (`type="password"`) | ApplicationUser.PasswordHash (indirect) | `login.tpl.html:39` |
| Remember me | checkbox/toggle | — (no domain-model field; see Widget Cross-Reference Findings) | `login.tpl.html:42` |
| Sign In | button | — | `login.tpl.html:45` |

**States evidenced in code:** none beyond the default view — failed/successful sign-in are transient toaster notifications (`Client/app/scripts/auth/auth.controller.js:28,43`), not a distinct rendered page state.
**Token groups referenced:** TK-001, TK-002, TK-003 (shared nav/footer chrome renders above/below every screen, including this one)

### SCR-002 — Patient List

**Purpose:** Inference: the clinic-staff landing screen — search/filter and browse all registered patients, and jump to every other patient-related screen.
**Defined in:** `Client/app/scripts/patient/patient.config.js:7-16` (state `root.patient`, template `app/views/patient/patient.tpl.html`, controller `PatientController`)
**Functional-spec UI Inventory line:** "`root.patient` | `patient.tpl.html` | `PatientController` | Controller opened; template **not opened this run**..." — this run additionally opened `patient.tpl.html` directly.
**Navigation in:** Post-login/post-logout-home routing for every valid role — `Client/app/scripts/app.service.js:16,19`; the SPA's unmatched-URL fallback — `Client/app/scripts/app.config.js:63`; "Back"/`ui-sref="root.patient"` buttons on nearly every other patient-family and admin screen.
**Navigation out:** "+ Patient" → `root.patient-create` (`toAddPatientView()`) — `Client/app/scripts/patient/patient.controller.js:87-91`, `patient.tpl.html:19`; "Medical" → `root.patient-info` — `patient.tpl.html:20`; "Service" → `root.patient-service` — `patient.tpl.html:21`; "Appoinment" [sic] → `root.patient-appointment` — `patient.tpl.html:22`; "Report" → `root.patient-report` — `patient.tpl.html:23`; "Stock" → `root.dashboard` — `patient.tpl.html:24`; clicking a grid row's Patient Id → `root.patient-detail` (`detail()`) — `patient.controller.js:76-84`.

**Layout structure:**
- A toolbar band across the top of the content region — `patient.tpl.html:4-28`.
  - Search box with an inline search-icon submit button, left-aligned — `patient.tpl.html:6-12`.
  - Due/Payment-Complete/All status filter dropdown, next to the search box — `patient.tpl.html:13-15`.
  - A right-aligned, evenly-justified button group (+ Patient, Medical, Service, Appoinment, Report, Stock) — `patient.tpl.html:17-26`.
- Below the toolbar, a full-width data grid listing every patient (Patient Id, Patient Name, Phone, Age, Gender, Last Visiting Date, Payable, Paid, Due columns; Patient Id is a clickable link) — `patient.tpl.html:33`, columns defined at `patient.controller.js:47-64`.

**Widgets:**

| Field / control | Widget type | Domain-model ref | Citation |
|---|---|---|---|
| Search (Patient Name/Id/Phone) | text input | Patient.Name, Patient.Code, Patient.Phone | `patient.tpl.html:8` |
| Filter (All/Due/Payment Complete) | select/combo (hardcoded 3-option list) | Prescription.TotalDue (derived) | `patient.tpl.html:14`, options at `patient.controller.js:14-18` |
| Patient grid | grid/list | Patient (Code/Name/Phone/Age/Gender), Prescription.TotalPayable/TotalPaid/TotalDue, Patient.LastVisitingDate (view-model field, not a raw `DM.Models.Patient` column) | `patient.tpl.html:33`, `patient.controller.js:47-64` |

**States evidenced in code:** none beyond the default view — search/filter reuse the same grid render path with different data (`patient.controller.js:32-44`), with no distinct empty/error branch in the template itself.
**Token groups referenced:** TK-001, TK-002, TK-003

### SCR-003 — Register Patient / Add Services to Bill

⚠ Note: this single routed screen renders two mutually exclusive panels selected by a `pageName` scope variable — treated here as one screen with two evidenced states, per the code's own `ng-if="pageName == '...'"` structure.

**Purpose:** Inference: register a brand-new patient, then (on success) immediately add priced dental services to that patient's newly auto-provisioned bill.
**Defined in:** `Client/app/scripts/patient/patient.config.js:17-25` (state `root.patient-create`, template `app/views/patient/patient-create.tpl.html`, controller `PatientCreateController`)
**Functional-spec UI Inventory line:** "`root.patient-create` | `patient-create.tpl.html` | `PatientCreateController` | Fully opened (both) | select (Gender), textarea×2, checkbox list (service selection), number (Quantity)"
**Navigation in:** From `root.patient`'s "+ Patient" button, `pageName` set to `'new-patient'` — `patient.controller.js:87-91`; from `root.patient-detail`'s "Add More Service" button, `pageName` set to `'add-services'` — `Client/app/scripts/patient/patient-detail.controller.js:226-230`.
**Navigation out:** "Back" (new-patient panel) → `root.patient` — `patient-create.tpl.html:16`; after "Save" on the services panel → `backToPatientDetail()` → `root.patient-detail` — `patient-create.tpl.html:212`, controller `patient-create.controller.js:186-203,263-265`.

**Layout structure:**
- **`new-patient` state:** a single centered panel (`col-sm-6 col-sm-offset-3`) titled "New Patient" — `patient-create.tpl.html:10-86`.
  - Panel heading with title and a "Back" button — `patient-create.tpl.html:12-19`.
  - A vertical form: Name, Age, Gender, Phone, Email, Address (textarea), Note (textarea), then a Save button — `patient-create.tpl.html:22-82`.
- **`add-services` state:** a full-width panel titled "Add Services" — `patient-create.tpl.html:88-246`.
  - Panel heading with title and a "Back" (to Patient Detail) button — `patient-create.tpl.html:90-96`.
  - Bill/patient summary strip (Bill No, Date, Patient ID, Name, Age) — `patient-create.tpl.html:113-131`.
  - A full-width table of catalog services, each row a checkbox + Name + Charge + editable Quantity + computed Total — `patient-create.tpl.html:133-156`.
  - Below the table, two side-by-side condensed tables: discount inputs (left) and running totals (right) — `patient-create.tpl.html:159-208`.
  - A "Save" button below the totals — `patient-create.tpl.html:210-215`.

**Widgets:**

| Field / control | Widget type | Domain-model ref | Citation |
|---|---|---|---|
| Name | text input (capitalized on submit) | Patient.Name | `patient-create.tpl.html:27` |
| Age | text input | Patient.Age | `patient-create.tpl.html:34` |
| Gender | select/combo (hardcoded `["Male","Female","Others"]`) | Patient.Gender (plain `string`, not the unused `Gender` enum — see domain-model.md) | `patient-create.tpl.html:41`, options at `patient-create.controller.js:13-17` |
| Phone | text input | Patient.Phone | `patient-create.tpl.html:49` |
| Email | text input | Patient.Email | `patient-create.tpl.html:56` |
| Address | memo/textarea | Patient.Address | `patient-create.tpl.html:63` |
| Note | memo/textarea | Patient.Note | `patient-create.tpl.html:70` |
| Save | button | — | `patient-create.tpl.html:77` |
| Service selection checkboxes | checkbox/toggle (one per catalog row) | PatientMedicalService (via MedicalService catalog) | `patient-create.tpl.html:146` |
| Quantity (per service row) | numeric input | PatientMedicalService.Quantity | `patient-create.tpl.html:149` |
| Discount (%) | text input (numeric-only in practice, `maxlength="3"`) | Prescription.DiscountPercent | `patient-create.tpl.html:164` |
| Fixed Discount | text input | Prescription.FixedDiscount | `patient-create.tpl.html:172` |
| Save (services) | button | — | `patient-create.tpl.html:212` |

**States evidenced in code:** `new-patient` (default) — `patient-create.controller.js:19`; `add-services` (entered after a successful patient create, or when arriving via "Add More Service") — `patient-create.controller.js:92`, `247`.
**Token groups referenced:** TK-001, TK-002, TK-003

### SCR-004 — Patient Detail

**Purpose:** Inference: the hub for one patient's current bill — service line items, medical conditions, payments, bill history, and the patient's own editable info, switched via an in-page tab-like button group.
**Defined in:** `Client/app/scripts/patient/patient.config.js:26-34` (state `root.patient-detail`, template `app/views/patient/patient-detail.tpl.html`, controller `PatientDetailControlller` — sic, three "l"s)
**Functional-spec UI Inventory line:** "`root.patient-detail` | `patient-detail.tpl.html` | `PatientDetailControlller` (sic) | Fully opened (both) | select (Gender), textarea×2, checkbox list (medical conditions), datetime text (payment date), number (Amount)"
**Navigation in:** Clicking a patient row on `root.patient` (`detail()`) — `patient.controller.js:76-84`; returning from `root.patient-create`'s Save-Services flow (`backToPatientDetail()`) — `patient-create.controller.js:263-265`.
**Navigation out:** "Back" → `root.patient` — `patient-detail.tpl.html:91`; "Add More Service" (Services tab) → `root.patient-create` — `patient-detail.tpl.html:131`, `patient-detail.controller.js:226-230`.

**Layout structure:**
- One panel spanning the full content width — `patient-detail.tpl.html:64-456`.
  - Panel heading: a dynamic title (one of "Services & Active Payment Detail" / "Medical Conditions" / "Manage Payment" / "History" / "Patient Info") on the left, a 5-button tab-switcher (Services/Medical Condition/Payment/History/Patient) plus a "Back" button on the right — `patient-detail.tpl.html:70-93`.
  - Body region, one of five mutually-exclusive panels selected by the tab buttons:
    - **Services** — a table of billed service line items, an "Add More Service" button, a payment-history table, and a running-totals table — `patient-detail.tpl.html:96-184`.
    - **Medical Condition** — two side-by-side tables: a checkbox list of every catalog medical condition (left) and a read-only list of the patient's currently-tagged conditions (right) — `patient-detail.tpl.html:187-227`.
    - **Payment** — a payment-entry form (Date/Amount/Comment + Add/Update button) above a payment-history table, running totals, and New Bill/Force New Bill buttons — `patient-detail.tpl.html:229-322`.
    - **History** — a single wide table of every past bill for this patient — `patient-detail.tpl.html:326-378`.
    - **Patient** — a two-column edit form for the patient's own info (Id/Name/Age/Gender/Phone/Email/Address/Note) — `patient-detail.tpl.html:379-451`.

**Widgets:**

| Field / control | Widget type | Domain-model ref | Citation |
|---|---|---|---|
| Payment Date | text input (`type="datetime"`, no picker directive attached) | Payment.Created | `patient-detail.tpl.html:237` |
| Payment Amount | numeric input | Payment.Amount | `patient-detail.tpl.html:245` |
| Paid for Service Taken (comment) | memo/textarea (single row) | Payment.Comment | `patient-detail.tpl.html:252` |
| Add (payment) | button | — | `patient-detail.tpl.html:257` |
| Update (payment) | button — **unreachable dead code, see Named Gaps** | — | `patient-detail.tpl.html:258` |
| Delete payment (icon) | button | Payment (delete) | `patient-detail.tpl.html:281` |
| Print payment (icon) | button (opens embedded print modal `patientPaymentModal.html`) | Payment, Prescription | `patient-detail.tpl.html:283`, `466-579` |
| Medical condition checkboxes | checkbox/toggle (one per catalog row) | PatientMedicalInfo (via MedicalInfo catalog) | `patient-detail.tpl.html:200` |
| Save (medical conditions) | button | — | `patient-detail.tpl.html:207` |
| Patient Id (Patient tab) | text input, disabled | Patient.Code | `patient-detail.tpl.html:386` |
| Patient Name (Patient tab) | text input | Patient.Name | `patient-detail.tpl.html:392` |
| Age (Patient tab) | text input | Patient.Age | `patient-detail.tpl.html:401` |
| Gender (Patient tab) | select/combo (hardcoded 3-option list) | Patient.Gender | `patient-detail.tpl.html:407`, options at `patient-detail.controller.js:9-13` |
| Phone (Patient tab) | text input | Patient.Phone | `patient-detail.tpl.html:417` |
| Email (Patient tab) | text input | Patient.Email | `patient-detail.tpl.html:423` |
| Address (Patient tab) | memo/textarea | Patient.Address | `patient-detail.tpl.html:432` |
| Note (Patient tab) | memo/textarea | Patient.Note | `patient-detail.tpl.html:439` |
| Update (Patient tab) | button | — | `patient-detail.tpl.html:446` |
| Services/History tables | grid/list (plain HTML tables, not `ng-grid`) | PatientMedicalService, Payment, Prescription | `patient-detail.tpl.html:104-155,266-287,331-373` |
| New Bill | button | Prescription (close+create) | `patient-detail.tpl.html:318` |
| Force New Bill | button | Prescription (close+create, bypasses due-balance guard) | `patient-detail.tpl.html:319` |

**States evidenced in code:** `services` (default) — `patient-detail.controller.js:15`; `medical`; `payment`; `history`; `patient` — all four selected by the button group at `patient-detail.tpl.html:81-89`; within the `payment` state, a validation-blocked sub-state "payment exceeds due" (toast, no HTTP call) — `patient-detail.controller.js:149-150`; and "bill-close blocked while due > 0" (toast, on the "New Bill" button only) — `patient-detail.controller.js:257-258`.
**Token groups referenced:** TK-001, TK-002, TK-003

### SCR-005 — Medical Condition Catalog

**Purpose:** Inference: staff manage the master list of medical conditions/allergies that can later be tagged onto a patient.
**Defined in:** `Client/app/scripts/patient/patient.config.js:35-43` (state `root.patient-info`, template `app/views/patient/patient-info.tpl.html`, controller `PatientInfoControlller`)
**Functional-spec UI Inventory line:** "`root.patient-info` | `patient-info.tpl.html` | `PatientInfoControlller` | Controller opened; template not opened this run | none confirmed..." — this run additionally opened `patient-info.tpl.html` directly.
**Navigation in:** "Medical" button on `root.patient` — `patient.tpl.html:20`.
**Navigation out:** "Back" → `root.patient` — `patient-info.tpl.html:15`.

**Layout structure:**
- One full-width panel titled "Medical Infos" with a "Back" button — `patient-info.tpl.html:9-19`.
  - A one-row create/edit form (Name field + Save/Update button) — `patient-info.tpl.html:24-41`.
  - A full-width table listing every catalog entry, each row with edit/delete icon links — `patient-info.tpl.html:43-67`.

**Widgets:**

| Field / control | Widget type | Domain-model ref | Citation |
|---|---|---|---|
| Name | text input | MedicalInfo.Name | `patient-info.tpl.html:30` |
| Save | button | — | `patient-info.tpl.html:35` |
| Update | button | — | `patient-info.tpl.html:36` |
| Catalog list | grid/list (plain HTML table) | MedicalInfo | `patient-info.tpl.html:46-64` |
| Edit (icon) | button | MedicalInfo | `patient-info.tpl.html:57` |
| Delete (icon) | button | MedicalInfo | `patient-info.tpl.html:59` |

**States evidenced in code:** `default` (Save visible); `edit-mode` (Update visible instead, form pre-filled) — toggled by `isUpdateMode`, set true in `edit()` — `Client/app/scripts/patient/patient-info.controller.js:59-62`, template toggle at `patient-info.tpl.html:35-36`.
**Token groups referenced:** TK-001, TK-002, TK-003

### SCR-006 — Service Catalog

**Purpose:** Inference: staff manage the priced catalog of dental services/treatments offered.
**Defined in:** `Client/app/scripts/patient/patient.config.js:44-52` (state `root.patient-service`, template `app/views/patient/patient-service.tpl.html`, controller `PatientServiceControlller`)
**Functional-spec UI Inventory line:** "`root.patient-service` | `patient-service.tpl.html` | `PatientServiceControlller` | Controller opened; template not opened this run | none confirmed" — this run additionally opened `patient-service.tpl.html` directly.
**Navigation in:** "Service" button on `root.patient` — `patient.tpl.html:21`.
**Navigation out:** "Back" → `root.patient` — `patient-service.tpl.html:15`.

**Layout structure:**
- One full-width panel titled "Services" with a "Back" button — `patient-service.tpl.html:9-18`.
  - A one-row create/edit form (Name, Charge, Save/Update button) — `patient-service.tpl.html:24-45`.
  - A full-width table listing every catalog service, each row with edit/delete icon links — `patient-service.tpl.html:49-75`.

**Widgets:**

| Field / control | Widget type | Domain-model ref | Citation |
|---|---|---|---|
| Name | text input | MedicalService.Name | `patient-service.tpl.html:30` |
| Charge | numeric input | MedicalService.Charge (declared `string` server-side, DR-019) | `patient-service.tpl.html:37` |
| Save | button | — | `patient-service.tpl.html:41` |
| Update | button | — | `patient-service.tpl.html:42` |
| Catalog list | grid/list (plain HTML table) | MedicalService | `patient-service.tpl.html:52-72` |
| Edit (icon) | button | MedicalService | `patient-service.tpl.html:65` |
| Delete (icon) | button | MedicalService | `patient-service.tpl.html:67` |

**States evidenced in code:** `default` (Save visible); `edit-mode` (Update visible instead) — same `isUpdateMode` pattern as SCR-005, confirmed reachable via `Client/app/scripts/patient/patient-service.controller.js` (grep-confirmed `isUpdateMode = true` assignment; controller not opened line-by-line this run — see Named Gaps), template toggle at `patient-service.tpl.html:41-42`.
**Token groups referenced:** TK-001, TK-002, TK-003

### SCR-007 — Patient Payment Report

**Purpose:** Inference: view/print a patient's payment history across a date range.
**Defined in:** `Client/app/scripts/patient/patient.config.js:53-61` (state `root.patient-report`, template `app/views/patient/patient-report.tpl.html`, controller `PatientReportController`)
**Functional-spec UI Inventory line:** "`root.patient-report` | `patient-report.tpl.html` | `PatientReportController` | Controller opened; template not opened this run | date filter fields..." — this run additionally opened `patient-report.tpl.html` directly.
**Navigation in:** "Report" button on `root.patient` — `patient.tpl.html:23`.
**Navigation out:** "Back" → `root.patient` — `patient-report.tpl.html:10`.

**Layout structure:**
- One full-width panel titled "Patient Service Report" with a "Back" button — `patient-report.tpl.html:4-13`.
  - A filter row: From date, To date, search button, and a right-aligned Print button — `patient-report.tpl.html:15-42`.
  - A printable report table (Date/Amount/Service Comment rows, total row) — `patient-report.tpl.html:45-72`.
  - An embedded print-preview modal (`patientReportModal.html`), reusing the same table layout — `patient-report.tpl.html:82-131`.

**Widgets:**

| Field / control | Widget type | Domain-model ref | Citation |
|---|---|---|---|
| From | date picker (native HTML5 `type="date"`) | — (report filter, not a persisted field) | `patient-report.tpl.html:17` |
| To | date picker (native HTML5 `type="date"`) | — | `patient-report.tpl.html:21` |
| Search | button | — | `patient-report.tpl.html:25` |
| Print | button (opens embedded print modal) | — | `patient-report.tpl.html:41` |
| Report table | grid/list (plain HTML table) | Payment | `patient-report.tpl.html:48-71` |

**States evidenced in code:** none beyond the default view.
**Token groups referenced:** TK-001, TK-002, TK-003

### SCR-008 — Appointments

**Purpose:** Inference: staff schedule and manage appointment slots, independent of any registered `Patient` record.
**Defined in:** `Client/app/scripts/patient/patient.config.js:62-70` (state `root.patient-appointment`, template `app/views/patient/patient-appointment.tpl.html`, controller `PatientAppointmentController`)
**Functional-spec UI Inventory line:** "`root.patient-appointment` | `patient-appointment.tpl.html` | `PatientAppointmentController` | Fully opened (both) | date-picker (`uib-datepicker-popup`), time-picker (`uib-timepicker`), number (Age), plain `type=\"date\"` filter input"
**Navigation in:** "Appoinment" [sic] button on `root.patient` — `patient.tpl.html:22`.
**Navigation out:** "Back" → `root.patient` — `patient-appointment.tpl.html:17`.

**Layout structure:**
- One full-width panel titled "Appointments" with a "Back" button — `patient-appointment.tpl.html:11-20`.
  - A create/edit form row: Patient Name, Age, Phone, Date (calendar-popup picker with a calendar-icon button), Time (hour/minute/AM-PM stepper), Save/Update button — `patient-appointment.tpl.html:24-77`.
  - A filter row below a divider: free-text search and a date filter — `patient-appointment.tpl.html:82-92`.
  - A full-width appointments table (Id/Name/Age/Phone/Date/Time/Status + edit/mark-visited/print icons per row) — `patient-appointment.tpl.html:99-133`.
  - An embedded print-preview modal (`patientAppointmentModal.html`) — `patient-appointment.tpl.html:145-198`.

**Widgets:**

| Field / control | Widget type | Domain-model ref | Citation |
|---|---|---|---|
| Patient Name | text input (capitalized on submit) | Appointment.PatientNameOrId | `patient-appointment.tpl.html:30` |
| Age | numeric input | Appointment.Age | `patient-appointment.tpl.html:37` |
| Phone | text input | Appointment.Phone | `patient-appointment.tpl.html:44` |
| Date | date picker (`uib-datepicker-popup` directive, with a calendar-icon trigger button) | Appointment.Date | `patient-appointment.tpl.html:52-55` |
| Time | date picker — time-of-day variant (`uib-timepicker` hour/minute stepper with AM/PM toggle; nearest fixed vocabulary term, not a calendar date) | Appointment.Time | `patient-appointment.tpl.html:64` |
| Save | button | — | `patient-appointment.tpl.html:72` |
| Update | button | — | `patient-appointment.tpl.html:73` |
| Search (Id/Name) | text input | Appointment.Code, Appointment.PatientNameOrId | `patient-appointment.tpl.html:84` |
| Date filter | date picker (native HTML5 `type="date"`) | Appointment.Date | `patient-appointment.tpl.html:88` |
| Appointments table | grid/list (plain HTML table) | Appointment, Status | `patient-appointment.tpl.html:99-131` |
| Edit (icon) | button | Appointment | `patient-appointment.tpl.html:122` |
| Mark visited (icon) | button | Appointment.StatusId (→8) | `patient-appointment.tpl.html:124` |
| Print (icon) | button (opens embedded print modal) | Appointment | `patient-appointment.tpl.html:126` |

**States evidenced in code:** `default`/`create` (Save visible); `edit-mode` (Update visible, form pre-filled) — toggled by `isUpdateMode`, set true in `edit()` — `Client/app/scripts/patient/patient-appointment.controller.js:131-134`, template toggle at `patient-appointment.tpl.html:72-73`.
**Token groups referenced:** TK-001, TK-002, TK-003

### SCR-009 — Dashboard (Product/Stock Hub)

**Purpose:** Inference: a landing hub for stock/inventory staff — search/filter products and jump to Inventory, Product, Report, or Patient.
**Defined in:** `Client/app/scripts/dashboard/dashboard.config.js:8-16` (state `root.dashboard`, url `"/"`, template `app/views/dashboard/dashboard.tpl.html`, controller `DashboardController`)
**Functional-spec UI Inventory line:** "`root.dashboard` | `dashboard.tpl.html` | `DashboardController` | Fully opened (both) | select (status filter); `ng-grid` (deprecated ngGrid directive)"
**Navigation in:** "Stock" button on `root.patient` — `patient.tpl.html:24`; also directly reachable at the SPA's bare `"/"` URL, though the app's own `$urlRouterProvider.otherwise("/patient")` never routes here automatically — `Client/app/scripts/app.config.js:63` vs `dashboard.config.js:9`.
**Navigation out:** "Inventory" → `root.stock` — `dashboard.tpl.html:25`; "Product" → `root.product` — `dashboard.tpl.html:26`; "Report" → `root.stock-report` — `dashboard.tpl.html:27`; "Patient" → `root.patient` — `dashboard.tpl.html:28`.

**Layout structure:**
- A toolbar band across the top — `dashboard.tpl.html:4-32`.
  - Search box with inline search-icon button — `dashboard.tpl.html:9-16`.
  - In-Stock/Out-Of-Stock/All status filter dropdown — `dashboard.tpl.html:19-21`.
  - A right-aligned, evenly-justified button group (Inventory/Product/Report/Patient) — `dashboard.tpl.html:23-30`.
- Below the toolbar, a full-width data grid of products with a highlighted "On Hand" column, plus a static (non-functional) pagination control — `dashboard.tpl.html:36-58`, columns at `dashboard.controller.js:67-89`.

**Widgets:**

| Field / control | Widget type | Domain-model ref | Citation |
|---|---|---|---|
| Search (product name/code) | text input | Product.Name, Product.Code | `dashboard.tpl.html:11` |
| Status filter | select/combo (hardcoded 3-option list) | Product.StatusId (via Status lookup) | `dashboard.tpl.html:20`, options at `dashboard.controller.js:15-19` |
| Product grid | grid/list | Product (Code/Name/StartingInventory/Received/Shipped/OnHand/UnitPrice/SalePrice), Status.Name | `dashboard.tpl.html:37`, `dashboard.controller.js:67-89` |

**States evidenced in code:** none beyond the default view.
**Token groups referenced:** TK-001, TK-002, TK-003, TK-004

### SCR-010 — Product Catalog

**Purpose:** Inference: manage the stocked-product catalog (create/edit/delete) and browse/search it.
**Defined in:** `Client/app/scripts/product/product.config.js:8-16` (state `root.product`, template `app/views/product/product.tpl.html`, controller `ProductController`)
**Functional-spec UI Inventory line:** "`root.product` | `product.tpl.html` | `ProductController` | Fully opened (both) | none — all fields are plain text inputs despite several being numeric"
**Navigation in:** "Product" button on `root.dashboard` — `dashboard.tpl.html:26`.
**Navigation out:** Back-arrow button → `root.dashboard` — `product.tpl.html:4`.

**Layout structure:**
- Back-arrow link above the content — `product.tpl.html:3-5`.
- Two side-by-side panels — `product.tpl.html:7-122`.
  - Left, narrower panel ("Add Product"): a vertical create/edit form — `product.tpl.html:7-63`.
  - Right, wider panel ("Product List"): a search row above a full-width product table with edit/delete icons per row — `product.tpl.html:64-121`.

**Widgets:**

| Field / control | Widget type | Domain-model ref | Citation |
|---|---|---|---|
| Code | text input (uppercased on submit) | Product.Code | `product.tpl.html:17` |
| Name | text input (capitalized on submit) | Product.Name | `product.tpl.html:24` |
| Starting Inventory | text input (numeric in practice) | Product.StartingInventory | `product.tpl.html:30` |
| Minimum Required | text input (numeric in practice) | Product.MinimumRequired | `product.tpl.html:36` |
| Unit Price | text input (numeric in practice) | Product.UnitPrice | `product.tpl.html:42` |
| Sale Price | text input (numeric in practice) | Product.SalePrice | `product.tpl.html:48` |
| Save | button | — | `product.tpl.html:54` |
| Update | button | — | `product.tpl.html:55` |
| Search (Code/Name) | text input | Product.Code, Product.Name | `product.tpl.html:74` |
| Product table | grid/list (plain HTML table) | Product | `product.tpl.html:86-115` |
| Edit (icon) | button | Product | `product.tpl.html:109` |
| Delete (icon) | button | Product | `product.tpl.html:111` |

**States evidenced in code:** `default` (Save visible); `edit-mode` (Update visible instead, form pre-filled) — toggled by `isUpdateMode`, set true in `getProduct()`'s success callback — `Client/app/scripts/product/product.controller.js:48-58`, template toggle at `product.tpl.html:54-55`.
**Token groups referenced:** TK-001, TK-002, TK-003

### SCR-011 — Stock / Inventory Movement

**Purpose:** Inference: record a stock movement (goods received or shipped) for a product and review its movement history.
**Defined in:** `Client/app/scripts/stock/stock.config.js:8-16` (state `root.stock`, template `app/views/stock/stock.tpl.html`, controller `StockController`)
**Functional-spec UI Inventory line:** "`root.stock` | `stock.tpl.html` | `StockController` | Fully opened (both) | datetime text, select (Product), disabled text (On Hand), number (quantity), radio pair (Stock Type)"
**Navigation in:** "Inventory" button on `root.dashboard` — `dashboard.tpl.html:25`.
**Navigation out:** Back-arrow button → `root.dashboard` — `stock.tpl.html:4`.

**Layout structure:**
- Back-arrow link above the content — `stock.tpl.html:3-5`.
- Two side-by-side panels — `stock.tpl.html:7-155`.
  - Left panel ("Add Stock"): a vertical movement-entry form — `stock.tpl.html:7-71`.
  - Right, wider panel ("Stock History of {{productName}}"): a date-range/preset-days filter row above a movement-history table, totals row, and a Print button — `stock.tpl.html:75-154`.
  - An embedded print-preview modal (`inventoryHistoryReportModal.html`) — `stock.tpl.html:159-218`.

**Widgets:**

| Field / control | Widget type | Domain-model ref | Citation |
|---|---|---|---|
| Date | text input (`type="datetime"`, no picker directive attached) | Inventory.Created | `stock.tpl.html:18` |
| Product Name | select/combo (options from `GetProductsName` API) | Inventory.ProductId (via Product) | `stock.tpl.html:25` |
| Cash Memo No | text input (uppercased on submit) | Inventory.CashMemoNo | `stock.tpl.html:34` |
| On Hand | text input, disabled | Product.OnHand (snapshot) | `stock.tpl.html:41` |
| Received / Shipped Quantity | numeric input | Inventory.ReceivedOrShippedQuantity | `stock.tpl.html:48` |
| Stock Type | checkbox/toggle — radio pair (Received=3 / Shipped=4; nearest fixed vocabulary term for a mutually-exclusive two-option radio group; default-selected option is not reliably determinable statically, see Named Gaps) | Inventory.StatusId | `stock.tpl.html:55-56` |
| Save | button | — | `stock.tpl.html:63` |
| Update | button | — | `stock.tpl.html:64` |
| From / To (history filter) | date picker (native HTML5 `type="date"`) | Inventory.Created (range filter) | `stock.tpl.html:85,89` |
| Days-preset filter | select/combo (Last 7/15/30 Days) | — (report filter) | `stock.tpl.html:99`, options at `stock.controller.js:14-18` |
| Movement history table | grid/list (plain HTML table) | Inventory, Status | `stock.tpl.html:108-144` |
| Print | button (opens embedded print modal) | Inventory | `stock.tpl.html:147` |

**States evidenced in code:** `default`/`create` (Save visible); `edit-mode` (Update visible instead) — toggled by `isUpdateMode`, set true in both branches of `getInventoryById()` — `Client/app/scripts/stock/stock.controller.js:155-164`, template toggle at `stock.tpl.html:63-64`; a blocked-submit validation state "shipment exceeds on-hand" (alert, no HTTP call) — `stock.controller.js:111-112`.
**Token groups referenced:** TK-001, TK-002, TK-003

### SCR-012 — Stock Report

**Purpose:** Inference: view/print an inventory report (received/shipped/on-hand per product) across a date range.
**Defined in:** `Client/app/scripts/stock/stock.config.js:17-25` (state `root.stock-report`, template `app/views/stock/stock-report.tpl.html`, controller `StockReportController`)
**Functional-spec UI Inventory line:** "`root.stock-report` | `stock-report.tpl.html` | `StockReportController` | Controller opened; template not opened this run | select×2 (Product, Status), per controller" — this run additionally opened `stock-report.tpl.html` directly; the template itself has the Product/Status selects commented out (`stock-report.tpl.html:30-40`) even though the controller still defines their option data (`stock-report.controller.js:8-19`) — see Named Gaps.
**Navigation in:** "Report" button on `root.dashboard` — `dashboard.tpl.html:27`.
**Navigation out:** Back-arrow button → `root.dashboard` — `stock-report.tpl.html:4`.

**Layout structure:**
- Back-arrow link above the content — `stock-report.tpl.html:3-5`.
- One full-width panel titled "Stock Report" — `stock-report.tpl.html:10-73`.
  - A filter row: From date, To date, search button, right-aligned Print button — `stock-report.tpl.html:17-44`.
  - A report table (Product Name/Total Received/Total Shipped/On Hand rows) — `stock-report.tpl.html:47-67`.
  - An embedded print-preview modal (`inventoryReportModal.html`) — `stock-report.tpl.html:78-122`.

**Widgets:**

| Field / control | Widget type | Domain-model ref | Citation |
|---|---|---|---|
| From | date picker (native HTML5 `type="date"`) | — (report filter) | `stock-report.tpl.html:19` |
| To | date picker (native HTML5 `type="date"`) | — | `stock-report.tpl.html:23` |
| Search | button | — | `stock-report.tpl.html:27` |
| Print | button (opens embedded print modal) | — | `stock-report.tpl.html:43` |
| Report table | grid/list (plain HTML table) | Product (Received/Shipped/OnHand) | `stock-report.tpl.html:49-66` |

**States evidenced in code:** none beyond the default view.
**Token groups referenced:** TK-001, TK-002, TK-003

### SCR-013 — Manage Users

**Purpose:** Inference: SystemAdmin/Admin staff create, edit, and delete staff login accounts and assign roles.
**Defined in:** `Client/app/scripts/user/user.config.js:8-16` (state `root.user`, template `app/views/user/user.tpl.html`, controller `UserController`)
**Functional-spec UI Inventory line:** "`root.user` | `user.tpl.html` | `UserController` | Fully opened (both) | select (Role), checkbox (\"Change Password\" toggle), password×2 (conditionally shown)"
**Navigation in:** Nav-bar admin dropdown, "Manage User" (shown only to Admin/SystemAdmin) — `Client/app/views/nav.html:58`.
**Navigation out:** "Back" → `root.patient` — `user.tpl.html:11`.

**Layout structure:**
- One full-width panel titled "Users" with a "Back" button — `user.tpl.html:5-14`.
  - A create/edit form: First/Last Name, Email, Phone, Username, Role, then a "Change Password" checkbox that conditionally reveals New/Retype Password fields, then Save/Update/Cancel — `user.tpl.html:19-104`.
  - A search row, then a full-width users table with edit/delete icons per row — `user.tpl.html:108-153`.

**Widgets:**

| Field / control | Widget type | Domain-model ref | Citation |
|---|---|---|---|
| First Name | text input | ApplicationUser.FirstName | `user.tpl.html:29` |
| Last Name | text input | ApplicationUser.LastName | `user.tpl.html:37` |
| Email | text input | ApplicationUser.Email | `user.tpl.html:45` |
| Phone | text input | ApplicationUser (PhoneNumber, standard Identity column) | `user.tpl.html:53` |
| Username | text input | ApplicationUser.UserName | `user.tpl.html:61` |
| Role | select/combo (options from `api/Role`) | IdentityRole (via ApplicationUser.Roles — see Widget Cross-Reference Findings for a cardinality mismatch) | `user.tpl.html:69` |
| Change Password | checkbox/toggle | — (UI-only reveal switch) | `user.tpl.html:79` |
| New Password | text input (`type="password"`, shown only when Change Password is checked) | ApplicationUser.PasswordHash | `user.tpl.html:87` |
| Retype Password | text input (`type="password"`, shown only when Change Password is checked) | — (client-side confirmation only, DR-012) | `user.tpl.html:94` |
| Save / Update / Cancel | button | — | `user.tpl.html:99-101` |
| Search by Role Name | text input | — (mislabeled — filters the user list, per `search.key`) | `user.tpl.html:113` |
| Users table | grid/list (plain HTML table) | ApplicationUser, IdentityRole (Role column) | `user.tpl.html:123-151` |
| Edit (icon) | button | ApplicationUser | `user.tpl.html:145` |
| Delete (icon) | button, hidden for the demo account | ApplicationUser | `user.tpl.html:147` |

**States evidenced in code:** `default`/`create` (Save visible); `edit-mode` (Update/Cancel visible instead, form pre-filled) — toggled by `isUpdateMode`, set true in `edit()` — `Client/app/scripts/user/user.controller.js:74-77`, template toggle at `user.tpl.html:99-101`; `password-fields-visible` (New/Retype Password shown) — toggled by `isChangedPassword` — `user.tpl.html:79,83,90`.
**Token groups referenced:** TK-001, TK-002, TK-003

### SCR-014 — Manage Roles

**Purpose:** Inference: SystemAdmin staff create, edit, and delete named staff roles.
**Defined in:** `Client/app/scripts/auth/auth.config.js:36-44` (state `root.role`, template `app/views/auth/role.tpl.html`, controller `RoleController`)
**Functional-spec UI Inventory line:** "`root.role` | `role.tpl.html` | `RoleController` (Angular) | Controller opened; template not opened this run | none confirmed" — this run additionally opened `role.tpl.html` directly.
**Navigation in:** Nav-bar admin dropdown, "Manage Role" (shown only to SystemAdmin) — `nav.html:60`.
**Navigation out:** "Back" → `root.patient` — `role.tpl.html:9`.

**Layout structure:**
- One full-width panel titled "Roles" with a "Back" button — `role.tpl.html:4-12`.
  - A one-row create/edit form (Name + Save/Update/Cancel) — `role.tpl.html:15-31`.
  - A search row, then a full-width roles table with edit/delete icons per row — `role.tpl.html:33-75`.

**Widgets:**

| Field / control | Widget type | Domain-model ref | Citation |
|---|---|---|---|
| Name | text input | IdentityRole (Name) | `role.tpl.html:21` |
| Save / Update / Cancel | button | — | `role.tpl.html:26-28` |
| Search by Role Name | text input | IdentityRole (Name) | `role.tpl.html:38` |
| Roles table | grid/list (plain HTML table) | IdentityRole | `role.tpl.html:46-69` |
| Edit (icon) | button | IdentityRole | `role.tpl.html:61` |
| Delete (icon) | button | IdentityRole | `role.tpl.html:66` |

**States evidenced in code:** `default` (Save visible); `edit-mode` (Update/Cancel visible instead) — toggled by `isUpdateMode`, set true in `edit()` — `Client/app/scripts/auth/role/role.controller.js:51-54`, template toggle at `role.tpl.html:26-28`.
**Token groups referenced:** TK-001, TK-002, TK-003

### SCR-015 — Manage Resources

**Purpose:** Inference: SystemAdmin staff maintain the catalog of protected screens/routes (`Resource` rows) that the permission system gates access to.
**Defined in:** `Client/app/scripts/auth/auth.config.js:45-53` (state `root.resource`, template `app/views/auth/resource.tpl.html`, controller `ResourceController`)
**Functional-spec UI Inventory line:** "`root.resource` | `resource.tpl.html` | `ResourceController` (Angular) | Fully opened (both) | radio pair (Public Yes/No) — see Named Gaps for a binding ambiguity"
**Navigation in:** Nav-bar admin dropdown, "Manage Resource" (shown only to SystemAdmin) — `nav.html:61`.
**Navigation out:** "Back" → `root.patient` — `resource.tpl.html:16`.

**Layout structure:**
- One full-width panel titled "Resources" with a "Back" button — `resource.tpl.html:9-19`.
  - A one-row create/edit form (Name, Route, Public radio pair, Save/Update/Cancel) — `resource.tpl.html:23-68`.
  - A search row, then a full-width resources table with edit/delete icons per row — `resource.tpl.html:70-112`.

**Widgets:**

| Field / control | Widget type | Domain-model ref | Citation |
|---|---|---|---|
| Name | text input | Resource.Name | `resource.tpl.html:29` |
| Route | text input | Resource.Route | `resource.tpl.html:36` |
| Public | checkbox/toggle — radio pair (Yes/No; double-bound via both a plain `value` attribute and an Angular `ng-value` to the same `ng-model` — which binding wins is unverified from static markup alone, per functional-spec Named Gap #8) | Resource.IsPublic | `resource.tpl.html:45,49` |
| Save / Update / Cancel | button | — | `resource.tpl.html:61-63` |
| Search by Role Name | text input | — (mislabeled — filters the resource list) | `resource.tpl.html:76` |
| Resources table | grid/list (plain HTML table) | Resource | `resource.tpl.html:83-108` |
| Edit (icon) | button | Resource | `resource.tpl.html:102` |
| Delete (icon) | button | Resource | `resource.tpl.html:105` |

**States evidenced in code:** `default` (Save visible); `edit-mode` (Update/Cancel visible instead) — toggled by `isUpdateMode`, set true in `edit()` — `Client/app/scripts/auth/resource/resource.controller.js:51-54`, template toggle at `resource.tpl.html:61-63`.
**Token groups referenced:** TK-001, TK-002, TK-003

### SCR-016 — Manage Permissions

**Purpose:** Inference: SystemAdmin staff select a role, then grant/revoke that role's access to individual resources/screens.
**Defined in:** `Client/app/scripts/auth/auth.config.js:54-62` (state `root.permission`, template `app/views/auth/permission.tpl.html`, controller `PermissionController`)
**Functional-spec UI Inventory line:** "`root.permission` | `permission.tpl.html` | `PermissionController` (Angular) | Fully opened (both) | checkbox list via `checklist-model` directive"
**Navigation in:** Nav-bar admin dropdown, "Manage Permission" (shown only to SystemAdmin) — `nav.html:62`.
**Navigation out:** "Back" → `root.patient` — `permission.tpl.html:46`.

**Layout structure:**
- Two side-by-side panels — `permission.tpl.html:7-112`.
  - Left, narrower panel ("Roles"): a read-only, clickable roles list — `permission.tpl.html:9-38`.
  - Right, wider panel ("Resources"): a search row, a "Cancel" button (edit mode only), a "selected role" label, a "Save" button, and a full-width resources table with a per-row checkbox plus "Check All"/"Uncheck All" bulk actions — `permission.tpl.html:40-108`.

**Widgets:**

| Field / control | Widget type | Domain-model ref | Citation |
|---|---|---|---|
| Roles list | grid/list (plain HTML table, clickable rows) | IdentityRole | `permission.tpl.html:16-34` |
| Search by Role Name | text input | — (filters the resources list, not roles, despite the placeholder text) | `permission.tpl.html:54` |
| Cancel | button | — | `permission.tpl.html:60` |
| Save | button | Permission (`AddList`, full replace of the selected role's permission set) | `permission.tpl.html:64` |
| Check All / Uncheck All | button | Permission | `permission.tpl.html:80-81` |
| Resource checkboxes | checkbox/toggle (one per resource row, via the `checklist-model` directive) | Permission (Resource ↔ Role grant) | `permission.tpl.html:96` |

**States evidenced in code:** `default` (no role selected — Save/Resource-checkbox table has nothing meaningful bound yet); `role-selected` (a role clicked in the left list — `Cancel` button and "Selected Role:" label appear, resource checkboxes reflect that role's existing grants) — `Client/app/scripts/auth/permission/permission.controller.js:68-79`, template toggles at `permission.tpl.html:60,62`.
**Token groups referenced:** TK-001, TK-002, TK-003

### SCR-017 — User Profile & Change Password

**Purpose:** Inference: any signed-in user views/edits their own profile and changes their own password.
**Defined in:** `Client/app/scripts/auth/auth.config.js:27-35` (state `root.profile`, template `app/views/auth/profile.tpl.html`, controller `ProfileController`)
**Functional-spec UI Inventory line:** "`root.profile` | `profile.tpl.html` | `ProfileController` | Controller opened; template not opened this run | password×3 (inferred...)" — this run additionally opened `profile.tpl.html` directly.
**Navigation in:** Nav-bar user dropdown, "Profile" — `nav.html:47`.
**Navigation out:** "Back" → `root.patient` — `profile.tpl.html:4`; a successful password change forces a logout → `root.login` — `Client/app/scripts/auth/auth.controller.js:125-128,143-148`.

**Layout structure:**
- Back-arrow link above the content — `profile.tpl.html:3-5`.
- Two side-by-side panels — `profile.tpl.html:7-104`.
  - Left panel ("User Profile"): Username (disabled), First/Last Name, Email, Phone, then an Update button (or a demo-user warning in its place) — `profile.tpl.html:7-61`.
  - Right panel ("Change Password"): Current/New/Retype Password, then an Update Password button (or a demo-user warning in its place) — `profile.tpl.html:63-104`.

**Widgets:**

| Field / control | Widget type | Domain-model ref | Citation |
|---|---|---|---|
| Username | text input, disabled | ApplicationUser.UserName | `profile.tpl.html:17` |
| First Name | text input | ApplicationUser.FirstName | `profile.tpl.html:24` |
| Last Name | text input | ApplicationUser.LastName | `profile.tpl.html:31` |
| Email | text input | ApplicationUser.Email | `profile.tpl.html:38` |
| Phone | text input | ApplicationUser (PhoneNumber) | `profile.tpl.html:45` |
| Update | button (hidden for a demo user) | — | `profile.tpl.html:53` |
| Current Password | text input (`type="password"`) | — (verified against ApplicationUser.PasswordHash server-side, DR-013) | `profile.tpl.html:73` |
| New Password | text input (`type="password"`) | ApplicationUser.PasswordHash | `profile.tpl.html:80` |
| Retype Password | text input (`type="password"`) | — (client/server confirmation only) | `profile.tpl.html:87` |
| Update Password | button (hidden for a demo user) | — | `profile.tpl.html:95` |

**States evidenced in code:** `default`; `demo-user-restricted` (both Update buttons replaced by a warning text) — toggled by `isDemoUser`, `Client/app/scripts/auth/auth.service.js:117-123`, template toggles at `profile.tpl.html:53-54,95-96`.
**Token groups referenced:** TK-001, TK-002, TK-003

### SCR-018 — Access Denied

**Purpose:** Inference: shown when an authenticated user's role has no `Permission` grant for the route they attempted to reach.
**Defined in:** `Client/app/scripts/auth/auth.config.js:18-26` (state `root.access-denied`, template `app/views/auth/access-denied.tpl.html`, controller `AccessDeniedController`)
**Functional-spec UI Inventory line:** "`root.access-denied` | `access-denied.tpl.html` | `AccessDeniedController` | Controller opened; template not opened this run | none — logic-only redirect controller" — this run additionally opened `access-denied.tpl.html` directly.
**Navigation in:** `$stateChangeStart` authorization-failure redirect — `Client/app/scripts/app.config.js:19-23,27-29`.
**Navigation out:** "Back To Home" → `AppService.nextRoute()` → `root.patient` (valid role) or `root.login` (no/invalid role) — `access-denied.tpl.html:5`, `auth.controller.js:60-66`, `app.service.js:7-28`.

**Layout structure:**
- A single centered text block: heading "Access Denied", a sub-message, and a "Back To Home" button — `access-denied.tpl.html:1-7`.

**Widgets:**

| Field / control | Widget type | Domain-model ref | Citation |
|---|---|---|---|
| Back To Home | button | — | `access-denied.tpl.html:5` |

**States evidenced in code:** none beyond the default view.
**Token groups referenced:** TK-001, TK-002, TK-003

### SCR-019 — About

⚠ PROVISIONAL — pending PQ-003 (proposed default: treat as SCOPE/TARGET-GAP, not a screen requiring pixel-fidelity capture)

**Purpose:** Inference: a static informational placeholder page.
**Defined in:** `Client/app/scripts/app.config.js:72-80` (state `root.about`, template `app/views/about/about.tpl.html`, controller `AboutController`)
**Functional-spec UI Inventory line:** "`root.about` | `about.tpl.html` | `AboutController` | Controller file exists but is an **empty placeholder** (`about.controller.js`: `// code goes here`); template not opened" — this run additionally opened `about.tpl.html` directly (its entire content is the literal word "about") and confirmed `AboutController` is registered nowhere in the codebase and `about.config.js`/`about.controller.js`/`about.service.js` are not in `index-dev.html`'s script list at all — see Named Gaps and PQ-003.
**Navigation in:** No `ui-sref`/button anywhere in `Client/app/views/**` targets `root.about` — only reachable by directly entering the `/about` URL.
**Navigation out:** none evidenced — the template has no links or controls.

**Layout structure:**
- The entire content region is the literal text "about" — `about.tpl.html:1`.

**Widgets:**

| Field / control | Widget type | Domain-model ref | Citation |
|---|---|---|---|
| — | label/static text | — | `about.tpl.html:1` |

**States evidenced in code:** none — see PQ-003 for whether this state renders at all.
**Token groups referenced:** TK-001, TK-002, TK-003

### SCR-020 — Contact

⚠ PROVISIONAL — pending PQ-003 (proposed default: treat as SCOPE/TARGET-GAP, not a screen requiring pixel-fidelity capture)

**Purpose:** Inference: a static informational placeholder page.
**Defined in:** `Client/app/scripts/app.config.js:81-89` (state `root.contact`, template `app/views/contact/contact.tpl.html`, controller `ContactController`)
**Functional-spec UI Inventory line:** "`root.contact` | `contact.tpl.html` | `ContactController` | **Neither found** — `app.config.js` names this controller but no `contact.controller.js`/equivalent script exists under `Client/app/scripts/` in this run" — this run additionally opened `contact.tpl.html` directly (its entire content is the literal word "Contact") and independently re-confirmed no `ContactController` definition exists anywhere in the repo — see Named Gaps and PQ-003.
**Navigation in:** No `ui-sref`/button anywhere in `Client/app/views/**` targets `root.contact` — only reachable by directly entering the `/contact` URL.
**Navigation out:** none evidenced — the template has no links or controls.

**Layout structure:**
- The entire content region is the literal text "Contact" — `contact.tpl.html:1`.

**Widgets:**

| Field / control | Widget type | Domain-model ref | Citation |
|---|---|---|---|
| — | label/static text | — | `contact.tpl.html:1` |

**States evidenced in code:** none — see PQ-003 for whether this state renders at all.
**Token groups referenced:** TK-001, TK-002, TK-003

## Widget Cross-Reference Findings

1. **Domain field with no widget on any screen: `Appointment.DoctorId`.** `domain-model.md` documents this as a required FK to `Doctor`, but no picker/select for it exists anywhere in `Client/app/views/patient/patient-appointment.tpl.html` — the value is hardcoded client-side to a single doctor's GUID and never exposed to the user. Domain-model side citation: `domain-model.md`'s Appointment entity entry (`DM.Models/Appointment.cs`). UI side citation confirming the absence: `Client/app/scripts/patient/patient-appointment.controller.js:10` (`DoctorId: "9b6ba3ad-c9be-e511-9bf4-402cf40f4b2f"`); no other file under `Client/app/views/patient/` or `Client/app/scripts/patient/` references `DoctorId`. Matches functional-spec Named Gap #1.
2. **Domain entity with no widget on any screen: `Doctor`.** `domain-model.md` documents `Doctor` (Code/Name/Phone) as a full entity with its own repository/service, but no view template anywhere under `Client/app/views/**` lists, creates, edits, or deletes a `Doctor` row — confirmed by the absence of any `doctor/` folder under `Client/app/views/` or `Client/app/scripts/` in this run's directory listing. Domain-model side citation: `domain-model.md`'s Doctor entity entry (`DM.Models/Doctor.cs`). UI side citation confirming the absence: no matching file found under `Client/app/views/**`/`Client/app/scripts/**`.
3. **Widget/domain cardinality mismatch: `ApplicationUser.Roles`.** `domain-model.md` documents `Roles` as a collection (`via IdentityUserRole`), implying a user can hold more than one role, but the Manage Users screen's Role field is a single `<select>` bound to the singular `model.RoleId` — the UI can only ever assign exactly one role per user. Domain-model side citation: `domain-model.md`'s ApplicationUser entity entry. UI side citation: `Client/app/views/user/user.tpl.html:69`.
4. **Widget with no domain-model field: "Remember me" (Login screen).** `login.tpl.html:42` binds a checkbox to `ng-model="isRemebered"`, but no entity in `domain-model.md` has a corresponding field, and `isRemebered` is never read anywhere else in `Client/app/scripts/auth/**` (confirmed by grep) — an apparently dead/no-op widget. UI side citation: `login.tpl.html:42`.

## Named Gaps

1. **`Client/app/views/auth/denied.tpl.html` is an orphaned, unrouted template**, distinct from the actually-wired `access-denied.tpl.html` (SCR-018). It references a function `backToDefaultRoute()` that is defined nowhere in the codebase (confirmed by grep across `Client/app/scripts/**`), and no `$stateProvider.state(...)` registration anywhere references `denied.tpl.html`. This is a new finding beyond `functional-spec.md`'s own Named Gaps list, not previously documented.
2. **The About/Contact controller wiring is broken in a way `functional-spec.md` did not fully characterize**: neither `about.config.js`/`about.controller.js`/`about.service.js` nor any contact script appear in `Client/index-dev.html`'s `<script>` list at all (lines 58-94), and `about.controller.js`'s only content is a comment, never an `angular.module(...).controller("AboutController", ...)` call. Whether navigating to `root.about`/`root.contact` throws a blocking AngularJS DI error or still renders the plain-text template underneath was not established from static reading — raised as PQ-003, and both SCR-019/SCR-020 are marked PROVISIONAL and listed under "Not Capturable" in the screenshot checklist.
3. **Which navbar background color actually renders — `style.css`'s `.navbar` (`#006A4E`) or `nav.html`'s own inline `!important` `.navbar-default` (`#218283`)** — could not be confirmed without visual observation of a running instance, despite a plausible CSS-cascade argument favoring the latter. Raised as PQ-004; TK-003 in `design-tokens.json` is marked PROVISIONAL against it.
4. **`style.css`'s `.footer` rule (`background-color: #006A4E`, `style.css:56-67`) and its `.navbar-inverse` rules (`style.css:27-41`) both appear to be dead CSS.** No element under `Client/app/views/**` carries `class="footer"` (confirmed by grep — `footer.html`'s own element uses `class="nav navbar navbar-fixed-bottom"` with an inline `style` attribute instead, which unambiguously wins the cascade for that element regardless of source order), and no element carries `class="navbar-inverse"` (`nav.html` uses `navbar-default`, not `navbar-inverse`). Both rules were likely inherited from a Bootstrap-admin-theme starter template and never actually exercised.
5. **Materialize CSS/JS (`Client/Content/materialize/**`, `Client/Scripts/materialize/**`) is vendored on disk but never loaded** — absent from `index-dev.html`'s `<link>`/`<script>` list, `index-prod.html`'s bundle references, and every source array in `Gulpfile.js` (`getVendorCssSources`, `getVendorJsSources`, `getThemeCssSources`, `getThemeJsSources`). Confirmed inactive, not merely unexamined.
6. **`Client/app/scripts/patient/patient-service.controller.js` was not opened line-by-line this run** (SCR-006's `edit-mode` state is confirmed reachable only via a grep match on `isUpdateMode = true`, not a full read) — its widget/state citations rely on the fully-opened `patient-service.tpl.html` template plus that one grep confirmation, consistent with `functional-spec.md`'s own Named Gap #12 about templates/controllers not fully opened.
7. **`patient-detail.tpl.html:258`'s "Update" (payment) button can never be shown.** Its guard, `ng-show="isUpdateMode"`, depends on `isUpdateMode` ever being set `true`, but the only code path that does so — `editPayment(payment)` — is entirely commented out in `Client/app/scripts/patient/patient-detail.controller.js:182-193`. This is dead, unreachable UI, not a capturable state (excluded from SCR-004's evidenced states above and from the screenshot checklist).
8. **`stock.tpl.html:55-56`'s "Stock Type" radio pair carries a static `checked="checked"` attribute on the "Shipped" option, but `stock.controller.js:8`'s `init()` sets the bound model (`stock.StatusId`) to `0`**, which matches neither radio's `value` (`3`/`4`). Under AngularJS's `ng-model` binding, the live rendered default-checked state (both unchecked, vs. the HTML's own static "Shipped" default) depends on Angular's own directive-priority/render-order behavior, which was not verified against a running instance — this is a computed/effective-state question the codebase alone does not pin down, so no default-selected state is asserted here.
9. **`stock-report.tpl.html:30-40`'s Product/Status filter selects are commented out in the markup**, even though `stock-report.controller.js:8-19` still builds and maintains their option data (`$scope.status`, and product names via `getProductsName()`) — dead UI-adjacent code, consistent with the template/controller drift already flagged generally in `functional-spec.md`.
10. **`resource.tpl.html`'s "Public" radio group's effective persisted value** (native `value="0"/"1"` vs. Angular `ng-value="isPublicEnum.False/True"`, both bound to the same `ng-model="model.IsPublic"`) is not resolvable from static markup alone — matches `functional-spec.md` Named Gap #8; no new PQ raised here since that finding already stands and does not block a widget-type or layout citation (the radio-pair widget type itself is unambiguous).
