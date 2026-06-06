# Testing and Diagnostics Pack

Purpose: Verification and privacy-safe diagnostics.

## Canonical Sources

- `docs/standards/testing-and-verification-standards.md`
- `docs/standards/security-and-privacy-standards.md`
- `docs/standards/change-impact-matrix.md`

## Must Preserve

- Domain/application invariants get tests.
- Sensitive student data is not logged.
- Verification reports name what was tested.

## Common Failure Modes

- Treating a passing build as enough.
- Logging grades, private notes, or file contents.
