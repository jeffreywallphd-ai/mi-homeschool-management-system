using HomeschoolManager.Domain.Curriculum;

namespace HomeschoolManager.Application.Courses;

public sealed record CreateCourseCommand(
    string Title,
    string Description,
    IReadOnlyList<string> SubjectAreas,
    CourseDuration Duration,
    decimal PlannedCreditValue,
    Guid? StudentId = null);

public sealed record UpdateCourseCommand(
    Guid CourseId,
    string Title,
    IReadOnlyList<string> SubjectAreas,
    CourseDuration Duration,
    decimal PlannedCreditValue);

public sealed record SaveCourseDescriptionCommand(
    Guid CourseId,
    string Description,
    string InstructionalMethods,
    string MajorTopics,
    string TextsAndResources,
    string AssessmentMethods,
    string GradingBasis);

public sealed record SaveCurriculumPlanCommand(
    Guid CourseId,
    string Goals,
    string LearningObjectives,
    string MajorResources,
    string PlannedSequence,
    string ParentNotes);

public sealed record RequirementMappingCommand(Guid RequirementAreaId, CoverageLevel CoverageLevel, string Notes);

public sealed record SetCourseRequirementMappingsCommand(Guid CourseId, IReadOnlyList<RequirementMappingCommand> Mappings);

public sealed record CreateLearningModuleCommand(
    Guid CourseId,
    string Title,
    string Description,
    Guid? TermId,
    string EstimatedLength,
    string Instructions,
    IReadOnlyList<ModuleLearningObjectiveCommand> LearningObjectives,
    IReadOnlyList<ModuleResourceCommand> Resources,
    string AssignmentEvidencePlaceholder,
    ModuleStatus Status);

public sealed record UpdateLearningModuleCommand(
    Guid CourseId,
    Guid ModuleId,
    string Title,
    string Description,
    Guid? TermId,
    string EstimatedLength,
    string Instructions,
    IReadOnlyList<ModuleLearningObjectiveCommand> LearningObjectives,
    IReadOnlyList<ModuleResourceCommand> Resources,
    string AssignmentEvidencePlaceholder,
    ModuleStatus Status);

public sealed record ReorderLearningModulesCommand(Guid CourseId, IReadOnlyList<Guid> ModuleIds);

public sealed record DeleteLearningModuleCommand(Guid CourseId, Guid ModuleId, string ConfirmationText);

public sealed record UpdateCourseCompletionStatusCommand(Guid CourseId, CompletionStatus CompletionStatus);

public sealed record UpdateModuleCompletionStatusCommand(Guid CourseId, Guid ModuleId, CompletionStatus CompletionStatus);

public sealed record UpdateLessonCompletionStatusCommand(Guid CourseId, Guid ModuleId, Guid LessonId, CompletionStatus CompletionStatus);

public sealed record ModuleLearningObjectiveCommand(string Text, string LinkedCourseObjective);

public sealed record ModuleResourceCommand(string Name, string Link, string FilePath, bool IsPhysicalResource);

public sealed record CreateLessonCommand(
    Guid CourseId,
    Guid ModuleId,
    string Title,
    string IntroductoryText,
    string LinkedModuleObjective,
    IReadOnlyList<LessonResourceCommand> Resources,
    LessonType LessonType = LessonType.SelfGuided,
    int EstimatedMinutes = 0,
    int SuggestedDays = 0,
    LessonDifficultyLevel DifficultyLevel = LessonDifficultyLevel.StandardHighSchool,
    IReadOnlyList<string>? SubjectAreas = null,
    IReadOnlyList<string>? Tags = null,
    IReadOnlyList<string>? Prerequisites = null,
    IReadOnlyList<LessonLearningObjectiveCommand>? LearningObjectives = null,
    IReadOnlyList<StandardsAlignmentCommand>? StandardsAlignments = null,
    IReadOnlyList<string>? SuccessCriteria = null,
    IReadOnlyList<LessonStepCommand>? LessonSteps = null,
    IReadOnlyList<LessonProblemSetCommand>? ProblemSets = null,
    IReadOnlyList<LessonPortfolioConnectionCommand>? PortfolioConnections = null,
    LessonRubricCommand? Rubric = null,
    IReadOnlyList<string>? ReflectionPrompts = null,
    LessonInstructorNotesCommand? InstructorNotes = null,
    IReadOnlyList<Guid>? LinkedAssignmentIds = null);

public sealed record UpdateLessonCommand(
    Guid CourseId,
    Guid ModuleId,
    Guid LessonId,
    string Title,
    string IntroductoryText,
    string LinkedModuleObjective,
    IReadOnlyList<LessonResourceCommand> Resources,
    LessonType LessonType = LessonType.SelfGuided,
    int EstimatedMinutes = 0,
    int SuggestedDays = 0,
    LessonDifficultyLevel DifficultyLevel = LessonDifficultyLevel.StandardHighSchool,
    IReadOnlyList<string>? SubjectAreas = null,
    IReadOnlyList<string>? Tags = null,
    IReadOnlyList<string>? Prerequisites = null,
    IReadOnlyList<LessonLearningObjectiveCommand>? LearningObjectives = null,
    IReadOnlyList<StandardsAlignmentCommand>? StandardsAlignments = null,
    IReadOnlyList<string>? SuccessCriteria = null,
    IReadOnlyList<LessonStepCommand>? LessonSteps = null,
    IReadOnlyList<LessonProblemSetCommand>? ProblemSets = null,
    IReadOnlyList<LessonPortfolioConnectionCommand>? PortfolioConnections = null,
    LessonRubricCommand? Rubric = null,
    IReadOnlyList<string>? ReflectionPrompts = null,
    LessonInstructorNotesCommand? InstructorNotes = null,
    IReadOnlyList<Guid>? LinkedAssignmentIds = null);

public sealed record ReorderLessonsCommand(Guid CourseId, Guid ModuleId, IReadOnlyList<Guid> LessonIds);

public sealed record DeleteLessonCommand(Guid CourseId, Guid ModuleId, Guid LessonId, string ConfirmationText);

public sealed record LessonResourceCommand(
    string Name,
    LessonResourceType Type,
    string Url,
    string FilePath,
    bool IsPhysicalResource,
    string SourceNote,
    bool Required = true,
    int EstimatedMinutes = 0,
    string StudentInstructions = "",
    string NotesPrompt = "",
    LessonResourceCitationCommand? Citation = null,
    bool OfflineAvailable = false,
    string License = "");

public sealed record LessonResourceCitationCommand(string Title, string Publisher, DateTimeOffset? AccessedAtUtc);

public sealed record LessonLearningObjectiveCommand(string ObjectiveId, string Text, BloomLevel BloomLevel);

public sealed record StandardsAlignmentCommand(string Framework, string Code, string Description);

public sealed record LessonStepCommand(
    int StepOrder,
    string Title,
    LessonStepType StepType,
    string Instructions,
    int EstimatedMinutes,
    bool Required);

public sealed record LessonProblemSetCommand(
    string ProblemSetId,
    string Title,
    string Instructions,
    int EstimatedMinutes,
    IReadOnlyList<LessonProblemCommand> Problems);

public sealed record LessonProblemCommand(
    string ProblemId,
    string Prompt,
    ProblemResponseType ResponseType,
    string ExpectedAnswer,
    string Solution,
    IReadOnlyList<string> Skills,
    string Difficulty);

public sealed record LessonPortfolioConnectionCommand(
    string PortfolioSection,
    string ArtifactTitle,
    string ArtifactPurpose,
    IReadOnlyList<string> CrossCourseLinks,
    string ReuseInstructions);

public sealed record LessonRubricCommand(
    string RubricId,
    string Scale,
    IReadOnlyList<LessonRubricCriterionCommand> Criteria);

public sealed record LessonRubricCriterionCommand(
    string Criterion,
    string Level4,
    string Level3,
    string Level2,
    string Level1);

public sealed record LessonInstructorNotesCommand(
    string Overview,
    IReadOnlyList<string> LookFors,
    IReadOnlyList<string> CommonIssues,
    IReadOnlyList<string> SuggestedFeedback);

public sealed record CreateAssignmentCommand(
    Guid CourseId,
    Guid ModuleId,
    string Title,
    AssignmentType Type,
    InstructionalMethodProfile MethodProfile,
    string Instructions,
    string EstimatedEffort,
    string DueTimingLabel,
    DateOnly? DueDate,
    IReadOnlyList<string> LinkedModuleObjectives,
    IReadOnlyList<Guid> LinkedLessonIds,
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
    AssignmentPortfolioConnectionCommand? PortfolioConnection = null,
    LessonRubricCommand? Rubric = null,
    string LinkedRubricId = "",
    IReadOnlyList<string>? AssessmentSkills = null,
    IReadOnlyList<string>? StudentChecklist = null,
    IReadOnlyList<AssignmentResourceCommand>? Resources = null,
    IReadOnlyList<AssignmentStepCommand>? AssignmentSteps = null,
    AssignmentRevisionPolicyCommand? RevisionPolicy = null,
    AssignmentCompletionCriteriaCommand? CompletionCriteria = null,
    IReadOnlyList<string>? ReflectionPrompts = null,
    AssignmentEvidenceRequirementsCommand? EvidenceRequirements = null,
    AssignmentScoringCommand? Scoring = null,
    AssignmentAttemptPolicy AttemptPolicy = AssignmentAttemptPolicy.SingleAttempt,
    AssignmentSubmissionStructure SubmissionStructure = AssignmentSubmissionStructure.SingleSubmission,
    int DraftCount = 1);

public sealed record UpdateAssignmentCommand(
    Guid CourseId,
    Guid ModuleId,
    Guid AssignmentId,
    string Title,
    AssignmentType Type,
    InstructionalMethodProfile MethodProfile,
    string Instructions,
    string EstimatedEffort,
    string DueTimingLabel,
    DateOnly? DueDate,
    IReadOnlyList<string> LinkedModuleObjectives,
    IReadOnlyList<Guid> LinkedLessonIds,
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
    AssignmentPortfolioConnectionCommand? PortfolioConnection = null,
    LessonRubricCommand? Rubric = null,
    string LinkedRubricId = "",
    IReadOnlyList<string>? AssessmentSkills = null,
    IReadOnlyList<string>? StudentChecklist = null,
    IReadOnlyList<AssignmentResourceCommand>? Resources = null,
    IReadOnlyList<AssignmentStepCommand>? AssignmentSteps = null,
    AssignmentRevisionPolicyCommand? RevisionPolicy = null,
    AssignmentCompletionCriteriaCommand? CompletionCriteria = null,
    IReadOnlyList<string>? ReflectionPrompts = null,
    AssignmentEvidenceRequirementsCommand? EvidenceRequirements = null,
    AssignmentScoringCommand? Scoring = null,
    AssignmentAttemptPolicy AttemptPolicy = AssignmentAttemptPolicy.SingleAttempt,
    AssignmentSubmissionStructure SubmissionStructure = AssignmentSubmissionStructure.SingleSubmission,
    int DraftCount = 1);

public sealed record AssignmentPortfolioConnectionCommand(
    bool IsPortfolioCandidate,
    string PortfolioSection,
    string ArtifactTitle,
    string ArtifactPurpose,
    string ReuseInstructions,
    IReadOnlyList<string> CrossCourseLinks);

public sealed record AssignmentResourceCommand(
    string Name,
    LessonResourceType Type,
    string Url,
    string FilePath,
    bool IsPhysicalResource,
    bool Required,
    string StudentInstructions,
    string SourceNote,
    LessonResourceCitationCommand? Citation = null);

public sealed record AssignmentStepCommand(
    int StepOrder,
    string Title,
    string Instructions,
    int EstimatedMinutes);

public sealed record AssignmentRevisionPolicyCommand(
    bool AllowRevision,
    string RevisionExpectation,
    int MinimumRevisionCount);

public sealed record AssignmentCompletionCriteriaCommand(
    IReadOnlyList<string> MinimumRequirements,
    bool RequiresParentReview,
    decimal? MasteryThreshold);

public sealed record AssignmentEvidenceRequirementsCommand(
    bool RetainForRecords,
    AssignmentEvidenceType EvidenceType,
    IReadOnlyList<string> RecommendedFileTypes,
    bool RequiresStudentExplanation,
    bool RequiresParentEvaluation);

public sealed record AssignmentScoringCommand(
    decimal? PlannedPoints,
    decimal? PlannedWeight,
    AssignmentGradingMode GradingMode,
    bool CountsTowardGrade,
    bool AllowPartialCredit);

public sealed record ReorderAssignmentsCommand(Guid CourseId, Guid ModuleId, IReadOnlyList<Guid> AssignmentIds);

public sealed record DeleteAssignmentCommand(Guid CourseId, Guid ModuleId, Guid AssignmentId, string ConfirmationText);

public sealed record CoursePackSelectionCommand(string TemplateId, string OptionId);

public sealed record CourseListActionCommand(Guid StudentId, IReadOnlyList<Guid> CourseIds, bool ApplyToEntireCourseList);

public sealed record ImportCoursePackCommand(
    string PackId,
    IReadOnlyList<string> TemplateIds,
    IReadOnlyList<CoursePackSelectionCommand> Selections,
    Guid? StudentId = null)
{
    public ImportCoursePackCommand(string packId, IReadOnlyList<string> templateIds)
        : this(packId, templateIds, [], null)
    {
    }
}
