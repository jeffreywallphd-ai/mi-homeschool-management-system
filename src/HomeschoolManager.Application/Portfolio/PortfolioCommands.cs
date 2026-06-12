using HomeschoolManager.Domain.Submissions;

namespace HomeschoolManager.Application.Portfolio;

public sealed record UpdatePortfolioDesignCommand(
    Guid StudentId,
    string Title,
    string Purpose,
    IReadOnlyList<PortfolioNarrativeInput> Narratives);

public sealed record PortfolioNarrativeInput(
    Guid NarrativeId,
    string Prompt,
    string Response,
    int SortOrder);

public sealed record AddPortfolioSectionCommand(
    Guid StudentId,
    string Heading,
    string Introduction);

public sealed record UpdatePortfolioSectionCommand(
    Guid StudentId,
    Guid SectionId,
    string Heading,
    string Introduction,
    int SortOrder,
    bool IncludeInPortfolio);

public sealed record AddPortfolioDraftItemCommand(
    Guid EvidenceRecordId,
    Guid? StudentId = null,
    Guid? PortfolioSectionId = null);

public sealed record UpdatePortfolioDraftItemCommand(
    Guid PortfolioDraftItemId,
    string DisplayTitle,
    Guid PortfolioSectionId,
    string StudentReflection,
    string ChosenReason,
    IReadOnlyList<string> SkillsShown,
    int SortOrder,
    bool IncludeInDraft);

public sealed record SubmitPortfolioDraftItemCommand(Guid PortfolioDraftItemId);

public sealed record SubmitPortfolioDesignCommand(Guid StudentId);

public sealed record ReviewPortfolioDraftItemCommand(
    Guid PortfolioDraftItemId,
    PortfolioDraftStatus Status,
    string ParentReviewNotes);

public sealed record AcceptPortfolioAssignmentCandidateCommand(
    Guid EvidenceRecordId,
    string ParentReviewNotes,
    Guid? PortfolioSectionId = null);

public sealed record SuggestPortfolioAssignmentCommand(
    Guid StudentId,
    Guid CourseId,
    Guid ModuleId,
    Guid AssignmentId,
    string StudentReason);

public sealed record AddPortfolioSuggestionCommand(
    Guid StudentId,
    PortfolioSuggestionTargetType TargetType,
    Guid? TargetId,
    string SuggestionText);

public sealed record ResolvePortfolioSuggestionCommand(
    Guid StudentId,
    Guid SuggestionId);

public sealed record RequestPortfolioRevisionCommand(
    Guid StudentId,
    string ParentReviewNotes);

public sealed record ApprovePortfolioDesignCommand(Guid StudentId);
