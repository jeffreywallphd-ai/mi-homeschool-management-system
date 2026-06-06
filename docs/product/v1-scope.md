# V1 Scope

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: first real product version scope
- Related ADRs: [ADR-0003](../adr/ADR-0003-records-and-credentials-module-from-start.md), [ADR-0005](../adr/ADR-0005-parent-defined-graduation-standards-before-diploma.md)
- Related docs: [Product Vision and Scope](product-vision-and-scope.md), [Non-Goals](non-goals.md), [System Overview](../architecture/system-overview.md)
- Related tests: not yet implemented
- Supersedes: none

## Included in V1

V1 should include enough structure to support a real 12th-grade homeschool year and later graduation archive.

Required V1 modules:

- Household and school profile.
- Student profile.
- School years and terms.
- Michigan requirement checklist.
- Courses and curriculum plans.
- Lessons and assignments.
- Submissions and local file storage.
- Gradebook.
- Activity, attendance, and instruction logs.
- Portfolio evidence.
- Parent-defined graduation plan.
- Credit awards and course completion.
- Report card generation.
- Transcript generation.
- Diploma generation.
- Course-description packet generation.
- Portfolio export and graduation packet export.
- Full backup and restore.

## V1 Quality Bar

V1 may have simple screens, but it must not have vague data ownership, loose null handling, or weak boundaries. UI inputs must map to explicit commands or view models. Domain operations must validate required state before records are generated, credits awarded, or credentials issued.

## V1 Sequencing

Build the core record model before visual polish:

1. Household, school profile, student, school year, and terms.
2. Requirement sets and course mappings.
3. Courses, curriculum plans, lessons, assignments, submissions, files.
4. Assessment, gradebook, credits, course completion, graduation plan.
5. Portfolio and official records.
6. Document generation and export.
7. Backup, restore, and archive export.

## Scope Guard

Do not delay transcripts, diplomas, course descriptions, or portfolio exports until a later product era. They can begin with simple templates, but the data model and module boundaries must support them from the start.
