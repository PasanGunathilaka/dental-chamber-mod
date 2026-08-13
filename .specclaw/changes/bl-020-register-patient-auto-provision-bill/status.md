# Status: BL-020 — Register New Patient & Auto-Provision Bill

**Change:** bl-020-register-patient-auto-provision-bill
**Started:** 2026-08-13
**Last Updated:** 2026-08-13

## Progress

| Phase | Status | Notes |
|-------|--------|-------|
| Proposal | ✅ Approved | 3 dependency bypasses recorded (ST-001, ST-002, ST-003). |
| Spec | ✅ Complete | 19 FRs, 6 NFRs, 20 ACs, 8 assumptions. Every AC labelled `[real]` or `[stub: ST-###]`. |
| Design | ✅ Complete | 9 key decisions, 6 risks. Seam layers pinned so GM-001/003/004 stay replayable. |
| Tasks | ✅ Complete | 9 tasks, 4 waves. T1 is the stub task; every endpoint task depends on it. |
| Build | ✅ Complete | 9/9 tasks, 0 failed. Merged as `4b7b79c`. 122 tests passing across 4 suites. |
| Verify | ⚠️ Partial | 16/20 ACs met, 4 partially met — all 4 are spec-wording defects, not implementation gaps. See `verify-report.md`. |

## Task Progress

**Completed:** 9 / 9
**Failed:** 0

_Wave 1: T1–T4 (independent). Wave 2: T5–T6. Wave 3: T7–T8. Wave 4: T9._

## Agent Runs

| Task | Agent | Model | Status | Duration |
|------|-------|-------|--------|----------|

## Issues

_None._
