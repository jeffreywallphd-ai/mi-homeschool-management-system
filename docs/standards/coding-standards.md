# Coding Standards

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: code quality and boundary expectations
- Related ADRs: [ADR-0003](../adr/ADR-0003-records-and-credentials-module-from-start.md)
- Related docs: [Modular Monolith Boundaries](../architecture/modular-monolith-boundaries.md)
- Related tests: not yet implemented
- Supersedes: none

## Boundary Discipline

- UI uses view models and commands.
- Application layer owns use-case contracts.
- Domain layer owns invariants.
- Infrastructure implements persistence, files, documents, and backup contracts.

## Null and State Rules

- Prefer required constructor parameters and validated command models.
- Use explicit optional states rather than ambiguous nulls.
- Do not use null as a hidden workflow status.
- Validate before domain mutation.

## Error Handling

- Domain errors should be explicit and meaningful.
- User-facing errors should use plain language.
- Infrastructure failures should include enough diagnostic context without leaking sensitive data.

## Style

Follow existing .NET conventions once the codebase exists. Keep code simple, readable, and contract-driven.
