using HomeschoolManager.Domain.Common;

namespace HomeschoolManager.Domain.Curriculum;

public sealed record ModuleAssignment
{
    public Guid Id { get; init; }
    public Guid ModuleId { get; init; }
    public string SourceAssignmentId { get; init; }
    public int SequenceOrder { get; init; }
    public string Title { get; init; }
    public AssignmentType Type { get; init; }
    public InstructionalMethodProfile MethodProfile { get; init; }
    public string Instructions { get; init; }
    public string EstimatedEffort { get; init; }
    public string DueTimingLabel { get; init; }
    public DateOnly? DueDate { get; init; }
    public IReadOnlyList<string> LinkedModuleObjectives { get; init; }
    public IReadOnlyList<Guid> LinkedLessonIds { get; init; }
    public string RequiredOutput { get; init; }
    public string ParentNotes { get; init; }
    public bool IsPortfolioCandidate { get; init; }
    public decimal? PlannedPoints { get; init; }
    public decimal? PlannedWeight { get; init; }
    public AssignmentStatus Status { get; init; }

    public ModuleAssignment(
        Guid id,
        Guid moduleId,
        string sourceAssignmentId,
        int sequenceOrder,
        string title,
        AssignmentType type,
        InstructionalMethodProfile methodProfile,
        string instructions,
        string estimatedEffort,
        string dueTimingLabel,
        DateOnly? dueDate,
        IReadOnlyList<string>? linkedModuleObjectives,
        IReadOnlyList<Guid>? linkedLessonIds,
        string requiredOutput,
        string parentNotes,
        bool isPortfolioCandidate,
        decimal? plannedPoints,
        decimal? plannedWeight,
        AssignmentStatus status)
    {
        if (moduleId == Guid.Empty)
        {
            throw new DomainException("Module is required for an assignment.");
        }

        if (sequenceOrder < 1)
        {
            throw new DomainException("Assignment sequence order must be one or greater.");
        }

        if (!Enum.IsDefined(type))
        {
            throw new DomainException("Assignment type is not recognized.");
        }

        if (!Enum.IsDefined(methodProfile))
        {
            throw new DomainException("Instructional method profile is not recognized.");
        }

        if (!Enum.IsDefined(status))
        {
            throw new DomainException("Assignment status is not recognized.");
        }

        if (plannedPoints.HasValue && plannedPoints.Value < 0)
        {
            throw new DomainException("Planned points cannot be negative.");
        }

        if (plannedWeight.HasValue && plannedWeight.Value < 0)
        {
            throw new DomainException("Planned weight cannot be negative.");
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        ModuleId = moduleId;
        SourceAssignmentId = string.IsNullOrWhiteSpace(sourceAssignmentId) ? "" : sourceAssignmentId.Trim();
        SequenceOrder = sequenceOrder;
        Title = Require.Text(title, nameof(title));
        Type = type;
        MethodProfile = methodProfile;
        Instructions = Require.Text(instructions, nameof(instructions));
        EstimatedEffort = string.IsNullOrWhiteSpace(estimatedEffort) ? "" : estimatedEffort.Trim();
        DueTimingLabel = string.IsNullOrWhiteSpace(dueTimingLabel) ? "" : dueTimingLabel.Trim();
        DueDate = dueDate;
        LinkedModuleObjectives = NormalizeObjectives(linkedModuleObjectives);
        LinkedLessonIds = NormalizeLessonIds(linkedLessonIds);
        RequiredOutput = Require.Text(requiredOutput, nameof(requiredOutput));
        ParentNotes = string.IsNullOrWhiteSpace(parentNotes) ? "" : parentNotes.Trim();
        IsPortfolioCandidate = isPortfolioCandidate;
        PlannedPoints = plannedPoints;
        PlannedWeight = plannedWeight;
        Status = status;
    }

    private static IReadOnlyList<string> NormalizeObjectives(IReadOnlyList<string>? objectives)
    {
        return (objectives ?? [])
            .Where(objective => !string.IsNullOrWhiteSpace(objective))
            .Select(objective => objective.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<Guid> NormalizeLessonIds(IReadOnlyList<Guid>? lessonIds)
    {
        return (lessonIds ?? [])
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
    }
}
