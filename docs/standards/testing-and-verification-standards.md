# Testing and Verification Standards

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: test priorities and verification expectations
- Related ADRs: [ADR-0003](../adr/ADR-0003-records-and-credentials-module-from-start.md), [ADR-0005](../adr/ADR-0005-parent-defined-graduation-standards-before-diploma.md)
- Related docs: [Credits and Graduation Rules](../domain/credits-and-graduation-rules.md), [Document Generation Architecture](../architecture/document-generation-architecture.md)
- Related tests: not yet implemented
- Supersedes: none

## Test Priorities

Prioritize automated tests for:

- Requirement mapping.
- Grade and GPA calculations.
- Null/optional state handling.
- Course completion and credit awards.
- Graduation-plan satisfaction.
- Diploma generation blocking rules.
- Transcript source traceability.
- File metadata and checksums.
- Backup/export manifest completeness.
- Restore validation.
- Role and authorization boundaries.
- Migration backup opt-in/opt-out behavior.

## Test Stack

Use xUnit for .NET domain, application, and infrastructure tests unless a future ADR selects a different stack.

Use bUnit for component-level Blazor behavior where useful. Use Playwright for stable critical end-to-end browser flows.

## Verification Rule

Every behavior-changing implementation should report what was tested and what remains unverified.

## UI Verification

UI work should verify that forms submit complete contracts, validation errors are visible, and high-stakes actions require explicit parent confirmation.
