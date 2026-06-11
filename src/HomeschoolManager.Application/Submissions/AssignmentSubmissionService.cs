using HomeschoolManager.Application.Access;
using HomeschoolManager.Application.Common;
using HomeschoolManager.Application.Persistence;
using HomeschoolManager.Domain.Access;
using HomeschoolManager.Domain.Curriculum;
using HomeschoolManager.Domain.Submissions;

namespace HomeschoolManager.Application.Submissions;

public sealed class AssignmentSubmissionService
{
    private readonly IHomeschoolRepository repository;
    private readonly ISubmissionFileStore fileStore;

    public AssignmentSubmissionService(IHomeschoolRepository repository, ISubmissionFileStore fileStore)
    {
        this.repository = repository;
        this.fileStore = fileStore;
    }

    public async Task<OperationResult<Guid>> SubmitAssignmentAsync(
        UserContext user,
        SubmitAssignmentCommand command,
        CancellationToken cancellationToken = default)
    {
        if (user.Role != UserRole.Student)
        {
            return OperationResult<Guid>.Failure("Only the true student portal can submit assignment work.");
        }

        if (string.IsNullOrWhiteSpace(command.ResponseText) && (command.Attachments is null || command.Attachments.Count == 0))
        {
            return OperationResult<Guid>.Failure("Add a written response or attach a file before submitting.");
        }

        var context = await ResolveAssignmentContextAsync(
            command.CourseId,
            command.ModuleId,
            command.AssignmentId,
            command.StudentId,
            cancellationToken);
        if (context is null)
        {
            return OperationResult<Guid>.Failure("Assignment was not found for this student.");
        }

        var existing = await repository.GetAssignmentSubmissionsAsync(cancellationToken);
        var attemptNumber = existing.Count(submission =>
            submission.StudentId == context.StudentId &&
            submission.CourseId == context.Course.Id &&
            submission.ModuleId == context.Module.Id &&
            submission.AssignmentId == context.Assignment.Id) + 1;

        var now = DateTimeOffset.UtcNow;
        var submissionId = Guid.NewGuid();
        var savedFiles = new List<StoredFileReference>();
        try
        {
            foreach (var attachment in command.Attachments ?? [])
            {
                var savedFile = await fileStore.SaveAssignmentSubmissionFileAsync(
                    context.StudentId,
                    submissionId,
                    attachment,
                    cancellationToken);
                savedFiles.Add(savedFile);
            }

            var submission = new AssignmentSubmission(
                submissionId,
                context.StudentId,
                context.Course.Id,
                context.Module.Id,
                context.Assignment.Id,
                attemptNumber,
                AssignmentSubmissionStatus.Submitted,
                command.ResponseText,
                command.StudentNotes,
                savedFiles,
                now,
                now,
                now,
                null,
                null,
                "",
                context.Assignment.IsPortfolioCandidate);

            await repository.SaveAssignmentSubmissionAsync(submission, cancellationToken);
            return OperationResult<Guid>.Success(submission.Id);
        }
        catch
        {
            foreach (var file in savedFiles)
            {
                await fileStore.DeleteStoredFileAsync(file, cancellationToken);
            }

            throw;
        }
    }

    public async Task<OperationResult> ReturnSubmissionAsync(
        UserContext user,
        ReturnSubmissionCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        if (string.IsNullOrWhiteSpace(command.ParentReviewNotes))
        {
            return OperationResult.Failure("Add parent review notes before returning the work.");
        }

        var submission = await repository.GetAssignmentSubmissionAsync(command.SubmissionId, cancellationToken);
        if (submission is null)
        {
            return OperationResult.Failure("Submission was not found.");
        }

        var now = DateTimeOffset.UtcNow;
        await repository.SaveAssignmentSubmissionAsync(submission with
        {
            Status = AssignmentSubmissionStatus.Returned,
            ParentReviewNotes = command.ParentReviewNotes.Trim(),
            ReturnedAtUtc = now,
            UpdatedAtUtc = now
        }, cancellationToken);

        return OperationResult.Success();
    }

    public async Task<OperationResult<Guid>> AcceptSubmissionAsync(
        UserContext user,
        AcceptSubmissionCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<Guid>.Failure(authorized.Errors.ToArray());
        }

        var submission = await repository.GetAssignmentSubmissionAsync(command.SubmissionId, cancellationToken);
        if (submission is null)
        {
            return OperationResult<Guid>.Failure("Submission was not found.");
        }

        var context = await ResolveAssignmentContextAsync(
            submission.CourseId,
            submission.ModuleId,
            submission.AssignmentId,
            submission.StudentId,
            cancellationToken);
        if (context is null)
        {
            return OperationResult<Guid>.Failure("Assignment context was not found.");
        }

        var evidenceRecords = await repository.GetEvidenceRecordsAsync(cancellationToken);
        var existingEvidence = evidenceRecords.FirstOrDefault(evidence => evidence.SubmissionId == submission.Id);
        var now = DateTimeOffset.UtcNow;
        var parentNotes = string.IsNullOrWhiteSpace(command.ParentReviewNotes)
            ? submission.ParentReviewNotes
            : command.ParentReviewNotes.Trim();

        var acceptedSubmission = submission with
        {
            Status = AssignmentSubmissionStatus.Accepted,
            ParentReviewNotes = parentNotes,
            AcceptedAtUtc = now,
            UpdatedAtUtc = now,
            PortfolioCandidate = submission.PortfolioCandidate || command.MarkPortfolioCandidate
        };

        var evidence = new EvidenceRecord(
            existingEvidence?.Id ?? Guid.NewGuid(),
            submission.StudentId,
            submission.CourseId,
            submission.ModuleId,
            submission.AssignmentId,
            submission.Id,
            context.Assignment.Title,
            EvidenceDescription(context.Course.Title, context.Module.Title, context.Assignment.RequiredOutput),
            submission.Attachments.Select(file => file.Id).ToArray(),
            existingEvidence?.CreatedAtUtc ?? now,
            now,
            parentNotes,
            acceptedSubmission.PortfolioCandidate);

        await repository.SaveAssignmentSubmissionAsync(acceptedSubmission, cancellationToken);
        await repository.SaveEvidenceRecordAsync(evidence, cancellationToken);
        return OperationResult<Guid>.Success(evidence.Id);
    }

    public async Task<OperationResult<IReadOnlyList<AssignmentSubmissionSummary>>> ListPendingReviewsAsync(
        UserContext user,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<IReadOnlyList<AssignmentSubmissionSummary>>.Failure(authorized.Errors.ToArray());
        }

        var submissions = await repository.GetAssignmentSubmissionsAsync(cancellationToken);
        var pending = new List<AssignmentSubmissionSummary>();
        foreach (var submission in submissions.Where(item => item.Status == AssignmentSubmissionStatus.Submitted))
        {
            var summary = await ToSummaryAsync(submission, cancellationToken);
            if (summary is not null)
            {
                pending.Add(summary);
            }
        }

        return OperationResult<IReadOnlyList<AssignmentSubmissionSummary>>.Success(pending);
    }

    public async Task<OperationResult<IReadOnlyList<AssignmentSubmissionSummary>>> ListStudentAssignmentSubmissionsAsync(
        UserContext user,
        Guid courseId,
        Guid moduleId,
        Guid assignmentId,
        Guid? studentId = null,
        CancellationToken cancellationToken = default)
    {
        if (user.Role is not (UserRole.Student or UserRole.ParentAdmin))
        {
            return OperationResult<IReadOnlyList<AssignmentSubmissionSummary>>.Failure("Sign in to view submissions.");
        }

        var context = await ResolveAssignmentContextAsync(courseId, moduleId, assignmentId, studentId, cancellationToken);
        if (context is null)
        {
            return OperationResult<IReadOnlyList<AssignmentSubmissionSummary>>.Failure("Assignment was not found for this student.");
        }

        var submissions = await repository.GetAssignmentSubmissionsAsync(cancellationToken);
        var summaries = new List<AssignmentSubmissionSummary>();
        foreach (var submission in submissions
            .Where(item => item.StudentId == context.StudentId)
            .Where(item => item.CourseId == courseId && item.ModuleId == moduleId && item.AssignmentId == assignmentId)
            .OrderByDescending(item => item.SubmittedAtUtc))
        {
            var summary = await ToSummaryAsync(submission, cancellationToken);
            if (summary is not null)
            {
                summaries.Add(summary);
            }
        }

        return OperationResult<IReadOnlyList<AssignmentSubmissionSummary>>.Success(summaries);
    }

    public async Task<OperationResult<SubmissionReviewDetail>> GetReviewDetailAsync(
        UserContext user,
        Guid submissionId,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<SubmissionReviewDetail>.Failure(authorized.Errors.ToArray());
        }

        var submission = await repository.GetAssignmentSubmissionAsync(submissionId, cancellationToken);
        if (submission is null)
        {
            return OperationResult<SubmissionReviewDetail>.Failure("Submission was not found.");
        }

        var detail = await ToDetailAsync(submission, cancellationToken);
        return detail is null
            ? OperationResult<SubmissionReviewDetail>.Failure("Submission context was not found.")
            : OperationResult<SubmissionReviewDetail>.Success(detail);
    }

    public async Task<OperationResult<IReadOnlyList<EvidenceRecordView>>> ListEvidenceAsync(
        UserContext user,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<IReadOnlyList<EvidenceRecordView>>.Failure(authorized.Errors.ToArray());
        }

        var records = await repository.GetEvidenceRecordsAsync(cancellationToken);
        return OperationResult<IReadOnlyList<EvidenceRecordView>>.Success(records.Select(ToEvidenceView).ToArray());
    }

    private async Task<AssignmentContext?> ResolveAssignmentContextAsync(
        Guid courseId,
        Guid moduleId,
        Guid assignmentId,
        Guid? studentId,
        CancellationToken cancellationToken)
    {
        var course = await repository.GetCourseAsync(courseId, cancellationToken);
        if (course is null || course.IsArchived)
        {
            return null;
        }

        if (studentId.HasValue && course.StudentId != studentId.Value)
        {
            return null;
        }

        var module = course.Modules.FirstOrDefault(item => item.Id == moduleId);
        var assignment = module?.Assignments.FirstOrDefault(item => item.Id == assignmentId);
        return module is null || assignment is null
            ? null
            : new AssignmentContext(course.StudentId, course, module, assignment);
    }

    private async Task<AssignmentSubmissionSummary?> ToSummaryAsync(
        AssignmentSubmission submission,
        CancellationToken cancellationToken)
    {
        var detail = await ToDetailAsync(submission, cancellationToken);
        return detail is null
            ? null
            : new AssignmentSubmissionSummary(
                detail.SubmissionId,
                detail.StudentId,
                detail.StudentName,
                detail.CourseId,
                detail.CourseTitle,
                detail.ModuleId,
                detail.ModuleTitle,
                detail.AssignmentId,
                detail.AssignmentTitle,
                detail.AttemptNumber,
                detail.Status,
                detail.SubmittedAtUtc,
                detail.ReturnedAtUtc,
                detail.AcceptedAtUtc,
                detail.ParentReviewNotes,
                detail.PortfolioCandidate,
                detail.Attachments.Count);
    }

    private async Task<SubmissionReviewDetail?> ToDetailAsync(
        AssignmentSubmission submission,
        CancellationToken cancellationToken)
    {
        var student = await repository.GetStudentAsync(submission.StudentId, cancellationToken);
        var context = await ResolveAssignmentContextAsync(
            submission.CourseId,
            submission.ModuleId,
            submission.AssignmentId,
            submission.StudentId,
            cancellationToken);
        if (student is null || context is null)
        {
            return null;
        }

        var evidence = (await repository.GetEvidenceRecordsAsync(cancellationToken))
            .FirstOrDefault(record => record.SubmissionId == submission.Id);

        return new SubmissionReviewDetail(
            submission.Id,
            submission.StudentId,
            $"{student.FirstName} {student.LastName}".Trim(),
            context.Course.Id,
            context.Course.Title,
            context.Module.Id,
            context.Module.Title,
            context.Assignment.Id,
            context.Assignment.Title,
            submission.AttemptNumber,
            submission.Status,
            submission.ResponseText,
            submission.StudentNotes,
            submission.SubmittedAtUtc,
            submission.ReturnedAtUtc,
            submission.AcceptedAtUtc,
            submission.ParentReviewNotes,
            submission.PortfolioCandidate,
            submission.Attachments.Select(ToAttachmentView).ToArray(),
            evidence is null ? null : ToEvidenceView(evidence));
    }

    private static SubmissionAttachmentView ToAttachmentView(StoredFileReference file)
    {
        return new SubmissionAttachmentView(
            file.Id,
            file.OriginalFileName,
            file.ContentType,
            file.SizeBytes,
            file.ChecksumSha256,
            file.CreatedAtUtc);
    }

    private static EvidenceRecordView ToEvidenceView(EvidenceRecord evidence)
    {
        return new EvidenceRecordView(
            evidence.Id,
            evidence.SubmissionId,
            evidence.Title,
            evidence.Description,
            evidence.ConfirmedAtUtc,
            evidence.ParentNotes,
            evidence.PortfolioCandidate,
            evidence.StoredFileIds.Count);
    }

    private static string EvidenceDescription(string courseTitle, string moduleTitle, string requiredOutput)
    {
        var output = string.IsNullOrWhiteSpace(requiredOutput) ? "Student-submitted assignment work." : requiredOutput.Trim();
        return $"{courseTitle} | {moduleTitle} | {output}";
    }

    private sealed record AssignmentContext(
        Guid StudentId,
        Course Course,
        LearningModule Module,
        ModuleAssignment Assignment);
}
