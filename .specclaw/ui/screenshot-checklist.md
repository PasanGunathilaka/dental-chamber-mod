# Screenshot Capture Checklist: Dental Management System (DentalManagement.sln)

**Path analyzed:** C:\Learnings\Projects\legacy\dental-chamber\Source\App
**Date generated:** 2026-08-12
**Legacy commit at design time:** 5ff87d3
**Screens to capture:** 18 (2 of the 20 documented screens — SCR-019 About, SCR-020 Contact — are listed under Not Capturable instead) · **Rows (screen × state):** 37

<!--
  NOTE ON THIS COMMENT: never write a literal double-brace placeholder
  token inside this comment's own prose — filling this template is a dumb
  global string replace and would corrupt the comment.
-->

## Capture Checklist

| SCR | Screen | State | Target file | Setup notes | Captured? |
|---|---|---|---|---|---|
| SCR-001 | Login | default | screens/SCR-001.png | Log out (or open in a fresh/incognito session) so the SPA redirects to `root.login` — `Client/app/scripts/app.config.js:43-46`. | [ ] |
| SCR-002 | Patient List | default | screens/SCR-002.png | Log in with any seeded account (e.g. `superadmin`/`123qwe` per `DM.Server/Migrations/Configuration.cs`'s `AddUsers()`, cited in `pending-questions.md`/`functional-spec.md` Named Gap #9) — every valid role lands here per `Client/app/scripts/app.service.js:14-24`. Seed at least one `Patient` row so the grid is non-empty. | [ ] |
| SCR-003 | Register Patient / Add Services to Bill | default | screens/SCR-003.png | From the Patient List, click "+ Patient" (`patient.tpl.html:19`) — renders the blank New Patient form (`pageName == 'new-patient'`). | [ ] |
| SCR-003 | Register Patient / Add Services to Bill | add-services | screens/SCR-003-add-services.png | Submit the New Patient form successfully (`patient-create.controller.js:81-98`) — the screen auto-switches to the Add Services panel with at least one catalog `MedicalService` seeded so the checkbox table has rows. | [ ] |
| SCR-004 | Patient Detail | default | screens/SCR-004.png | From the Patient List, click a patient row's Id (`patient.controller.js:76-84`) — lands on the "Services" tab (`pageName == 'services'`, default). Use a patient with at least one billed service line item for a non-empty table. | [ ] |
| SCR-004 | Patient Detail | medical | screens/SCR-004-medical.png | On Patient Detail, click the "Medical Condition" tab button (`patient-detail.tpl.html:83`). | [ ] |
| SCR-004 | Patient Detail | payment | screens/SCR-004-payment.png | On Patient Detail, click the "Payment" tab button (`patient-detail.tpl.html:85`). Use a patient with at least one recorded payment for a non-empty history table. | [ ] |
| SCR-004 | Patient Detail | history | screens/SCR-004-history.png | On Patient Detail, click the "History" tab button (`patient-detail.tpl.html:87`). Use a patient who has closed at least one prior bill (via "New Bill") for a non-empty history table. | [ ] |
| SCR-004 | Patient Detail | patient | screens/SCR-004-patient.png | On Patient Detail, click the "Patient" tab button (`patient-detail.tpl.html:89`). | [ ] |
| SCR-004 | Patient Detail | payment-exceeds-due | screens/SCR-004-payment-exceeds-due.png | On the Payment tab, enter an Amount greater than the current `patientPrescription.TotalDue` and submit — triggers the client-side toast warning without an HTTP call (`patient-detail.controller.js:149-150`). | [ ] |
| SCR-004 | Patient Detail | bill-close-blocked-due | screens/SCR-004-bill-close-blocked-due.png | On the Payment tab, with `TotalDue > 0`, click "New Bill" — triggers the client-side toast error blocking the close (`patient-detail.controller.js:257-258`). ("Force New Bill" bypasses this guard — do not use it for this row.) | [ ] |
| SCR-005 | Medical Condition Catalog | default | screens/SCR-005.png | From the Patient List, click "Medical" (`patient.tpl.html:20`). Seed at least one catalog `MedicalInfo` row for a non-empty list. | [ ] |
| SCR-005 | Medical Condition Catalog | edit-mode | screens/SCR-005-edit-mode.png | On the Medical Condition Catalog, click a row's edit icon (`patient-info.tpl.html:57`) — sets `isUpdateMode = true` (`patient-info.controller.js:59-62`), swapping Save for Update. | [ ] |
| SCR-006 | Service Catalog | default | screens/SCR-006.png | From the Patient List, click "Service" (`patient.tpl.html:21`). Seed at least one catalog `MedicalService` row for a non-empty list. | [ ] |
| SCR-006 | Service Catalog | edit-mode | screens/SCR-006-edit-mode.png | On the Service Catalog, click a row's edit icon (`patient-service.tpl.html:65`) — swaps Save for Update (grep-confirmed `isUpdateMode = true` in `patient-service.controller.js`; that file was not opened line-by-line this run — see Named Gap 6 in `ui-inventory.md`). | [ ] |
| SCR-007 | Patient Payment Report | default | screens/SCR-007.png | From the Patient List, click "Report" (`patient.tpl.html:23`). Use a patient with at least one payment inside the default filter's date range for a non-empty report table. | [ ] |
| SCR-008 | Appointments | default | screens/SCR-008.png | From the Patient List, click "Appoinment" [sic] (`patient.tpl.html:22`) — renders the blank appointment form. Seed at least one appointment for the current filter date so the table is non-empty. | [ ] |
| SCR-008 | Appointments | edit-mode | screens/SCR-008-edit-mode.png | On Appointments, click a row's edit icon (`patient-appointment.tpl.html:122`) — sets `isUpdateMode = true` (`patient-appointment.controller.js:131-134`), swapping Save for Update. | [ ] |
| SCR-009 | Dashboard (Product/Stock Hub) | default | screens/SCR-009.png | From the Patient List, click "Stock" (`patient.tpl.html:24`). Seed at least one `Product` row for a non-empty grid. | [ ] |
| SCR-010 | Product Catalog | default | screens/SCR-010.png | From Dashboard, click "Product" (`dashboard.tpl.html:26`). Seed at least one `Product` row for a non-empty list. | [ ] |
| SCR-010 | Product Catalog | edit-mode | screens/SCR-010-edit-mode.png | On Product Catalog, click a row's edit icon (`product.tpl.html:109`) — sets `isUpdateMode = true` (`product.controller.js:48-58`), swapping Save for Update. | [ ] |
| SCR-011 | Stock / Inventory Movement | default | screens/SCR-011.png | From Dashboard, click "Inventory" (`dashboard.tpl.html:25`). Seed at least one `Product` (for the Product Name dropdown) and one prior `Inventory` movement for a non-empty history table. | [ ] |
| SCR-011 | Stock / Inventory Movement | edit-mode | screens/SCR-011-edit-mode.png | On Stock, click a movement row's edit icon (template edit affordance not directly cited in `stock.tpl.html` this run — reached via `stock.controller.js:166-168`'s `edit(id)`, which calls `getInventoryById()` and sets `isUpdateMode = true` at lines 159/162). | [ ] |
| SCR-011 | Stock / Inventory Movement | shipment-exceeds-onhand | screens/SCR-011-shipment-exceeds-onhand.png | On Stock, select a product, choose "Shipped", enter a quantity greater than that product's current On Hand, and submit — triggers the client-side alert without an HTTP call (`stock.controller.js:111-112`). | [ ] |
| SCR-012 | Stock Report | default | screens/SCR-012.png | From Dashboard, click "Report" (`dashboard.tpl.html:27`). Seed at least one product with stock movements inside the default date range for a non-empty report table. | [ ] |
| SCR-013 | Manage Users | default | screens/SCR-013.png | Log in as an Admin or SystemAdmin account (nav-bar admin dropdown gated by `isAdminUser \|\| isSystemAdminUser`, `nav.html:55,58`), then click "Manage User". | [ ] |
| SCR-013 | Manage Users | edit-mode | screens/SCR-013-edit-mode.png | On Manage Users, click a row's edit icon (`user.tpl.html:145`) — sets `isUpdateMode = true` (`user.controller.js:74-77`). | [ ] |
| SCR-013 | Manage Users | password-fields-visible | screens/SCR-013-password-fields-visible.png | On Manage Users (create or edit mode), check the "Change Password" checkbox (`user.tpl.html:79`) — reveals New/Retype Password fields (`user.tpl.html:83,90`). | [ ] |
| SCR-014 | Manage Roles | default | screens/SCR-014.png | Log in as SystemAdmin (nav-bar dropdown item gated by `isSystemAdminUser`, `nav.html:59-60`), then click "Manage Role". Seed at least one role beyond the defaults for a non-trivial list. | [ ] |
| SCR-014 | Manage Roles | edit-mode | screens/SCR-014-edit-mode.png | On Manage Roles, click a row's edit icon (`role.tpl.html:61`) — sets `isUpdateMode = true` (`role.controller.js:51-54`). | [ ] |
| SCR-015 | Manage Resources | default | screens/SCR-015.png | Log in as SystemAdmin, then click "Manage Resource" (`nav.html:61`). | [ ] |
| SCR-015 | Manage Resources | edit-mode | screens/SCR-015-edit-mode.png | On Manage Resources, click a row's edit icon (`resource.tpl.html:102`) — sets `isUpdateMode = true` (`resource.controller.js:51-54`). | [ ] |
| SCR-016 | Manage Permissions | default | screens/SCR-016.png | Log in as SystemAdmin, then click "Manage Permission" (`nav.html:62`) — no role yet selected. | [ ] |
| SCR-016 | Manage Permissions | role-selected | screens/SCR-016-role-selected.png | On Manage Permissions, click a role row in the left-hand list (`permission.tpl.html:25`) — reveals the Cancel button, "Selected Role" label, and that role's existing grants (`permission.controller.js:68-79`). | [ ] |
| SCR-017 | User Profile & Change Password | default | screens/SCR-017.png | Log in as any non-demo account, then click "Profile" in the nav-bar user dropdown (`nav.html:47`). | [ ] |
| SCR-017 | User Profile & Change Password | demo-user-restricted | screens/SCR-017-demo-user-restricted.png | Log in as a demo account (`auth.service.js:117-123`: `demo`, `demo-admin`, `demo-doctor`, or `demo-inventory`), then open Profile — both Update buttons are replaced by a warning message (`profile.tpl.html:53-54,95-96`). | [ ] |
| SCR-018 | Access Denied | default | screens/SCR-018.png | Log in as a role with no `Permission` grant for some private `Resource` (per DR-015/DR-016, every non-SystemAdmin seeded role starts with zero grants), then navigate to that resource's route — redirected here by `app.config.js:19-23,27-29`. | [ ] |

## Setup Prerequisites

- A running instance of the legacy application reachable at the URL configured in `Client/app/scripts/app.service.js:47` (`http://localhost:51633/` by default) — the SPA's `UrlService` hardcodes this base URL, so the backend must be running at that address (or the human capturer must adjust it) for any screen beyond the bare login form to load data.
- A seeded database with at least the default migration seed data — `DM.Server/Migrations/Configuration.cs`'s `AddUsers()`/`AddRoles()`/`AddStatus()`/`AddDoctor()` seed the two accounts (`superadmin`, `admin`, both password `123qwe` per `functional-spec.md` Named Gap #9), the eight role names, the shared `Status` lookup rows, and the single seeded `Doctor`.
- Additional seed data for non-empty grids/tables in several rows above: at least one `Patient` (with a current active `Prescription`, at least one billed service, one payment, and one closed prior bill for the History tab), at least one catalog `MedicalService` and `MedicalInfo` row, at least one `Product` with at least one `Inventory` movement, and at least one `Appointment` on the default filter date. None of these are provided by the default migration seed (`Configuration.cs` seeds only Users/Roles/Status/Doctor/Resources/Permissions, not domain data) — a human must create them via the running app itself before capturing.
- A desktop-width browser window (Bootstrap's `col-lg-*`/`col-md-*` classes drive most of the layout bullets documented in `ui-inventory.md`; capturing at a narrower width will trigger Bootstrap's responsive column-stacking behavior, which is a different, uncited rendering this checklist does not cover).
- For SCR-013 (Manage Users): sign in as an Admin- or SystemAdmin-role account. For SCR-014/SCR-015/SCR-016 (Manage Roles/Resources/Permissions): sign in as a SystemAdmin-role account specifically — the nav-bar links to these three are hidden for Admin (`nav.html:59-62`).

## Not Capturable

1. **SCR-019 (About)** and **SCR-020 (Contact)** — `⚠ PROVISIONAL — pending PQ-003`. Neither `AboutController` nor `ContactController` is registered by any script actually loaded by the application (`about.controller.js` contains only a comment and is not in `Client/index-dev.html`'s script list at all; no `ContactController` definition exists anywhere in the repository). Attempting to navigate to `root.about`/`root.contact` may throw an AngularJS dependency-injection error rather than rendering the underlying plain-text template — whether the text becomes visible despite that error was not established without running the app. A human attempting to capture these should first resolve PQ-003 (or note in `decisions.md` that these screens are out of scope) rather than treating a failed capture attempt as this checklist's error.
