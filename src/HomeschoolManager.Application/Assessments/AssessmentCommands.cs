using HomeschoolManager.Domain.Assessments;

namespace HomeschoolManager.Application.Assessments;

public sealed record SaveAssessmentCommand(
    Guid? AssessmentId,
    Guid StudentId,
    Guid CourseId,
    Guid? ModuleId,
    Guid? AssignmentId,
    Guid? SubmissionId,
    Guid? EvidenceRecordId,
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
    bool FeedbackVisibleToStudent);

public sealed record ArchiveAssessmentCommand(Guid AssessmentId);
