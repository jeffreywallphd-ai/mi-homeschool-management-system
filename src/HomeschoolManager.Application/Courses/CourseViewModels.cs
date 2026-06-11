using HomeschoolManager.Domain.Curriculum;

namespace HomeschoolManager.Application.Courses;

public sealed record CourseListItem(
    Guid Id,
    Guid StudentId,
    string Title,
    string Description,
    IReadOnlyList<string> SubjectAreas,
    CourseDuration Duration,
    decimal PlannedCreditValue,
    CompletionStatus CompletionStatus,
    int MappingCount);

public sealed record CourseListActionResult(
    int SuccessCount,
    IReadOnlyList<CourseListActionFailure> Failures);

public sealed record CourseListActionFailure(Guid CourseId, string CourseTitle, string Message);

public sealed record CourseRequirementMappingView(
    Guid RequirementAreaId,
    string RequirementAreaName,
    string RequirementView,
    CoverageLevel CoverageLevel,
    string Notes);

public sealed record CourseDetail(
    Guid Id,
    Guid StudentId,
    string StudentName,
    string Title,
    IReadOnlyList<string> SubjectAreas,
    CourseDuration Duration,
    decimal PlannedCreditValue,
    CompletionStatus CompletionStatus,
    string Description,
    string InstructionalMethods,
    string MajorTopics,
    string TextsAndResources,
    string AssessmentMethods,
    string GradingBasis,
    string Goals,
    string LearningObjectives,
    string MajorResources,
    string PlannedSequence,
    string ParentNotes,
    IReadOnlyList<CourseTermView> Terms,
    IReadOnlyList<LearningModuleView> Modules,
    IReadOnlyList<CourseRequirementMappingView> Mappings);

public sealed record CourseTermView(Guid Id, string Name, DateOnly StartDate, DateOnly EndDate);

public sealed record LearningModuleView(
    Guid Id,
    Guid CourseId,
    string SourceModuleId,
    int SequenceOrder,
    string Title,
    string Description,
    Guid? TermId,
    string TermName,
    string EstimatedLength,
    string Instructions,
    string MajorTopics,
    string LearningObjectives,
    IReadOnlyList<ModuleLearningObjectiveView> LearningObjectiveItems,
    string Resources,
    IReadOnlyList<ModuleResourceView> ResourceItems,
    string AssignmentEvidencePlaceholder,
    ModuleStatus Status,
    CompletionStatus CompletionStatus,
    IReadOnlyList<LessonView> Lessons,
    IReadOnlyList<AssignmentView> Assignments,
    IReadOnlyList<AssignmentVariantView> AssignmentVariants);

public sealed record ModuleLearningObjectiveView(string Text, string LinkedCourseObjective);

public sealed record ModuleResourceView(string Name, string Link, string FilePath, bool IsPhysicalResource);

public sealed record LessonView(
    Guid Id,
    Guid ModuleId,
    string SourceLessonId,
    int SequenceOrder,
    string Title,
    string IntroductoryText,
    string LinkedModuleObjective,
    LessonType LessonType,
    int EstimatedMinutes,
    int SuggestedDays,
    LessonDifficultyLevel DifficultyLevel,
    CompletionStatus CompletionStatus,
    IReadOnlyList<string> SubjectAreas,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Prerequisites,
    IReadOnlyList<LessonLearningObjectiveView> LearningObjectives,
    IReadOnlyList<StandardsAlignmentView> StandardsAlignments,
    IReadOnlyList<string> SuccessCriteria,
    IReadOnlyList<LessonStepView> LessonSteps,
    IReadOnlyList<LessonResourceView> Resources,
    IReadOnlyList<LessonProblemSetView> ProblemSets,
    IReadOnlyList<LessonPortfolioConnectionView> PortfolioConnections,
    LessonRubricView? Rubric,
    IReadOnlyList<string> ReflectionPrompts,
    LessonInstructorNotesView? InstructorNotes,
    IReadOnlyList<Guid> LinkedAssignmentIds);

public sealed record LessonResourceView(
    Guid Id,
    string Name,
    LessonResourceType Type,
    string Url,
    string FilePath,
    bool IsPhysicalResource,
    string SourceNote,
    bool Required,
    int EstimatedMinutes,
    string StudentInstructions,
    string NotesPrompt,
    LessonResourceCitationView? Citation,
    bool OfflineAvailable,
    string License);

public sealed record LessonResourceCitationView(string Title, string Publisher, DateTimeOffset? AccessedAtUtc);

public sealed record LessonLearningObjectiveView(string ObjectiveId, string Text, BloomLevel BloomLevel);

public sealed record StandardsAlignmentView(string Framework, string Code, string Description);

public sealed record LessonStepView(
    int StepOrder,
    string Title,
    LessonStepType StepType,
    string Instructions,
    int EstimatedMinutes,
    bool Required);

public sealed record LessonProblemSetView(
    string ProblemSetId,
    string Title,
    string Instructions,
    int EstimatedMinutes,
    IReadOnlyList<LessonProblemView> Problems);

public sealed record LessonProblemView(
    string ProblemId,
    string Prompt,
    ProblemResponseType ResponseType,
    string ExpectedAnswer,
    string Solution,
    IReadOnlyList<string> Skills,
    string Difficulty);

public sealed record LessonPortfolioConnectionView(
    string PortfolioSection,
    string ArtifactTitle,
    string ArtifactPurpose,
    IReadOnlyList<string> CrossCourseLinks,
    string ReuseInstructions);

public sealed record LessonRubricView(
    string RubricId,
    string Scale,
    IReadOnlyList<LessonRubricCriterionView> Criteria);

public sealed record LessonRubricCriterionView(
    string Criterion,
    string Level4,
    string Level3,
    string Level2,
    string Level1);

public sealed record LessonInstructorNotesView(
    string Overview,
    IReadOnlyList<string> LookFors,
    IReadOnlyList<string> CommonIssues,
    IReadOnlyList<string> SuggestedFeedback);

public sealed record AssignmentView(
    Guid Id,
    Guid ModuleId,
    string SourceAssignmentId,
    int SequenceOrder,
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
    AssignmentAttemptPolicy AttemptPolicy,
    AssignmentSubmissionStructure SubmissionStructure,
    int DraftCount,
    string AssignmentSummary,
    string StudentFacingGoal,
    int? EstimatedMinutesMin,
    int? EstimatedMinutesMax,
    IReadOnlyList<string> RequiredDeliverables,
    IReadOnlyList<AssignmentSubmissionFormat> SubmissionFormats,
    AssignmentPortfolioConnection? PortfolioConnection,
    LessonRubric? Rubric,
    string LinkedRubricId,
    IReadOnlyList<string> AssessmentSkills,
    IReadOnlyList<string> StudentChecklist,
    IReadOnlyList<AssignmentResource> Resources,
    IReadOnlyList<AssignmentStep> AssignmentSteps,
    AssignmentRevisionPolicy? RevisionPolicy,
    AssignmentCompletionCriteria? CompletionCriteria,
    IReadOnlyList<string> ReflectionPrompts,
    AssignmentEvidenceRequirements? EvidenceRequirements,
    AssignmentScoring? Scoring);

public sealed record AssignmentVariantView(
    string VariantId,
    string SourceAssignmentId,
    string Title,
    AssignmentType Type,
    InstructionalMethodProfile MethodProfile,
    string Instructions,
    string EstimatedEffort,
    string DueTimingLabel,
    IReadOnlyList<string> LinkedModuleObjectives,
    IReadOnlyList<string> LinkedSourceLessonIds,
    string RequiredOutput,
    string ParentNotes,
    bool IsPortfolioCandidate,
    decimal? PlannedPoints,
    decimal? PlannedWeight,
    AssignmentStatus Status,
    AssignmentAttemptPolicy AttemptPolicy,
    AssignmentSubmissionStructure SubmissionStructure,
    int DraftCount,
    string AssignmentSummary,
    string StudentFacingGoal,
    int? EstimatedMinutesMin,
    int? EstimatedMinutesMax,
    IReadOnlyList<string> RequiredDeliverables,
    IReadOnlyList<AssignmentSubmissionFormat> SubmissionFormats,
    AssignmentPortfolioConnection? PortfolioConnection,
    LessonRubric? Rubric,
    string LinkedRubricId,
    IReadOnlyList<string> AssessmentSkills,
    IReadOnlyList<string> StudentChecklist,
    IReadOnlyList<AssignmentResource> Resources,
    IReadOnlyList<AssignmentStep> AssignmentSteps,
    AssignmentRevisionPolicy? RevisionPolicy,
    AssignmentCompletionCriteria? CompletionCriteria,
    IReadOnlyList<string> ReflectionPrompts,
    AssignmentEvidenceRequirements? EvidenceRequirements,
    AssignmentScoring? Scoring);

public sealed record CoverageSummaryItem(
    Guid RequirementAreaId,
    string Source,
    string Name,
    string GradeBand,
    bool IsMapped,
    CoverageLevel? CoverageLevel,
    IReadOnlyList<string> CourseTitles);

public sealed record CoursePackSummary(
    string Id,
    string Name,
    string Description,
    int CourseCount,
    decimal TotalPlannedCredits);

public sealed record CoursePackDownloadFile(
    string FileName,
    string ContentType,
    byte[] Content,
    bool IsArchive);

public sealed record CourseImportResult(Guid CourseId, string CourseTitle);

public sealed record CoursePlanBundleImportResult(int CourseCount, int ModuleCount, int LessonCount, int AssignmentCount);

public sealed record LessonPackDownloadFile(
    string FileName,
    string ContentType,
    byte[] Content);

public sealed record LessonPackImportResult(int LessonCount);

public sealed record AssignmentPackDownloadFile(
    string FileName,
    string ContentType,
    byte[] Content);

public sealed record AssignmentPackImportResult(int AssignmentCount);

public sealed record ModulePackDownloadFile(
    string FileName,
    string ContentType,
    byte[] Content);

public sealed record ModulePackImportResult(Guid ModuleId, string ModuleTitle);

public sealed record CoursePackDetail(
    string Id,
    string Name,
    string Description,
    IReadOnlyList<CoursePackCourseView> Courses);

public sealed record CoursePackCourseView(
    string TemplateId,
    string Title,
    IReadOnlyList<string> SubjectAreas,
    CourseDuration Duration,
    decimal PlannedCreditValue,
    string Description,
    string DefaultOptionId,
    IReadOnlyList<CoursePackOptionView> Options);

public sealed record CoursePackOptionView(
    string OptionId,
    string Title,
    IReadOnlyList<string> SubjectAreas,
    CourseDuration Duration,
    decimal PlannedCreditValue,
    string Description);
