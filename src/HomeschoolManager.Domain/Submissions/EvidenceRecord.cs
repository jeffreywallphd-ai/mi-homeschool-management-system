using HomeschoolManager.Domain.Common;

namespace HomeschoolManager.Domain.Submissions;

public sealed record EvidenceRecord
{
    public Guid Id { get; init; }
    public Guid StudentId { get; init; }
    public Guid CourseId { get; init; }
    public Guid ModuleId { get; init; }
    public Guid AssignmentId { get; init; }
    public Guid SubmissionId { get; init; }
    public string Title { get; init; }
    public string Description { get; init; }
    public IReadOnlyList<Guid> StoredFileIds { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset ConfirmedAtUtc { get; init; }
    public string ParentNotes { get; init; }
    public bool PortfolioCandidate { get; init; }

    public EvidenceRecord(
        Guid id,
        Guid studentId,
        Guid courseId,
        Guid moduleId,
        Guid assignmentId,
        Guid submissionId,
        string title,
        string description,
        IReadOnlyList<Guid>? storedFileIds,
        DateTimeOffset createdAtUtc,
        DateTimeOffset confirmedAtUtc,
        string parentNotes,
        bool portfolioCandidate)
    {
        if (studentId == Guid.Empty)
        {
            throw new DomainException("Student is required for evidence.");
        }

        if (courseId == Guid.Empty || moduleId == Guid.Empty || assignmentId == Guid.Empty || submissionId == Guid.Empty)
        {
            throw new DomainException("Course, module, assignment, and submission are required for evidence.");
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        StudentId = studentId;
        CourseId = courseId;
        ModuleId = moduleId;
        AssignmentId = assignmentId;
        SubmissionId = submissionId;
        Title = Require.Text(title, nameof(title));
        Description = string.IsNullOrWhiteSpace(description) ? "" : description.Trim();
        StoredFileIds = (storedFileIds ?? [])
            .Where(fileId => fileId != Guid.Empty)
            .Distinct()
            .ToArray();
        CreatedAtUtc = createdAtUtc == default ? DateTimeOffset.UtcNow : createdAtUtc;
        ConfirmedAtUtc = confirmedAtUtc == default ? CreatedAtUtc : confirmedAtUtc;
        ParentNotes = string.IsNullOrWhiteSpace(parentNotes) ? "" : parentNotes.Trim();
        PortfolioCandidate = portfolioCandidate;
    }
}
