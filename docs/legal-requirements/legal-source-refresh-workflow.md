# Legal Source Refresh Workflow

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: refreshing official legal and MDE sources before legal-facing changes
- Related ADRs: [ADR-0001](../adr/ADR-0001-parent-owned-records-not-state-filings.md), [ADR-0002](../adr/ADR-0002-michigan-as-seeded-jurisdiction-not-hardcoded-app.md)
- Related docs: [Michigan Homeschool Context](michigan-homeschool-context.md), [Legal Language Boundaries](legal-language-boundaries.md), [Source Review Log](source-review-log.md)
- Related tests: not yet implemented
- Supersedes: none

## When To Use

Use this workflow before changing:

- Michigan requirement areas.
- Requirement checklist or coverage wording.
- Generated report card, transcript, diploma, or packet legal wording.
- Any UI text that sounds like legal, compliance, approval, accreditation, or MDE guidance.
- Any documentation that summarizes Michigan homeschool legal posture.

## Official Source Priority

Use official sources first:

1. Michigan Legislature source for MCL 380.1561.
2. Michigan Department of Education home-school pages.
3. Current MDE homeschool PDFs/manuals when directly relevant.

Secondary homeschool summaries may help locate topics, but they must not override official sources.

## Refresh Steps

1. Re-open the official source URLs listed in `source-review-log.md`.
2. Confirm whether the statutory subject-area list has changed.
3. Confirm whether MDE's exemption (f) no-role/no-reporting posture has changed.
4. Confirm whether MDE's parent-record responsibility language has changed.
5. Check whether any new official MDE homeschool document supersedes a source already logged.
6. Update `source-review-log.md` with review date, source, and impact.
7. Update affected legal-requirements docs.
8. Update affected context packs if repeated agent routing would change.

## Required Output For Legal-Facing Changes

Any task that changes legal-facing behavior or wording should report:

- Sources checked.
- Date checked.
- Whether source wording changed.
- Docs updated.
- Any unresolved uncertainty.

## Wording Guardrail

Even after refresh, the app must not claim legal advice, legal compliance, state approval, MDE approval, accreditation, or state-issued credentials unless a future accepted ADR and official source review explicitly authorize that wording.
