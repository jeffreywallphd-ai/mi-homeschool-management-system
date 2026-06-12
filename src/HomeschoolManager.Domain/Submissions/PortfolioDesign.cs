using HomeschoolManager.Domain.Common;

namespace HomeschoolManager.Domain.Submissions;

public sealed record PortfolioDesign
{
    public Guid Id { get; init; }
    public Guid StudentId { get; init; }
    public string Title { get; init; }
    public string Purpose { get; init; }
    public PortfolioDesignStatus Status { get; init; }
    public int Version { get; init; }
    public IReadOnlyList<PortfolioNarrativeBlock> Narratives { get; init; }
    public IReadOnlyList<PortfolioSection> Sections { get; init; }
    public IReadOnlyList<PortfolioSuggestion> Suggestions { get; init; }
    public IReadOnlyList<PortfolioAssignmentSuggestion> AssignmentSuggestions { get; init; }
    public IReadOnlyList<PortfolioApprovalSnapshot> ApprovalSnapshots { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }
    public DateTimeOffset? SubmittedAtUtc { get; init; }
    public DateTimeOffset? ReviewedAtUtc { get; init; }

    public PortfolioDesign(
        Guid id,
        Guid studentId,
        string title,
        string purpose,
        PortfolioDesignStatus status,
        int version,
        IReadOnlyList<PortfolioNarrativeBlock>? narratives,
        IReadOnlyList<PortfolioSection>? sections,
        IReadOnlyList<PortfolioSuggestion>? suggestions,
        IReadOnlyList<PortfolioAssignmentSuggestion>? assignmentSuggestions,
        IReadOnlyList<PortfolioApprovalSnapshot>? approvalSnapshots,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? submittedAtUtc = null,
        DateTimeOffset? reviewedAtUtc = null)
    {
        if (studentId == Guid.Empty)
        {
            throw new DomainException("Student is required for a portfolio design.");
        }

        if (!Enum.IsDefined(status))
        {
            throw new DomainException("Portfolio design status is not recognized.");
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        StudentId = studentId;
        Title = Require.Text(title, nameof(title));
        Purpose = Clean(purpose);
        Status = status;
        Version = Math.Max(1, version);
        Narratives = (narratives ?? []).OrderBy(item => item.SortOrder).ToArray();
        Sections = (sections ?? []).OrderBy(item => item.SortOrder).ToArray();
        Suggestions = (suggestions ?? []).OrderByDescending(item => item.CreatedAtUtc).ToArray();
        AssignmentSuggestions = (assignmentSuggestions ?? []).OrderByDescending(item => item.CreatedAtUtc).ToArray();
        ApprovalSnapshots = (approvalSnapshots ?? []).OrderByDescending(item => item.ApprovedAtUtc).ToArray();
        CreatedAtUtc = createdAtUtc == default ? DateTimeOffset.UtcNow : createdAtUtc;
        UpdatedAtUtc = updatedAtUtc == default ? CreatedAtUtc : updatedAtUtc;
        SubmittedAtUtc = submittedAtUtc;
        ReviewedAtUtc = reviewedAtUtc;
    }

    private static string Clean(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
    }
}

public sealed record PortfolioNarrativeBlock
{
    public Guid Id { get; init; }
    public string Prompt { get; init; }
    public string Response { get; init; }
    public int SortOrder { get; init; }

    public PortfolioNarrativeBlock(Guid id, string prompt, string response, int sortOrder)
    {
        if (sortOrder < 0)
        {
            throw new DomainException("Portfolio narrative sort order cannot be negative.");
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Prompt = Require.Text(prompt, nameof(prompt));
        Response = Clean(response);
        SortOrder = sortOrder;
    }

    private static string Clean(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
    }
}

public sealed record PortfolioSection
{
    public Guid Id { get; init; }
    public string Heading { get; init; }
    public string Introduction { get; init; }
    public int SortOrder { get; init; }
    public bool IncludeInPortfolio { get; init; }
    public PortfolioSectionStatus Status { get; init; }

    public PortfolioSection(
        Guid id,
        string heading,
        string introduction,
        int sortOrder,
        bool includeInPortfolio,
        PortfolioSectionStatus status)
    {
        if (sortOrder < 0)
        {
            throw new DomainException("Portfolio section sort order cannot be negative.");
        }

        if (!Enum.IsDefined(status))
        {
            throw new DomainException("Portfolio section status is not recognized.");
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Heading = Require.Text(heading, nameof(heading));
        Introduction = Clean(introduction);
        SortOrder = sortOrder;
        IncludeInPortfolio = includeInPortfolio;
        Status = status;
    }

    private static string Clean(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
    }
}

public sealed record PortfolioSuggestion
{
    public Guid Id { get; init; }
    public PortfolioSuggestionTargetType TargetType { get; init; }
    public Guid? TargetId { get; init; }
    public string SuggestionText { get; init; }
    public string AuthorDisplayName { get; init; }
    public PortfolioSuggestionStatus Status { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset? ResolvedAtUtc { get; init; }

    public PortfolioSuggestion(
        Guid id,
        PortfolioSuggestionTargetType targetType,
        Guid? targetId,
        string suggestionText,
        string authorDisplayName,
        PortfolioSuggestionStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? resolvedAtUtc = null)
    {
        if (!Enum.IsDefined(targetType))
        {
            throw new DomainException("Portfolio suggestion target is not recognized.");
        }

        if (!Enum.IsDefined(status))
        {
            throw new DomainException("Portfolio suggestion status is not recognized.");
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        TargetType = targetType;
        TargetId = targetId == Guid.Empty ? null : targetId;
        SuggestionText = Require.Text(suggestionText, nameof(suggestionText));
        AuthorDisplayName = Require.Text(authorDisplayName, nameof(authorDisplayName));
        Status = status;
        CreatedAtUtc = createdAtUtc == default ? DateTimeOffset.UtcNow : createdAtUtc;
        ResolvedAtUtc = resolvedAtUtc;
    }
}

public sealed record PortfolioAssignmentSuggestion
{
    public Guid Id { get; init; }
    public Guid CourseId { get; init; }
    public Guid ModuleId { get; init; }
    public Guid AssignmentId { get; init; }
    public string StudentReason { get; init; }
    public string ParentNotes { get; init; }
    public PortfolioAssignmentSuggestionStatus Status { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }

    public PortfolioAssignmentSuggestion(
        Guid id,
        Guid courseId,
        Guid moduleId,
        Guid assignmentId,
        string studentReason,
        string parentNotes,
        PortfolioAssignmentSuggestionStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        if (courseId == Guid.Empty || moduleId == Guid.Empty || assignmentId == Guid.Empty)
        {
            throw new DomainException("Course, module, and assignment are required for a portfolio assignment suggestion.");
        }

        if (!Enum.IsDefined(status))
        {
            throw new DomainException("Portfolio assignment suggestion status is not recognized.");
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        CourseId = courseId;
        ModuleId = moduleId;
        AssignmentId = assignmentId;
        StudentReason = Clean(studentReason);
        ParentNotes = Clean(parentNotes);
        Status = status;
        CreatedAtUtc = createdAtUtc == default ? DateTimeOffset.UtcNow : createdAtUtc;
        UpdatedAtUtc = updatedAtUtc == default ? CreatedAtUtc : updatedAtUtc;
    }

    private static string Clean(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
    }
}

public sealed record PortfolioApprovalSnapshot(
    Guid Id,
    int Version,
    string Title,
    string Purpose,
    IReadOnlyList<PortfolioApprovedNarrativeSnapshot> Narratives,
    IReadOnlyList<PortfolioApprovedSectionSnapshot> Sections,
    DateTimeOffset ApprovedAtUtc,
    string ApprovedBy,
    string ExportManifestName);

public sealed record PortfolioApprovedNarrativeSnapshot(
    string Prompt,
    string Response,
    int SortOrder);

public sealed record PortfolioApprovedSectionSnapshot(
    Guid SectionId,
    string Heading,
    string Introduction,
    int SortOrder,
    IReadOnlyList<PortfolioApprovedItemSnapshot> Items);

public sealed record PortfolioApprovedItemSnapshot(
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
