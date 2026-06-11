using HomeschoolManager.Domain.Submissions;

namespace HomeschoolManager.Application.Submissions;

public interface ISubmissionFileStore
{
    Task<StoredFileReference> SaveAssignmentSubmissionFileAsync(
        Guid studentId,
        Guid submissionId,
        AssignmentAttachmentUpload attachment,
        CancellationToken cancellationToken = default);

    Task DeleteStoredFileAsync(StoredFileReference file, CancellationToken cancellationToken = default);
}
