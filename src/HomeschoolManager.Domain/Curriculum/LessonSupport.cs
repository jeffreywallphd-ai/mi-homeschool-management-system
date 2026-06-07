using HomeschoolManager.Domain.Common;

namespace HomeschoolManager.Domain.Curriculum;

public enum LessonType
{
    SelfGuided = 0,
    ParentLed = 1,
    Discussion = 2,
    LabOrFieldwork = 3,
    ProjectStudio = 4,
    AssessmentPrep = 5
}

public enum LessonDifficultyLevel
{
    IntroductoryHighSchool = 0,
    StandardHighSchool = 1,
    AdvancedHighSchool = 2,
    CollegePreparatory = 3
}

public enum BloomLevel
{
    Remember = 0,
    Understand = 1,
    Apply = 2,
    Analyze = 3,
    Evaluate = 4,
    Create = 5
}

public enum LessonStepType
{
    Reading = 0,
    Video = 1,
    Notes = 2,
    Discussion = 3,
    Practice = 4,
    ProblemSet = 5,
    LabOrSimulation = 6,
    PortfolioArtifact = 7,
    Reflection = 8,
    ParentConference = 9,
    Planning = 10,
    Research = 11
}

public enum ProblemResponseType
{
    ShortAnswer = 0,
    WorkedSolution = 1,
    Essay = 2,
    Diagram = 3,
    Spreadsheet = 4,
    OralExplanation = 5,
    WrittenExplanation = 6,
    GraphAndWrittenAnalysis = 7
}

public sealed record LessonLearningObjective
{
    public string ObjectiveId { get; init; }
    public string Text { get; init; }
    public BloomLevel BloomLevel { get; init; }

    public LessonLearningObjective(string objectiveId, string text, BloomLevel bloomLevel)
    {
        if (!Enum.IsDefined(bloomLevel))
        {
            throw new DomainException("Lesson objective Bloom level is not recognized.");
        }

        ObjectiveId = string.IsNullOrWhiteSpace(objectiveId) ? "" : objectiveId.Trim();
        Text = Require.Text(text, nameof(text));
        BloomLevel = bloomLevel;
    }
}

public sealed record StandardsAlignment(string Framework, string Code, string Description);

public sealed record LessonStep
{
    public int StepOrder { get; init; }
    public string Title { get; init; }
    public LessonStepType StepType { get; init; }
    public string Instructions { get; init; }
    public int EstimatedMinutes { get; init; }
    public bool Required { get; init; }

    public LessonStep(int stepOrder, string title, LessonStepType stepType, string instructions, int estimatedMinutes, bool required)
    {
        if (stepOrder < 1)
        {
            throw new DomainException("Lesson step order must be one or greater.");
        }

        if (!Enum.IsDefined(stepType))
        {
            throw new DomainException("Lesson step type is not recognized.");
        }

        if (estimatedMinutes < 0)
        {
            throw new DomainException("Lesson step estimated minutes cannot be negative.");
        }

        StepOrder = stepOrder;
        Title = Require.Text(title, nameof(title));
        StepType = stepType;
        Instructions = Require.Text(instructions, nameof(instructions));
        EstimatedMinutes = estimatedMinutes;
        Required = required;
    }
}

public sealed record LessonProblemSet
{
    public string ProblemSetId { get; init; }
    public string Title { get; init; }
    public string Instructions { get; init; }
    public int EstimatedMinutes { get; init; }
    public IReadOnlyList<LessonProblem> Problems { get; init; }

    public LessonProblemSet(string problemSetId, string title, string instructions, int estimatedMinutes, IReadOnlyList<LessonProblem>? problems)
    {
        if (estimatedMinutes < 0)
        {
            throw new DomainException("Problem set estimated minutes cannot be negative.");
        }

        ProblemSetId = string.IsNullOrWhiteSpace(problemSetId) ? "" : problemSetId.Trim();
        Title = Require.Text(title, nameof(title));
        Instructions = string.IsNullOrWhiteSpace(instructions) ? "" : instructions.Trim();
        EstimatedMinutes = estimatedMinutes;
        Problems = (problems ?? [])
            .Where(problem => !string.IsNullOrWhiteSpace(problem.Prompt))
            .Select(problem => new LessonProblem(
                problem.ProblemId,
                problem.Prompt,
                problem.ResponseType,
                problem.ExpectedAnswer,
                problem.Solution,
                problem.Skills,
                problem.Difficulty))
            .ToArray();
    }
}

public sealed record LessonProblem
{
    public string ProblemId { get; init; }
    public string Prompt { get; init; }
    public ProblemResponseType ResponseType { get; init; }
    public string ExpectedAnswer { get; init; }
    public string Solution { get; init; }
    public IReadOnlyList<string> Skills { get; init; }
    public string Difficulty { get; init; }

    public LessonProblem(
        string problemId,
        string prompt,
        ProblemResponseType responseType,
        string expectedAnswer,
        string solution,
        IReadOnlyList<string>? skills,
        string difficulty)
    {
        if (!Enum.IsDefined(responseType))
        {
            throw new DomainException("Problem response type is not recognized.");
        }

        ProblemId = string.IsNullOrWhiteSpace(problemId) ? "" : problemId.Trim();
        Prompt = Require.Text(prompt, nameof(prompt));
        ResponseType = responseType;
        ExpectedAnswer = string.IsNullOrWhiteSpace(expectedAnswer) ? "" : expectedAnswer.Trim();
        Solution = string.IsNullOrWhiteSpace(solution) ? "" : solution.Trim();
        Skills = NormalizeLines(skills);
        Difficulty = string.IsNullOrWhiteSpace(difficulty) ? "" : difficulty.Trim();
    }

    private static IReadOnlyList<string> NormalizeLines(IReadOnlyList<string>? values)
    {
        return (values ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

public sealed record LessonPortfolioConnection(
    string PortfolioSection,
    string ArtifactTitle,
    string ArtifactPurpose,
    IReadOnlyList<string> CrossCourseLinks,
    string ReuseInstructions);

public sealed record LessonRubric(
    string RubricId,
    string Scale,
    IReadOnlyList<LessonRubricCriterion> Criteria);

public sealed record LessonRubricCriterion(
    string Criterion,
    string Level4,
    string Level3,
    string Level2,
    string Level1);

public sealed record LessonInstructorNotes(
    string Overview,
    IReadOnlyList<string> LookFors,
    IReadOnlyList<string> CommonIssues,
    IReadOnlyList<string> SuggestedFeedback);

public sealed record LessonResourceCitation(
    string Title,
    string Publisher,
    DateTimeOffset? AccessedAtUtc);
