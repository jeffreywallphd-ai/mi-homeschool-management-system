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
- Course rows are clickable navigation targets with hover affordance; avoid separate Open columns.
- Course forms do not expose subject-area text fields; coverage is managed through requirement mappings.
- Course credit displays should include at least one decimal place and preserve more precise values when present.
- Course detail uses two columns: identity/description/resources/assessment on the left, plan/mapping/current mappings on the right.
- Course detail uses autosave for field changes; requirement mapping remains an explicit add/update action.
- Course detail autosave feedback belongs in the page header bar above the main content.
- Page columns should fill available width and have generous horizontal spacing.
- Course detail text areas allow at least four visible lines before scrolling.
- Preset dropdown `Other` choices clear the associated text area so parent text starts clean.
- Preset dropdown plus textarea pairs use one visible label and hidden secondary labels as needed for accessibility.
- Texts/resources and learning objectives are edited as itemized lists with Add/Hide controls.
- UI does not mutate domain objects directly.
- Student PIN sessions cannot reach admin actions.
- Logged-out navigation exposes only Login and startup routes to Login.

## Common Failure Modes

- Nullable form state leaks into domain operations.
- Support panels become hidden decision points instead of visible helpers.
- UI screens hide legal or credential boundaries.
