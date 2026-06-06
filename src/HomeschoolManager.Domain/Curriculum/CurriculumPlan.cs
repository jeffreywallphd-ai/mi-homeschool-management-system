namespace HomeschoolManager.Domain.Curriculum;

public sealed record CurriculumPlan
{
    public string Goals { get; init; }
    public string LearningObjectives { get; init; }
    public string MajorResources { get; init; }
    public string PlannedSequence { get; init; }
    public string ParentNotes { get; init; }

    public CurriculumPlan(
        string goals,
        string learningObjectives,
        string majorResources,
        string plannedSequence,
        string parentNotes)
    {
        Goals = goals.Trim();
        LearningObjectives = learningObjectives.Trim();
        MajorResources = majorResources.Trim();
        PlannedSequence = plannedSequence.Trim();
        ParentNotes = parentNotes.Trim();
    }

    public static CurriculumPlan Empty => new("", "", "", "", "");
}
