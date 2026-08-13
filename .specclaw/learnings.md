# Learnings: bl-001-data-layer-consolidation-migration-schema

Build learnings, spec gaps, and patterns discovered.

**Categories:** spec_gap | design_gap | pattern | best_practice | agent_issue

---

## [L1] design_gap — design.md's file map places ApplicationUser in DentalMana...

**When:** 2026-08-12 09:08 UTC
**Category:** design_gap
**Priority:** medium
**Status:** pending

### Detail
design.md's file map places ApplicationUser in DentalManagement.Domain/Entities, but ApplicationUser must derive from ASP.NET Core Identity's IdentityUser, which requires a package reference the Domain project is explicitly forbidden (T1/FR-02: 'no package reference beyond the BCL'). Resolved by placing ApplicationUser in the Infrastructure project; Resource and Permission stay in Domain as plain POCOs, with Permission's FK to IdentityRole configured navigation-lessly in Infrastructure.

### Action
Update design.md's file map: ApplicationUser belongs to src/DentalManagement.Infrastructure. When a clean-architecture design places a framework-derived type in a dependency-free project, check the base type's package requirement at design time.

---

## [L2] design_gap — Seed order was unspecified in design.md and the wrong ord...

**When:** 2026-08-12 10:22 UTC
**Category:** design_gap
**Priority:** high
**Status:** pending

### Detail
Seed order was unspecified in design.md and the wrong order silently duplicates data: seeding the fresh-install default doctor (DR001/'Dental Doctor') before migrating leaves two identical doctors, because legacy supplies its own DR001 and neither schema indexes Doctor.Code uniquely. Found by a test assertion, not by review.

### Action
The migration runbook must prescribe migrate-then-seed. When a design has both a fresh-install seeder and a data-migration path writing the same tables, specify their order and what each guards on.

---

## [L3] design_gap — Reconciliation initially compared legacy-only row counts ...

**When:** 2026-08-12 10:22 UTC
**Category:** design_gap
**Priority:** medium
**Status:** pending

### Detail
Reconciliation initially compared legacy-only row counts against a target that also holds seed data, and ignored transitive exclusion (a blocked patient's bills/line-items/payments cannot migrate either). Both produced false reconciliation failures on the first real run.

### Action
Reconciliation over a target that is both migrated and seeded needs two check shapes: exact counts for pure-domain tables, presence checks for tables the seeder also writes. Extract the expected-set computation (MigrationPlan) so writer and reconciler cannot drift.

---

## [L4] design_gap — Directory.Packages.props and src/DentalManagement.Infrast...

**When:** 2026-08-13 04:28 UTC
**Category:** design_gap
**Priority:** low
**Status:** pending

### Detail
Directory.Packages.props and src/DentalManagement.Infrastructure/Persistence/DentalDbContextFactory.cs were created but declared in no task. The first adds central package version management (not in T1's file list); the second is required for 'dotnet ef migrations add' to build the model without starting the API host, which design.md's file map omitted entirely.

### Action
Add both to design.md's file map. When a design specifies EF migrations, it should also specify how design-time model construction happens (IDesignTimeDbContextFactory or a host).

---

## [L5] design_gap — T7 declared DevelopmentSeedData.cs, which was never creat...

**When:** 2026-08-13 04:28 UTC
**Category:** design_gap
**Priority:** low
**Status:** pending

### Detail
T7 declared DevelopmentSeedData.cs, which was never created; its responsibility split into AdminAccountSeeder.cs plus AdminBootstrapOptions.cs, and the seeder also needed SeedCatalog.cs and DeterministicGuid.cs — four undeclared files replacing one declared one. The split was driven by CQ-017 needing a real environment boundary rather than a development-only data file.

### Action
When a decision distinguishes environments (CQ-017's dev-vs-production credentials), the design should name the options/boundary type, not just a *SeedData.cs file — the boundary is the deliverable.

---

## [L6] pattern — Three separate build failures came from environment/toolc...

**When:** 2026-08-13 04:28 UTC
**Category:** pattern
**Priority:** medium
**Status:** pending

### Detail
Three separate build failures came from environment/toolchain defaults rather than logic: InvariantGlobalization=true broke Microsoft.Data.SqlClient at runtime; Microsoft.NET.Test.Sdk (VSTest) discovered zero xunit.v3 tests until TestingPlatformDotnetTestSupport was set; and 'dotnet new sln' defaulted to the .slnx format the design did not specify.

### Action
On a greenfield .NET solution, validate the toolchain triangle early — test runner discovery, globalization mode against the data providers in use, and solution file format — before writing feature code. Each cost a full build-test cycle to find.

---

## [L7] spec_gap — spec.md contradicts itself: its Out of Scope list exclude...

**When:** 2026-08-13 04:45 UTC
**Category:** spec_gap
**Priority:** medium
**Status:** pending

### Detail
spec.md contradicts itself: its Out of Scope list excludes 'CI/CD ... (SQ-011)' while AC-23 requires tests run 'in CI-reproducible fashion'. An acceptance criterion cannot demand what the scope section excludes; verify could only return PARTIAL.

### Action
When writing an AC that mentions CI, check the scope section first. Either scope the pipeline in or word the AC around the property that actually matters (deterministic, no manual setup beyond a documented daemon).

---

## [L8] pattern — A high-severity transitive vulnerability (SSH.NET 2025.1....

**When:** 2026-08-13 04:45 UTC
**Category:** pattern
**Priority:** high
**Status:** pending

### Detail
A high-severity transitive vulnerability (SSH.NET 2025.1.0 via Testcontainers, NU1903) was invisible across every build during T1-T16 because the local restore cache was warm, and only surfaced on a clean clone during verify. Structurally identical to the legacy defect BL-001 exists to fix: the EF6 chain also only failed against a genuinely empty database.

### Action
Add a clean-clone build (fresh clone, cold restore) as an explicit verify gate on any greenfield change. A warm cache hides both NuGet audit findings and restore-order problems.

---

## [L9] design_gap — T3 delivered PatientCodeSequence.NextAsync with no test, ...

**When:** 2026-08-13 06:54 UTC
**Category:** design_gap
**Priority:** high
**Status:** pending

### Detail
T3 delivered PatientCodeSequence.NextAsync with no test, because tasks.md scoped its test to T5's file list. Its raw SqlQuery<long> omitted the AS "Value" alias EF's scalar SqlQuery convention requires, so every call threw 'column s.Value does not exist'. The defect was invisible until T5 consumed it a wave later, and T3 had reported green (build clean, 73/73 pre-existing tests passing) because nothing exercised the new code.

### Action
When a task delivers a new runtime code path, its own task must include at least one test that executes it. A task whose only verification is 'the build compiles and the pre-existing suite still passes' verifies nothing about what it just added. Check task file lists at plan time for any impl task with no test file of its own.

---

## [L10] pattern — Stub registry Implementation citations are file:line, and...

**When:** 2026-08-13 07:02 UTC
**Category:** pattern
**Priority:** medium
**Status:** pending

### Detail
Stub registry Implementation citations are file:line, and a later task in the same change edited the cited file (T6 added controllers/CORS to Program.cs), shifting the stub gate from line 33 to 50 and the throw from 48 to 65. The citations written when T1 landed silently went stale mid-build.

### Action
Re-verify every file:line citation in module-stubs.md after the last task that touches the cited file, not at the moment the stub lands. Cheapest fix is to refresh citations during the close-out task rather than the stub task.

---

## [L11] design_gap — File client/src/App.tsx was modified by T8 but not declar...

**When:** 2026-08-13 07:24 UTC
**Category:** design_gap
**Priority:** low
**Status:** pending

### Detail
File client/src/App.tsx was modified by T8 but not declared in T8's Files list in tasks.md — the task required wiring the new screen as a route, which necessarily edits the router, yet only the api/ and features/ files were declared.

### Action
When a task adds a routed screen, a registered service, or anything else that must be referenced from a composition root, declare that composition root (App.tsx, Program.cs, DependencyInjection.cs) in the task's Files list. T5 and T6 hit the same shape and did declare theirs.

---

## [L12] spec_gap — AC-13's third clause asks a test to assert the dev stub t...

**When:** 2026-08-13 07:36 UTC
**Category:** spec_gap
**Priority:** medium
**Status:** pending

### Detail
AC-13's third clause asks a test to assert the dev stub types are 'absent from the service collection' in a non-Development boot. That is not observable: Program.cs's gate throws before builder.Build(), so no IServiceProvider is ever constructed for such a boot and there is no container to query. T7 substituted the strongest available proof — the factory throws before returning, plus a contrast test showing the same types ARE resolvable in a Development boot, so the gate is shown to discriminate rather than merely always fail.

### Action
When writing a criterion about a component's absence from a container, first check whether the container is ever built on that path. A fail-fast gate makes 'assert it is not registered' unobservable by construction; the checkable form is 'startup throws with a message naming X, and a contrasting boot does resolve it'.

---

## [L13] design_gap — tasks.md put '/specclaw:bf-replay' inside T9, a build tas...

**When:** 2026-08-13 07:44 UTC
**Category:** design_gap
**Priority:** medium
**Status:** pending

### Detail
tasks.md put '/specclaw:bf-replay' inside T9, a build task. The bf-replay skill documents itself as running after /specclaw:verify and before /specclaw:pr, so the plan placed a post-verify acceptance gate inside the build phase. T9 delivered the registry and docs; the replay is deferred to after verify and recorded as outstanding in README.md.

### Action
Do not schedule /specclaw:bf-replay as a build task. Golden-master replay is a verification-phase gate; a build task can at most prepare for it (seam placement, fixture-shaped tests), which T5 already did.

---

## [L14] design_gap — build.test_command in config.yaml still names only tests/...

**When:** 2026-08-13 07:58 UTC
**Category:** design_gap
**Priority:** high
**Status:** pending

### Detail
build.test_command in config.yaml still names only tests/DentalManagement.Infrastructure.Tests, so finalize reported tests_passed true after running 84 of the 105 tests this change now has. The 12 new DentalManagement.Api.Tests and the 9 client Vitest tests were never executed by the gate that merged the branch. Both were run manually afterwards and passed, but the automated green was narrower than it appeared.

### Action
When a change adds a test project or a second toolchain, update build.test_command and add a frontend command in the same change. A finalize gate that silently skips a new suite is worse than no gate, because it reads as coverage.

---
