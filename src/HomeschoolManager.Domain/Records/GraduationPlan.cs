using HomeschoolManager.Domain.Common;

namespace HomeschoolManager.Domain.Records;

public sealed record GraduationPlan
{
    public Guid Id { get; init; }
    public Guid StudentId { get; init; }
    public string Title { get; init; }
    public string StandardsSummary { get; init; }
    public bool StandardsAccepted { get; init; }
    public bool RequirementsSatisfiedOrWaived { get; init; }
    public string ParentDecisionNotes { get; init; }
    public string AcceptedBy { get; init; }
    public DateTimeOffset? AcceptedAtUtc { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }

    public GraduationPlan(
        Guid id,
        Guid studentId,
        string title,
        string standardsSummary,
        bool standardsAccepted,
        bool requirementsSatisfiedOrWaived,
        string parentDecisionNotes,
        string acceptedBy,
        DateTimeOffset? acceptedAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        if (studentId == Guid.Empty)
        {
            throw new DomainException("Student is required for a graduation plan.");
        }

        if (standardsAccepted && string.IsNullOrWhiteSpace(standardsSummary))
        {
            throw new DomainException("Describe the parent-defined graduation standards before accepting the plan.");
        }

        if (standardsAccepted && string.IsNullOrWhiteSpace(acceptedBy))
        {
            throw new DomainException("A parent or administrator name is required to accept the graduation plan.");
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        StudentId = studentId;
        Title = Clean(title, "Parent-Defined Graduation Plan");
        StandardsSummary = Clean(standardsSummary);
        StandardsAccepted = standardsAccepted;
        RequirementsSatisfiedOrWaived = requirementsSatisfiedOrWaived;
        ParentDecisionNotes = Clean(parentDecisionNotes);
        AcceptedBy = Clean(acceptedBy);
        AcceptedAtUtc = standardsAccepted ? acceptedAtUtc ?? DateTimeOffset.UtcNow : null;
        UpdatedAtUtc = updatedAtUtc == default ? DateTimeOffset.UtcNow : updatedAtUtc;
    }

    private static string Clean(string value, string fallback = "")
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
