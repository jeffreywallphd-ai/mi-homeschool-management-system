namespace HomeschoolManager.Application.Portfolio;

public sealed record CreatePortfolioExportCommand(Guid StudentId, Guid? ApprovalSnapshotId = null);
