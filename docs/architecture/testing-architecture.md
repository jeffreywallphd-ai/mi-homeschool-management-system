# Testing Architecture

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: test project structure and verification strategy
- Related ADRs: [ADR-0003](../adr/ADR-0003-records-and-credentials-module-from-start.md), [ADR-0005](../adr/ADR-0005-parent-defined-graduation-standards-before-diploma.md)
- Related docs: [Testing and Verification Standards](../standards/testing-and-verification-standards.md), [Modular Monolith Boundaries](modular-monolith-boundaries.md)
- Related tests: not yet implemented
- Supersedes: none

## Test Layers

Use xUnit for .NET tests unless a future ADR changes the test stack.

When package restore is unavailable during early slice work, a package-free console test runner may be used as a temporary verification harness. It must remain focused on the same domain/application/infrastructure contracts and should be replaceable by xUnit tests once package restore is available.

Recommended test projects:

- Domain tests.
- Application tests.
- Infrastructure tests.
- Web/UI tests when critical flows exist.

## Domain Tests

Domain tests should cover invariants:

- Required fields and explicit optional states.
- Requirement mappings.
- Grade states and GPA calculations.
- Course completion.
- Credit awards.
- Graduation-plan satisfaction.
- Diploma-readiness rules.

## Application Tests

Application tests should cover use-case contracts:

- Commands reject incomplete or unauthorized requests.
- Student role cannot perform parent/admin actions.
- Parent/admin actions enforce required source records.
- Generated-record commands require complete document models.

## Infrastructure Tests

Infrastructure tests should cover:

- SQLite persistence mappings.
- File metadata and checksums.
- Backup manifests.
- Restore validation.
- Generated-document file records.

## UI Tests

Add UI tests after critical Blazor flows exist. Preferred focus:

- Forms submit complete view models/commands.
- Validation messages appear.
- Parent-only controls require parent/admin authorization.
- Student PIN sessions cannot reach admin actions.

Use bUnit for component-level behavior where useful. Use Playwright for end-to-end browser flows when the app has stable critical workflows.

## Verification Priority

Prioritize contract and invariant tests before broad visual UI tests. The first testing goal is to prevent bad records, null-state failures, unauthorized grade changes, and invalid credential generation.
