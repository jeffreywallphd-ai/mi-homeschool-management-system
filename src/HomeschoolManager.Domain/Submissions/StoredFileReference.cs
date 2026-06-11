using HomeschoolManager.Domain.Common;

namespace HomeschoolManager.Domain.Submissions;

public sealed record StoredFileReference
{
    public Guid Id { get; init; }
    public StoredFileCategory Category { get; init; }
    public string OriginalFileName { get; init; }
    public string StoredPath { get; init; }
    public string ContentType { get; init; }
    public long SizeBytes { get; init; }
    public string ChecksumSha256 { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public Guid StudentId { get; init; }
    public Guid SubmissionId { get; init; }

    public StoredFileReference(
        Guid id,
        StoredFileCategory category,
        string originalFileName,
        string storedPath,
        string contentType,
        long sizeBytes,
        string checksumSha256,
        DateTimeOffset createdAtUtc,
        Guid studentId,
        Guid submissionId)
    {
        if (!Enum.IsDefined(category))
        {
            throw new DomainException("Stored file category is not recognized.");
        }

        if (sizeBytes < 0)
        {
            throw new DomainException("Stored file size cannot be negative.");
        }

        if (studentId == Guid.Empty)
        {
            throw new DomainException("Student is required for a stored file.");
        }

        if (submissionId == Guid.Empty)
        {
            throw new DomainException("Submission is required for a stored file.");
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Category = category;
        OriginalFileName = Require.Text(originalFileName, nameof(originalFileName));
        StoredPath = Require.Text(storedPath, nameof(storedPath));
        ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim();
        SizeBytes = sizeBytes;
        ChecksumSha256 = Require.Text(checksumSha256, nameof(checksumSha256));
        CreatedAtUtc = createdAtUtc == default ? DateTimeOffset.UtcNow : createdAtUtc;
        StudentId = studentId;
        SubmissionId = submissionId;
    }
}
