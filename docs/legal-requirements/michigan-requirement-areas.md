# Michigan Requirement Areas

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: Michigan seeded requirement areas
- Related ADRs: [ADR-0002](../adr/ADR-0002-michigan-as-seeded-jurisdiction-not-hardcoded-app.md)
- Related docs: [Michigan Homeschool Context](michigan-homeschool-context.md), [Requirement Set Model](requirement-set-model.md)
- Related tests: not yet implemented
- Supersedes: none

## Statutory Subject Areas

Seed these Michigan exemption (f) subject areas:

- Reading.
- Spelling.
- Mathematics.
- Science.
- History.
- Civics.
- Literature.
- Writing.
- English Grammar.

## MDE Guidance / Course-of-Study Summary Areas

Seed only related planning/checklist areas that are not already represented by the statutory subject list:

- U.S. Constitution.
- Michigan Constitution.

## MMC Reference Areas

Seed only distinct high-school transcript-planning reference areas that are not already represented by the statutory subject list:

- Online Learning Experience.
- Personal Finance.
- Physical Education and Health.
- Visual, Performing, and Applied Arts.
- World Language.

## Modeling Rule

The statutory subject areas are the canonical Michigan requirement checklist in this app.

MDE summary areas and MMC reference areas are secondary planning views. They should be seeded only when they add a distinct planning category that is not already represented by the statutory subject list.

Where MDE or MMC use different terms for coverage already represented by statutory areas, do not duplicate those rows. Example: English Language Arts is represented through the statutory Reading, Spelling, Literature, Writing, and English Grammar rows.

Statutory areas should be listed first in requirement lists and coverage summaries.

When a coverage row is statutory, its source display should be `Statutory` only. Do not display combined source labels such as `Statutory and MDE Summary` or `Statutory; MDE Summary; MMC Reference`.

## Naming Rule

Use clear display labels for parents. Preserve the source view for seeded non-statutory rows, but avoid duplicating overlapping labels such as English / English Language Arts, Mathematics, Science, Social Studies, or Civics when statutory rows already represent the practical coverage area.
