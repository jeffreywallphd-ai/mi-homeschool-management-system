# Official Records Rules

- Status: accepted
- Last reviewed: 2026-06-12
- Canonical for: report cards, transcripts, diplomas, and official family-issued packets
- Related ADRs: [ADR-0001](../adr/ADR-0001-parent-owned-records-not-state-filings.md), [ADR-0003](../adr/ADR-0003-records-and-credentials-module-from-start.md), [ADR-0005](../adr/ADR-0005-parent-defined-graduation-standards-before-diploma.md)
- Related docs: [Records and Credentials Use Cases](../product/records-and-credentials-use-cases.md), [Legal Language Boundaries](../legal-requirements/legal-language-boundaries.md)
- Related tests: not yet implemented
- Supersedes: none

## Official Family-Issued Records

Official records are family-issued artifacts generated from parent-owned source records. They include:

- Report cards.
- Transcripts.
- Diplomas.
- Course-description packets.
- Portfolio packets.
- Graduation packets.

A student-authored portfolio design is a working portfolio record, not an issued official family record until parent/admin final approval and export. For grade 6 and above, the student may edit the portfolio design and narrative; the parent/admin approves the reviewed packet that becomes family-issued output. For K-5 students, the parent may author and control the portfolio directly.

## Report Card

A report card summarizes courses, grades, progress, credits where relevant, attendance/activity summaries when selected, and parent notes for a reporting period.

## Transcript

A transcript summarizes academic course history, grade levels, school years, terms, credits attempted or planned, credits earned when explicitly parent-recorded, final grades when explicitly parent-recorded, GPA when a known grade scale exists, graduation date if applicable, and parent/school signature information.

Transcript course lines must not fabricate final grades, earned credits, GPA, or completion from course existence, assignment status, assessment records, planned points, planned credit values, or in-progress work. Missing final grades and missing earned credits must remain visible as not recorded until the parent/admin records them through a transcript or credit-award workflow.

Transcript titles, grade-span labels, and export wording must describe only the course data available in the system. If a requested conventional span, such as high school grades 9-12, is only partially represented by local course records, the transcript must identify the actual included grades and include a note that other transcripts or records may be available for grades not included.

The transcript may support:

- A high school span, conventionally grades 9-12.
- A middle school span, conventionally grades 6-8.
- An all-recorded-courses span for family archive use.

Student access may include read-only transcript preview. Student access must not allow editing transcript course records, final grades, earned credits, GPA, report cards, diplomas, or official packet exports.

Course descriptions may be included with a transcript packet as supporting material. They should remain an appendix or companion packet so the transcript itself keeps a conventional record layout.

## Diploma

A diploma is family-issued based on parent-defined graduation standards. Diploma generation must not imply state approval, accreditation, or MDE issuance.

Diploma generation must be blocked until the parent/admin has accepted parent-defined graduation standards, marked requirements satisfied or explicitly waived, supplied an awarded date, and supplied parent/admin signature labeling. A diploma design may include editable wording and line-level typography, but saved or generated wording must not imply state approval, accreditation, legal certification, legal compliance, or MDE issuance.

Diploma preview and PDF output should be printable on diploma cardstock. The rendered diploma must not paint a page background color; cardstock is responsible for the paper color. Decorative borders, rule lines, seal wording, signature lines, and date lines are presentation details layered over the transparent page.

## Course Description Packet

A course-description packet explains courses deeply enough to support colleges, employers, trade schools, transfer evaluators, or personal archives.

## Generated Record Versioning

Issued records should be versioned by issue date and generated file identity. Re-generating after source changes creates a new generated document record.
