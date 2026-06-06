# Repository Baseline Pack

Purpose: Baseline project context for all non-trivial work.

## Canonical Sources

- `docs/README.md`
- `docs/adr/README.md`
- `docs/adr/decision-readiness-register.md`
- `docs/standards/change-impact-matrix.md`

## Must Preserve

- Parent-owned records, not state filings.
- Michigan seeded first, not hard-coded as the only jurisdiction.
- Records and credentials from the beginning.
- Local-first parent data ownership.
- Parent-defined graduation standards before diploma generation.
- Tight contracts across UI, application, domain, infrastructure, and tests.

## Common Failure Modes

- Treating the app as a generic assignment tracker.
- Letting UI state bypass domain invariants.
- Overclaiming legal compliance.
- Generating official records without source traceability.

## Before Finishing

- Report docs/ADRs consulted.
- Report tests or verification.
- Report documentation impact.
