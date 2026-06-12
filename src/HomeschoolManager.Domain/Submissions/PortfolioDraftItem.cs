using HomeschoolManager.Domain.Common;

namespace HomeschoolManager.Domain.Submissions;

public sealed record PortfolioDraftItem
{
    public Guid Id { get; init; }
    public Guid StudentId { get; init; }
    public Guid PortfolioDesignId { get; init; }
    public Guid PortfolioSectionId { get; init; }
    public Guid EvidenceRecordId { get; init; }
    public string DisplayTitle { get; init; }
    public string PortfolioSection { get; init; }
    public string StudentReflection { get; init; }
    public string ChosenReason { get; init; }
    public IReadOnlyList<string> SkillsShown { get; init; }
    public int SortOrder { get; init; }
    public bool IncludeInDraft { get; init; }
    public PortfolioDraftStatus Status { get; init; }
    public string ParentReviewNotes { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }
    public DateTimeOffset? SubmittedAtUtc { get; init; }
    public DateTimeOffset? ReviewedAtUtc { get; init; }

    public PortfolioDraftItem(
        Guid id,
        Guid studentId,
        Guid evidenceRecordId,
        string displayTitle,
        string portfolioSection,
        string studentReflection,
        string chosenReason,
        IReadOnlyList<string>? skillsShown,
        int sortOrder,
        bool includeInDraft,
        PortfolioDraftStatus status,
        string parentReviewNotes,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? submittedAtUtc = null,
        DateTimeOffset? reviewedAtUtc = null,
        Guid portfolioDesignId = default,
        Guid portfolioSectionId = default)
    {
        if (studentId == Guid.Empty)
        {
            throw new DomainException("Student is required for a portfolio draft item.");
        }

        if (evidenceRecordId == Guid.Empty)
        {
            throw new DomainException("Evidence is required for a portfolio draft item.");
        }

        if (!Enum.IsDefined(status))
        {
            throw new DomainException("Portfolio draft status is not recognized.");
        }

        if (sortOrder < 0)
        {
            throw new DomainException("Portfolio draft sort order cannot be negative.");
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        StudentId = studentId;
        PortfolioDesignId = portfolioDesignId;
        PortfolioSectionId = portfolioSectionId;
        EvidenceRecordId = evidenceRecordId;
        DisplayTitle = Require.Text(displayTitle, nameof(displayTitle));
        PortfolioSection = Clean(portfolioSection);
        StudentReflection = Clean(studentReflection);
        ChosenReason = Clean(chosenReason);
        SkillsShown = (skillsShown ?? [])
            .Where(skill => !string.IsNullOrWhiteSpace(skill))
            .Select(skill => skill.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        SortOrder = sortOrder;
        IncludeInDraft = includeInDraft;
        Status = status;
        ParentReviewNotes = Clean(parentReviewNotes);
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
