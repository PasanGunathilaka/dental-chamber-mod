# Baseline Scenarios: Dental Management System (DentalManagement.sln)

**Date generated:** 2026-08-12
**Grounded in:** .specclaw/analysis/domain-model.md's numbered Business Rules, cross-referenced against .specclaw/analysis/module-map.md (Status: PROPOSED — awaiting human confirmation), .specclaw/analysis/functional-spec.md, and .specclaw/baseline/seams.md. `rebuild-backlog.md` does not exist yet, so every scenario's "Verifies backlog item" reads "not yet backlog-linked."

This is a first-ever design run — `prior_scenarios` was empty, so every id below is freshly assigned starting at GM-001; nothing is carried forward or reconciled. `module-map.md`'s Status is PROPOSED (not yet confirmed by a human), so every `Modules` tag below should be read as provisional to that document's own confirmation, not to any PQ/CQ.

## Scenarios

### GM-001 — Patient code format, boundary of the 6-digit zero-pad rollover

- **Seam:** `DM.RequestModels/HelperRequestModel.GetThisPatientCode(string right)` (`DM.RequestModels/HelperRequestModel.cs:29-40`)
- **Seam layer:** pure-function
- **Modules:** MOD-001
- **Business rules pinned:** rule 1 (DR-001)
- **Arrange:** none — pure function, no DB, no clock.
- **Act:** call `GetThisPatientCode` with `right` = `"1"`, `"999999"` (exactly 6 digits, no padding needed), `"9999999"` (7 digits — the loop condition `i < 6 - len` is never true for `len=7`, so no padding is added and the result is 8 characters, exactly at `Patient.Code`'s `[StringLength(8, MinimumLength=7)]` ceiling), and `"99999999"` (8 digits, producing a 9-character code that would exceed the `StringLength(8)` ceiling if it were ever posted — this scenario only asserts the raw string the function produces, not what a later model-validation step would do with it).
- **Assert (shape):** `output.results[*].input_right`, `output.results[*].generated_code`, `output.results[*].generated_code_length` — the exact zero-padded string for each input, and its length, so the 7-char/8-char/9-char boundary is pinned precisely.
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-002 — Patient creation silently "succeeds" over a Code collision caused by a prior manual Code edit

- **Seam:** `DM.Server/Controllers/PatientCreateController.cs:50-79`'s `Post(Patient patient)`
- **Seam layer:** service
- **Modules:** MOD-001
- **Business rules pinned:** rule 1 (DR-001) ⚠ PROVISIONAL — pending PQ-005 (proposed default: treat as a DEFECT to fix in the rebuild)
- **Arrange:** create Patient A via a normal `Post` (auto-generated `Code = "P000001"`). `PUT`-edit Patient A's own `Code` field to `"P000002"` (the exact value the count-based formula will generate next, since exactly one patient row exists) — `PatientCreateController.Put` performs no uniqueness pre-check beyond `ModelState.IsValid`.
- **Act:** `Post` a new Patient B.
- **Assert (shape):** `output.http_status` (expect 200 per the unconditional `return Ok(patient.Id)`), `output.outcome` (`"OK"` from the controller's own perspective — it never inspects `add`), `output.patient_b_persisted` (boolean — did a second row with `Code = "P000002"` actually land in the database, or did the unique index reject the insert?), `output.patient_b_id_returned` — the id the controller returned regardless. The scenario's entire point is to capture whether `output.outcome`/`output.http_status` diverge from `output.patient_b_persisted`.
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-003 — New patient is auto-provisioned with an initial Active bill

- **Seam:** `DM.Server/Controllers/PatientCreateController.cs:50-79`'s `Post(Patient patient)`
- **Seam layer:** service
- **Modules:** MOD-001
- **Business rules pinned:** rule 2 (DR-002)
- **Arrange:** empty `Prescriptions` table for the new patient (true of any brand-new patient).
- **Act:** `Post` a new Patient.
- **Assert (shape):** `output.patient.code`, `output.prescription_created` (boolean), `output.prescription.status_id` (expect `5`), `output.prescription.code_format_matches_bill_pattern` (boolean, since the raw generated `Code` value itself is covered by GM-004, not re-asserted here), `output.prescription.total_due` (expect `0`). `normalized_fields`: `output.patient.id`, `output.prescription.id`, `output.patient.created`, `output.patient.last_update`, `output.prescription.created`, `output.prescription.last_update` (per seams.md's Capture Blocker #1).
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-004 — Bill code format, boundary of the 3-digit zero-pad rollover

- **Seam:** `DM.RequestModels/HelperRequestModel.GenerateBillCode(string patientCode, string right)` (`HelperRequestModel.cs:12-24`)
- **Seam layer:** pure-function
- **Modules:** MOD-001
- **Business rules pinned:** rule 3 (DR-003)
- **Arrange:** none.
- **Act:** call `GenerateBillCode("P000001", right)` with `right` = `"1"`, `"999"` (exactly 3 digits, no padding needed), `"1000"` (4 digits — no padding added, so the visual "3-digit" convention is not actually enforced as a ceiling).
- **Assert (shape):** `output.results[*].input_right`, `output.results[*].generated_code` — exact string for each, e.g. `"BILL001-P000001"`, `"BILL999-P000001"`, `"BILL1000-P000001"`.
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-005 — Discount percent outside 0–100 is accepted server-side

- **Seam:** `DM.Service` (`BaseService<Prescription>.Edit`) via `DM.Server/Controllers/PrescriptionController.cs:58-61`'s `Put`
- **Seam layer:** service
- **Modules:** MOD-001
- **Business rules pinned:** rule 4 (DR-004)
- **Arrange:** an existing Active Prescription with `DiscountPercent = 0`.
- **Act:** `Edit` the Prescription with `DiscountPercent = 150` (and, in a second sub-case, `-25`).
- **Assert (shape):** `output.outcome` (expect `"OK"`, `threw: false`), `output.error_code` (expect `null`), `output.prescription.discount_percent` (expect the out-of-range value persisted verbatim, `150`/`-25`) — capturing the CQ-011-documented server-side gap directly: the value the client's `calculateDiscount()` (`Client/app/scripts/patient/patient-create.controller.js:100-102`) would block is accepted unconditionally here.
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-006 — A payment exceeding the bill's due amount is accepted server-side

- **Seam:** `DM.Service` (`BaseService<Payment>.Add`) via `DM.Server/Controllers/PaymentController.cs:41-49`'s `Post`
- **Seam layer:** service
- **Modules:** MOD-001
- **Business rules pinned:** rule 5 (DR-005)
- **Arrange:** an Active Prescription with `TotalDue = 50`.
- **Act:** `Add` a Payment with `Amount = 500`.
- **Assert (shape):** `output.outcome` (expect `"OK"`), `output.payment.amount` (expect `500`, persisted verbatim), `output.prescription_totals_recomputed` (boolean — expect `false`, since nothing server-side recomputes `Prescription.TotalPaid`/`TotalDue` from the `Payments` collection; that recomputation is entirely the client's own follow-up `PUT`, per `functional-spec.md`'s "Record Payment Against Bill" workflow, not this seam) — capturing the CQ-011-documented gap that `Client/app/scripts/patient/patient-detail.controller.js:149-150`'s guard is the only check that exists.
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-007 — A bill can be closed and reopened while due > 0, with no "Force" distinction server-side

- **Seam:** `DM.Service` (`BaseService<Prescription>.Edit`) via `DM.Server/Controllers/PrescriptionController.cs:58-61`'s `Put`, then `BaseService<Prescription>.Add` via `PrescriptionController.cs:45-54`'s `Post`
- **Seam layer:** service
- **Modules:** MOD-001
- **Business rules pinned:** rule 6 (DR-006)
- **Arrange:** an Active Prescription with `TotalDue = 100`.
- **Act:** `Put` the Prescription with `StatusId = 6` (Closed), then `Post` a new Prescription for the same patient with `StatusId = 5`, all totals zeroed — the exact two-call sequence `generatePatientBill()` performs (`patient-detail.controller.js:232-253`), run against the server with no client-side gate in front of it (i.e. simulating what "Force New Bill" does today, and what plain "New Bill" would also do if its client-side check were bypassed, since the server makes no distinction between the two).
- **Assert (shape):** `output.old_bill.status_id` (expect `6`), `output.old_bill.total_due_at_close` (expect `100`, unchanged/unenforced), `output.new_bill_created` (boolean, expect `true`), `output.new_bill.total_due` (expect `0`) — capturing that the server accepts the close-with-due-balance sequence unconditionally, matching CQ-011's finding for this rule.
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-008 — Closing a bill immediately opens a new one (happy path)

- **Seam:** same two calls as GM-007
- **Seam layer:** service
- **Modules:** MOD-001
- **Business rules pinned:** rule 7 (DR-007)
- **Arrange:** an Active Prescription with `TotalDue = 0`.
- **Act:** the same `generatePatientBill()` two-call sequence.
- **Assert (shape):** `output.old_bill_status_closed` (boolean), `output.new_bill_status_active` (boolean), `output.new_bill_totals_zeroed` (boolean) — boolean assertions per `CONTRACT.md` (k)'s convention for a workflow with a real identity/sequencing character, rather than comparing raw generated ids. `normalized_fields`: `output.old_bill.id`, `output.new_bill.id`, `output.old_bill.last_update`, `output.new_bill.created`, `output.new_bill.last_update`.
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-009 — Saving a bill's service list deletes the prior list scoped only to the first submitted item's PrescriptionId

- **Seam:** `DM.Service/PatientMedicalServiceService.cs:24-36`'s `AddList`, wrapping `DM.Repository/PatientMedicalServiceRepository.cs:25-38`'s `AddList`
- **Seam layer:** service
- **Modules:** MOD-001
- **Business rules pinned:** rule 18 (DR-018)
- **Arrange:** Prescription A has 2 existing `PatientMedicalService` rows; Prescription B (different patient/bill) has 1 existing row.
- **Act:** call `AddList` with a list whose **first** item's `PrescriptionId` is Prescription B, followed by items for Prescription A (a mixed batch — something no real caller sends today, per `DM.Server/Controllers/PatientMedicalServiceController.cs:36-45`'s single caller always submitting one `PrescriptionId`, but the type system does not prevent it, per DR-018's own text).
- **Assert (shape):** `output.prescription_a_prior_rows_survived` (boolean, expect `true` — never targeted, since the `foreach`+`break` at `PatientMedicalServiceRepository.cs:27-33` only ever deletes rows for the **first** item's `PrescriptionId`), `output.prescription_b_prior_rows_deleted` (boolean, expect `true`), `output.final_row_count_for_prescription_a`, `output.final_row_count_for_prescription_b` — pinning the exact scope of the delete precisely.
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-010 — Saving an empty service list is a silent no-op, not a clear

- **Seam:** same as GM-009
- **Seam layer:** service
- **Modules:** MOD-001
- **Business rules pinned:** rule 18 (DR-018)
- **Arrange:** Prescription A has 2 existing `PatientMedicalService` rows.
- **Act:** call `AddList([])` (an empty list) for Prescription A.
- **Assert (shape):** `output.outcome` (expect `"OK"`, `threw: false` — the empty list does not raise, per `PatientMedicalServiceRepository.cs:25-38`'s `foreach` over an empty collection never executing its body), `output.prescription_a_rows_after_call` (expect `2`, unchanged) — pinning that an empty submission leaves the prior list fully intact rather than clearing it, the direct boundary contrast to GM-009's deletion behaviour.
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-011 — Two independently-coexisting "current bill" mechanisms disagree once a patient has no Active prescription

- **Seam:** `DM.Server/Controllers/PatientController.cs:29-56`'s `Get()` (its per-patient prescription lookup) and `DM.Server/Controllers/PrescriptionController.cs:39-43`'s `GetPatientCurrentPrescription`
- **Seam layer:** service
- **Modules:** MOD-001
- **Business rules pinned:** no numbered rule (directly follows from DR-002's/DR-007's documented un-transacted two-write workflow) ⚠ PROVISIONAL — pending PQ-008 (proposed default: standardize on the `StatusId == 5` filter everywhere)
- **Arrange:** a patient whose only two Prescriptions are both `StatusId = 6` (Closed) — simulating the documented failure mode where a "close bill" write succeeded but the "open new bill" write never completed.
- **Act:** call both (a) the per-patient lookup `_prescriptionService.GetPatientCurrentPrescription(patientId).Last()` (the mechanism `PatientController.Get()`/`Search()` use) and (b) `_prescriptionService.GetPatientCurrentPrescription(patientId).LastOrDefault(x => x.StatusId == 5)` (the mechanism `PrescriptionController.GetPatientCurrentPrescription` uses) for the same patient.
- **Assert (shape):** `output.mechanism_a_result_is_null` (boolean, expect `false` — it returns the last Closed bill), `output.mechanism_a_result_status_id` (expect `6`), `output.mechanism_b_result_is_null` (boolean, expect `true`), `output.mechanisms_agree` (boolean, expect `false`) — the scenario's entire point is pinning that these two disagree today.
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-012 — Deleting a Patient cascades to delete every Prescription, PatientMedicalService, and Payment row for that patient

- **Seam:** `DM.Server/Controllers/PatientCreateController.cs:91-96`'s `Delete`, observed at the EF6/SQL Server cascade-delete configuration
- **Seam layer:** persistence
- **Modules:** MOD-001
- **Business rules pinned:** no numbered rule (cascade/delete-rule behaviour per the explicit instruction to capture every reachable cascade finding)
- **Arrange:** a Patient with 2 Prescriptions, each with 1 `PatientMedicalService` row and 1 `Payment` row (4 total dependent rows).
- **Act:** `Delete` the Patient.
- **Assert (shape):** `output.patient_deleted` (boolean), `output.prescriptions_remaining_count` (expect `0`), `output.patient_medical_services_remaining_count` (expect `0`), `output.payments_remaining_count` (expect `0`) — pinning the full multi-level cascade chain (`Patient` → `Prescription` → `PatientMedicalService`/`Payment`) confirmed at `DM.Models/Prescription.cs:20-21`, `PatientMedicalService.cs:16-17`, `Payment.cs:13-14`.
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-013 — Duplicate MedicalService.Name is rejected at the service layer, cause indistinguishable

- **Seam:** `DM.Service` (`BaseService<MedicalService>.Add`) via `DM.Server/Controllers/MedicalServiceController.cs:44-52`'s `Post`
- **Seam layer:** service
- **Modules:** MOD-002
- **Business rules pinned:** rule 17 (DR-017)
- **Arrange:** an existing `MedicalService` named `"Scaling"`.
- **Act:** `Add` a new `MedicalService` also named `"Scaling"`.
- **Assert (shape):** `output.outcome` (expect `"REJECTED"`), `output.threw` (expect `false` — `BaseService<T>.Add`'s `catch (Exception)` at `DM.Service/BaseService.cs:40-55` swallows the underlying `DbUpdateException` and returns `false`), `output.error_code` (this fixture cannot distinguish "duplicate name" from any other failure cause at this layer — see seams.md Capture Blocker #5; `error_code` mapping is a harness-mode/`error-map.md` concern, not resolved here).
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-014 — Duplicate MedicalService.Name raises the raw unique-constraint exception at the persistence layer

- **Seam:** direct repository/`DbContext` call bypassing `BaseService<T>.Add`'s catch — `DM.Repository/BaseRepository.cs:36-54`'s `Add`+`Commit`, against `DM.Models/MedicalService.cs:20`'s `[Index("IX_Name", IsUnique = true)]`
- **Seam layer:** persistence
- **Modules:** MOD-002
- **Business rules pinned:** rule 17 (DR-017)
- **Arrange:** an existing `MedicalService` named `"Scaling"`.
- **Act:** call the repository's `Add` + `Commit` directly (no service-layer try/catch in front) with a new `MedicalService` also named `"Scaling"`.
- **Assert (shape):** `output.outcome` (expect `"REJECTED"`), `output.threw` (expect `true`), `output.ExceptionType`, `output.InnerExceptionType`, `output.ExceptionMessage`, `output.InnerExceptionMessage` (the four representation-class fields per `CONTRACT.md` (b.2) — recorded as evidence, expected to be `DbUpdateException` wrapping a `SqlException`/unique-constraint violation, but not asserted as behaviour).
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-015 — Duplicate MedicalInfo.Name is rejected at the service layer

- **Seam:** `DM.Service` (`BaseService<MedicalInfo>.Add`) via `DM.Server/Controllers/MedicalInfoController.cs:45-53`'s `Post`
- **Seam layer:** service
- **Modules:** MOD-002
- **Business rules pinned:** rule 17 (DR-017)
- **Arrange:** an existing `MedicalInfo` named `"Diabetic"`.
- **Act:** `Add` a new `MedicalInfo` also named `"Diabetic"`.
- **Assert (shape):** `output.outcome` (expect `"REJECTED"`), `output.threw` (expect `false`) — same mechanics as GM-013, confirming the identical pattern applies to `MedicalInfo.cs:12`'s own `[Index("IX_Name", IsUnique = true)]`.
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-016 — MedicalService.TotalCharge, integer Charge boundary values

- **Seam:** `DM.Models/MedicalService.cs:36`'s `TotalCharge` computed property
- **Seam layer:** pure-function
- **Modules:** MOD-002
- **Business rules pinned:** rule 19 (DR-019)
- **Arrange:** none — construct `MedicalService` instances directly with `Charge = "10"`, `Quantity` = `0`, `1`, `5` (no validation prevents `Quantity = 0`, since it is `[NotMapped]` with only a default of `1`, `MedicalService.cs:35`).
- **Act:** read `TotalCharge` for each.
- **Assert (shape):** `output.results[*].charge`, `output.results[*].quantity`, `output.results[*].total_charge` — expect `0`, `10`, `50` respectively.
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-017 — MedicalService.TotalCharge, non-integer Charge strings — outcome not yet confirmed

- **Seam:** same as GM-016
- **Seam layer:** pure-function
- **Modules:** MOD-002
- **Business rules pinned:** rule 19 (DR-019)
- **Arrange:** construct `MedicalService` instances with `Charge = "10.50"` (fractional) and `Charge = "abc"` (non-numeric), `Quantity = 1` for both.
- **Act:** read `TotalCharge` for each, capturing whatever actually happens.
- **Assert (shape):** `output.results[*].charge`, `output.results[*].outcome` (`"OK"` or `"REJECTED"`), `output.results[*].threw`, `output.ExceptionType`/`output.ExceptionMessage` when it throws. **Open question, not resolved here:** `domain-model.md`'s DR-019 text describes `Convert.ToInt32(Charge)` as "truncat[ing] any fractional currency amount," but `Convert.ToInt32(string)` resolves to `Int32.Parse`-style parsing in .NET, which (to the best of my knowledge of the framework, not verified by executing this code) rejects a decimal point outright rather than truncating it — i.e. `Charge = "10.50"` may well **throw** `FormatException` rather than truncate to `10`. I cannot execute the legacy app to settle this, per this agent's own constraints, so this scenario is deliberately designed to let the harness's actual capture (Mode B) settle which behaviour is real, rather than asserting either in advance.
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-018 — Saving an empty medical-conditions list crashes instead of clearing the prior tags

- **Seam:** `DM.Service/MedicalInfoService.cs:40-60`'s `SavePatientMedicalInfos`
- **Seam layer:** service
- **Modules:** MOD-002
- **Business rules pinned:** no numbered rule (sibling defect pattern to DR-018) ⚠ PROVISIONAL — pending PQ-006 (proposed default: treat as a DEFECT to fix in the rebuild, consistent with CQ-012's decision for DR-018)
- **Arrange:** a Patient with 2 existing tagged `PatientMedicalInfo` rows.
- **Act:** call `SavePatientMedicalInfos([])` (the empty list produced by unchecking every condition on `patient-detail.tpl.html`'s Medical Condition tab).
- **Assert (shape):** `output.outcome` (expect `"REJECTED"`), `output.threw` (expect `true`), `output.ExceptionType` (expect `InvalidOperationException`, from `.First()` on an empty sequence at `MedicalInfoService.cs:42`), `output.prior_tags_survived` (boolean, expect `true` — nothing was deleted before the crash).
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-019 — Deleting a Patient does not cascade-clean their tagged medical conditions (orphaned, unlike Prescriptions)

- **Seam:** `DM.Server/Controllers/PatientCreateController.cs:91-96`'s `Delete`, observed at the (absent) FK configuration on `PatientMedicalInfo`
- **Seam layer:** persistence
- **Modules:** MOD-001, MOD-002
- **Business rules pinned:** no numbered rule (the direct contrast case to GM-012's cascade finding; tagged with both MOD-001, which owns the `Patient` entity whose delete triggers the act, and MOD-002, which owns the `PatientMedicalInfo` entity whose rows are left behind, since no `DR-###` exists to derive ownership from mechanically for this finding)
- **Arrange:** a Patient with 2 tagged `PatientMedicalInfo` rows.
- **Act:** `Delete` the Patient.
- **Assert (shape):** `output.patient_deleted` (boolean, expect `true`), `output.patient_medical_info_rows_remaining_count` (expect `2`, unchanged — confirmed via `DM.Models/PatientMedicalInfo.cs`'s plain `Guid PatientId`/`MedicalInfoId` with no `[ForeignKey]`/navigation, so EF6 never configures a DB-level relationship to cascade in the first place), `output.orphaned_rows_reference_deleted_patient_id` (boolean, expect `true`).
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-020 — A shipment exceeding on-hand quantity is accepted server-side

- **Seam:** `DM.Service` (`BaseService<Inventory>.Add`) via `DM.Server/Controllers/InventoryController.cs:62-71`'s `Post`
- **Seam layer:** service
- **Modules:** MOD-003
- **Business rules pinned:** rule 8 (DR-008)
- **Arrange:** a Product with `OnHand = 10`.
- **Act:** `Add` an `Inventory` row with `StatusId = 4` (Shipped), `ReceivedOrShippedQuantity = 999`.
- **Assert (shape):** `output.outcome` (expect `"OK"`), `output.inventory.received_or_shipped_quantity` (expect `999`, persisted verbatim), `output.product_onhand_changed` (boolean, expect `false` — this call alone does not touch `Product.OnHand`; that is a separate client-driven `PUT` per DR-009) — capturing the CQ-011-documented gap that `Client/app/scripts/stock/stock.controller.js:111-112`'s alert is the only check that exists.
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-021 — Product totals are persisted exactly as sent, with no server-side recomputation against Inventory history

- **Seam:** `DM.Service` (`BaseService<Product>.Edit`) via `DM.Server/Controllers/ProductController.cs:112-117`'s `Put`
- **Seam layer:** service
- **Modules:** MOD-003
- **Business rules pinned:** rule 9 (DR-009)
- **Arrange:** a Product with `OnHand = 10`, `Received = 20`, `Shipped = 10`, `StatusId = 1` (In Stock) — an internally-consistent starting state.
- **Act:** `Edit` the Product with `OnHand = 9999` (a value not derivable from any real Inventory movement — deliberately inconsistent with the arranged history) and `StatusId = 2` (Out Of Stock, also inconsistent with a positive `OnHand`).
- **Assert (shape):** `output.outcome` (expect `"OK"`), `output.product.on_hand` (expect `9999`, accepted verbatim), `output.product.status_id` (expect `2`, accepted verbatim, no cross-check against `OnHand`'s sign) — pinning that the arithmetic `stock.controller.js`'s `save()` (`stock.controller.js:117-139`) performs client-side is not re-verified or recomputed by the server at all; the server-observable half of DR-009 is a blind accept.
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-022 — Inventory report OnHand fallback: zero movements in the window, a later movement exists within one month after

- **Seam:** `DM.Server/Controllers/InventoryReportController.cs:26-110`'s `GetReport`/`GetOnHand`
- **Seam layer:** service
- **Modules:** MOD-003
- **Business rules pinned:** rule 20 (DR-020)
- **Arrange:** a Product with zero `Inventory` movements inside the report window `[From, To]`, and exactly one movement dated after `To` but within `To.AddMonths(1)`, with `StatusId = 3` (Received), `OnHand = 5`, `ReceivedOrShippedQuantity = 3`.
- **Act:** call `GetReport` for that window.
- **Assert (shape):** `output.report[*].on_hand` (expect `5 + 3 = 8`, per `InventoryReportController.cs:95-96`'s `searchOnHandOnNextMonth.OnHand + searchOnHandOnNextMonth.ReceivedOrShippedQuantity`), `output.report[*].received` (expect `0`), `output.report[*].shipped` (expect `0`). **Evidence-discipline note, not a new PQ:** my own reading of `GetOnHand`'s first branch (`InventoryReportController.cs:78`, `model.From.AddMonths(1)`) shows it queries a date range that is provably always a subset of (or an invalid, always-empty superset of) the already-confirmed-empty outer window — meaning `searchOnHandOnPreviousMonth` can never be non-null given the precondition under which `GetOnHand` is even called, and the `if (searchOnHandOnPreviousMonth != null)` branch at lines 82-86 is dead code. This scenario is designed around the reachable "next month" branch accordingly; see "No Legacy Behaviour Exists" below for the dead branch itself, and CQ-010 (already decided) for the eventual fix.
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-023 — Inventory report OnHand fallback: zero movements ever, falls back to the product's live OnHand

- **Seam:** same as GM-022
- **Seam layer:** service
- **Modules:** MOD-003
- **Business rules pinned:** rule 20 (DR-020)
- **Arrange:** a Product with zero `Inventory` movements, ever (none inside the window, none within a month before or after), `OnHand = 42`.
- **Act:** call `GetReport` for any window.
- **Assert (shape):** `output.report[*].on_hand` (expect `42`, per `InventoryReportController.cs:101-104`'s final `else { onHand = product.OnHand; }` fallback).
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-024 — Deleting a Product cascades to delete all of its Inventory movement rows

- **Seam:** `DM.Server/Controllers/ProductController.cs:119-124`'s `Delete`, observed at the EF6/SQL Server cascade-delete configuration
- **Seam layer:** persistence
- **Modules:** MOD-003
- **Business rules pinned:** no numbered rule (cascade/delete-rule behaviour per the explicit instruction to capture every reachable cascade finding)
- **Arrange:** a Product with 3 `Inventory` rows.
- **Act:** `Delete` the Product.
- **Assert (shape):** `output.product_deleted` (boolean), `output.inventory_rows_remaining_count` (expect `0`) — confirmed via `DM.Models/Inventory.cs:17-18`'s required `ProductId` FK.
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-025 — Appointment date lookup excludes Visited appointments

- **Seam:** `DM.Repository/AppointmentRepository.cs:21-30`'s `GetByDate(DateTime date)`, called via the corresponding `AppointmentService`
- **Seam layer:** service
- **Modules:** MOD-004
- **Business rules pinned:** rule 10 (DR-010)
- **Arrange:** two Appointments on the same date, distinct `Time` values (per seams.md Capture Blocker #4, to avoid an unspecified tie): one `StatusId = 7` (Appointed), one `StatusId = 8` (Visited).
- **Act:** call `GetByDate` for that date.
- **Assert (shape):** `output.appointments_returned_count` (expect `1`), `output.appointments[*].status_id` (expect only `7` present) — pinning the exact `x.StatusId == 7` filter at `AppointmentRepository.cs:27`. This is captured as legacy AS-IS behaviour regardless of CQ-009's already-decided fix (show both statuses in the rebuild) — the golden master documents what the legacy app does today, independent of that future divergence.
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-026 — DeleteUser blocks deleting your own account

- **Seam:** `DM.Server/Controllers/UserController.cs:58-66`'s `DeleteUser`
- **Seam layer:** service
- **Modules:** MOD-005
- **Business rules pinned:** rule 11 (DR-011)
- **Arrange:** authenticate as User X (`HttpContext.Current.User.Identity.GetUserId()` resolves to X's id).
- **Act:** call `DeleteUser(X.Id)`.
- **Assert (shape):** `output.outcome` (expect `"REJECTED"`), `output.threw` (expect `false` — a plain early `return BadRequest()`, `UserController.cs:62-63`), `output.error_code` (`"CANNOT_DELETE_OWN_ACCOUNT"` or equivalent, mapped in harness mode), `output.user_x_still_exists` (boolean, expect `true`).
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-027 — DeleteUser succeeds for a different user

- **Seam:** same as GM-026
- **Seam layer:** service
- **Modules:** MOD-005
- **Business rules pinned:** rule 11 (DR-011)
- **Arrange:** authenticate as User X; a separate User Y exists.
- **Act:** call `DeleteUser(Y.Id)`.
- **Assert (shape):** `output.outcome` (expect `"OK"`), `output.user_y_still_exists` (boolean, expect `false`) — the direct contrast case to GM-026.
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-028 — CreateUser with a password/retype mismatch — outcome not yet confirmed

- **Seam:** `DM.Server/Service/UserService.cs:71-90`'s `CreateUser`
- **Seam layer:** service
- **Modules:** MOD-005
- **Business rules pinned:** rule 12 (DR-012) ⚠ PROVISIONAL — pending PQ-009 (proposed default: treat as a DEFECT to fix in the rebuild)
- **Arrange:** none beyond a valid `RoleId` to reference.
- **Act:** call `CreateUser` with `model.PasswordHash = "abc"`, `model.RetypePassword = "xyz"`.
- **Assert (shape):** `output.outcome`, `output.threw`, `output.ExceptionType`/`output.ExceptionMessage` if it throws — this scenario is deliberately designed to let the actual capture settle whether `_repository.CreateUser(null)` (`UserService.cs:73`) surfaces as a clean rejection or an unhandled framework exception (per PQ-009, I believe the latter based on ASP.NET Identity 2.x's own null-guard convention, but I have not executed this code to confirm it), rather than asserting either outcome in advance.
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-029 — UpdateUser with a password/retype mismatch silently discards every submitted field edit, not just the password

- **Seam:** `DM.Server/Service/UserService.cs:93-110`'s `UpdateUser`
- **Seam layer:** service
- **Modules:** MOD-005
- **Business rules pinned:** rule 12 (DR-012)
- **Arrange:** an existing User with `FirstName = "Old"`.
- **Act:** call `UpdateUser` with `model.FirstName = "New"`, `model.PasswordHash = "abc"`, `model.RetypePassword = "xyz"` (mismatched).
- **Assert (shape):** `output.user_first_name_after_call` (expect `"Old"`, unchanged — the `if (model.PasswordHash != model.RetypePassword) return _repository.UpdateUser(user);` guard at `UserService.cs:100` fires **before** any of the profile-field assignments at lines 102-107, so a password mismatch discards the `FirstName`/`LastName`/`Email`/`PhoneNumber` edits too, not just the password) — a distinct, more subtle finding than Create's outright-reject path.
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-030 — Changing your own password with the wrong current password is rejected

- **Seam:** `DM.Server/Service/ProfileService.cs:64-80`'s `UpdatePassword`
- **Seam layer:** service
- **Modules:** MOD-005
- **Business rules pinned:** rule 13 (DR-013)
- **Arrange:** a User with a known password hash.
- **Act:** call `UpdatePassword` with `CurrentPassword` = an incorrect value, `NewPassword == RetypePassword` (both matching, so only the current-password check is exercised).
- **Assert (shape):** `output.outcome` (expect `"REJECTED"`), `output.threw` (expect `false` — `VerifyHashedPassword` returns an enum, never throws), `output.password_changed` (boolean, expect `false`).
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-031 — Changing your own password with mismatched new/retype is rejected before the current password is even checked

- **Seam:** same as GM-030
- **Seam layer:** service
- **Modules:** MOD-005
- **Business rules pinned:** rule 13 (DR-013)
- **Arrange:** a User with a known password hash.
- **Act:** call `UpdatePassword` with `NewPassword != RetypePassword`, and a deliberately **correct** `CurrentPassword` (to prove the check order).
- **Assert (shape):** `output.outcome` (expect `"REJECTED"`), `output.password_changed` (expect `false`) — pinning that `ProfileService.cs:68`'s `if (model.NewPassword != model.RetypePassword) return false;` short-circuits before `VerifyHashedPassword` is ever called at line 72, so a correct current password does not matter when new/retype disagree.
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-032 — Changing your own password succeeds (happy path)

- **Seam:** same as GM-030
- **Seam layer:** service
- **Modules:** MOD-005
- **Business rules pinned:** rule 13 (DR-013)
- **Arrange:** a User with a known password hash.
- **Act:** call `UpdatePassword` with the correct `CurrentPassword`, and `NewPassword == RetypePassword`.
- **Assert (shape):** `output.outcome` (expect `"OK"`), `output.password_changed` (expect `true`).
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-033 — RoleService.GetAll hides the SystemAdmin role from a non-SystemAdmin caller

- **Seam:** `DM.Server/Service/RoleService.cs:31-45`'s `GetAll`
- **Seam layer:** service
- **Modules:** MOD-005
- **Business rules pinned:** rule 14 (DR-014)
- **Arrange:** the seeded 8 roles exist; authenticate as a caller in the `"Admin"` role (not `"SystemAdmin"`).
- **Act:** call `GetAll()`.
- **Assert (shape):** `output.roles_returned_count` (expect `7`), `output.roles_include_system_admin` (boolean, expect `false`).
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-034 — UserService.GetUsers hides users holding the SystemAdmin role from a non-SystemAdmin caller

- **Seam:** `DM.Server/Service/UserService.cs:42-60`'s `GetUsers`
- **Seam layer:** service
- **Modules:** MOD-005
- **Business rules pinned:** rule 14 (DR-014)
- **Arrange:** two seeded users, one `SystemAdmin`, one `Admin`; authenticate as the `Admin` caller.
- **Act:** call `GetUsers()`.
- **Assert (shape):** `output.users_returned_count` (expect `1`), `output.users_include_system_admin_user` (boolean, expect `false`). **Open question, not resolved here:** `UserService.cs:54-57`'s `users.Remove(user)` relies on `users` and `systemAdminRoleUsers` (two independently-queried lists) containing the exact same object instance per user for reference-equality removal to succeed; whether EF6's per-`DbContext` identity map actually guarantees that across these two separate queries is not something I can confirm by static reading alone — the actual capture will settle whether the removal genuinely happens, not this design.
- **Kind:** edge case
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-035 — Both list-filtering calls show everything when the caller IS SystemAdmin

- **Seam:** same two calls as GM-033/GM-034
- **Seam layer:** service
- **Modules:** MOD-005
- **Business rules pinned:** rule 14 (DR-014)
- **Arrange:** same seed data as GM-033/GM-034; authenticate as a `SystemAdmin` caller.
- **Act:** call `RoleService.GetAll()` and `UserService.GetUsers()`.
- **Assert (shape):** `output.roles_returned_count` (expect `8`), `output.users_returned_count` (expect `2`) — the direct contrast case pinning both sides of the caller-identity branch.
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-036 — CheckPermission grants access unconditionally when the Resource is public

- **Seam:** `DM.Server/Service/PermissionService.cs:35-58`'s `CheckPermission`
- **Seam layer:** service
- **Modules:** MOD-005
- **Business rules pinned:** rule 15 (DR-015)
- **Arrange:** a `Resource` with `IsPublic = true` and zero `Permission` rows for any role.
- **Act:** call `CheckPermission` for any authenticated caller, any role.
- **Assert (shape):** `output.permitted` (expect `true`) — pinning `PermissionService.cs:39`'s early `if (resource.IsPublic) return true;`.
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-037 — CheckPermission denies access when the Resource is private and no matching Permission row exists

- **Seam:** same as GM-036
- **Seam layer:** service
- **Modules:** MOD-005
- **Business rules pinned:** rule 15 (DR-015)
- **Arrange:** a `Resource` with `IsPublic = false`; the caller's role has zero `Permission` rows for that Resource.
- **Act:** call `CheckPermission`.
- **Assert (shape):** `output.permitted` (expect `false`).
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-038 — CheckPermission grants access when the Resource is private and a matching Permission row exists

- **Seam:** same as GM-036
- **Seam layer:** service
- **Modules:** MOD-005
- **Business rules pinned:** rule 15 (DR-015)
- **Arrange:** a `Resource` with `IsPublic = false`; a `Permission` row exists granting the caller's role that Resource.
- **Act:** call `CheckPermission`.
- **Assert (shape):** `output.permitted` (expect `true`) — completing the truth table with GM-036/GM-037.
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-039 — Fresh-install permission seeding grants only SystemAdmin, against every private Resource

- **Seam:** `DM.Server/Migrations/Configuration.cs:63-87`'s `AddPermissions(ApplicationDbContext db)`
- **Seam layer:** service
- **Modules:** MOD-005
- **Business rules pinned:** rule 16 (DR-016)
- **Arrange:** roles and resources already seeded (`AddRoles`/`AddResources` already run, per `Configuration.cs:18-24`'s `Seed` ordering); zero `Permission` rows exist yet.
- **Act:** call `AddPermissions(db)`.
- **Assert (shape):** `output.permission_rows_created_count` (expect one per private `Resource`, i.e. every seeded `Resource` with `IsPublic = false`), `output.all_created_rows_role_is_system_admin` (boolean, expect `true`), `output.other_roles_have_any_permission` (boolean, expect `false`).
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-040 — Permission seeding is a no-op once any Permission row already exists

- **Seam:** same as GM-039
- **Seam layer:** service
- **Modules:** MOD-005
- **Business rules pinned:** rule 16 (DR-016)
- **Arrange:** one `Permission` row already exists (any role/resource).
- **Act:** call `AddPermissions(db)` again.
- **Assert (shape):** `output.permission_rows_created_count` (expect `0` — `Configuration.cs:70`'s `if (!db.Permissions.Any())` guard skips the whole build-list step entirely) — the direct boundary contrast to GM-039.
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

### GM-041 — Prescription.TotalDiscountAmount computed property, boundary values

- **Seam:** `DM.Models/Prescription.cs:29`'s `TotalDiscountAmount` computed property
- **Seam layer:** pure-function
- **Modules:** MOD-001
- **Business rules pinned:** no numbered rule (boundary values of a computed read-model property identified as a pure-function seam, per the explicit instruction)
- **Arrange:** none — construct `Prescription` instances directly with `DiscountAmount`/`FixedDiscount` = `(0, 0)`, `(10.5, 5)`, and `(0, -5)` (no validation prevents a negative `FixedDiscount`).
- **Act:** read `TotalDiscountAmount` for each.
- **Assert (shape):** `output.results[*].discount_amount`, `output.results[*].fixed_discount`, `output.results[*].total_discount_amount` — expect `0`, `15.5`, `-5` respectively (plain, unguarded addition with no floor at zero).
- **Kind:** boundary
- **Verifies backlog item:** not yet backlog-linked — rebuild-backlog.md does not exist yet

## No Legacy Behaviour Exists

- **`Doctor` deletion, and the `Doctor → Appointment` cascade it would trigger.** `DM.Server/Controllers/DoctorController.cs` exposes only `Get()`/`Get(string request)` (`GetAll`/`GetById`) — confirmed directly, no `Delete` action exists anywhere in this controller or in any other file that references `IDoctorService.Delete`. The cascade is configured at the DB level (`Appointment.DoctorId` is `[Required]`, `Appointment.cs:37-38`) but no code path can ever trigger it. This should become a `SCOPE` question for `/specclaw:bf-clarify` — is a Doctor-management CRUD screen genuinely planned (CQ-014 already decided "yes, build genuine multi-doctor support," which would presumably add a Delete path in the rebuild, but the legacy app itself has none today).
- **`Status` row deletion, and the four-way cascade (`Status → Prescription`/`Product`/`Inventory`/`Appointment`) it would trigger.** No controller, service, or repository anywhere in the codebase creates, updates, or deletes a `Status` row outside the one-time migration seed (`DM.Models/Migrations/Configuration.cs:46-62`'s `AddStatus`) — confirmed by my own search across `DM.Server/Controllers/*.cs`, `DM.Service/*.cs`, and `DM.Repository/*.cs` for any `Status`-mutating call site, finding none, and consistent with `module-map.md`'s own "Unassigned" finding for this entity. This is moot in any case once CQ-006's decision (split the shared `Status` lookup into typed per-entity enumerations) lands in the rebuild.
- **`Prescription` deletion via its own dedicated endpoint.** `DM.Server/Controllers/PrescriptionController.cs:64-69`'s `Delete` action is a no-op stub — it parses and echoes the request `Guid` without calling any service/repository method (see PQ-007). The `Prescription → PatientMedicalService`/`Payment` cascade this endpoint would trigger is therefore unreachable through this specific code path; the same cascade **is** reachable indirectly via a Patient-level delete, covered by GM-012.
- **The `InventoryReportController.GetOnHand`'s "previous month" branch** (`InventoryReportController.cs:78-87`). As detailed in GM-022's own note: given the precondition under which `GetOnHand` is called (the outer report window already confirmed to have zero movements), the narrower/shifted sub-range `[model.From.AddMonths(1), model.To]` this branch queries can never itself be non-empty — it is either a strict subset of the already-empty outer range, or an invalid (`from > to`) range that also returns nothing. This branch is provably dead code given the only caller that ever reaches it. Already covered by CQ-010's decision to remove this fixed-window logic entirely in the rebuild; no new pending question is raised for this specific finding.
- **The `Gender` enum type** (`DM.Models/Patient.cs:51-56`, `Male=1, Female=2, Others=3`) as an actual property type. `Patient.Gender` is declared `string`, not `Gender` — no code path anywhere casts to, constructs, or persists a value of the `Gender` enum type itself; only the three string literals `"Male"`/`"Female"`/`"Others"`, hardcoded independently in `patient-create.tpl.html`, are ever actually used. The enum *type* itself is unreachable dead code, even though its member names happen to coincide with the strings actually used. Already covered by CQ-007's decision to formalize `Gender` as a real typed enum in the rebuild.

## Rule Coverage Check

1. DR-001 — covered by GM-001, GM-002
2. DR-002 — covered by GM-003
3. DR-003 — covered by GM-004
4. DR-004 — covered by GM-005
5. DR-005 — covered by GM-006
6. DR-006 — covered by GM-007
7. DR-007 — covered by GM-008
8. DR-008 — covered by GM-020
9. DR-009 — covered by GM-021
10. DR-010 — covered by GM-025
11. DR-011 — covered by GM-026, GM-027
12. DR-012 — covered by GM-028, GM-029
13. DR-013 — covered by GM-030, GM-031, GM-032
14. DR-014 — covered by GM-033, GM-034, GM-035
15. DR-015 — covered by GM-036, GM-037, GM-038
16. DR-016 — covered by GM-039, GM-040
17. DR-017 — covered by GM-013, GM-014, GM-015
18. DR-018 — covered by GM-009, GM-010
19. DR-019 — covered by GM-016, GM-017
20. DR-020 — covered by GM-022, GM-023

**Additional findings not tied to a numbered rule, listed for completeness of coverage:** GM-011 (dual "current bill" mechanism), GM-012 (Patient cascade delete), GM-018 (MedicalInfoService sibling defect), GM-019 (PatientMedicalInfo no-cascade orphan), GM-024 (Product cascade delete), GM-041 (Prescription.TotalDiscountAmount boundary).

**Provisional pending decision:**

- DR-001 — GM-002 — pending PQ-005
- No numbered rule (dual "current bill" mechanism) — GM-011 — pending PQ-008
- No numbered rule (MedicalInfoService sibling defect to DR-018) — GM-018 — pending PQ-006
- DR-012 — GM-028 — pending PQ-009

Every other scenario in this design is unblocked: all of `clarifications.md`'s prior questions are already answered (`decisions.md`'s "Outstanding Questions: All questions in clarifications.md have been answered"), and `pending-questions.md`'s PQ-001 through PQ-004 are all `PROMOTED` (resolved), so no rule they touch carries a marker on their account. PQ-007 (Prescription.Delete stub) blocks no scenario directly — it is filed under "No Legacy Behaviour Exists" instead, since the endpoint it questions performs no observable action to capture.
