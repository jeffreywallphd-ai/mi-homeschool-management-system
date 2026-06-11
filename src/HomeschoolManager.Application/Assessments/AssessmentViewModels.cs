using HomeschoolManager.Domain.Assessments;
using HomeschoolManager.Domain.Curriculum;
using HomeschoolManager.Domain.Submissions;

namespace HomeschoolManager.Application.Assessments;

public sealed record GradebookDashboardSummary(
    int StudentCount,
    int ActiveCourseCount,
    int NeedsReviewCount,
    int AssessedCount,
    int ReturnedForRevisionCount,
    int IncompleteCount);

public sealed record GradebookPage(
    Guid SelectedStudentId,
    Guid? SelectedCourseId,
    IReadOnlyList<GradebookStudentOption> Students,
    IReadOnlyList<GradebookCourseOption> Courses,
    GradebookCourseSummary? Summary,
    IReadOnlyList<GradebookAssessmentRow> Rows);

public sealed record GradebookStudentOption(Guid StudentId, string Name);

public sealed record GradebookCourseOption(Guid CourseId, string Title);

public sealed record GradebookCourseSummary(
    Guid StudentId,
    Guid CourseId,
    string CourseTitle,
    int AssignmentCount,
    int NeedsReviewCount,
    int AssessedCount,
    int ReturnedForRevisionCount,
    int IncompleteCount,
    int ExcusedCount,
    int NotApplicableCount);

public sealed record GradebookAssessmentRow(
    Guid StudentId,
    Guid CourseId,
    Guid ModuleId,
    string ModuleTitle,
    int ModuleSequence,
    Guid AssignmentId,
    string AssignmentTitle,
    int AssignmentSequence,
    AssignmentStatus AssignmentStatus,
    decimal? PlannedPoints,
    decimal? PlannedWeight,
    Guid? LatestSubmissionId,
    AssignmentSubmissionStatus? LatestSubmissionStatus,
    DateTimeOffset? LatestSubmittedAtUtc,
    Guid? EvidenceRecordId,
    AssessmentDetail? Assessment,
    AssessmentState EffectiveState);

public sealed record AssessmentDetail(
    Guid AssessmentId,
    AssessmentSourceType SourceType,
    AssessmentState State,
    AssessmentResultType ResultType,
    string ResultValue,
    decimal? PointsEarned,
    decimal? PointsPossible,
    decimal? Percentage,
    string Narrative,
    string RubricSummary,
    string ParentNotes,
    string StudentFeedback,
    bool FeedbackVisibleToStudent,
    DateTimeOffset UpdatedAtUtc);

public sealed record StudentAssessmentFeedbackView(
    Guid AssessmentId,
    Guid CourseId,
    Guid? ModuleId,
    Guid? AssignmentId,
    AssessmentState State,
    AssessmentResultType ResultType,
    string ResultLabel,
    string StudentFeedback,
    DateTimeOffset UpdatedAtUtc);
