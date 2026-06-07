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
    public string AssignmentSummary { get; init; }
    public string StudentFacingGoal { get; init; }
    public string Instructions { get; init; }
    public string EstimatedEffort { get; init; }
    public int? EstimatedMinutesMin { get; init; }
    public int? EstimatedMinutesMax { get; init; }
    public string DueTimingLabel { get; init; }
    public DateOnly? DueDate { get; init; }
    public IReadOnlyList<string> LinkedModuleObjectives { get; init; }
    public IReadOnlyList<Guid> LinkedLessonIds { get; init; }
    public string RequiredOutput { get; init; }
    public IReadOnlyList<string> RequiredDeliverables { get; init; }
    public IReadOnlyList<AssignmentSubmissionFormat> SubmissionFormats { get; init; }
    public AssignmentPortfolioConnection? PortfolioConnection { get; init; }
    public LessonRubric? Rubric { get; init; }
    public string LinkedRubricId { get; init; }
    public IReadOnlyList<string> AssessmentSkills { get; init; }
    public IReadOnlyList<string> StudentChecklist { get; init; }
    public IReadOnlyList<AssignmentResource> Resources { get; init; }
    public IReadOnlyList<AssignmentStep> AssignmentSteps { get; init; }
    public AssignmentRevisionPolicy? RevisionPolicy { get; init; }
    public AssignmentCompletionCriteria? CompletionCriteria { get; init; }
    public IReadOnlyList<string> ReflectionPrompts { get; init; }
    public AssignmentEvidenceRequirements? EvidenceRequirements { get; init; }
    public AssignmentScoring? Scoring { get; init; }
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
        AssignmentStatus status,
        string assignmentSummary = "",
        string studentFacingGoal = "",
        int? estimatedMinutesMin = null,
        int? estimatedMinutesMax = null,
        IReadOnlyList<string>? requiredDeliverables = null,
        IReadOnlyList<AssignmentSubmissionFormat>? submissionFormats = null,
        AssignmentPortfolioConnection? portfolioConnection = null,
        LessonRubric? rubric = null,
        string linkedRubricId = "",
        IReadOnlyList<string>? assessmentSkills = null,
        IReadOnlyList<string>? studentChecklist = null,
        IReadOnlyList<AssignmentResource>? resources = null,
        IReadOnlyList<AssignmentStep>? assignmentSteps = null,
        AssignmentRevisionPolicy? revisionPolicy = null,
        AssignmentCompletionCriteria? completionCriteria = null,
        IReadOnlyList<string>? reflectionPrompts = null,
        AssignmentEvidenceRequirements? evidenceRequirements = null,
        AssignmentScoring? scoring = null)
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

        if (estimatedMinutesMin.HasValue && estimatedMinutesMin.Value < 0 ||
            estimatedMinutesMax.HasValue && estimatedMinutesMax.Value < 0)
        {
            throw new DomainException("Estimated minutes cannot be negative.");
        }

        if (estimatedMinutesMin.HasValue &&
            estimatedMinutesMax.HasValue &&
            estimatedMinutesMax.Value < estimatedMinutesMin.Value)
        {
            throw new DomainException("Maximum estimated minutes cannot be less than minimum estimated minutes.");
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        ModuleId = moduleId;
        SourceAssignmentId = string.IsNullOrWhiteSpace(sourceAssignmentId) ? "" : sourceAssignmentId.Trim();
        SequenceOrder = sequenceOrder;
        Title = Require.Text(title, nameof(title));
        Type = type;
        MethodProfile = methodProfile;
        AssignmentSummary = string.IsNullOrWhiteSpace(assignmentSummary) ? "" : assignmentSummary.Trim();
        StudentFacingGoal = string.IsNullOrWhiteSpace(studentFacingGoal) ? "" : studentFacingGoal.Trim();
        Instructions = Require.Text(instructions, nameof(instructions));
        EstimatedEffort = string.IsNullOrWhiteSpace(estimatedEffort) ? "" : estimatedEffort.Trim();
        EstimatedMinutesMin = estimatedMinutesMin;
        EstimatedMinutesMax = estimatedMinutesMax;
        DueTimingLabel = string.IsNullOrWhiteSpace(dueTimingLabel) ? "" : dueTimingLabel.Trim();
        DueDate = dueDate;
        LinkedModuleObjectives = NormalizeObjectives(linkedModuleObjectives);
        LinkedLessonIds = NormalizeLessonIds(linkedLessonIds);
        RequiredOutput = Require.Text(requiredOutput, nameof(requiredOutput));
        RequiredDeliverables = NormalizeTextList(requiredDeliverables);
        SubmissionFormats = NormalizeSubmissionFormats(submissionFormats);
        PortfolioConnection = NormalizePortfolioConnection(portfolioConnection, isPortfolioCandidate);
        Rubric = NormalizeRubric(rubric);
        LinkedRubricId = string.IsNullOrWhiteSpace(linkedRubricId) ? "" : linkedRubricId.Trim();
        AssessmentSkills = NormalizeTextList(assessmentSkills);
        StudentChecklist = NormalizeTextList(studentChecklist);
        Resources = NormalizeResources(resources);
        AssignmentSteps = NormalizeSteps(assignmentSteps);
        RevisionPolicy = NormalizeRevisionPolicy(revisionPolicy);
        CompletionCriteria = NormalizeCompletionCriteria(completionCriteria);
        ReflectionPrompts = NormalizeTextList(reflectionPrompts);
        EvidenceRequirements = NormalizeEvidenceRequirements(evidenceRequirements);
        Scoring = NormalizeScoring(scoring, plannedPoints, plannedWeight);
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

    private static IReadOnlyList<string> NormalizeTextList(IReadOnlyList<string>? values)
    {
        return (values ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<AssignmentSubmissionFormat> NormalizeSubmissionFormats(IReadOnlyList<AssignmentSubmissionFormat>? formats)
    {
        return (formats ?? [])
            .Where(Enum.IsDefined)
            .Distinct()
            .ToArray();
    }

    private static AssignmentPortfolioConnection? NormalizePortfolioConnection(
        AssignmentPortfolioConnection? connection,
        bool isPortfolioCandidate)
    {
        if (connection is null)
        {
            return isPortfolioCandidate ? new AssignmentPortfolioConnection(true, "", "", "", "", []) : null;
        }

        return new AssignmentPortfolioConnection(
            connection.IsPortfolioCandidate || isPortfolioCandidate,
            Clean(connection.PortfolioSection),
            Clean(connection.ArtifactTitle),
            Clean(connection.ArtifactPurpose),
            Clean(connection.ReuseInstructions),
            NormalizeTextList(connection.CrossCourseLinks));
    }

    private static LessonRubric? NormalizeRubric(LessonRubric? rubric)
    {
        if (rubric is null || string.IsNullOrWhiteSpace(rubric.Scale) && (rubric.Criteria is null || rubric.Criteria.Count == 0))
        {
            return null;
        }

        return new LessonRubric(
            Clean(rubric.RubricId),
            Clean(rubric.Scale),
            (rubric.Criteria ?? [])
                .Where(criteria => !string.IsNullOrWhiteSpace(criteria.Criterion))
                .Select(criteria => new LessonRubricCriterion(
                    criteria.Criterion,
                    criteria.Level4,
                    criteria.Level3,
                    criteria.Level2,
                    criteria.Level1))
                .ToArray());
    }

    private static IReadOnlyList<AssignmentResource> NormalizeResources(IReadOnlyList<AssignmentResource>? resources)
    {
        return (resources ?? [])
            .Where(resource => !string.IsNullOrWhiteSpace(resource.Name))
            .Select(resource => new AssignmentResource(
                Clean(resource.Name),
                Enum.IsDefined(resource.Type) ? resource.Type : LessonResourceType.Website,
                Clean(resource.Url),
                Clean(resource.FilePath),
                resource.IsPhysicalResource || resource.Type == LessonResourceType.PhysicalResource,
                resource.Required,
                Clean(resource.StudentInstructions),
                Clean(resource.SourceNote),
                resource.Citation))
            .ToArray();
    }

    private static IReadOnlyList<AssignmentStep> NormalizeSteps(IReadOnlyList<AssignmentStep>? steps)
    {
        return (steps ?? [])
            .Where(step => !string.IsNullOrWhiteSpace(step.Title))
            .OrderBy(step => step.StepOrder)
            .ThenBy(step => step.Title, StringComparer.OrdinalIgnoreCase)
            .Select((step, index) => new AssignmentStep(index + 1, step.Title, step.Instructions, step.EstimatedMinutes))
            .ToArray();
    }

    private static AssignmentRevisionPolicy? NormalizeRevisionPolicy(AssignmentRevisionPolicy? policy)
    {
        if (policy is null || !policy.AllowRevision && string.IsNullOrWhiteSpace(policy.RevisionExpectation) && policy.MinimumRevisionCount <= 0)
        {
            return null;
        }

        return new AssignmentRevisionPolicy(policy.AllowRevision, Clean(policy.RevisionExpectation), Math.Max(0, policy.MinimumRevisionCount));
    }

    private static AssignmentCompletionCriteria? NormalizeCompletionCriteria(AssignmentCompletionCriteria? criteria)
    {
        if (criteria is null || (criteria.MinimumRequirements is null || criteria.MinimumRequirements.Count == 0) && !criteria.RequiresParentReview && criteria.MasteryThreshold is null)
        {
            return null;
        }

        return new AssignmentCompletionCriteria(NormalizeTextList(criteria.MinimumRequirements), criteria.RequiresParentReview, criteria.MasteryThreshold);
    }

    private static AssignmentEvidenceRequirements? NormalizeEvidenceRequirements(AssignmentEvidenceRequirements? requirements)
    {
        if (requirements is null || !requirements.RetainForRecords && (requirements.RecommendedFileTypes is null || requirements.RecommendedFileTypes.Count == 0) && !requirements.RequiresStudentExplanation && !requirements.RequiresParentEvaluation)
        {
            return null;
        }

        return new AssignmentEvidenceRequirements(
            requirements.RetainForRecords,
            Enum.IsDefined(requirements.EvidenceType) ? requirements.EvidenceType : AssignmentEvidenceType.PracticeWork,
            NormalizeTextList(requirements.RecommendedFileTypes),
            requirements.RequiresStudentExplanation,
            requirements.RequiresParentEvaluation);
    }

    private static AssignmentScoring? NormalizeScoring(AssignmentScoring? scoring, decimal? plannedPoints, decimal? plannedWeight)
    {
        if (scoring is null)
        {
            return plannedPoints is null && plannedWeight is null
                ? null
                : new AssignmentScoring(plannedPoints, plannedWeight, AssignmentGradingMode.Points, true, true);
        }

        return new AssignmentScoring(
            scoring.PlannedPoints ?? plannedPoints,
            scoring.PlannedWeight ?? plannedWeight,
            Enum.IsDefined(scoring.GradingMode) ? scoring.GradingMode : AssignmentGradingMode.Points,
            scoring.CountsTowardGrade,
            scoring.AllowPartialCredit);
    }

    private static string Clean(string value) => string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
}
