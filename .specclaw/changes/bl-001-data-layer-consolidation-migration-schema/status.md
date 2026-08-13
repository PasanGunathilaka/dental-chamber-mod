# Status: BL-001 — Data Layer Consolidation, Migration & Schema Setup

**Change:** bl-001-data-layer-consolidation-migration-schema
**Started:** 2026-08-12
**Last Updated:** 2026-08-13

## Progress

| Phase | Status | Notes |
|-------|--------|-------|
| Proposal | ✅ Approved | Approved implicitly by invoking `/specclaw:plan`; 7 open questions carried into spec as assumptions A1–A8 |
| Spec | 🟡 Draft | 23 FRs/NFRs, 23 acceptance criteria. A5 reverses the proposal's `PatientMedicalInfo` recommendation on GM-019 evidence |
| Design | 🟡 Draft | 9 key decisions, 9 risks, grounding sources cited |
| Tasks | ✅ Complete | 16 tasks across 5 waves |
| Build | ✅ Complete | 16/16 tasks, 0 failed. Merged to `main` (4dfd636). 90 tests pass against real PostgreSQL + SQL Server |
| Verify | ⚪ Pending | Run `/specclaw:verify`. Two items outstanding — see Issues |

## Task Progress

**Completed:** 16 / 16
**Failed:** 0

All five waves complete:

- **Wave 1** — T1 solution skeleton, T2 domain model (13 entities, 5 enums, `IClock`)
- **Wave 2** — T3 single `DbContext` + 13 EF configurations, T4 initial migration + real-PostgreSQL harness, T5 schema/value tests, T6 captured-fixture delete behaviour
- **Wave 3** — T7 seeder + environment-split credentials, T8 seeding tests
- **Wave 4** — T9 synthetic legacy SQL scripts, T10 ADO.NET readers, T11 value audit, T12 reconciliation, T13 orchestrator + non-empty guard, T14 end-to-end tests
- **Wave 5** — T15 API host wiring, T16 verify commands + runbook

**Test results:** 73 passed (`Infrastructure.Tests`), 17 passed (`DataMigration.Tests`), 0 failed.

**Fixture replay:** GM-012, GM-019, GM-024, GM-039, GM-040, GM-041 all MATCH.

## Agent Runs

| Task | Agent | Model | Status | Duration |
|------|-------|-------|--------|----------|
| T1–T16 | inline (no subagents) | claude-opus-5 | complete | — |

Implemented directly rather than via spawned coding agents: `dynamic_agents` is off,
and the schema tasks are tightly interdependent enough that one context holding all
the fixture and decision constraints was the safer option.

## Issues

Neither blocks `/specclaw:verify`; both are recorded in `README.md`.

1. **No real production export has been reconciled.** The tooling is validated
   against the synthetic legacy database (spec A4). Running the migration against a
   genuine export and reviewing both reports is a blocking **pre-cutover** gate that
   has not been done.
2. **Human sign-off on the live-build confirmation is unticked.** The live run was
   performed and succeeded (21 tables, 8 roles, 18 resources, 16 grants, no `Status`
   table, `/health` Healthy) with evidence recorded in `README.md`, but BL-001 asks
   for a human's confirmation and that is a human's to give.

## Deviations from design (logged as learnings L1–L6)

- `ApplicationUser` lives in Infrastructure, not Domain — it derives from
  `IdentityUser`, which needs a package the Domain project is forbidden.
- Spec **A5 reversed the proposal**: `PatientMedicalInfo` keeps no FK, because GM-019
  pins the orphaning behaviour and no CQ sanctions changing it.
- Migration runbook now prescribes **migrate-then-seed**; the other order duplicates
  the default doctor. Found by a test, not review.
- `MigrationPlan` extracted so the writer and reconciler cannot drift on which rows
  are expected to migrate.
