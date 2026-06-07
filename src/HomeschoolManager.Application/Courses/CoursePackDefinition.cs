using HomeschoolManager.Domain.Curriculum;

namespace HomeschoolManager.Application.Courses;

public sealed record CoursePackDefinition(
    string Id,
    string Name,
    string Description,
    string RequirementJurisdiction,
    IReadOnlyList<CourseTemplateDefinition> Courses);

public sealed record CourseTemplateDefinition(
    string TemplateId,
    string Title,
    IReadOnlyList<string> SubjectAreas,
    CourseDuration Duration,
    decimal PlannedCreditValue,
    CourseDescription Description,
    CurriculumPlan CurriculumPlan,
    IReadOnlyList<CourseTemplateRequirementMapping> RequirementMappings,
    IReadOnlyList<CourseTemplateModuleDefinition> Modules,
    string DefaultOptionId,
    IReadOnlyList<CourseTemplateOptionDefinition> Options)
{
    public CourseTemplateOptionDefinition DefaultOption =>
        Options.FirstOrDefault(option => string.Equals(option.OptionId, DefaultOptionId, StringComparison.OrdinalIgnoreCase))
        ?? Options.First();
}

public sealed record CourseTemplateOptionDefinition(
    string OptionId,
    string Title,
    IReadOnlyList<string> SubjectAreas,
    CourseDuration Duration,
    decimal PlannedCreditValue,
    CourseDescription Description,
    CurriculumPlan CurriculumPlan,
    IReadOnlyList<CourseTemplateRequirementMapping> RequirementMappings,
    IReadOnlyList<CourseTemplateModuleDefinition> Modules);

public sealed record CourseTemplateModuleDefinition(
    string ModuleId,
    int SequenceOrder,
    string Title,
    string Description,
    int? TermNumber,
    string EstimatedLength,
    string Instructions,
    IReadOnlyList<CourseTemplateModuleObjectiveDefinition> LearningObjectives,
    IReadOnlyList<CourseTemplateModuleResourceDefinition> Resources,
    IReadOnlyList<CourseTemplateLessonDefinition> Lessons,
    IReadOnlyList<CourseTemplateAssignmentDefinition> Assignments,
    string AssignmentEvidencePlaceholder,
    ModuleStatus Status);

public sealed record CourseTemplateModuleObjectiveDefinition(string Text, string LinkedCourseObjective);

public sealed record CourseTemplateModuleResourceDefinition(string Name, string Link, bool IsPhysicalResource);

public sealed record CourseTemplateLessonDefinition(
    string LessonId,
    int SequenceOrder,
    string Title,
    string IntroductoryText,
    string LinkedModuleObjective,
    IReadOnlyList<CourseTemplateLessonResourceDefinition> Resources,
    LessonType LessonType = LessonType.SelfGuided,
    int EstimatedMinutes = 0,
    int SuggestedDays = 0,
    LessonDifficultyLevel DifficultyLevel = LessonDifficultyLevel.StandardHighSchool,
    IReadOnlyList<string>? SubjectAreas = null,
    IReadOnlyList<string>? Tags = null,
    IReadOnlyList<string>? Prerequisites = null,
    IReadOnlyList<LessonLearningObjective>? LearningObjectives = null,
    IReadOnlyList<StandardsAlignment>? StandardsAlignments = null,
    IReadOnlyList<string>? SuccessCriteria = null,
    IReadOnlyList<LessonStep>? LessonSteps = null,
    IReadOnlyList<LessonProblemSet>? ProblemSets = null,
    IReadOnlyList<LessonPortfolioConnection>? PortfolioConnections = null,
    LessonRubric? Rubric = null,
    IReadOnlyList<string>? ReflectionPrompts = null,
    LessonInstructorNotes? InstructorNotes = null,
    IReadOnlyList<string>? LinkedAssignmentIds = null);

public sealed record CourseTemplateLessonResourceDefinition(
    string Name,
    LessonResourceType Type,
    string Url,
    bool IsPhysicalResource,
    string SourceNote,
    bool Required = true,
    int EstimatedMinutes = 0,
    string StudentInstructions = "",
    string NotesPrompt = "",
    LessonResourceCitation? Citation = null,
    bool OfflineAvailable = false,
    string License = "");

public sealed record CourseTemplateAssignmentDefinition(
    string AssignmentId,
    int SequenceOrder,
    IReadOnlyList<CourseTemplateAssignmentVariantDefinition> Variants);

public sealed record CourseTemplateAssignmentVariantDefinition(
    string VariantId,
    AssignmentType Type,
    InstructionalMethodProfile MethodProfile,
    string Title,
    string Instructions,
    string EstimatedEffort,
    string DueTimingLabel,
    IReadOnlyList<string> LinkedModuleObjectives,
    IReadOnlyList<string> LinkedLessonIds,
    string RequiredOutput,
    string ParentNotes,
    bool IsPortfolioCandidate,
    decimal? PlannedPoints,
    decimal? PlannedWeight,
    AssignmentStatus Status,
    string AssignmentSummary = "",
    string StudentFacingGoal = "",
    int? EstimatedMinutesMin = null,
    int? EstimatedMinutesMax = null,
    IReadOnlyList<string>? RequiredDeliverables = null,
    IReadOnlyList<AssignmentSubmissionFormat>? SubmissionFormats = null,
    AssignmentPortfolioConnection? PortfolioConnection = null,
    LessonRubric? Rubric = null,
    string LinkedRubricId = "",
    IReadOnlyList<string>? AssessmentSkills = null,
    IReadOnlyList<string>? StudentChecklist = null,
    IReadOnlyList<AssignmentResource>? Resources = null,
    IReadOnlyList<AssignmentStep>? AssignmentSteps = null,
    AssignmentRevisionPolicy? RevisionPolicy = null,
    AssignmentCompletionCriteria? CompletionCriteria = null,
    IReadOnlyList<string>? ReflectionPrompts = null,
    AssignmentEvidenceRequirements? EvidenceRequirements = null,
    AssignmentScoring? Scoring = null);

public sealed record CourseTemplateRequirementMapping(
    string RequirementAreaView,
    string RequirementAreaName,
    CoverageLevel CoverageLevel,
    string Notes);
