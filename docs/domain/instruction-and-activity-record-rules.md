# Instruction and Activity Record Rules

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: instruction, activity, attendance, reading, project, and fieldwork records
- Related ADRs: [ADR-0001](../adr/ADR-0001-parent-owned-records-not-state-filings.md)
- Related docs: [Michigan Homeschool Context](../legal-requirements/michigan-homeschool-context.md), [Homeschool Record Lifecycle](homeschool-record-lifecycle.md)
- Related tests: not yet implemented
- Supersedes: none

## Purpose

Instruction and activity records show what educational work actually happened. They are evidence records for the family, not routine state-submitted attendance filings.

## Record Types

The system should support:

- Instruction sessions.
- General activity logs.
- Attendance or activity-day summaries.
- Reading log entries.
- Project work logs.
- Fieldwork logs.
- External course or dual-enrollment references.

## Attendance Posture

For Michigan 3(f) homeschool use, the system should treat attendance/activity tracking as internal evidence and planning support. It should not imply a state daily-hour reporting requirement.

## Required Activity Fields

Activity records should capture:

- Student.
- Date or date range.
- Course or subject context when applicable.
- Activity type.
- Description.
- Optional duration.
- Optional attached artifacts.
- Parent notes.

## Evidence Quality

Records should favor clear narrative evidence over false precision. A well-described project session can be more useful than a bare hour count.
