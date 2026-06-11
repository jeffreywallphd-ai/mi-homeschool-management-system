using System.Security.Cryptography;
using HomeschoolManager.Application.Submissions;
using HomeschoolManager.Domain.Submissions;

namespace HomeschoolManager.Infrastructure.Persistence;

public sealed class LocalSubmissionFileStore : ISubmissionFileStore
{
    private readonly AppDataPaths paths;

    public LocalSubmissionFileStore(AppDataPaths paths)
    {
        this.paths = paths;
    }

    public async Task<StoredFileReference> SaveAssignmentSubmissionFileAsync(
        Guid studentId,
        Guid submissionId,
        AssignmentAttachmentUpload attachment,
        CancellationToken cancellationToken = default)
    {
        if (attachment.Content.Length == 0)
        {
            throw new InvalidOperationException("Attached files cannot be empty.");
        }

        var fileId = Guid.NewGuid();
        var extension = SafeExtension(attachment.FileName);
        var folder = Path.Combine(
            paths.FilesDirectory,
            "students",
            studentId.ToString("N"),
            "submissions",
            submissionId.ToString("N"));
        Directory.CreateDirectory(folder);

        var fileName = extension.Length == 0
            ? fileId.ToString("N")
            : $"{fileId:N}{extension}";
        var finalPath = Path.Combine(folder, fileName);
        var tempPath = Path.Combine(folder, $"{fileId:N}.tmp");

        try
        {
            await File.WriteAllBytesAsync(tempPath, attachment.Content, cancellationToken);
            File.Move(tempPath, finalPath, true);

            return new StoredFileReference(
                fileId,
                StoredFileCategory.AssignmentSubmission,
                SafeDisplayName(attachment.FileName),
                RelativePath(finalPath),
                attachment.ContentType,
                attachment.Content.LongLength,
                Convert.ToHexString(SHA256.HashData(attachment.Content)).ToLowerInvariant(),
                DateTimeOffset.UtcNow,
                studentId,
                submissionId);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    public Task DeleteStoredFileAsync(StoredFileReference file, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(Path.Combine(paths.DataRoot, file.StoredPath));
        var rootPath = Path.GetFullPath(paths.FilesDirectory);
        if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private string RelativePath(string fullPath)
    {
        return Path.GetRelativePath(paths.DataRoot, fullPath).Replace(Path.DirectorySeparatorChar, '/');
    }

    private static string SafeExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension) || extension.Length > 16)
        {
            return "";
        }

        var safeChars = extension
            .Where(character => char.IsAsciiLetterOrDigit(character) || character == '.')
            .ToArray();
        return new string(safeChars).ToLowerInvariant();
    }

    private static string SafeDisplayName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        return string.IsNullOrWhiteSpace(name) ? "attachment" : name.Trim();
    }
}
