# Michigan Homeschool Context

- Status: accepted
- Last reviewed: 2026-06-06
- Canonical for: Michigan homeschool legal-reference posture used by the app
- Related ADRs: [ADR-0001](../adr/ADR-0001-parent-owned-records-not-state-filings.md), [ADR-0002](../adr/ADR-0002-michigan-as-seeded-jurisdiction-not-hardcoded-app.md)
- Related docs: [Recordkeeping Position](recordkeeping-position.md), [Legal Language Boundaries](legal-language-boundaries.md)
- Related tests: not yet implemented
- Supersedes: none

## Source Summary

Michigan's home-school exemption under MCL 380.1561(3)(f) describes a child educated at home by a parent or legal guardian in an organized educational program covering reading, spelling, mathematics, science, history, civics, literature, writing, and English grammar.

The Michigan Department of Education's exemption (f) home-school page states that MDE plays no role with the home-school family under exemption (f), the family does not report as a nonpublic school to MDE, and the family must provide an organized educational program in those subject areas.

MDE's Homeschooling in Michigan PDF states that parents are not required to submit curriculum or student data to MDE and are responsible for records including gradebooks, progress reports, transcripts, and diplomas.

## Product Interpretation

The system should treat Michigan records as parent-owned evidence artifacts, not state-submitted compliance filings.

The system may provide:

- Michigan subject-area checklist.
- Requirement-area mapping.
- Coverage summaries.
- Parent-owned progress records.
- Report cards, transcripts, diplomas, and packets.

The system must not provide:

- Legal certification.
- State approval.
- MDE submission workflow.
- Accreditation claims.

## Source Links

- MDE Exemption (f) Home School: https://www.michigan.gov/mde/Services/flexible-learning/options/nonpub-home/home-school-information/exemption-f-home-school
- Michigan Legislature MCL 380.1561: https://legislature.mi.gov/Laws/MCL?objectName=MCL-380-1561
- MDE Homeschooling in Michigan PDF: https://www.michigan.gov/mde/-/media/Project/Websites/mde/Flexible-Learning-Options/homeschool/Homeschooling-in-Michigan.pdf

## Review Rule

This document should be reviewed when Michigan homeschool law or MDE home-school guidance changes, or before adding any new legal wording to generated documents.
