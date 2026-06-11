using HomeschoolManager.Domain.Submissions;

namespace HomeschoolManager.Application.Submissions;

public sealed record SubmissionAttachmentView(
    Guid FileId,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string ChecksumSha256,
    DateTimeOffset CreatedAtUtc);

public sealed record AssignmentSubmissionSummary(
    Guid SubmissionId,
    Guid StudentId,
    string StudentName,
    Guid CourseId,
    string CourseTitle,
    Guid ModuleId,
    string ModuleTitle,
    Guid AssignmentId,
    string AssignmentTitle,
    int AttemptNumber,
    AssignmentSubmissionStatus Status,
    DateTimeOffset SubmittedAtUtc,
    DateTimeOffset? ReturnedAtUtc,
    DateTimeOffset? AcceptedAtUtc,
    DateTimeOffset? ClearedAtUtc,
    string ParentReviewNotes,
    bool PortfolioCandidate,
    int AttachmentCount,
    int DraftNumber,
    int DraftCount,
    bool IsFinalDraft);

public sealed record SubmissionReviewDetail(
    Guid SubmissionId,
    Guid StudentId,
    string StudentName,
    Guid CourseId,
    string CourseTitle,
    Guid ModuleId,
    string ModuleTitle,
    Guid AssignmentId,
    string AssignmentTitle,
    int AttemptNumber,
    AssignmentSubmissionStatus Status,
    string ResponseText,
    string StudentNotes,
    DateTimeOffset SubmittedAtUtc,
    DateTimeOffset? ReturnedAtUtc,
    DateTimeOffset? AcceptedAtUtc,
    DateTimeOffset? ClearedAtUtc,
    string ParentReviewNotes,
    bool PortfolioCandidate,
    int DraftNumber,
    int DraftCount,
    bool IsFinalDraft,
    IReadOnlyList<SubmissionAttachmentView> Attachments,
    EvidenceRecordView? Evidence);

public sealed record EvidenceRecordView(
    Guid EvidenceId,
    Guid SubmissionId,
    string Title,
    string Description,
    DateTimeOffset ConfirmedAtUtc,
    string ParentNotes,
    bool PortfolioCandidate,
    int FileCount);
