# Identity and Access Architecture

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: parent login, student PIN access, and role boundaries
- Related ADRs: [ADR-0004](../adr/ADR-0004-local-first-parent-pc-data-ownership.md), [ADR-0005](../adr/ADR-0005-parent-defined-graduation-standards-before-diploma.md)
- Related docs: [User and Household Model](../product/user-and-household-model.md), [Security and Privacy Standards](../standards/security-and-privacy-standards.md), [Accessibility and Nontechnical UX Standards](../standards/accessibility-and-nontechnical-ux-standards.md)
- Related tests: not yet implemented
- Supersedes: none

## Identity Model

The system should support login. V1 should keep login simple and local.

The parent/admin login may use Windows credentials or a similarly simple local authentication approach. The system should not require cloud identity, external OAuth, or hosted account infrastructure for V1.

The student may have a simple PIN-based access flow for student-facing work.

The student portal should be served from a separate web build and local listener from the parent/admin area. Development
uses `HomeschoolManager.Web` for parent/admin on `127.0.0.1:5171` and `HomeschoolManager.StudentPortal.Web` for student
access on port `5172`, with the student listener bound for same Wi-Fi access. The parent/admin build may redirect true
student routes to the student portal, and the student build should not serve parent/admin setup, course editing,
requirement mapping, records, backup/restore, or admin routes.

Parent/admin may have a student preview route inside the admin build. This preview is for reviewing the course, module,
lesson, and assignment material assigned to a student. It is distinct from the live student portal, runs in the
parent/admin build, and should not expose student submissions or other student-owned submission activity.

Browser refresh should restore the last selected local role when possible. V1 may keep this as process-wide local session state because the app is designed for one parent-owned PC, not multi-family concurrent hosting. This is local usability state only; application services remain responsible for parent/admin authorization on every mutation.

When no local session is active, startup should route to the login page and the left navigation should expose only Login.

## Roles

| Role | Purpose |
| --- | --- |
| Parent/Admin | Owns homeschool administration, records, grading, credits, graduation, official documents, backup, restore, and configuration. |
| Student | Views assigned work, submits work, reviews feedback, and views selected portfolio/course information. |

## Parent/Admin Permissions

Only the parent/admin can:

- Configure household and school profile.
- Configure requirement sets and mappings.
- Create or finalize grades.
- Award credits.
- Mark courses complete.
- Configure or approve graduation plans.
- Generate report cards, transcripts, diplomas, and official packets.
- Manage backups, restores, and archive exports.
- Change security/access settings.

## Student Permissions

Student access may allow:

- Viewing assigned courses and assignments.
- Submitting work.
- Viewing parent-approved feedback.
- Viewing selected portfolio artifacts.
- Viewing student-facing progress summaries.

Student access must not allow:

- Editing grades.
- Awarding credits.
- Changing course completion.
- Editing graduation plans.
- Generating or editing transcripts, diplomas, report cards, or official packets.
- Changing requirement mappings.
- Running restore or destructive data operations.
- Changing parent/admin settings.

## Access Boundary Rules

- Authorization checks belong in application/use-case contracts, not only in UI visibility.
- Hiding a UI button is not sufficient access control.
- High-stakes parent actions should require an authenticated parent/admin session.
- Student PIN sessions should be scoped, revocable, and limited.

## Local-First Rule

Authentication and access records should remain local unless a future accepted ADR authorizes external identity or sync.
