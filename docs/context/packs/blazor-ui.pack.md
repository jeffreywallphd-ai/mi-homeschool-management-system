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
- Required fields use an asterisk.
- Planning screens put main forms/records on the left and support tools on the right.
- Optional helper tables in support panels may be collapsed by default when they are not the main task.
- Course coverage support summaries belong with related support tools when the main workflow is course list/editing.
- UI does not mutate domain objects directly.
- Student PIN sessions cannot reach admin actions.
- Logged-out navigation exposes only Login and startup routes to Login.

## Common Failure Modes

- Nullable form state leaks into domain operations.
- Support panels become hidden decision points instead of visible helpers.
- UI screens hide legal or credential boundaries.
