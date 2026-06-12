using HomeschoolManager.Domain.Common;

namespace HomeschoolManager.Domain.Records;

public sealed record TranscriptCourseRecord
{
    public Guid Id { get; init; }
    public Guid StudentId { get; init; }
    public Guid CourseId { get; init; }
    public string FinalGrade { get; init; }
    public decimal? EarnedCreditValue { get; init; }
    public DateOnly? CompletionDate { get; init; }
    public string CreditBasis { get; init; }
    public string ParentNotes { get; init; }
    public bool IncludeInTranscript { get; init; }
    public string RecordedBy { get; init; }
    public DateTimeOffset RecordedAtUtc { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }

    public TranscriptCourseRecord(
        Guid id,
        Guid studentId,
        Guid courseId,
        string finalGrade,
        decimal? earnedCreditValue,
        DateOnly? completionDate,
        string creditBasis,
        string parentNotes,
        bool includeInTranscript,
        string recordedBy,
        DateTimeOffset recordedAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        if (studentId == Guid.Empty)
        {
            throw new DomainException("Student is required for a transcript course record.");
        }

        if (courseId == Guid.Empty)
        {
            throw new DomainException("Course is required for a transcript course record.");
        }

        if (earnedCreditValue.HasValue && (earnedCreditValue.Value < 0 || earnedCreditValue.Value > 3))
        {
            throw new DomainException("Earned credit must be between 0 and 3.");
        }

        if (earnedCreditValue.HasValue && earnedCreditValue.Value > 0 && string.IsNullOrWhiteSpace(creditBasis))
        {
            throw new DomainException("Add a credit basis before recording earned credit.");
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        StudentId = studentId;
        CourseId = courseId;
        FinalGrade = Clean(finalGrade);
        EarnedCreditValue = earnedCreditValue;
        CompletionDate = completionDate;
        CreditBasis = Clean(creditBasis);
        ParentNotes = Clean(parentNotes);
        IncludeInTranscript = includeInTranscript;
        RecordedBy = Require.Text(recordedBy, nameof(recordedBy));
        RecordedAtUtc = recordedAtUtc == default ? DateTimeOffset.UtcNow : recordedAtUtc;
        UpdatedAtUtc = updatedAtUtc == default ? RecordedAtUtc : updatedAtUtc;
    }

    private static string Clean(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
    }
}
