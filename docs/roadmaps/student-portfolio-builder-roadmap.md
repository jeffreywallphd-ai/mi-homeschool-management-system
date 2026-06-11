# Student Portfolio Builder Roadmap

- Status: implemented
- Last reviewed: 2026-06-11
- Canonical for: student-curated portfolio draft slice
- Related ADRs: [ADR-0003](../adr/ADR-0003-records-and-credentials-module-from-start.md), [ADR-0004](../adr/ADR-0004-local-first-parent-pc-data-ownership.md)
- Related docs: [Portfolio Evidence Rules](../domain/portfolio-evidence-rules.md), [Identity and Access Architecture](../architecture/identity-and-access-architecture.md), [Portfolio and Files Pack](../context/packs/portfolio-and-files.pack.md)
- Related tests: `src/HomeschoolManager.Tests/Program.cs`

## Scope

Add a true student-portal workflow where the student can build a draft portfolio from accepted evidence, and add a parent/admin review workflow for approving, returning, or excluding those draft entries.

## Non-Scope

- Portfolio export packet generation.
- Transcript, diploma, grade, credit, or completion changes.
- Student editing of evidence records, files, grades, assessments, records, or admin settings.
- Legal compliance certification language.

## Phases

1. Research existing evidence, access, repository, and UI conventions.
2. Add domain/application/persistence support for student portfolio draft items.
3. Add the student portal portfolio workspace using existing student portal layout patterns.
4. Add the parent/admin portfolio review page and actions.
5. Surface portfolio review counts and navigation in the parent/admin dashboard.
6. Update docs and context packs.
7. Verify with application tests and isolated admin/student builds.

## Exit Criteria

- Student can add accepted evidence to a portfolio draft, edit draft metadata, and submit it for review from the student portal.
- Parent/admin can view draft portfolio items, approve them, request revision, or exclude them from the admin build.
- Parent/admin student preview remains separate from the true student portal.
- Portfolio drafting does not duplicate evidence or mutate grades, credits, assessments, or completion status.

## Verification

- `dotnet run --project src/HomeschoolManager.Tests/HomeschoolManager.Tests.csproj`
- Isolated student portal build using `.build-tmp/student-out/`
- Isolated parent/admin build using `.build-tmp/admin-out/`
