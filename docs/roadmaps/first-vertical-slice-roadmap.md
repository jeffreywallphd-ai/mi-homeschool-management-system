# First Vertical Slice Roadmap

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: phased implementation plan for the first local app slice
- Related ADRs: [ADR-0001](../adr/ADR-0001-parent-owned-records-not-state-filings.md), [ADR-0002](../adr/ADR-0002-michigan-as-seeded-jurisdiction-not-hardcoded-app.md), [ADR-0003](../adr/ADR-0003-records-and-credentials-module-from-start.md), [ADR-0004](../adr/ADR-0004-local-first-parent-pc-data-ownership.md)
- Related docs: [V1 Scope](../product/v1-scope.md), [ASP.NET Blazor SQLite Stack](../architecture/aspnet-blazor-sqlite-stack.md), [Identity and Access Architecture](../architecture/identity-and-access-architecture.md), [Michigan Requirement Areas](../legal-requirements/michigan-requirement-areas.md), [Testing Architecture](../architecture/testing-architecture.md)
- Related tests: not yet implemented
- Supersedes: none

## Slice Goal

Create the local app foundation and first parent/admin setup workflow for household, school profile, student, school year, and Michigan requirement seed.

This slice should prove that the application can run locally, persist records in SQLite, enforce parent/admin setup boundaries, seed Michigan requirement areas, and move data through contract-backed UI, application, domain, and infrastructure layers.

## In Scope

- Solution and project structure.
- ASP.NET Core Blazor Web App shell.
- SQLite persistence foundation.
- Local data path configuration.
- Parent/admin login boundary using simple local/Windows-oriented authentication direction.
- Student PIN model foundation without student admin permissions.
- Household setup.
- School profile setup.
- Student setup.
- School year and term setup.
- Michigan requirement set and area seed.
- Requirement checklist read view.
- Domain/application/infrastructure tests for the slice.

## Out of Scope

- Courses and curriculum planning.
- Assignments and submissions.
- Gradebook.
- Credits and graduation plan.
- Report cards, transcripts, diplomas, and generated documents.
- Backup/restore implementation beyond respecting local data path and versioning assumptions.
- Cloud sync, external identity, public API, or multi-device server behavior.

## Phase 1: Scaffold the Local App Foundation

### Build

- Create the solution and project structure matching the modular monolith.
- Add Web, Application, Domain, Infrastructure, and Tests projects.
- Configure Blazor Web App shell.
- Configure local app settings for data root and development mode.

### Exit Criteria

- App starts locally.
- Projects reference only allowed layers.
- Root navigation exists without implementing later modules.
- No cloud, sync, or external identity assumptions are introduced.

### Verification

- Build succeeds.
- Basic smoke test or manual run confirms the app loads.
- Project references preserve intended boundaries.

## Phase 2: Establish Persistence and Local Data Path

### Build

- Add SQLite database foundation.
- Define local data root resolution.
- Create initial schema/version tracking.
- Add Michigan requirement seed storage shape.

### Exit Criteria

- Database is created under the configured local data root.
- Schema/version record exists.
- Seed operation can run idempotently.

### Verification

- Infrastructure test confirms database creation.
- Seed test confirms repeated seed does not duplicate requirement areas.

### Implementation Note

The accepted architecture targets SQLite. The first implementation may use the repository abstraction with a package-free local data-file adapter when external SQLite package restore is unavailable. That adapter must remain replaceable by a SQLite-backed implementation without changing domain or application contracts.

## Phase 3: Implement Identity and Role Boundary Foundation

### Build

- Add parent/admin authentication boundary.
- Add student PIN model foundation.
- Add authorization contracts for parent/admin-only setup.

### Exit Criteria

- Parent/admin session is required for setup screens.
- Student PIN session cannot access setup/admin use cases.
- Authorization exists in application/use-case contracts, not only in UI visibility.

### Verification

- Application tests confirm student role cannot run household, school profile, student setup, or requirement seed commands.
- UI or smoke verification confirms admin-only screens are not reachable from student access.

## Phase 4: Implement Setup Domain and Application Contracts

### Build

- Add household model and setup command.
- Add school profile model and setup command.
- Add student model and setup command.
- Add school year and term model and setup command.
- Add validation for required fields and explicit optional states.

### Exit Criteria

- Setup records can be created through application commands.
- Invalid commands fail before domain mutation.
- Nullable or missing values do not cause runtime null failures.

### Verification

- Domain tests cover required fields and invariants.
- Application tests cover command validation.
- Persistence tests confirm saved setup records can be reloaded.

## Phase 5: Seed Michigan Requirement Profile

### Build

- Add Michigan requirement set.
- Seed statutory subject areas.
- Seed value-added MDE guidance/checklist areas as a related view without duplicating statutory subjects.
- Add requirement checklist query.

### Exit Criteria

- Michigan is represented as a jurisdiction profile.
- Statutory areas remain the visible core; only differentiated MDE guidance areas remain as distinct rows.
- Checklist language says records show selected coverage, not legal compliance.

### Verification

- Seed tests verify all expected areas.
- Query tests verify both views.
- Legal wording scan confirms no compliance, approval, accreditation, or submission claims.

## Phase 6: Build Setup UI Flow

### Build

- Add parent/admin setup pages for household, school profile, student, school year, and terms.
- Add requirement checklist read view.
- Use explicit view models and commands.
- Show validation errors in plain language.

### Exit Criteria

- Parent can complete the first setup flow from the UI.
- UI submits typed contracts.
- Requirement checklist displays seeded Michigan areas.
- Student role cannot change setup data.

### Verification

- Manual browser check of setup flow.
- Component or end-to-end test when stable enough.
- Confirm validation errors are visible and no high-stakes action lacks parent/admin boundary.

## Phase 7: Closeout and Readiness Review

### Build

- Update docs only if implementation changes accepted behavior.
- Record any deferred decisions encountered.
- Prepare the next slice recommendation.

### Exit Criteria

- Slice has a working local setup foundation.
- Tests pass.
- No context pack exceeds 200 lines.
- No docs conflict with accepted ADRs.
- Next slice can begin with courses and curriculum planning.

### Verification

- Build and test suite run.
- Local link/doc impact check if docs changed.
- Final report names files changed, tests run, docs consulted, and known limitations.

### Implementation Note

If NuGet package restore is unavailable for xUnit during this slice, use a package-free console test runner for contract verification and document the limitation. The target testing architecture remains xUnit-centered.

## Next Slice

After this slice, the recommended next vertical slice is courses and curriculum planning with requirement mappings. That slice should add course creation, course descriptions, curriculum plans, and parent-selected Michigan requirement mappings while preserving the setup and authorization contracts established here.
