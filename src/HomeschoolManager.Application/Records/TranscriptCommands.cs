namespace HomeschoolManager.Application.Records;

public sealed record SaveTranscriptCourseRecordCommand(
    Guid StudentId,
    Guid CourseId,
    string FinalGrade,
    decimal? EarnedCreditValue,
    DateOnly? CompletionDate,
    string CreditBasis,
    string ParentNotes,
    bool IncludeInTranscript);

public sealed record CreateTranscriptExportCommand(Guid StudentId, TranscriptSpan Span);
