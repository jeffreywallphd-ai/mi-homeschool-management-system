using HomeschoolManager.Domain.Common;

namespace HomeschoolManager.Domain.Submissions;

public sealed record AssignmentSubmission
{
    public Guid Id { get; init; }
    public Guid StudentId { get; init; }
    public Guid CourseId { get; init; }
    public Guid ModuleId { get; init; }
    public Guid AssignmentId { get; init; }
    public int AttemptNumber { get; init; }
    public AssignmentSubmissionStatus Status { get; init; }
    public string ResponseText { get; init; }
    public string StudentNotes { get; init; }
    public IReadOnlyList<StoredFileReference> Attachments { get; init; }
    public DateTimeOffset SubmittedAtUtc { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }
    public DateTimeOffset? ReturnedAtUtc { get; init; }
    public DateTimeOffset? AcceptedAtUtc { get; init; }
    public DateTimeOffset? ClearedAtUtc { get; init; }
    public string ParentReviewNotes { get; init; }
    public bool PortfolioCandidate { get; init; }
    public int DraftNumber { get; init; }

    public AssignmentSubmission(
        Guid id,
        Guid studentId,
        Guid courseId,
        Guid moduleId,
        Guid assignmentId,
        int attemptNumber,
        AssignmentSubmissionStatus status,
        string responseText,
        string studentNotes,
        IReadOnlyList<StoredFileReference>? attachments,
        DateTimeOffset submittedAtUtc,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? returnedAtUtc,
        DateTimeOffset? acceptedAtUtc,
        string parentReviewNotes,
        bool portfolioCandidate,
        DateTimeOffset? clearedAtUtc = null,
        int draftNumber = 1)
    {
        if (studentId == Guid.Empty)
        {
            throw new DomainException("Student is required for a submission.");
        }

        if (courseId == Guid.Empty || moduleId == Guid.Empty || assignmentId == Guid.Empty)
        {
            throw new DomainException("Course, module, and assignment are required for a submission.");
        }

        if (attemptNumber < 1)
        {
            throw new DomainException("Submission attempt number must be one or greater.");
        }

        if (draftNumber < 1)
        {
            throw new DomainException("Submission draft number must be one or greater.");
        }

        if (!Enum.IsDefined(status))
        {
            throw new DomainException("Submission status is not recognized.");
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        StudentId = studentId;
        CourseId = courseId;
        ModuleId = moduleId;
        AssignmentId = assignmentId;
        AttemptNumber = attemptNumber;
        Status = status;
        ResponseText = string.IsNullOrWhiteSpace(responseText) ? "" : responseText.Trim();
        StudentNotes = string.IsNullOrWhiteSpace(studentNotes) ? "" : studentNotes.Trim();
        Attachments = (attachments ?? [])
            .Where(file => file.SubmissionId == Id)
            .ToArray();
        SubmittedAtUtc = submittedAtUtc == default ? DateTimeOffset.UtcNow : submittedAtUtc;
        CreatedAtUtc = createdAtUtc == default ? SubmittedAtUtc : createdAtUtc;
        UpdatedAtUtc = updatedAtUtc == default ? SubmittedAtUtc : updatedAtUtc;
        ReturnedAtUtc = returnedAtUtc;
        AcceptedAtUtc = acceptedAtUtc;
        ClearedAtUtc = clearedAtUtc;
        ParentReviewNotes = string.IsNullOrWhiteSpace(parentReviewNotes) ? "" : parentReviewNotes.Trim();
        PortfolioCandidate = portfolioCandidate;
        DraftNumber = draftNumber;
    }
}
