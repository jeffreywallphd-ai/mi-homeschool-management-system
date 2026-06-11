using HomeschoolManager.Application.Access;
using HomeschoolManager.Application.Common;
using HomeschoolManager.Application.Persistence;
using HomeschoolManager.Domain.Access;
using HomeschoolManager.Domain.Assessments;
using HomeschoolManager.Domain.Curriculum;
using HomeschoolManager.Domain.Students;
using HomeschoolManager.Domain.Submissions;

namespace HomeschoolManager.Application.Assessments;

public sealed class GradebookService
{
    private readonly IHomeschoolRepository repository;

    public GradebookService(IHomeschoolRepository repository)
    {
        this.repository = repository;
    }

    public async Task<OperationResult<GradebookDashboardSummary>> GetDashboardSummaryAsync(
        UserContext user,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<GradebookDashboardSummary>.Failure(authorized.Errors.ToArray());
        }

        var students = await repository.GetStudentsAsync(cancellationToken);
        var courses = (await repository.GetCoursesAsync(cancellationToken))
            .Where(course => !course.IsArchived)
            .ToArray();
        var submissions = await repository.GetAssignmentSubmissionsAsync(cancellationToken);
        var evidence = await repository.GetEvidenceRecordsAsync(cancellationToken);
        var assessments = (await repository.GetAssessmentRecordsAsync(cancellationToken))
            .Where(record => !record.IsArchived)
            .ToArray();

        var rows = courses.SelectMany(course => BuildRows(course, submissions, evidence, assessments)).ToArray();
        return OperationResult<GradebookDashboardSummary>.Success(new GradebookDashboardSummary(
            students.Count,
            courses.Length,
            rows.Count(row => row.EffectiveState == AssessmentState.NeedsReview),
            rows.Count(row => row.EffectiveState == AssessmentState.Assessed),
            rows.Count(row => row.EffectiveState == AssessmentState.ReturnedForRevision),
            rows.Count(row => row.EffectiveState == AssessmentState.Incomplete)));
    }

    public async Task<OperationResult<GradebookPage>> GetGradebookAsync(
        UserContext user,
        Guid? studentId = null,
        Guid? courseId = null,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<GradebookPage>.Failure(authorized.Errors.ToArray());
        }

        var students = await repository.GetStudentsAsync(cancellationToken);
        var selectedStudent = ResolveStudent(students, studentId);
        if (selectedStudent is null)
        {
            return OperationResult<GradebookPage>.Failure("Student was not found.");
        }

        var allCourses = (await repository.GetCoursesAsync(cancellationToken))
            .Where(course => !course.IsArchived)
            .Where(course => course.StudentId == selectedStudent.Id)
            .OrderBy(course => course.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var selectedCourse = courseId.HasValue
            ? allCourses.FirstOrDefault(course => course.Id == courseId.Value)
            : allCourses.FirstOrDefault();

        var submissions = await repository.GetAssignmentSubmissionsAsync(cancellationToken);
        var evidence = await repository.GetEvidenceRecordsAsync(cancellationToken);
        var assessments = (await repository.GetAssessmentRecordsAsync(cancellationToken))
            .Where(record => !record.IsArchived)
            .ToArray();
        var rows = selectedCourse is null
            ? []
            : BuildRows(selectedCourse, submissions, evidence, assessments);

        var page = new GradebookPage(
            selectedStudent.Id,
            selectedCourse?.Id,
            students.Select(student => new GradebookStudentOption(student.Id, StudentName(student))).ToArray(),
            allCourses.Select(course => new GradebookCourseOption(course.Id, course.Title)).ToArray(),
            selectedCourse is null ? null : BuildSummary(selectedCourse, rows),
            rows);

        return OperationResult<GradebookPage>.Success(page);
    }

    public async Task<OperationResult<Guid>> SaveAssessmentAsync(
        UserContext user,
        SaveAssessmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<Guid>.Failure(authorized.Errors.ToArray());
        }

        var course = await repository.GetCourseAsync(command.CourseId, cancellationToken);
        if (course is null || course.IsArchived || course.StudentId != command.StudentId)
        {
            return OperationResult<Guid>.Failure("Course was not found for this student.");
        }

        if (!SourceBelongsToCourse(course, command))
        {
            return OperationResult<Guid>.Failure("Assessment source was not found for this course.");
        }

        if (command.SubmissionId.HasValue)
        {
            var submission = await repository.GetAssignmentSubmissionAsync(command.SubmissionId.Value, cancellationToken);
            if (submission is null ||
                submission.StudentId != command.StudentId ||
                submission.CourseId != command.CourseId ||
                submission.ModuleId != command.ModuleId ||
                submission.AssignmentId != command.AssignmentId)
            {
                return OperationResult<Guid>.Failure("Submission was not found for this assessment source.");
            }
        }

        if (command.EvidenceRecordId.HasValue)
        {
            var evidence = (await repository.GetEvidenceRecordsAsync(cancellationToken))
                .FirstOrDefault(record => record.Id == command.EvidenceRecordId.Value);
            if (evidence is null ||
                evidence.StudentId != command.StudentId ||
                evidence.CourseId != command.CourseId ||
                evidence.ModuleId != command.ModuleId ||
                evidence.AssignmentId != command.AssignmentId)
            {
                return OperationResult<Guid>.Failure("Evidence was not found for this assessment source.");
            }
        }

        var existing = command.AssessmentId.HasValue
            ? await repository.GetAssessmentRecordAsync(command.AssessmentId.Value, cancellationToken)
            : null;
        if (command.AssessmentId.HasValue && existing is null)
        {
            return OperationResult<Guid>.Failure("Assessment record was not found.");
        }

        var now = DateTimeOffset.UtcNow;
        try
        {
            var record = new AssessmentRecord(
                existing?.Id ?? command.AssessmentId ?? Guid.NewGuid(),
                command.StudentId,
                command.CourseId,
                command.ModuleId,
                command.AssignmentId,
                command.SubmissionId,
                command.EvidenceRecordId,
                command.SourceType,
                command.State,
                command.ResultType,
                command.ResultValue,
                command.PointsEarned,
                command.PointsPossible,
                command.Percentage,
                command.Narrative,
                command.RubricSummary,
                command.ParentNotes,
                command.StudentFeedback,
                command.FeedbackVisibleToStudent,
                existing?.CreatedAtUtc ?? now,
                now,
                false);

            await repository.SaveAssessmentRecordAsync(record, cancellationToken);
            return OperationResult<Guid>.Success(record.Id);
        }
        catch (Exception ex) when (ex is HomeschoolManager.Domain.Common.DomainException)
        {
            return OperationResult<Guid>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> ArchiveAssessmentAsync(
        UserContext user,
        ArchiveAssessmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        var record = await repository.GetAssessmentRecordAsync(command.AssessmentId, cancellationToken);
        if (record is null)
        {
            return OperationResult.Failure("Assessment record was not found.");
        }

        await repository.SaveAssessmentRecordAsync(record.Archive(DateTimeOffset.UtcNow), cancellationToken);
        return OperationResult.Success();
    }

    public async Task<OperationResult<IReadOnlyList<StudentAssessmentFeedbackView>>> ListStudentFeedbackAsync(
        UserContext user,
        Guid studentId,
        Guid? courseId = null,
        Guid? moduleId = null,
        CancellationToken cancellationToken = default)
    {
        if (user.Role is not (UserRole.Student or UserRole.ParentAdmin))
        {
            return OperationResult<IReadOnlyList<StudentAssessmentFeedbackView>>.Failure("Sign in to view assessment feedback.");
        }

        var student = await repository.GetStudentAsync(studentId, cancellationToken);
        if (student is null)
        {
            return OperationResult<IReadOnlyList<StudentAssessmentFeedbackView>>.Failure("Student was not found.");
        }

        var records = (await repository.GetAssessmentRecordsAsync(cancellationToken))
            .Where(record => !record.IsArchived)
            .Where(record => record.StudentId == student.Id)
            .Where(record => record.FeedbackVisibleToStudent)
            .Where(record => courseId is null || record.CourseId == courseId.Value)
            .Where(record => moduleId is null || record.ModuleId == moduleId.Value)
            .OrderByDescending(record => record.UpdatedAtUtc)
            .Select(ToStudentFeedback)
            .ToArray();

        return OperationResult<IReadOnlyList<StudentAssessmentFeedbackView>>.Success(records);
    }

    private static Student? ResolveStudent(IReadOnlyList<Student> students, Guid? studentId)
    {
        return studentId.HasValue
            ? students.FirstOrDefault(student => student.Id == studentId.Value)
            : students.FirstOrDefault();
    }

    private static bool SourceBelongsToCourse(Course course, SaveAssessmentCommand command)
    {
        if (command.ModuleId is null && command.AssignmentId is null)
        {
            return command.SourceType == AssessmentSourceType.CourseContext ||
                command.SourceType == AssessmentSourceType.ParentEvaluation ||
                command.SourceType == AssessmentSourceType.TestRecord ||
                command.SourceType == AssessmentSourceType.PortfolioArtifact;
        }

        var module = course.Modules.FirstOrDefault(item => item.Id == command.ModuleId);
        return module?.Assignments.Any(assignment => assignment.Id == command.AssignmentId) == true;
    }

    private static IReadOnlyList<GradebookAssessmentRow> BuildRows(
        Course course,
        IReadOnlyList<AssignmentSubmission> submissions,
        IReadOnlyList<EvidenceRecord> evidenceRecords,
        IReadOnlyList<AssessmentRecord> assessments)
    {
        return course.Modules
            .OrderBy(module => module.SequenceOrder)
            .SelectMany(module => module.Assignments
                .OrderBy(assignment => assignment.SequenceOrder)
                .Select(assignment =>
                {
                    var assignmentSubmissions = submissions
                        .Where(submission => submission.StudentId == course.StudentId)
                        .Where(submission => submission.CourseId == course.Id)
                        .Where(submission => submission.ModuleId == module.Id)
                        .Where(submission => submission.AssignmentId == assignment.Id)
                        .OrderByDescending(submission => submission.SubmittedAtUtc)
                        .ToArray();
                    var activeSubmissions = assignmentSubmissions
                        .Where(IsActiveReviewSubmission)
                        .OrderBy(submission => submission.DraftNumber)
                        .ThenByDescending(submission => submission.SubmittedAtUtc)
                        .ToArray();
                    var latestSubmission = activeSubmissions
                        .OrderByDescending(submission => submission.SubmittedAtUtc)
                        .FirstOrDefault();
                    var evidence = evidenceRecords
                        .Where(record => record.StudentId == course.StudentId)
                        .Where(record => record.CourseId == course.Id)
                        .Where(record => record.ModuleId == module.Id)
                        .Where(record => record.AssignmentId == assignment.Id)
                        .OrderByDescending(record => record.ConfirmedAtUtc)
                        .FirstOrDefault();
                    var assessment = assessments
                        .Where(record => record.StudentId == course.StudentId)
                        .Where(record => record.CourseId == course.Id)
                        .Where(record => record.ModuleId == module.Id)
                        .Where(record => record.AssignmentId == assignment.Id)
                        .OrderByDescending(record => record.UpdatedAtUtc)
                        .FirstOrDefault();

                    var detail = assessment is null ? null : ToDetail(assessment);
                    return new GradebookAssessmentRow(
                        course.StudentId,
                        course.Id,
                        module.Id,
                        module.Title,
                        module.SequenceOrder,
                        assignment.Id,
                        assignment.Title,
                        assignment.SequenceOrder,
                        assignment.Status,
                        assignment.PlannedPoints,
                        assignment.PlannedWeight,
                        latestSubmission?.Id,
                        latestSubmission?.Status,
                        latestSubmission?.SubmittedAtUtc,
                        latestSubmission?.DraftNumber,
                        assignment.DraftCount,
                        latestSubmission is not null &&
                            assignment.SubmissionStructure == AssignmentSubmissionStructure.MultiDraft &&
                            latestSubmission.DraftNumber == assignment.DraftCount,
                        latestSubmission?.Attachments.Select(ToAttachment).ToArray() ?? [],
                        activeSubmissions.Select(submission => ToSubmissionView(assignment, submission)).ToArray(),
                        evidence?.Id,
                        detail,
                        detail?.State ?? EffectiveState(latestSubmission, evidence));
                }))
            .ToArray();
    }

    private static GradebookCourseSummary BuildSummary(Course course, IReadOnlyList<GradebookAssessmentRow> rows)
    {
        return new GradebookCourseSummary(
            course.StudentId,
            course.Id,
            course.Title,
            rows.Count,
            rows.Count(row => row.EffectiveState == AssessmentState.NeedsReview),
            rows.Count(row => row.EffectiveState == AssessmentState.Assessed),
            rows.Count(row => row.EffectiveState == AssessmentState.ReturnedForRevision),
            rows.Count(row => row.EffectiveState == AssessmentState.Incomplete),
            rows.Count(row => row.EffectiveState == AssessmentState.Excused),
            rows.Count(row => row.EffectiveState == AssessmentState.NotApplicable));
    }

    private static AssessmentState EffectiveState(AssignmentSubmission? submission, EvidenceRecord? evidence)
    {
        if (submission?.Status == AssignmentSubmissionStatus.Returned)
        {
            return AssessmentState.ReturnedForRevision;
        }

        if (submission?.Status == AssignmentSubmissionStatus.Accepted || evidence is not null)
        {
            return AssessmentState.NeedsReview;
        }

        return AssessmentState.NotAssessed;
    }

    private static bool IsActiveReviewSubmission(AssignmentSubmission submission)
    {
        return submission.Status is not (AssignmentSubmissionStatus.Archived or AssignmentSubmissionStatus.Cleared);
    }

    private static AssessmentDetail ToDetail(AssessmentRecord record)
    {
        return new AssessmentDetail(
            record.Id,
            record.SourceType,
            record.State,
            record.ResultType,
            record.ResultValue,
            record.PointsEarned,
            record.PointsPossible,
            record.Percentage,
            record.Narrative,
            record.RubricSummary,
            record.ParentNotes,
            record.StudentFeedback,
            record.FeedbackVisibleToStudent,
            record.UpdatedAtUtc);
    }

    private static GradebookSubmissionAttachment ToAttachment(StoredFileReference file)
    {
        return new GradebookSubmissionAttachment(
            file.Id,
            file.OriginalFileName,
            file.ContentType,
            file.SizeBytes,
            file.ChecksumSha256,
            file.CreatedAtUtc);
    }

    private static GradebookSubmissionView ToSubmissionView(ModuleAssignment assignment, AssignmentSubmission submission)
    {
        return new GradebookSubmissionView(
            submission.Id,
            submission.Status,
            submission.SubmittedAtUtc,
            submission.AttemptNumber,
            submission.DraftNumber,
            assignment.DraftCount,
            assignment.SubmissionStructure == AssignmentSubmissionStructure.MultiDraft &&
                submission.DraftNumber == assignment.DraftCount,
            submission.Attachments.Select(ToAttachment).ToArray());
    }

    private static StudentAssessmentFeedbackView ToStudentFeedback(AssessmentRecord record)
    {
        return new StudentAssessmentFeedbackView(
            record.Id,
            record.CourseId,
            record.ModuleId,
            record.AssignmentId,
            record.State,
            record.ResultType,
            ResultLabel(record),
            record.StudentFeedback,
            record.UpdatedAtUtc);
    }

    private static string ResultLabel(AssessmentRecord record)
    {
        return record.ResultType switch
        {
            AssessmentResultType.Points when record.PointsEarned.HasValue && record.PointsPossible.HasValue => $"{record.PointsEarned:g} / {record.PointsPossible:g}",
            AssessmentResultType.Percentage when record.Percentage.HasValue => $"{record.Percentage:g}%",
            AssessmentResultType.LetterGrade => record.ResultValue,
            AssessmentResultType.PassFail => record.ResultValue,
            AssessmentResultType.TestScore => !string.IsNullOrWhiteSpace(record.ResultValue) ? record.ResultValue : record.Percentage.HasValue ? $"{record.Percentage:g}%" : "Test score recorded",
            AssessmentResultType.RubricSummary => "Rubric summary recorded",
            AssessmentResultType.Narrative => "Narrative evaluation recorded",
            _ => StateLabel(record.State)
        };
    }

    private static string StateLabel(AssessmentState state)
    {
        return state switch
        {
            AssessmentState.NotAssessed => "Not assessed",
            AssessmentState.NeedsReview => "Needs review",
            AssessmentState.Assessed => "Assessed",
            AssessmentState.ReturnedForRevision => "Returned for revision",
            AssessmentState.Excused => "Excused",
            AssessmentState.Incomplete => "Incomplete",
            AssessmentState.NotApplicable => "Not applicable",
            _ => "Assessment recorded"
        };
    }

    private static string StudentName(Student student)
    {
        return $"{student.FirstName} {student.LastName}".Trim();
    }
}
