namespace HomeschoolManager.Domain.Curriculum;

public sealed record CourseDescription
{
    public string Description { get; init; }
    public string InstructionalMethods { get; init; }
    public string MajorTopics { get; init; }
    public string TextsAndResources { get; init; }
    public string AssessmentMethods { get; init; }
    public string GradingBasis { get; init; }

    public CourseDescription(
        string description,
        string instructionalMethods,
        string majorTopics,
        string textsAndResources,
        string assessmentMethods,
        string gradingBasis)
    {
        Description = description.Trim();
        InstructionalMethods = instructionalMethods.Trim();
        MajorTopics = majorTopics.Trim();
        TextsAndResources = textsAndResources.Trim();
        AssessmentMethods = assessmentMethods.Trim();
        GradingBasis = gradingBasis.Trim();
    }

    public static CourseDescription Empty => new("", "", "", "", "", "");
}
