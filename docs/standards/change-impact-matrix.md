# Change Impact Matrix

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: routing changes to docs, ADRs, tests, privacy, and verification
- Related ADRs: [ADR-0001](../adr/ADR-0001-parent-owned-records-not-state-filings.md), [ADR-0002](../adr/ADR-0002-michigan-as-seeded-jurisdiction-not-hardcoded-app.md), [ADR-0003](../adr/ADR-0003-records-and-credentials-module-from-start.md), [ADR-0004](../adr/ADR-0004-local-first-parent-pc-data-ownership.md), [ADR-0005](../adr/ADR-0005-parent-defined-graduation-standards-before-diploma.md)
- Related docs: [Documentation Standards](documentation-standards.md), [AI-Agent Development Standards](ai-agent-development-standards.md)
- Related tests: not yet implemented
- Supersedes: none

## How to Use

Before implementation, identify all affected rows. Read the referenced docs. During implementation, preserve the required invariants. Before finishing, update docs/context packs and tests when behavior changed.

| Change type | Required docs/ADRs | Verification focus | Special risk |
| --- | --- | --- | --- |
| Curriculum/course planning | Curriculum rules, requirement mapping, ADR-0002 | Course contract validation | Missing mappings silently inferred |
| Activity/instruction logging | Instruction rules, record lifecycle, ADR-0001 | Activity required fields | False state-reporting implication |
| Assignment/submission/files | File taxonomy, storage architecture, privacy standards | File metadata/checksum | Lost or orphaned files |
| Grading/GPA | Assessment rules, testing standards | Grade scale and missing grade states | Null becomes zero/pass |
| Credits/course completion | Credits rules, ADR-0005 | Parent approval and traceability | Credit awarded without basis |
| Graduation plan/diploma | Credits rules, official records, ADR-0005 | Diploma blocked until plan accepted | Credential generated too early |
| Transcript/report card | Official records, document architecture | Source traceability | Generated document fabricates data |
| Requirement checklist | Legal docs, ADR-0001, ADR-0002 | Coverage language | Compliance overclaim |
| Portfolio/export | Portfolio rules, file taxonomy, privacy standards | Metadata retained in export | Files exported without context |
| Backup/restore | Backup architecture, operations, ADR-0004 | Manifest/checksum/restore validation | Silent data loss |
| UI forms/workflows | Modular boundaries, UX standards, coding standards | Complete command/view-model contracts | UI bypasses domain invariants |
| Legal wording | Legal language boundaries, source review log | Prohibited phrase scan | Legal advice or approval implication |
| New dependency/provider | Decision register, security standards | License/security/privacy review | Data leaves local boundary |
| Auth/student access | Decision register, privacy standards | Permission boundaries | Student changes parent-owned records |

## ADR Escalation

Create or update an ADR when a change alters an accepted invariant, chooses a deferred core technology, changes privacy/data ownership, changes legal-facing behavior, or changes credential-generation rules.
