using HomeschoolManager.Domain.Submissions;

namespace HomeschoolManager.Application.Portfolio;

public sealed record StudentPortfolioWorkspace(
    Guid StudentId,
    string StudentName,
    int GradeLevel,
    bool CanStudentAuthor,
    PortfolioDesignView Design,
    IReadOnlyList<PortfolioDraftItemView> DraftItems,
    IReadOnlyList<PortfolioEvidenceOptionView> AvailableEvidence,
    IReadOnlyList<PortfolioAssignmentSuggestionView> AssignmentSuggestions,
    IReadOnlyList<PortfolioAssignmentPlanOptionView> AssignmentPlanOptions,
    PortfolioPreviewView Preview,
    PortfolioPreviewView? LatestApprovedPreview);

public sealed record PortfolioReviewWorkspace(
    Guid StudentId,
    string StudentName,
    int GradeLevel,
    bool StudentAuthoringAllowed,
    IReadOnlyList<PortfolioStudentOption> Students,
    PortfolioDesignView Design,
    IReadOnlyList<PortfolioDraftItemView> Items,
    IReadOnlyList<PortfolioAssignmentCandidateView> AssignmentCandidates,
    IReadOnlyList<PortfolioEvidenceOptionView> AvailableEvidence,
    IReadOnlyList<PortfolioAssignmentSuggestionView> AssignmentSuggestions,
    IReadOnlyList<PortfolioAssignmentPlanOptionView> AssignmentPlanOptions,
    PortfolioPreviewView Preview,
    PortfolioPreviewView? LatestApprovedPreview);

public sealed record PortfolioStudentOption(Guid StudentId, string Name, int GradeLevel);

public sealed record PortfolioDesignView(
    Guid PortfolioDesignId,
    Guid StudentId,
    string Title,
    string Purpose,
    PortfolioDesignStatus Status,
    int Version,
    IReadOnlyList<PortfolioNarrativeBlockView> Narratives,
    IReadOnlyList<PortfolioSectionView> Sections,
    IReadOnlyList<PortfolioSuggestionView> Suggestions,
    IReadOnlyList<PortfolioApprovalSnapshotView> ApprovalSnapshots,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? SubmittedAtUtc,
    DateTimeOffset? ReviewedAtUtc);

public sealed record PortfolioNarrativeBlockView(
    Guid NarrativeId,
    string Prompt,
    string Response,
    int SortOrder,
    int OpenSuggestionCount);

public sealed record PortfolioSectionView(
    Guid SectionId,
    string Heading,
    string Introduction,
    int SortOrder,
    bool IncludeInPortfolio,
    PortfolioSectionStatus Status,
    int ItemCount,
    int OpenSuggestionCount);

public sealed record PortfolioSuggestionView(
    Guid SuggestionId,
    PortfolioSuggestionTargetType TargetType,
    Guid? TargetId,
    string SuggestionText,
    string AuthorDisplayName,
    PortfolioSuggestionStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ResolvedAtUtc);

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

public sealed record PortfolioAssignmentCandidateView(
    Guid EvidenceRecordId,
    Guid StudentId,
    string StudentName,
    Guid CourseId,
    string CourseTitle,
    Guid ModuleId,
    string ModuleTitle,
    Guid AssignmentId,
    string AssignmentTitle,
    Guid SubmissionId,
    string Title,
    string Description,
    string StudentNotes,
    string ParentNotes,
    DateTimeOffset ConfirmedAtUtc,
    bool StudentSuggested,
    int FileCount,
    Guid? PortfolioDraftItemId,
    PortfolioDraftStatus? PortfolioDraftStatus);

public sealed record PortfolioAssignmentSuggestionView(
    Guid SuggestionId,
    Guid StudentId,
    string StudentName,
    Guid CourseId,
    string CourseTitle,
    Guid ModuleId,
    string ModuleTitle,
    Guid AssignmentId,
    string AssignmentTitle,
    string StudentReason,
    string ParentNotes,
    PortfolioAssignmentSuggestionStatus Status,
    bool AcceptedEvidenceExists,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record PortfolioAssignmentPlanOptionView(
    Guid CourseId,
    string CourseTitle,
    Guid ModuleId,
    string ModuleTitle,
    Guid AssignmentId,
    string AssignmentTitle,
    string RequiredOutput,
    bool AlreadySuggested,
    bool AcceptedEvidenceExists);

public sealed record PortfolioDraftItemView(
    Guid PortfolioDraftItemId,
    Guid StudentId,
    string StudentName,
    Guid PortfolioDesignId,
    Guid PortfolioSectionId,
    Guid EvidenceRecordId,
    Guid SubmissionId,
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
    int FileCount,
    int OpenSuggestionCount);

public sealed record PortfolioPreviewView(
    Guid StudentId,
    string StudentName,
    string Title,
    string Purpose,
    PortfolioDesignStatus Status,
    int Version,
    DateTimeOffset? ApprovedAtUtc,
    IReadOnlyList<PortfolioPreviewNarrativeView> Narratives,
    IReadOnlyList<PortfolioPreviewSectionView> Sections);

public sealed record PortfolioPreviewNarrativeView(
    string Prompt,
    string Response,
    int SortOrder);

public sealed record PortfolioPreviewSectionView(
    Guid SectionId,
    string Heading,
    string Introduction,
    int SortOrder,
    IReadOnlyList<PortfolioPreviewItemView> Items);

public sealed record PortfolioPreviewItemView(
    Guid PortfolioDraftItemId,
    Guid EvidenceRecordId,
    string DisplayTitle,
    string CourseTitle,
    string ModuleTitle,
    string AssignmentTitle,
    string StudentReflection,
    string ChosenReason,
    IReadOnlyList<string> SkillsShown,
    string ParentReviewNotes,
    int FileCount,
    int SortOrder);

public sealed record PortfolioApprovalSnapshotView(
    Guid ApprovalSnapshotId,
    int Version,
    string Title,
    DateTimeOffset ApprovedAtUtc,
    string ApprovedBy,
    string ExportManifestName,
    int SectionCount,
    int ItemCount);
