using HomeschoolManager.Application.Access;
using HomeschoolManager.Application.Common;
using HomeschoolManager.Domain.Access;

namespace HomeschoolManager.Application.Backups;

public sealed class BackupService
{
    private readonly IBackupArchiveStore archiveStore;

    public BackupService(IBackupArchiveStore archiveStore)
    {
        this.archiveStore = archiveStore;
    }

    public async Task<OperationResult<BackupDownloadFile>> CreateBackupAsync(
        UserContext user,
        CreateBackupCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<BackupDownloadFile>.Failure(authorized.Errors.ToArray());
        }

        if (!Enum.IsDefined(command.Kind))
        {
            return OperationResult<BackupDownloadFile>.Failure("Backup type is not recognized.");
        }

        return OperationResult<BackupDownloadFile>.Success(
            await archiveStore.CreateBackupAsync(user.DisplayName, command.Kind, cancellationToken));
    }

    public async Task<OperationResult<BackupValidationReport>> ValidateBackupAsync(
        UserContext user,
        ValidateBackupCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<BackupValidationReport>.Failure(authorized.Errors.ToArray());
        }

        if (command.Content.Length == 0)
        {
            return OperationResult<BackupValidationReport>.Failure("Choose a backup ZIP file first.");
        }

        return OperationResult<BackupValidationReport>.Success(
            await archiveStore.ValidateBackupAsync(command.Content, command.FileName, cancellationToken));
    }

    public async Task<OperationResult<BackupRestorePreview>> PreviewRestoreAsync(
        UserContext user,
        ValidateBackupCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<BackupRestorePreview>.Failure(authorized.Errors.ToArray());
        }

        if (command.Content.Length == 0)
        {
            return OperationResult<BackupRestorePreview>.Failure("Choose a backup ZIP file first.");
        }

        var validation = await archiveStore.ValidateBackupAsync(command.Content, command.FileName, cancellationToken);
        if (!validation.IsValid)
        {
            return OperationResult<BackupRestorePreview>.Failure(validation.Errors.ToArray());
        }

        return OperationResult<BackupRestorePreview>.Success(
            await archiveStore.PreviewRestoreAsync(command.Content, command.FileName, cancellationToken));
    }

    public async Task<OperationResult<BackupRestoreResult>> RestoreBackupAsync(
        UserContext user,
        RestoreBackupCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<BackupRestoreResult>.Failure(authorized.Errors.ToArray());
        }

        if (!command.ConfirmReplaceCurrentRecords)
        {
            return OperationResult<BackupRestoreResult>.Failure("Confirm that you want to replace the current records before restoring.");
        }

        var validation = await archiveStore.ValidateBackupAsync(command.Content, command.FileName, cancellationToken);
        if (!validation.IsValid)
        {
            return OperationResult<BackupRestoreResult>.Failure(validation.Errors.ToArray());
        }

        return OperationResult<BackupRestoreResult>.Success(
            await archiveStore.RestoreBackupAsync(command.Content, command.FileName, user.DisplayName, cancellationToken));
    }

    public async Task<OperationResult<IReadOnlyList<BackupHistoryItem>>> ListBackupsAsync(
        UserContext user,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<IReadOnlyList<BackupHistoryItem>>.Failure(authorized.Errors.ToArray());
        }

        return OperationResult<IReadOnlyList<BackupHistoryItem>>.Success(
            await archiveStore.ListBackupsAsync(cancellationToken));
    }

    public async Task<OperationResult<BackupDownloadFile>> DownloadBackupAsync(
        UserContext user,
        DownloadBackupCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<BackupDownloadFile>.Failure(authorized.Errors.ToArray());
        }

        var backup = await archiveStore.ReadBackupAsync(command.BackupId, cancellationToken);
        return backup is null
            ? OperationResult<BackupDownloadFile>.Failure("Backup file was not found.")
            : OperationResult<BackupDownloadFile>.Success(backup);
    }

    public async Task<OperationResult> DeleteBackupAsync(
        UserContext user,
        DeleteBackupCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        if (!command.ConfirmDelete)
        {
            return OperationResult.Failure("Confirm that you want to delete this backup.");
        }

        var deleted = await archiveStore.DeleteBackupAsync(command.BackupId, cancellationToken);
        return deleted
            ? OperationResult.Success()
            : OperationResult.Failure("Backup file was not found.");
    }
}
