# Verify Report: BL-001 — Data Layer Consolidation, Migration & Schema Setup

**Change:** bl-001-data-layer-consolidation-migration-schema
**Verified:** 2026-08-13
**Verdict:** ⚠️ **PARTIAL** — 22 of 23 acceptance criteria pass; AC-23 partially fails.

## Gates

| Gate | Result | Evidence |
|---|---|---|
| Build | ✅ PASS | `dotnet build DentalManagement.sln` — 0 warnings, 0 errors |
| Build from a clean clone | ✅ PASS **after a fix made during this verification** — see Finding 1 |
| Tests (fast tier) | ✅ PASS | 73 passed, 0 failed, 0 skipped — `Infrastructure.Tests` |
| Tests (slow tier) | ✅ PASS | 17 passed, 0 failed, 0 skipped — `DataMigration.Tests` |
| Lint | ✅ PASS | No separate linter; analyzers run in-build with `TreatWarningsAsErrors` |

**90 test cases across 57 test methods**, executed against real PostgreSQL 17 and real
SQL Server 2022 (Testcontainers), never the EF in-memory provider.

## Acceptance criteria

| AC | Verdict | Evidence |
|---|---|---|
| AC-01 — clean clone restores and builds, no manual steps | ✅ PASS | Fresh `git clone` + `dotnet build`: 0 warnings, 0 errors. **Failed on first attempt** — see Finding 1 |
| AC-02 — exactly one `DbContext`, one migration history | ✅ PASS | One class (`DentalDbContext`), one `Migrations/` folder, 3 files (migration + designer + snapshot). Test `Exactly_one_migration_history_table_exists` |
| AC-03 — chain applies to an empty database in one pass | ✅ PASS | `Migration_chain_applies_to_a_genuinely_empty_database`; `Migrated_schema_contains_table` (16 cases) |
| AC-04 — no index created twice across the chain | ✅ PASS | `No_index_is_created_more_than_once_across_the_chain`; `Chain_does_not_reuse_the_legacy_EF6_migration_history` |
| AC-05 — unique indexes reject duplicates at the database | ✅ PASS | `Duplicate_patient_code_…`, `Duplicate_medical_service_name_…`, `Duplicate_medical_info_name_…` |
| AC-06 — `Gender` rejects out-of-set values | ✅ PASS | `Gender_outside_the_typed_set_is_rejected_by_the_database`; `Check_constraint_exists(CK_Patient_Gender)` |
| AC-07 — `10.50m × 3 = 31.50m` exactly | ✅ PASS | `TotalCharge_keeps_fractional_currency` (3 cases); `TotalCharge_still_matches_GM016_for_integer_charges` (3 cases) |
| AC-08 — money columns `numeric(_,2)`, round-trip unchanged | ✅ PASS | `Money_column_is_numeric_with_two_decimal_places` (7 cases); `Money_value_round_trips_without_loss` |
| AC-09 — no `Status` table; cross-entity status rejected | ✅ PASS | `Migrated_schema_has_no_Status_lookup_table`; `Status_value_from_another_entitys_set_is_rejected` (5 cases); 4 status check constraints exist |
| AC-10 — **GM-012** cascade replays MATCH | ✅ PASS | `GM012_deleting_a_patient_cascades_to_bills_line_items_and_payments` — all three counts `0` |
| AC-11 — **GM-024** cascade replays MATCH | ✅ PASS | `GM024_deleting_a_product_cascades_to_its_inventory_movements` — `0` remain |
| AC-12 — **GM-019** orphaning replays MATCH | ✅ PASS | `GM019_deleting_a_patient_leaves_tagged_conditions_orphaned` — 2 remain; `PatientMedicalInfo_has_no_foreign_key_constraint` asserts the mechanism |
| AC-13 — **GM-041** → `0`, `15.5`, `-5` | ✅ PASS | `TotalDiscountAmount_matches_GM041` (3 cases) |
| AC-14 — **GM-039** SystemAdmin-only grants | ✅ PASS | `GM039_fresh_install_grants_only_SystemAdmin_against_every_private_resource`; `Public_resources_receive_no_permission_rows` |
| AC-15 — **GM-040** re-seed creates `0` | ✅ PASS | `GM040_reseeding_creates_no_additional_permission_rows`; `Reseeding_does_not_duplicate_roles_resources_or_doctors` |
| AC-16 — seeded doctor id persists; appointment inserts | ✅ PASS | `AC16_seeded_doctor_id_persists_and_accepts_an_appointment`; `Appointment_referencing_an_unknown_doctor_is_rejected` proves the FK is live |
| AC-17 — a second role is rejected | ✅ PASS | `AC17_a_user_cannot_hold_a_second_role` — unique index on the Identity join table |
| AC-18 — no hardcoded password outside dev; prod fails clearly | ✅ PASS | Repo search: `123qwe` appears only in a doc comment and in a test asserting rejection. One password literal (`DevelopmentDemoPassword`), gated. No credential in `appsettings*.json`. `AC18_production_bootstrap_fails_clearly_without_configured_credentials` |
| AC-19 — full migration reconciles on synthetic data | ✅ PASS | 8 `AC19_*` tests. Reconciliation passes all 19 checks. See Caveat 1 |
| AC-20 — all planted bad values reported; `"10.50"` migrates | ✅ PASS | `AC20_every_planted_problem_value_is_reported` asserts 4 charge + 3 gender + 2 status + 2 duplicate + 2 orphan + 1 missing findings, and that `"10.50"` is **not** a finding |
| AC-21 — a planted discrepancy makes reconciliation fail | ✅ PASS | `AC21_a_planted_discrepancy_makes_reconciliation_fail` — both the row-count and money-total checks flip |
| AC-22 — non-empty target refused, never half-applied | ✅ PASS | `AC22_second_run_against_a_populated_target_is_refused`; `AC22_dry_run_writes_nothing_but_still_audits` |
| AC-23 — all of AC-03…AC-22 run against real PostgreSQL, CI-reproducibly | ⚠️ **PARTIAL** | See Finding 2 |

## Findings

### Finding 1 — AC-01 failed on a clean clone (fixed during verification)

A fresh `git clone` + `dotnet build` failed with **2 errors**:

```
error NU1903: Warning As Error: Package 'SSH.NET' 2025.1.0 has a known high
severity vulnerability, https://github.com/advisories/GHSA-q939-rpr3-3284
```

`SSH.NET` is a transitive dependency of Testcontainers, not a direct one. The working
tree did not fail because its restore cache was already warm — **the defect was
invisible to every build run during T1–T16 and only appeared on a genuinely fresh
restore.** That is the same class of blind spot BL-001 exists to close: the legacy EF6
migration chain also only failed against a genuinely empty database.

Fixed by pinning forward via central package management
(`CentralPackageTransitivePinningEnabled` was already on):

```xml
<PackageVersion Include="SSH.NET" Version="2026.0.0" />
```

Pinned rather than suppressed — NU1903 was reporting a real high-severity
vulnerability, and silencing it would have kept the vulnerability while hiding it.
Re-verified: clean clone builds with **0 warnings, 0 errors**, and all 90 tests still
pass.

### Finding 2 — AC-23's "CI-reproducible" clause is not met

AC-23 reads: *"Every test in AC-03 through AC-22 runs against a real PostgreSQL
instance in CI-reproducible fashion."* Two sub-claims, judged separately:

**(a) Runs against real PostgreSQL — substantially true, with a correct exception.**
Every database-touching test uses a real PostgreSQL 17 container. AC-07 and AC-13
(`ValueSemanticsTests`) do **not** touch a database — they are pure unit tests, which
is correct, because GM-041 and GM-016/GM-017 were captured at the `pure-function` seam
layer and `/specclaw:bf-replay` refuses to replay a fixture at a different layer than
it was captured at. So the AC is worded more strictly than it should be; no defect.

**(b) CI-reproducible — not demonstrated.** There is **no CI pipeline in the
repository**: no `.github/workflows/`, no `azure-pipelines.yml`. The tests are
deterministic and need only a Docker daemon (documented in `README.md`), but nothing
makes them run automatically anywhere. Design risk **R-9** predicted this exact
outcome — *"A test suite needing a real database is skipped in CI and rots"* — and T16
was scoped to record "how the real-PostgreSQL test database is provisioned in CI." It
documented the prerequisite and wired `config.yaml`'s commands, but created no
pipeline.

**This also exposes a contradiction inside `spec.md` itself.** Its Notes section places
out of scope: *"CI/CD, structured logging, centralized error monitoring, health checks,
and backup/restore procedures (SQ-011) beyond the configuration the data layer needs."*
AC-23 nonetheless requires CI reproducibility. One of the two has to move — the
acceptance criterion cannot demand what the scope excludes. Resolving that is a
decision for a human, not something to quietly reinterpret, which is why this verdict
is PARTIAL rather than PASS.

## Caveats on what a PASS here does and does not mean

1. **AC-19 passes against synthetic data, not a real export.** Spec A4 records this
   deliberately, and AC-19 is worded to permit it (*"or the real export, if
   supplied"*). Reconciling a genuine production export remains a **blocking
   pre-cutover gate that has not been done**. A green AC-19 is not permission to cut
   over.

2. **The human live-build sign-off is still unticked.** The live run was performed and
   succeeded — 21 tables, 8 roles, 18 resources, 16 grants (exactly the 16 private
   resources), no `Status` table, `/health` returning `Healthy` — with evidence recorded
   in `README.md`. BL-001 asks for a *human's* confirmation, so the line stays open.

3. **Two fixtures are expected to diverge and were not run here.** GM-016 still
   matches, but GM-017 (`"10.50"` → `NON_INTEGER_CHARGE`) must diverge under CQ-008.
   That divergence belongs to BL-010's replay run. `/specclaw:bf-replay` should confirm
   CQ-008 sanctions it rather than reading it as a regression.

4. **This verification was performed by the same context that wrote the code.** No
   independent verifier agent was used, per this session's standing instruction. Every
   verdict above is grounded in command output captured during verification rather than
   recollection, but a fresh-context reviewer would still be worth running before the
   PR.

## Remediation

One item requires a decision, not a code fix:

- **AC-23** — either add a CI pipeline (which SQ-011 places outside BL-001's scope) or
  amend AC-23 to drop the CI clause and keep the real-database requirement. Recommend
  amending AC-23 here and carrying CI into the SQ-011 operational work, since a
  pipeline for a repo with one backlog item's worth of code will be rewritten as soon
  as the frontend lands.

Nothing else is outstanding: Finding 1 is fixed and re-verified.
