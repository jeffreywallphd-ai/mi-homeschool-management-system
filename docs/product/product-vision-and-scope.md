# Product Vision and Scope

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: product purpose, primary user, and core scope
- Related ADRs: [ADR-0001](../adr/ADR-0001-parent-owned-records-not-state-filings.md), [ADR-0003](../adr/ADR-0003-records-and-credentials-module-from-start.md)
- Related docs: [V1 Scope](v1-scope.md), [Records and Credentials Use Cases](records-and-credentials-use-cases.md), [Michigan Homeschool Context](../legal-requirements/michigan-homeschool-context.md)
- Related tests: not yet implemented
- Supersedes: none

## Vision

The system helps a parent manage a Michigan homeschool year for a 12th-grade student by turning everyday homeschool work into credible, organized, parent-owned educational records.

The product is not primarily an assignment tracker. It is a records and credentials system for a parent-directed homeschool: curriculum planning, instruction records, grading evidence, credits, portfolio artifacts, report cards, transcripts, diploma generation, and graduation/archive exports.

## Primary User

The primary user is the parent/legal guardian acting as homeschool administrator. The student may use selected student-facing workflows, but parent review, records ownership, official records, graduation standards, and document issuance remain parent-controlled.

## Core Product Tracks

1. Curriculum planning: what the parent intended to teach.
2. Instruction and activity records: what was taught, practiced, completed, read, built, tested, or demonstrated.
3. Assessment and grading: what evidence supports grades, progress, credits, and completion.
4. Official family-issued records: report cards, transcripts, diplomas, course-description packets, portfolio exports, and graduation packets.

## Product Principles

- Parent-owned records come first.
- Michigan is the first jurisdiction profile, not a hard-coded product boundary.
- The app must never imply state approval, accreditation, or legal certification.
- Family-issued records must be clear, credible, internally consistent, and exportable.
- Practical and nontraditional work, including homesteading, farming, entrepreneurship, fieldwork, and project work, must be expressible as academic evidence.
- UI, application, and domain boundaries must be contract-backed and tight enough to avoid null-prone, ambiguous, or state-leaking behavior.

## Success Definition

The product succeeds when a parent can confidently answer:

- What did we plan to teach?
- What did the student actually do?
- What evidence supports the grade, credit, or completion decision?
- Which Michigan subject areas did our records cover?
- What parent-defined graduation standard was used?
- Can we produce a transcript, diploma, course-description packet, portfolio export, and full student archive later?
