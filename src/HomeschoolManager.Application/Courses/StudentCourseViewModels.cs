using HomeschoolManager.Domain.Curriculum;
using HomeschoolManager.Domain.Submissions;
using HomeschoolManager.Domain.Assessments;

namespace HomeschoolManager.Application.Courses;

public sealed record StudentCourseCard(
    Guid StudentId,
    Guid CourseId,
    string Title,
    string Description,
    CourseDuration Duration,
    decimal PlannedCreditValue,
    CompletionStatus CompletionStatus,
    string CurrentGrade,
    int ModuleCount,
    int CompletedModuleCount,
    IReadOnlyList<string> TermNames);

public sealed record StudentCoursePage(
    Guid StudentId,
    string StudentFirstName,
    Guid CourseId,
    string Title,
    string Description,
    CourseDuration Duration,
    decimal PlannedCreditValue,
    CompletionStatus CompletionStatus,
    string CurrentGrade,
    IReadOnlyList<string> TermNames,
    IReadOnlyList<string> LearningObjectives,
    IReadOnlyList<StudentModuleLink> Modules);

public sealed record StudentCourseSyllabus(
    Guid StudentId,
    Guid CourseId,
    string Title,
    string Description,
    CourseDuration Duration,
    decimal PlannedCreditValue,
    CompletionStatus CompletionStatus,
    string InstructionalMethods,
    IReadOnlyList<StudentResourceView> TextsAndResources,
    string AssessmentMethods,
    string GradingBasis,
    string Goals,
    IReadOnlyList<string> LearningObjectives,
    string PlannedSequence,
    string ParentNotes,
    IReadOnlyList<string> TermNames,
    IReadOnlyList<StudentModuleLink> Modules);

public sealed record StudentModulePage(
    Guid StudentId,
    Guid CourseId,
    string CourseTitle,
    Guid ModuleId,
    Guid? PreviousModuleId,
    Guid? NextModuleId,
    int SequenceOrder,
    string Title,
    string Description,
    string TermName,
    string EstimatedLength,
    ModuleStatus Status,
    CompletionStatus CompletionStatus,
    string Instructions,
    IReadOnlyList<StudentModuleObjectiveView> LearningObjectives,
    IReadOnlyList<StudentModuleResourceView> Resources,
    IReadOnlyList<StudentLessonView> Lessons,
    IReadOnlyList<StudentAssignmentView> Assignments,
    string AssignmentEvidencePlaceholder);

public sealed record StudentModuleLink(
    Guid ModuleId,
    int SequenceOrder,
    string Title,
    string TermName,
    ModuleStatus Status,
    CompletionStatus CompletionStatus);

public sealed record StudentCourseDashboard(
    Guid StudentId,
    string StudentFirstName,
    IReadOnlyList<string> TermNames,
    IReadOnlyList<StudentCourseCard> Courses);

public sealed record StudentModuleObjectiveView(string Text, string LinkedCourseObjective);

public sealed record StudentModuleResourceView(
    string Name,
    string Link,
    string FileName,
    bool IsPhysicalResource);

public sealed record StudentResourceView(string Name, string Link);

public sealed record StudentLessonView(
    Guid LessonId,
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
    IReadOnlyList<StudentLessonObjectiveView> LearningObjectives,
    IReadOnlyList<StudentStandardsAlignmentView> StandardsAlignments,
    IReadOnlyList<string> SuccessCriteria,
    IReadOnlyList<StudentLessonStepView> LessonSteps,
    IReadOnlyList<StudentLessonResourceView> Resources,
    IReadOnlyList<StudentLessonProblemSetView> ProblemSets,
    IReadOnlyList<StudentLessonPortfolioConnectionView> PortfolioConnections,
    StudentLessonRubricView? Rubric,
    IReadOnlyList<string> ReflectionPrompts,
    IReadOnlyList<string> LinkedAssignmentTitles);

public sealed record StudentLessonResourceView(
    string Name,
    LessonResourceType Type,
    string Url,
    string FileName,
    bool IsPhysicalResource,
    string SourceNote,
    bool Required,
    int EstimatedMinutes,
    string StudentInstructions,
    string NotesPrompt,
    StudentLessonResourceCitationView? Citation,
    bool OfflineAvailable,
    string License);

public sealed record StudentLessonResourceCitationView(string Title, string Publisher, DateTimeOffset? AccessedAtUtc);

public sealed record StudentLessonObjectiveView(string ObjectiveId, string Text, BloomLevel BloomLevel);

public sealed record StudentStandardsAlignmentView(string Framework, string Code, string Description);

public sealed record StudentLessonStepView(
    int StepOrder,
    string Title,
    LessonStepType StepType,
    string Instructions,
    int EstimatedMinutes,
    bool Required);

public sealed record StudentLessonProblemSetView(
    string ProblemSetId,
    string Title,
    string Instructions,
    int EstimatedMinutes,
    IReadOnlyList<StudentLessonProblemView> Problems);

public sealed record StudentLessonProblemView(
    string ProblemId,
    string Prompt,
    ProblemResponseType ResponseType,
    string ExpectedAnswer,
    string Solution,
    IReadOnlyList<string> Skills,
    string Difficulty);

public sealed record StudentLessonPortfolioConnectionView(
    string PortfolioSection,
    string ArtifactTitle,
    string ArtifactPurpose,
    IReadOnlyList<string> CrossCourseLinks,
    string ReuseInstructions);

public sealed record StudentLessonRubricView(
    string RubricId,
    string Scale,
    IReadOnlyList<StudentLessonRubricCriterionView> Criteria);

public sealed record StudentLessonRubricCriterionView(
    string Criterion,
    string Level4,
    string Level3,
    string Level2,
    string Level1);

public sealed record StudentAssignmentView(
    int SequenceOrder,
    string Title,
    AssignmentType Type,
    InstructionalMethodProfile MethodProfile,
    string Instructions,
    string EstimatedEffort,
    int? EstimatedMinutesMin,
    int? EstimatedMinutesMax,
    string DueTimingLabel,
    DateOnly? DueDate,
    IReadOnlyList<string> LinkedModuleObjectives,
    IReadOnlyList<Guid> RelatedLessonIds,
    IReadOnlyList<string> RelatedLessonTitles,
    string RequiredOutput,
    bool IsPortfolioCandidate,
    AssignmentStatus Status,
    AssignmentAttemptPolicy AttemptPolicy,
    AssignmentSubmissionStructure SubmissionStructure,
    int DraftCount,
    int CurrentDraftNumber,
    bool CurrentDraftIsFinal,
    string AssignmentSummary,
    string StudentFacingGoal,
    IReadOnlyList<string> RequiredDeliverables,
    IReadOnlyList<AssignmentSubmissionFormat> SubmissionFormats,
    AssignmentPortfolioConnection? PortfolioConnection,
    LessonRubric? Rubric,
    IReadOnlyList<string> AssessmentSkills,
    IReadOnlyList<string> StudentChecklist,
    IReadOnlyList<AssignmentResource> Resources,
    IReadOnlyList<AssignmentStep> AssignmentSteps,
    AssignmentRevisionPolicy? RevisionPolicy,
    AssignmentCompletionCriteria? CompletionCriteria,
    IReadOnlyList<string> ReflectionPrompts,
    AssignmentEvidenceRequirements? EvidenceRequirements,
    AssignmentScoring? Scoring,
    Guid AssignmentId,
    IReadOnlyList<StudentAssignmentSubmissionView> Submissions,
    IReadOnlyList<StudentAssignmentAssessmentFeedbackView> AssessmentFeedback);

public sealed record StudentAssignmentSubmissionView(
    Guid SubmissionId,
    int AttemptNumber,
    AssignmentSubmissionStatus Status,
    DateTimeOffset SubmittedAtUtc,
    DateTimeOffset? ReturnedAtUtc,
    DateTimeOffset? AcceptedAtUtc,
    DateTimeOffset? ClearedAtUtc,
    string ParentReviewNotes,
    bool PortfolioCandidate,
    int DraftNumber,
    bool IsFinalDraft,
    int AttachmentCount);

public sealed record StudentAssignmentAssessmentFeedbackView(
    Guid AssessmentId,
    AssessmentState State,
    AssessmentResultType ResultType,
    string ResultLabel,
    string StudentFeedback,
    DateTimeOffset UpdatedAtUtc);
