# User and Household Model

- Status: accepted
- Last reviewed: 2026-06-12
- Canonical for: product-level household, parent, student, and school-profile concepts
- Related ADRs: [ADR-0005](../adr/ADR-0005-parent-defined-graduation-standards-before-diploma.md)
- Related docs: [Product Vision and Scope](product-vision-and-scope.md), [Identity and Access Architecture](../architecture/identity-and-access-architecture.md)
- Related tests: not yet implemented
- Supersedes: none

## Household

A household is the family record container. V1 may optimize for one active household, but the model must not assume that all data belongs to a global singleton.

## Parent or Legal Guardian

The parent/legal guardian is the homeschool administrator. The parent owns:

- Curriculum decisions.
- Requirement mappings.
- Grades and evaluations.
- Credit awards.
- Graduation standards.
- Official family-issued records.
- Backup and archive exports.

## Students

The first target student is a 12th grader. The model should still support multiple homeschooled children with K-12 grade levels so future records can be added without reworking core identity.

Each configured child should have a stable id and student-facing path. Courses, school years, grades, credits, modules, evidence, and generated records should be attributable to the correct child.

Student-facing access may exist for assignments, submissions, feedback, and portfolio creation. For middle-school and high-school students, the student may author and organize the working official portfolio design, including sections, introductions, narrative text, item placement, and assignment or evidence selections. The student must not issue final records, alter graduation standards, finalize credits, or generate approved official packets.

## School Profile

The school profile is a parent-defined administrative identity used for report cards, transcripts, diplomas, and course-description packets. It does not imply state registration, state approval, accreditation, or recognition beyond the family's chosen homeschool operating basis.

Required concepts:

- School name.
- Parent administrator name.
- Optional address/contact fields.
- Jurisdiction.
- Homeschool start date.
- Operating basis.
- Diploma signature name.
- Diploma issue city and state.

## Access Principle

Parent-first does not mean parent-only. Student workflows are allowed, and older students should be able to meaningfully author their portfolio. High-stakes finalization still passes through explicit parent-owned approval commands.
