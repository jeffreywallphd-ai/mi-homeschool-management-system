using HomeschoolManager.Domain.Submissions;

namespace HomeschoolManager.Application.Portfolio;

public sealed record StudentPortfolioWorkspace(
    Guid StudentId,
    string StudentName,
    IReadOnlyList<PortfolioDraftItemView> DraftItems,
    IReadOnlyList<PortfolioEvidenceOptionView> AvailableEvidence);

public sealed record PortfolioReviewWorkspace(
    IReadOnlyList<PortfolioDraftItemView> Items);

public sealed record PortfolioEvidenceOptionView(
    Guid EvidenceRecordId,
    Guid StudentId,
    string StudentName,
    Guid CourseId,
    string CourseTitle,
    Guid ModuleId,
    string ModuleTitle,
    Guid AssignmentId,
    string AssignmentTitle,
    string Title,
    string Description,
    DateTimeOffset ConfirmedAtUtc,
    bool PortfolioCandidate,
    int FileCount,
    bool AlreadyAdded);

public sealed record PortfolioDraftItemView(
    Guid PortfolioDraftItemId,
    Guid StudentId,
    string StudentName,
    Guid EvidenceRecordId,
    Guid CourseId,
    string CourseTitle,
    Guid ModuleId,
    string ModuleTitle,
    Guid AssignmentId,
    string AssignmentTitle,
    string DisplayTitle,
    string PortfolioSection,
    string StudentReflection,
    string ChosenReason,
    IReadOnlyList<string> SkillsShown,
    int SortOrder,
    bool IncludeInDraft,
    PortfolioDraftStatus Status,
    string ParentReviewNotes,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? SubmittedAtUtc,
    DateTimeOffset? ReviewedAtUtc,
    bool PortfolioCandidate,
    int FileCount);
