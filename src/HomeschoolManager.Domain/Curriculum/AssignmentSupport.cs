namespace HomeschoolManager.Domain.Curriculum;

public enum AssignmentSubmissionFormat
{
    WrittenResponse,
    WorkedSolutions,
    Spreadsheet,
    Graph,
    DataTable,
    FieldNotes,
    PhotoEvidence,
    PortfolioEntry,
    DecisionMemo,
    Budget,
    Reflection,
    Presentation,
    PracticalDemonstration,
    WrittenMemo,
    WrittenAnalysis,
    SpreadsheetOptional,
    GraphOptional
}

public enum AssignmentGradingMode
{
    Completion,
    Points,
    Rubric,
    ParentReview,
    NotGraded
}

public enum AssignmentEvidenceType
{
    PracticeWork,
    PortfolioArtifact,
    Assessment,
    Project,
    Reflection,
    FieldObservation,
    ParentConference
}

public sealed record AssignmentStep
{
    public int StepOrder { get; init; }
    public string Title { get; init; }
    public string Instructions { get; init; }
    public int EstimatedMinutes { get; init; }

    public AssignmentStep(int stepOrder, string title, string instructions, int estimatedMinutes)
    {
        StepOrder = stepOrder < 1 ? 1 : stepOrder;
        Title = Clean(title);
        Instructions = Clean(instructions);
        EstimatedMinutes = estimatedMinutes < 0 ? 0 : estimatedMinutes;
    }

    private static string Clean(string value) => string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
}

public sealed record AssignmentResource(
    string Name,
    LessonResourceType Type,
    string Url,
    string FilePath,
    bool IsPhysicalResource,
    bool Required,
    string StudentInstructions,
    string SourceNote,
    LessonResourceCitation? Citation = null);

public sealed record AssignmentPortfolioConnection(
    bool IsPortfolioCandidate,
    string PortfolioSection,
    string ArtifactTitle,
    string ArtifactPurpose,
    string ReuseInstructions,
    IReadOnlyList<string> CrossCourseLinks);

public sealed record AssignmentRevisionPolicy(
    bool AllowRevision,
    string RevisionExpectation,
    int MinimumRevisionCount);

public sealed record AssignmentCompletionCriteria(
    IReadOnlyList<string> MinimumRequirements,
    bool RequiresParentReview,
    decimal? MasteryThreshold);

public sealed record AssignmentEvidenceRequirements(
    bool RetainForRecords,
    AssignmentEvidenceType EvidenceType,
    IReadOnlyList<string> RecommendedFileTypes,
    bool RequiresStudentExplanation,
    bool RequiresParentEvaluation);

public sealed record AssignmentScoring(
    decimal? PlannedPoints,
    decimal? PlannedWeight,
    AssignmentGradingMode GradingMode,
    bool CountsTowardGrade,
    bool AllowPartialCredit);
