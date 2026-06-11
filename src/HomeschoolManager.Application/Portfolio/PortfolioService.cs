using HomeschoolManager.Application.Access;
using HomeschoolManager.Application.Common;
using HomeschoolManager.Application.Persistence;
using HomeschoolManager.Domain.Access;
using HomeschoolManager.Domain.Curriculum;
using HomeschoolManager.Domain.Students;
using HomeschoolManager.Domain.Submissions;

namespace HomeschoolManager.Application.Portfolio;

public sealed class PortfolioService
{
    private readonly IHomeschoolRepository repository;

    public PortfolioService(IHomeschoolRepository repository)
    {
        this.repository = repository;
    }

    public async Task<OperationResult<StudentPortfolioWorkspace>> GetStudentWorkspaceAsync(
        UserContext user,
        Guid? studentId = null,
        CancellationToken cancellationToken = default)
    {
        var authorized = RequireStudentReadAccess(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<StudentPortfolioWorkspace>.Failure(authorized.Errors.ToArray());
        }

        var student = await ResolveStudentAsync(studentId, cancellationToken);
        if (student is null)
        {
            return OperationResult<StudentPortfolioWorkspace>.Failure("Student was not found.");
        }

        var context = await LoadContextAsync(cancellationToken);
        var draftItems = context.DraftItems
            .Where(item => item.StudentId == student.Id)
            .Select(item => ToDraftView(item, student, context))
            .Where(item => item is not null)
            .Select(item => item!)
            .ToArray();
        var availableEvidence = context.EvidenceRecords
            .Where(evidence => evidence.StudentId == student.Id)
            .Select(evidence => ToEvidenceOption(evidence, student, context, draftItems))
            .Where(item => item is not null)
            .Select(item => item!)
            .ToArray();

        return OperationResult<StudentPortfolioWorkspace>.Success(new StudentPortfolioWorkspace(
            student.Id,
            StudentName(student),
            draftItems,
            availableEvidence));
    }

    public async Task<OperationResult<PortfolioReviewWorkspace>> GetReviewWorkspaceAsync(
        UserContext user,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<PortfolioReviewWorkspace>.Failure(authorized.Errors.ToArray());
        }

        var context = await LoadContextAsync(cancellationToken);
        var items = context.DraftItems
            .Select(item =>
            {
                var student = context.Students.FirstOrDefault(candidate => candidate.Id == item.StudentId);
                return student is null ? null : ToDraftView(item, student, context);
            })
            .Where(item => item is not null)
            .Select(item => item!)
            .ToArray();

        return OperationResult<PortfolioReviewWorkspace>.Success(new PortfolioReviewWorkspace(items));
    }

    public async Task<OperationResult<Guid>> AddDraftItemAsync(
        UserContext user,
        AddPortfolioDraftItemCommand command,
        CancellationToken cancellationToken = default)
    {
        if (user.Role != UserRole.Student)
        {
            return OperationResult<Guid>.Failure("Only the true student portal can add portfolio draft items.");
        }

        var student = await ResolveStudentAsync(command.StudentId, cancellationToken);
        if (student is null)
        {
            return OperationResult<Guid>.Failure("Student was not found.");
        }

        var evidence = await FindEvidenceAsync(command.EvidenceRecordId, cancellationToken);
        if (evidence is null || evidence.StudentId != student.Id)
        {
            return OperationResult<Guid>.Failure("Evidence was not found for this student.");
        }

        var draftItems = await repository.GetPortfolioDraftItemsAsync(cancellationToken);
        var existing = draftItems.FirstOrDefault(item =>
            item.StudentId == student.Id &&
            item.EvidenceRecordId == evidence.Id);
        if (existing is not null)
        {
            return OperationResult<Guid>.Success(existing.Id);
        }

        var now = DateTimeOffset.UtcNow;
        var nextSortOrder = draftItems
            .Where(item => item.StudentId == student.Id)
            .Select(item => item.SortOrder)
            .DefaultIfEmpty(0)
            .Max() + 1;
        var item = new PortfolioDraftItem(
            Guid.NewGuid(),
            student.Id,
            evidence.Id,
            evidence.Title,
            "Course Portfolio",
            "",
            "",
            [],
            nextSortOrder,
            true,
            PortfolioDraftStatus.Draft,
            "",
            now,
            now);

        await repository.SavePortfolioDraftItemAsync(item, cancellationToken);
        return OperationResult<Guid>.Success(item.Id);
    }

    public async Task<OperationResult> UpdateDraftItemAsync(
        UserContext user,
        UpdatePortfolioDraftItemCommand command,
        CancellationToken cancellationToken = default)
    {
        if (user.Role != UserRole.Student)
        {
            return OperationResult.Failure("Only the true student portal can update portfolio draft items.");
        }

        var item = await repository.GetPortfolioDraftItemAsync(command.PortfolioDraftItemId, cancellationToken);
        if (item is null)
        {
            return OperationResult.Failure("Portfolio draft item was not found.");
        }

        var student = await ResolveStudentAsync(item.StudentId, cancellationToken);
        if (student is null || item.StudentId != student.Id)
        {
            return OperationResult.Failure("Portfolio draft item was not found for this student.");
        }

        if (item.Status is not (PortfolioDraftStatus.Draft or PortfolioDraftStatus.NeedsRevision))
        {
            return OperationResult.Failure("Only draft or revision-requested portfolio items can be edited by the student.");
        }

        await repository.SavePortfolioDraftItemAsync(item with
        {
            DisplayTitle = Clean(command.DisplayTitle),
            PortfolioSection = Clean(command.PortfolioSection),
            StudentReflection = Clean(command.StudentReflection),
            ChosenReason = Clean(command.ChosenReason),
            SkillsShown = CleanList(command.SkillsShown),
            SortOrder = Math.Max(0, command.SortOrder),
            IncludeInDraft = command.IncludeInDraft,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        }, cancellationToken);

        return OperationResult.Success();
    }

    public async Task<OperationResult> SubmitDraftItemAsync(
        UserContext user,
        SubmitPortfolioDraftItemCommand command,
        CancellationToken cancellationToken = default)
    {
        if (user.Role != UserRole.Student)
        {
            return OperationResult.Failure("Only the true student portal can submit portfolio draft items for review.");
        }

        var item = await repository.GetPortfolioDraftItemAsync(command.PortfolioDraftItemId, cancellationToken);
        if (item is null)
        {
            return OperationResult.Failure("Portfolio draft item was not found.");
        }

        var student = await ResolveStudentAsync(item.StudentId, cancellationToken);
        if (student is null || item.StudentId != student.Id)
        {
            return OperationResult.Failure("Portfolio draft item was not found for this student.");
        }

        if (item.Status is not (PortfolioDraftStatus.Draft or PortfolioDraftStatus.NeedsRevision))
        {
            return OperationResult.Failure("This portfolio item is not ready for student submission.");
        }

        if (string.IsNullOrWhiteSpace(item.StudentReflection) && string.IsNullOrWhiteSpace(item.ChosenReason))
        {
            return OperationResult.Failure("Add a reflection or reason before submitting the portfolio item for review.");
        }

        var now = DateTimeOffset.UtcNow;
        await repository.SavePortfolioDraftItemAsync(item with
        {
            Status = PortfolioDraftStatus.SubmittedForReview,
            SubmittedAtUtc = now,
            UpdatedAtUtc = now
        }, cancellationToken);

        return OperationResult.Success();
    }

    public async Task<OperationResult> ReviewDraftItemAsync(
        UserContext user,
        ReviewPortfolioDraftItemCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        if (command.Status is not (PortfolioDraftStatus.ParentApproved or PortfolioDraftStatus.NeedsRevision or PortfolioDraftStatus.Excluded))
        {
            return OperationResult.Failure("Choose approved, needs revision, or excluded for the portfolio review.");
        }

        if (command.Status is PortfolioDraftStatus.NeedsRevision or PortfolioDraftStatus.Excluded &&
            string.IsNullOrWhiteSpace(command.ParentReviewNotes))
        {
            return OperationResult.Failure("Add parent review notes before sending the item back or excluding it.");
        }

        var item = await repository.GetPortfolioDraftItemAsync(command.PortfolioDraftItemId, cancellationToken);
        if (item is null)
        {
            return OperationResult.Failure("Portfolio draft item was not found.");
        }

        if (item.Status == PortfolioDraftStatus.Draft)
        {
            return OperationResult.Failure("Only portfolio items submitted by the student can be reviewed.");
        }

        var now = DateTimeOffset.UtcNow;
        await repository.SavePortfolioDraftItemAsync(item with
        {
            Status = command.Status,
            ParentReviewNotes = Clean(command.ParentReviewNotes),
            ReviewedAtUtc = now,
            UpdatedAtUtc = now
        }, cancellationToken);

        return OperationResult.Success();
    }

    private async Task<PortfolioContext> LoadContextAsync(CancellationToken cancellationToken)
    {
        return new PortfolioContext(
            await repository.GetStudentsAsync(cancellationToken),
            await repository.GetCoursesAsync(cancellationToken),
            await repository.GetEvidenceRecordsAsync(cancellationToken),
            await repository.GetPortfolioDraftItemsAsync(cancellationToken));
    }

    private async Task<Student?> ResolveStudentAsync(Guid? studentId, CancellationToken cancellationToken)
    {
        return studentId.HasValue
            ? await repository.GetStudentAsync(studentId.Value, cancellationToken)
            : await repository.GetStudentAsync(cancellationToken);
    }

    private async Task<EvidenceRecord?> FindEvidenceAsync(Guid evidenceRecordId, CancellationToken cancellationToken)
    {
        var evidenceRecords = await repository.GetEvidenceRecordsAsync(cancellationToken);
        return evidenceRecords.FirstOrDefault(evidence => evidence.Id == evidenceRecordId);
    }

    private static OperationResult RequireStudentReadAccess(UserContext user)
    {
        return user.Role is UserRole.Student or UserRole.ParentAdmin
            ? OperationResult.Success()
            : OperationResult.Failure("Sign in to view portfolio information.");
    }

    private static PortfolioDraftItemView? ToDraftView(PortfolioDraftItem item, Student student, PortfolioContext context)
    {
        var evidence = context.EvidenceRecords.FirstOrDefault(record => record.Id == item.EvidenceRecordId);
        if (evidence is null)
        {
            return null;
        }

        var course = context.Courses.FirstOrDefault(candidate => candidate.Id == evidence.CourseId);
        var module = course?.Modules.FirstOrDefault(candidate => candidate.Id == evidence.ModuleId);
        var assignment = module?.Assignments.FirstOrDefault(candidate => candidate.Id == evidence.AssignmentId);
        if (course is null || module is null || assignment is null)
        {
            return null;
        }

        return new PortfolioDraftItemView(
            item.Id,
            item.StudentId,
            StudentName(student),
            item.EvidenceRecordId,
            course.Id,
            course.Title,
            module.Id,
            module.Title,
            assignment.Id,
            assignment.Title,
            item.DisplayTitle,
            item.PortfolioSection,
            item.StudentReflection,
            item.ChosenReason,
            item.SkillsShown,
            item.SortOrder,
            item.IncludeInDraft,
            item.Status,
            item.ParentReviewNotes,
            item.UpdatedAtUtc,
            item.SubmittedAtUtc,
            item.ReviewedAtUtc,
            evidence.PortfolioCandidate,
            evidence.StoredFileIds.Count);
    }

    private static PortfolioEvidenceOptionView? ToEvidenceOption(
        EvidenceRecord evidence,
        Student student,
        PortfolioContext context,
        IReadOnlyList<PortfolioDraftItemView> draftItems)
    {
        var course = context.Courses.FirstOrDefault(candidate => candidate.Id == evidence.CourseId);
        var module = course?.Modules.FirstOrDefault(candidate => candidate.Id == evidence.ModuleId);
        var assignment = module?.Assignments.FirstOrDefault(candidate => candidate.Id == evidence.AssignmentId);
        if (course is null || module is null || assignment is null)
        {
            return null;
        }

        return new PortfolioEvidenceOptionView(
            evidence.Id,
            evidence.StudentId,
            StudentName(student),
            course.Id,
            course.Title,
            module.Id,
            module.Title,
            assignment.Id,
            assignment.Title,
            evidence.Title,
            evidence.Description,
            evidence.ConfirmedAtUtc,
            evidence.PortfolioCandidate,
            evidence.StoredFileIds.Count,
            draftItems.Any(item => item.EvidenceRecordId == evidence.Id));
    }

    private static string StudentName(Student student)
    {
        return $"{student.FirstName} {student.LastName}".Trim();
    }

    private static string Clean(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
    }

    private static IReadOnlyList<string> CleanList(IReadOnlyList<string>? values)
    {
        return (values ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private sealed record PortfolioContext(
        IReadOnlyList<Student> Students,
        IReadOnlyList<Course> Courses,
        IReadOnlyList<EvidenceRecord> EvidenceRecords,
        IReadOnlyList<PortfolioDraftItem> DraftItems);
}
