using HomeschoolManager.Domain.Common;

namespace HomeschoolManager.Domain.Curriculum;

public sealed record Lesson
{
    public Guid Id { get; init; }
    public Guid ModuleId { get; init; }
    public string SourceLessonId { get; init; }
    public int SequenceOrder { get; init; }
    public string Title { get; init; }
    public string IntroductoryText { get; init; }
    public string LinkedModuleObjective { get; init; }
    public LessonType LessonType { get; init; }
    public int EstimatedMinutes { get; init; }
    public int SuggestedDays { get; init; }
    public LessonDifficultyLevel DifficultyLevel { get; init; }
    public IReadOnlyList<string> SubjectAreas { get; init; }
    public IReadOnlyList<string> Tags { get; init; }
    public IReadOnlyList<string> Prerequisites { get; init; }
    public IReadOnlyList<LessonLearningObjective> LearningObjectives { get; init; }
    public IReadOnlyList<StandardsAlignment> StandardsAlignments { get; init; }
    public IReadOnlyList<string> SuccessCriteria { get; init; }
    public IReadOnlyList<LessonStep> LessonSteps { get; init; }
    public IReadOnlyList<LessonResource> Resources { get; init; }
    public IReadOnlyList<LessonProblemSet> ProblemSets { get; init; }
    public IReadOnlyList<LessonPortfolioConnection> PortfolioConnections { get; init; }
    public LessonRubric? Rubric { get; init; }
    public IReadOnlyList<string> ReflectionPrompts { get; init; }
    public LessonInstructorNotes? InstructorNotes { get; init; }
    public IReadOnlyList<Guid> LinkedAssignmentIds { get; init; }

    public Lesson(
        Guid id,
        Guid moduleId,
        string sourceLessonId,
        int sequenceOrder,
        string title,
        string introductoryText,
        string linkedModuleObjective,
        IReadOnlyList<LessonResource>? resources,
        LessonType lessonType = LessonType.SelfGuided,
        int estimatedMinutes = 0,
        int suggestedDays = 0,
        LessonDifficultyLevel difficultyLevel = LessonDifficultyLevel.StandardHighSchool,
        IReadOnlyList<string>? subjectAreas = null,
        IReadOnlyList<string>? tags = null,
        IReadOnlyList<string>? prerequisites = null,
        IReadOnlyList<LessonLearningObjective>? learningObjectives = null,
        IReadOnlyList<StandardsAlignment>? standardsAlignments = null,
        IReadOnlyList<string>? successCriteria = null,
        IReadOnlyList<LessonStep>? lessonSteps = null,
        IReadOnlyList<LessonProblemSet>? problemSets = null,
        IReadOnlyList<LessonPortfolioConnection>? portfolioConnections = null,
        LessonRubric? rubric = null,
        IReadOnlyList<string>? reflectionPrompts = null,
        LessonInstructorNotes? instructorNotes = null,
        IReadOnlyList<Guid>? linkedAssignmentIds = null)
    {
        if (moduleId == Guid.Empty)
        {
            throw new DomainException("Module is required for a lesson.");
        }

        if (sequenceOrder < 1)
        {
            throw new DomainException("Lesson sequence order must be one or greater.");
        }

        if (!Enum.IsDefined(lessonType))
        {
            throw new DomainException("Lesson type is not recognized.");
        }

        if (!Enum.IsDefined(difficultyLevel))
        {
            throw new DomainException("Lesson difficulty level is not recognized.");
        }

        if (estimatedMinutes < 0)
        {
            throw new DomainException("Lesson estimated minutes cannot be negative.");
        }

        if (suggestedDays < 0)
        {
            throw new DomainException("Lesson suggested days cannot be negative.");
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        ModuleId = moduleId;
        SourceLessonId = string.IsNullOrWhiteSpace(sourceLessonId) ? "" : sourceLessonId.Trim();
        SequenceOrder = sequenceOrder;
        Title = Require.Text(title, nameof(title));
        IntroductoryText = Require.Text(introductoryText, nameof(introductoryText));
        LinkedModuleObjective = string.IsNullOrWhiteSpace(linkedModuleObjective) ? "" : linkedModuleObjective.Trim();
        LessonType = lessonType;
        EstimatedMinutes = estimatedMinutes;
        SuggestedDays = suggestedDays;
        DifficultyLevel = difficultyLevel;
        SubjectAreas = NormalizeTextList(subjectAreas);
        Tags = NormalizeTextList(tags);
        Prerequisites = NormalizeTextList(prerequisites);
        LearningObjectives = NormalizeLearningObjectives(learningObjectives);
        StandardsAlignments = NormalizeStandards(standardsAlignments);
        SuccessCriteria = NormalizeTextList(successCriteria);
        LessonSteps = NormalizeSteps(lessonSteps);
        Resources = NormalizeResources(resources);
        ProblemSets = NormalizeProblemSets(problemSets);
        PortfolioConnections = NormalizePortfolioConnections(portfolioConnections);
        Rubric = NormalizeRubric(rubric);
        ReflectionPrompts = NormalizeTextList(reflectionPrompts);
        InstructorNotes = NormalizeInstructorNotes(instructorNotes);
        LinkedAssignmentIds = NormalizeAssignmentIds(linkedAssignmentIds);
    }

    private static IReadOnlyList<LessonResource> NormalizeResources(IReadOnlyList<LessonResource>? resources)
    {
        var normalized = (resources ?? [])
            .Where(resource => !string.IsNullOrWhiteSpace(resource.Name))
            .Select(resource => new LessonResource(
                resource.Id,
                resource.Name,
                resource.Type,
                resource.Url,
                resource.FilePath,
                resource.IsPhysicalResource,
                resource.SourceNote,
                resource.Required,
                resource.EstimatedMinutes,
                resource.StudentInstructions,
                resource.NotesPrompt,
                resource.Citation,
                resource.OfflineAvailable,
                resource.License))
            .ToArray();

        if (normalized.Length == 0)
        {
            throw new DomainException("At least one lesson resource is required.");
        }

        return normalized;
    }

    private static IReadOnlyList<string> NormalizeTextList(IReadOnlyList<string>? values)
    {
        return (values ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<LessonLearningObjective> NormalizeLearningObjectives(IReadOnlyList<LessonLearningObjective>? objectives)
    {
        return (objectives ?? [])
            .Where(objective => !string.IsNullOrWhiteSpace(objective.Text))
            .Select(objective => new LessonLearningObjective(objective.ObjectiveId, objective.Text, objective.BloomLevel))
            .ToArray();
    }

    private static IReadOnlyList<StandardsAlignment> NormalizeStandards(IReadOnlyList<StandardsAlignment>? standards)
    {
        return (standards ?? [])
            .Where(standard => !string.IsNullOrWhiteSpace(standard.Framework) || !string.IsNullOrWhiteSpace(standard.Code) || !string.IsNullOrWhiteSpace(standard.Description))
            .Select(standard => new StandardsAlignment(
                string.IsNullOrWhiteSpace(standard.Framework) ? "" : standard.Framework.Trim(),
                string.IsNullOrWhiteSpace(standard.Code) ? "" : standard.Code.Trim(),
                string.IsNullOrWhiteSpace(standard.Description) ? "" : standard.Description.Trim()))
            .ToArray();
    }

    private static IReadOnlyList<LessonStep> NormalizeSteps(IReadOnlyList<LessonStep>? steps)
    {
        return (steps ?? [])
            .Where(step => !string.IsNullOrWhiteSpace(step.Title))
            .OrderBy(step => step.StepOrder)
            .ThenBy(step => step.Title, StringComparer.OrdinalIgnoreCase)
            .Select((step, index) => new LessonStep(index + 1, step.Title, step.StepType, step.Instructions, step.EstimatedMinutes, step.Required))
            .ToArray();
    }

    private static IReadOnlyList<LessonProblemSet> NormalizeProblemSets(IReadOnlyList<LessonProblemSet>? problemSets)
    {
        return (problemSets ?? [])
            .Where(problemSet => !string.IsNullOrWhiteSpace(problemSet.Title))
            .Select(problemSet => new LessonProblemSet(
                problemSet.ProblemSetId,
                problemSet.Title,
                problemSet.Instructions,
                problemSet.EstimatedMinutes,
                problemSet.Problems))
            .ToArray();
    }

    private static IReadOnlyList<LessonPortfolioConnection> NormalizePortfolioConnections(IReadOnlyList<LessonPortfolioConnection>? connections)
    {
        return (connections ?? [])
            .Where(connection => !string.IsNullOrWhiteSpace(connection.PortfolioSection) || !string.IsNullOrWhiteSpace(connection.ArtifactTitle))
            .Select(connection => new LessonPortfolioConnection(
                string.IsNullOrWhiteSpace(connection.PortfolioSection) ? "" : connection.PortfolioSection.Trim(),
                string.IsNullOrWhiteSpace(connection.ArtifactTitle) ? "" : connection.ArtifactTitle.Trim(),
                string.IsNullOrWhiteSpace(connection.ArtifactPurpose) ? "" : connection.ArtifactPurpose.Trim(),
                NormalizeTextList(connection.CrossCourseLinks),
                string.IsNullOrWhiteSpace(connection.ReuseInstructions) ? "" : connection.ReuseInstructions.Trim()))
            .ToArray();
    }

    private static LessonRubric? NormalizeRubric(LessonRubric? rubric)
    {
        if (rubric is null || string.IsNullOrWhiteSpace(rubric.Scale) && (rubric.Criteria is null || rubric.Criteria.Count == 0))
        {
            return null;
        }

        var criteria = (rubric.Criteria ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item.Criterion))
            .Select(item => new LessonRubricCriterion(
                item.Criterion,
                item.Level4,
                item.Level3,
                item.Level2,
                item.Level1))
            .ToArray();

        return new LessonRubric(
            string.IsNullOrWhiteSpace(rubric.RubricId) ? "" : rubric.RubricId.Trim(),
            string.IsNullOrWhiteSpace(rubric.Scale) ? "" : rubric.Scale.Trim(),
            criteria);
    }

    private static LessonInstructorNotes? NormalizeInstructorNotes(LessonInstructorNotes? notes)
    {
        if (notes is null ||
            string.IsNullOrWhiteSpace(notes.Overview) &&
            (notes.LookFors is null || notes.LookFors.Count == 0) &&
            (notes.CommonIssues is null || notes.CommonIssues.Count == 0) &&
            (notes.SuggestedFeedback is null || notes.SuggestedFeedback.Count == 0))
        {
            return null;
        }

        return new LessonInstructorNotes(
            string.IsNullOrWhiteSpace(notes.Overview) ? "" : notes.Overview.Trim(),
            NormalizeTextList(notes.LookFors),
            NormalizeTextList(notes.CommonIssues),
            NormalizeTextList(notes.SuggestedFeedback));
    }

    private static IReadOnlyList<Guid> NormalizeAssignmentIds(IReadOnlyList<Guid>? assignmentIds)
    {
        return (assignmentIds ?? [])
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
    }
}
