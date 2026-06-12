# Source Review Log

- Status: accepted
- Last reviewed: 2026-06-12
- Canonical for: legal-reference source tracking
- Related ADRs: [ADR-0001](../adr/ADR-0001-parent-owned-records-not-state-filings.md), [ADR-0002](../adr/ADR-0002-michigan-as-seeded-jurisdiction-not-hardcoded-app.md)
- Related docs: [Michigan Homeschool Context](michigan-homeschool-context.md), [Legal Language Boundaries](legal-language-boundaries.md)
- Related tests: not yet implemented
- Supersedes: none

## Reviewed Sources

| Date reviewed | Source | Use in project |
| --- | --- | --- |
| 2026-06-06 | MDE Exemption (f) Home School page | Confirms MDE no-role/no-reporting posture for exemption (f) and the organized educational program subject areas. |
| 2026-06-06 | Michigan Legislature MCL 380.1561 | Confirms statutory exemption text and subject-area list. |
| 2026-06-06 | MDE Homeschooling in Michigan PDF | Confirms no required curriculum/student-data submission to MDE and parent responsibility for records including transcripts and diplomas. |
| 2026-06-06 | MDE Michigan Merit Curriculum FAQ Introduction | Confirms the 18-credit MMC categories, nonpublic/home school graduation-criteria boundary, and one-semester Civics/Government note. |
| 2026-06-06 | MDE Michigan Merit Curriculum Overview PDF | Confirms MMC reference categories and that courses, CTE programs, internships, and other learning opportunities can provide pieces of multiple credits. |
| 2026-06-06 | MDE Exemption (f), MCL 380.1561, and MMC overview refresh | Supports treating statutory homeschool subject areas as canonical and seeding MDE/MMC rows only for distinct planning categories not already represented by statutory rows. |
| 2026-06-06 | MDE Exemption (f), MCL 380.1561, and MMC overview refresh | Supports displaying statutory rows as the core source and omitting duplicated MDE/MMC aliases in parent-facing requirement lists. |
| 2026-06-12 | MDE Exemption (f) Home School page refresh for transcript packet wording | No change to project posture. Transcript wording remains family-issued, parent-owned, and avoids state approval, accreditation, compliance, or MDE submission claims. |
| 2026-06-12 | MDE Exemption (f) Home School page and MCL 380.1561 refresh for transcript coverage wording | No change to project posture. Partial transcript coverage wording remains a records-accuracy statement, not legal compliance, approval, accreditation, or MDE submission guidance. |

## Source URLs

- https://www.michigan.gov/mde/Services/flexible-learning/options/nonpub-home/home-school-information/exemption-f-home-school
- https://legislature.mi.gov/Laws/MCL?objectName=MCL-380-1561
- https://www.michigan.gov/mde/-/media/Project/Websites/mde/Flexible-Learning-Options/homeschool/Homeschooling-in-Michigan.pdf
- https://www.michigan.gov/mde/services/academic-standards/mmc/michigan_merit_curriculum_faq_guidance/general-mmc-topics/introduction
- https://www.michigan.gov/mde/-/media/Project/Websites/mde/Academic-Standards/Personal-Finance/Michigan_Merit_Curriculum_Overview.pdf

## Maintenance Rule

Before adding or changing legal-facing behavior, follow [Legal Source Refresh Workflow](legal-source-refresh-workflow.md), re-check current official sources, and update this log. Prefer official MDE and Michigan Legislature sources over secondary summaries.
