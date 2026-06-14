# Decision Readiness Register

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: unresolved decisions that agents must not silently decide
- Related ADRs: [ADR-0001](ADR-0001-parent-owned-records-not-state-filings.md), [ADR-0002](ADR-0002-michigan-as-seeded-jurisdiction-not-hardcoded-app.md), [ADR-0003](ADR-0003-records-and-credentials-module-from-start.md), [ADR-0004](ADR-0004-local-first-parent-pc-data-ownership.md), [ADR-0005](ADR-0005-parent-defined-graduation-standards-before-diploma.md)
- Related docs: [Prompt Routing](../context/prompt-routing.md), [Change Impact Matrix](../standards/change-impact-matrix.md)
- Related tests: not yet implemented
- Supersedes: none

## Accepted Direction

- Local-first modular monolith.
- ASP.NET Core Blazor Web App with SQLite as the initial local stack.
- Parent-owned records, not state filings.
- Michigan seeded first, but not hard-coded as the only future jurisdiction.
- Records and credentials supported from the beginning.
- Parent-defined graduation standards required before diploma generation.
- Parent/admin login with simple local/Windows credentials, student PIN access, and strict parent/student roles.
- Production distribution uses a desktop host plus Velopack packaging, with per-portal localhost/Wi-Fi sharing settings.
- Optional background service production mode uses `%PROGRAMDATA%/HomeschoolManager` as the protected machine-level data root.
- Optional Google Drive and Gmail backup destinations are allowed only for parent-authorized encrypted full backups under ADR-0008.
- xUnit-centered testing with bUnit/Playwright added for critical Blazor flows.
- Tight contracts across UI, application, domain, infrastructure, and tests.

## Decisions Still Proposed or Deferred

| Decision | Current posture | Agent rule |
| --- | --- | --- |
| Exact Blazor template/package details | Proposed | Use accepted stack direction, but do not lock package-level details without implementation task authority |
| Document renderer library | Deferred | Evaluate when implementing generation |
| PDF engine | Deferred | Evaluate security, portability, licensing, and rendering quality |
| Automatic backup schedule | Deferred | Manual backup can precede scheduling |
| General encryption at rest | Deferred | Treat broad database/file encryption as a separate privacy/security decision requiring review |
| Additional jurisdictions | Deferred | Keep model extensible, but seed Michigan first |

## Escalation Rule

If implementation requires a deferred decision, stop and create or request an ADR/product update rather than embedding a durable assumption.
