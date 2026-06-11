using HomeschoolManager.Domain.Common;

namespace HomeschoolManager.Domain.Assessments;

public sealed record AssessmentRecord
{
    public Guid Id { get; init; }
    public Guid StudentId { get; init; }
    public Guid CourseId { get; init; }
    public Guid? ModuleId { get; init; }
    public Guid? AssignmentId { get; init; }
    public Guid? SubmissionId { get; init; }
    public Guid? EvidenceRecordId { get; init; }
    public AssessmentSourceType SourceType { get; init; }
    public AssessmentState State { get; init; }
    public AssessmentResultType ResultType { get; init; }
    public string ResultValue { get; init; }
    public decimal? PointsEarned { get; init; }
    public decimal? PointsPossible { get; init; }
    public decimal? Percentage { get; init; }
    public string Narrative { get; init; }
    public string RubricSummary { get; init; }
    public string ParentNotes { get; init; }
    public string StudentFeedback { get; init; }
    public bool FeedbackVisibleToStudent { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }
    public bool IsArchived { get; init; }

    public AssessmentRecord(
        Guid id,
        Guid studentId,
        Guid courseId,
        Guid? moduleId,
        Guid? assignmentId,
        Guid? submissionId,
        Guid? evidenceRecordId,
        AssessmentSourceType sourceType,
        AssessmentState state,
        AssessmentResultType resultType,
        string resultValue,
        decimal? pointsEarned,
        decimal? pointsPossible,
        decimal? percentage,
        string narrative,
        string rubricSummary,
        string parentNotes,
        string studentFeedback,
        bool feedbackVisibleToStudent,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        bool isArchived = false)
    {
        if (studentId == Guid.Empty)
        {
            throw new DomainException("Student is required for an assessment record.");
        }

        if (courseId == Guid.Empty)
        {
            throw new DomainException("Course is required for an assessment record.");
        }

        if (!Enum.IsDefined(sourceType))
        {
            throw new DomainException("Assessment source type is not recognized.");
        }

        if (!Enum.IsDefined(state))
        {
            throw new DomainException("Assessment state is not recognized.");
        }

        if (!Enum.IsDefined(resultType))
        {
            throw new DomainException("Assessment result type is not recognized.");
        }

        ValidateSource(sourceType, moduleId, assignmentId, submissionId, evidenceRecordId);
        ValidateResult(resultType, resultValue, pointsEarned, pointsPossible, percentage, narrative, rubricSummary);

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        StudentId = studentId;
        CourseId = courseId;
        ModuleId = moduleId == Guid.Empty ? null : moduleId;
        AssignmentId = assignmentId == Guid.Empty ? null : assignmentId;
        SubmissionId = submissionId == Guid.Empty ? null : submissionId;
        EvidenceRecordId = evidenceRecordId == Guid.Empty ? null : evidenceRecordId;
        SourceType = sourceType;
        State = state;
        ResultType = resultType;
        ResultValue = Clean(resultValue);
        PointsEarned = pointsEarned;
        PointsPossible = pointsPossible;
        Percentage = percentage;
        Narrative = Clean(narrative);
        RubricSummary = Clean(rubricSummary);
        ParentNotes = Clean(parentNotes);
        StudentFeedback = Clean(studentFeedback);
        FeedbackVisibleToStudent = feedbackVisibleToStudent && !string.IsNullOrWhiteSpace(studentFeedback);
        CreatedAtUtc = createdAtUtc == default ? DateTimeOffset.UtcNow : createdAtUtc;
        UpdatedAtUtc = updatedAtUtc == default ? CreatedAtUtc : updatedAtUtc;
        IsArchived = isArchived;
    }

    public AssessmentRecord Archive(DateTimeOffset archivedAtUtc)
    {
        return this with
        {
            IsArchived = true,
            UpdatedAtUtc = archivedAtUtc == default ? DateTimeOffset.UtcNow : archivedAtUtc
        };
    }

    private static void ValidateSource(
        AssessmentSourceType sourceType,
        Guid? moduleId,
        Guid? assignmentId,
        Guid? submissionId,
        Guid? evidenceRecordId)
    {
        if (sourceType is AssessmentSourceType.Assignment or AssessmentSourceType.Submission or AssessmentSourceType.Evidence &&
            (!HasValue(moduleId) || !HasValue(assignmentId)))
        {
            throw new DomainException("Module and assignment are required for assignment assessment records.");
        }

        if (sourceType == AssessmentSourceType.Submission && (submissionId is null || submissionId == Guid.Empty))
        {
            throw new DomainException("Submission is required for a submission assessment record.");
        }

        if (sourceType == AssessmentSourceType.Evidence && (evidenceRecordId is null || evidenceRecordId == Guid.Empty))
        {
            throw new DomainException("Evidence is required for an evidence assessment record.");
        }
    }

    private static void ValidateResult(
        AssessmentResultType resultType,
        string resultValue,
        decimal? pointsEarned,
        decimal? pointsPossible,
        decimal? percentage,
        string narrative,
        string rubricSummary)
    {
        if (pointsEarned.HasValue && pointsEarned.Value < 0 ||
            pointsPossible.HasValue && pointsPossible.Value < 0 ||
            percentage.HasValue && (percentage.Value < 0 || percentage.Value > 100))
        {
            throw new DomainException("Assessment numeric values must be within a valid range.");
        }

        if (pointsEarned.HasValue && pointsPossible.HasValue && pointsEarned.Value > pointsPossible.Value)
        {
            throw new DomainException("Points earned cannot exceed points possible.");
        }

        switch (resultType)
        {
            case AssessmentResultType.Narrative when string.IsNullOrWhiteSpace(narrative):
                throw new DomainException("Narrative assessment text is required.");
            case AssessmentResultType.RubricSummary when string.IsNullOrWhiteSpace(rubricSummary):
                throw new DomainException("Rubric summary is required.");
            case AssessmentResultType.PassFail when string.IsNullOrWhiteSpace(resultValue):
                throw new DomainException("Pass/fail result is required.");
            case AssessmentResultType.Points when !pointsEarned.HasValue || !pointsPossible.HasValue || pointsPossible.Value <= 0:
                throw new DomainException("Points earned and points possible are required.");
            case AssessmentResultType.Percentage when !percentage.HasValue:
                throw new DomainException("Percentage is required.");
            case AssessmentResultType.LetterGrade when string.IsNullOrWhiteSpace(resultValue):
                throw new DomainException("Letter grade value is required.");
            case AssessmentResultType.TestScore when string.IsNullOrWhiteSpace(resultValue) && !percentage.HasValue && !pointsEarned.HasValue:
                throw new DomainException("Test score value is required.");
        }
    }

    private static string Clean(string value) => string.IsNullOrWhiteSpace(value) ? "" : value.Trim();

    private static bool HasValue(Guid? value) => value.HasValue && value.Value != Guid.Empty;
}
