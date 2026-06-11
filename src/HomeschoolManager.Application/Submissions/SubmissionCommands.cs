namespace HomeschoolManager.Application.Submissions;

public sealed record SubmitAssignmentCommand(
    Guid CourseId,
    Guid ModuleId,
    Guid AssignmentId,
    string ResponseText,
    string StudentNotes,
    IReadOnlyList<AssignmentAttachmentUpload> Attachments,
    Guid? StudentId = null,
    int DraftNumber = 1);

public sealed record AssignmentAttachmentUpload(
    string FileName,
    string ContentType,
    byte[] Content);

public sealed record ReturnSubmissionCommand(Guid SubmissionId, string ParentReviewNotes);

public sealed record AcceptSubmissionCommand(Guid SubmissionId, string ParentReviewNotes, bool MarkPortfolioCandidate);

public sealed record ClearSubmissionCommand(Guid SubmissionId, string ParentReviewNotes, string ConfirmationText);
