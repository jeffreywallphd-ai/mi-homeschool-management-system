using HomeschoolManager.Domain.Submissions;

namespace HomeschoolManager.Application.Portfolio;

public sealed record AddPortfolioDraftItemCommand(Guid EvidenceRecordId, Guid? StudentId = null);

public sealed record UpdatePortfolioDraftItemCommand(
    Guid PortfolioDraftItemId,
    string DisplayTitle,
    string PortfolioSection,
    string StudentReflection,
    string ChosenReason,
    IReadOnlyList<string> SkillsShown,
    int SortOrder,
    bool IncludeInDraft);

public sealed record SubmitPortfolioDraftItemCommand(Guid PortfolioDraftItemId);

public sealed record ReviewPortfolioDraftItemCommand(
    Guid PortfolioDraftItemId,
    PortfolioDraftStatus Status,
    string ParentReviewNotes);
