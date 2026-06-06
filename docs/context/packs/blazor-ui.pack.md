# Blazor UI Pack

Purpose: UI workflow and form-boundary guidance.

## Canonical Sources

- `docs/architecture/modular-monolith-boundaries.md`
- `docs/architecture/identity-and-access-architecture.md`
- `docs/standards/accessibility-and-nontechnical-ux-standards.md`
- `docs/standards/coding-standards.md`
- Task-specific domain docs

## Must Preserve

- UI uses explicit view models and commands.
- High-stakes actions require explicit parent confirmation.
- Validation errors are visible and plain.
- UI does not mutate domain objects directly.
- Student PIN sessions cannot reach admin actions.

## Common Failure Modes

- Nullable form state leaks into domain operations.
- UI screens hide legal or credential boundaries.
