# Prompt Routing

This document routes AI agents to minimum-sufficient context.

## Universal Startup

1. Read `docs/README.md`.
2. Read `docs/context/packs/index.pack.md`.
3. Read `docs/standards/change-impact-matrix.md`.
4. Classify the task.
5. Load relevant specialized packs.
6. Read canonical docs named by the packs.
7. Inspect affected code/tests/docs.
8. Implement narrowly.
9. Verify behavior and documentation impact.

## Pack Size Rule

No single context pack may exceed 200 physical lines. When routing needs more detail, prefer canonical docs plus narrower specialized packs over one large pack.

## Task Routing

| Task | Add packs | Required canonical sources |
| --- | --- | --- |
| Product scope or roadmap | `product-and-domain`, `documentation-and-adr-governance` | Product docs, ADR index, decision register |
| Michigan requirement/checklist wording | `legal-michigan`, `documentation-and-adr-governance` | Legal docs, ADR-0001, ADR-0002 |
| Legal-facing wording or source refresh | `legal-michigan`, `documentation-and-adr-governance` | Legal source refresh workflow, source review log, legal language boundaries |
| Courses, curriculum, lessons, activity logs | `curriculum-and-instruction`, `legal-michigan` as needed | Domain curriculum/instruction docs, requirement mapping |
| Assignments, submissions, grading | `assessment-credits-graduation`, `portfolio-and-files` as needed | Assessment rules, file taxonomy |
| Credits, GPA, graduation plan | `assessment-credits-graduation`, `records-and-credentials` | Credits rules, ADR-0005 |
| Report cards, transcripts, diplomas | `records-and-credentials`, `document-generation`, `legal-michigan` | Official records rules, document architecture, legal language boundaries |
| Portfolio, projects, homesteading evidence | `portfolio-and-files`, `product-and-domain` | Portfolio rules, homesteading use cases |
| Local storage, backups, restore, exports | `local-data-backup-restore`, `portfolio-and-files` as needed | Storage architecture, backup architecture, operations docs |
| Blazor UI or form workflow | `blazor-ui`, task-specific domain pack | UI standards, modular boundary docs |
| Tests, diagnostics, bug fixes | `testing-and-diagnostics`, affected domain pack | Testing standards, change impact matrix |
| Docs/ADR changes | `documentation-and-adr-governance`, affected pack | Documentation standards, ADR index, decision register |

## Stop Conditions

Stop or request a decision when:

- Canonical docs conflict.
- Legal wording would imply compliance, approval, accreditation, or MDE submission.
- A diploma would be generated without parent-defined standards.
- UI would bypass application/domain contracts.
- Work requires a deferred decision from the readiness register.
- Sensitive data would leave local storage without an accepted ADR.
