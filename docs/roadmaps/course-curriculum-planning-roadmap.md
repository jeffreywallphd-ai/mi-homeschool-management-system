# Course Curriculum Planning Roadmap

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: phased implementation plan for the courses, curriculum planning, and requirement mapping slice
- Related ADRs: [ADR-0002](../adr/ADR-0002-michigan-as-seeded-jurisdiction-not-hardcoded-app.md), [ADR-0003](../adr/ADR-0003-records-and-credentials-module-from-start.md), [ADR-0004](../adr/ADR-0004-local-first-parent-pc-data-ownership.md)
- Related docs: [Curriculum Planning Rules](../domain/curriculum-planning-rules.md), [Requirement Mapping Rules](../legal-requirements/requirement-mapping-rules.md), [Michigan Requirement Areas](../legal-requirements/michigan-requirement-areas.md), [Domain Module Map](../architecture/domain-module-map.md), [First Vertical Slice Roadmap](first-vertical-slice-roadmap.md)
- Related tests: not yet implemented
- Supersedes: none

## Slice Goal

Let the parent/admin create senior-year courses, write course descriptions and curriculum plans, and map courses to seeded Michigan requirement areas with primary, secondary, or supporting coverage.

This slice should turn the app from setup-only into a usable academic planning system while preserving tight contracts, parent-owned records, explicit mappings, and non-compliance wording.

## In Scope

- Course domain model.
- Subject/category field.
- School year association.
- Credit value as planned credit, not awarded credit.
- Course description model.
- Curriculum plan model.
- Texts/resources field.
- Major topics.
- Instructional methods.
- Assessment methods.
- Parent notes.
- Requirement mapping model.
- Parent/admin course list, create/edit flow, detail page, and mapping UI.
- Michigan coverage summary based on course mappings.
- Domain/application/infrastructure tests for course contracts and mappings.

## Out of Scope

- Lessons and assignments.
- Submissions.
- Gradebook and final grades.
- Credit awards and course completion.
- GPA.
- Transcript generation.
- Diploma generation.
- Course-description packet rendering.
- File uploads or stored curriculum-resource files.

## Phase 1: Extend the Domain Model

### Build

- Add Course entity.
- Add CourseDescription value/object record.
- Add CurriculumPlan value/object record.
- Add RequirementMapping entity.
- Add CoverageLevel enum with primary, secondary, and supporting.
- Add validation for title, subject area, school year, planned credit value, and mapping fields.

### Exit Criteria

- Course is the primary planning unit.
- Course description and curriculum plan are attached to a course.
- Requirement mappings are separate from course identity.
- Invalid or missing required values fail before persistence.

### Verification

- Domain tests cover required course fields.
- Domain tests cover credit-value bounds.
- Domain tests cover valid and invalid coverage levels.
- Domain tests confirm mappings require both course and requirement area identifiers.

## Phase 2: Add Application Contracts

### Build

- Add create course command.
- Add update course command.
- Add add/update course description command.
- Add add/update curriculum plan command.
- Add set course requirement mappings command.
- Add course list query.
- Add course detail query.
- Add Michigan coverage summary query.

### Exit Criteria

- Parent/admin authorization is required for all course mutations.
- Student role cannot create, edit, or map courses.
- Queries return stable view models for UI use.
- Missing mappings remain visible; they are not silently inferred.

### Verification

- Application tests confirm parent/admin can create and edit courses.
- Application tests confirm student role cannot mutate courses.
- Application tests confirm commands reject incomplete contracts.
- Coverage summary tests confirm mapped and unmapped areas are reported.

## Phase 3: Extend Persistence

### Build

- Persist courses.
- Persist course descriptions.
- Persist curriculum plans.
- Persist course requirement mappings.
- Preserve existing setup and requirement seed data.
- Keep repository contracts replaceable by the future SQLite-backed adapter.

### Exit Criteria

- Course records persist and reload.
- Mappings persist and reload.
- Existing household, student, school year, and requirement data remains intact.
- Seeded Michigan requirement areas are available for mapping.

### Verification

- Infrastructure tests confirm course persistence.
- Infrastructure tests confirm mapping idempotency or replacement behavior.
- Regression tests confirm first-slice setup records still reload.

## Phase 4: Build Parent/Admin Course UI

### Build

- Add Courses navigation item.
- Add course list page.
- Add create/edit course form.
- Add course detail page.
- Add course description and curriculum plan sections.
- Use explicit UI view models and application commands.
- Show validation errors near affected fields.

### Exit Criteria

- Parent/admin can create and edit a course from the UI.
- Parent/admin can add course description and curriculum plan details.
- Student role cannot access course mutation actions.
- UI does not edit domain objects directly.

### Verification

- Manual browser check for create/edit flow.
- Component or end-to-end test when stable enough.
- Authorization check confirms student PIN session cannot change course data.

## Phase 5: Build Requirement Mapping UI and Coverage Summary

### Build

- Add mapping section to course detail.
- Show seeded Michigan requirement areas.
- Allow parent/admin to select coverage level for each mapped area.
- Add coverage summary page or section.
- Use wording such as "records show selected coverage" rather than legal compliance.

### Exit Criteria

- Parent/admin can map a course to multiple requirement areas.
- Mappings can be primary, secondary, or supporting.
- Unmapped Michigan areas remain visible in the summary.
- Statutory areas remain the visible core; only differentiated MDE/MMC planning areas remain as distinct rows.
- No UI text implies compliance, state approval, accreditation, or MDE submission.

### Verification

- Mapping command tests pass.
- Coverage summary query tests pass.
- Legal wording scan passes.
- Manual UI check confirms unmapped areas are visible.

## Phase 6: Closeout and Readiness Review

### Build

- Update docs only if implementation changes accepted behavior.
- Update README if run/test commands change.
- Record any deferred decisions encountered.
- Recommend the next slice.

### Exit Criteria

- Build passes.
- Contract tests pass.
- App starts locally.
- No context pack exceeds 200 lines.
- No docs conflict with accepted ADRs.
- Next slice can begin with lessons, assignments, submissions, and portfolio evidence.

### Verification

- Build and test suite run.
- Local link/doc impact check if docs changed.
- Legal wording scan for course/mapping UI.
- Final report names files changed, tests run, docs consulted, and known limitations.

## Next Slice

After this slice, the recommended next vertical slice is lessons, assignments, submissions, and portfolio evidence. That slice should turn planned courses into activity and evidence records while preserving course-first planning and requirement mapping contracts.
