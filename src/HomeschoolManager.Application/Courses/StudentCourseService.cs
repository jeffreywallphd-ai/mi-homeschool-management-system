using HomeschoolManager.Application.Common;
using HomeschoolManager.Application.Persistence;
using HomeschoolManager.Domain.Access;
using HomeschoolManager.Domain.Assessments;
using HomeschoolManager.Domain.Curriculum;
using HomeschoolManager.Domain.Students;
using HomeschoolManager.Domain.Submissions;

namespace HomeschoolManager.Application.Courses;

public sealed class StudentCourseService
{
    private const string NoGradeYet = "No grade yet";
    private readonly IHomeschoolRepository repository;

    public StudentCourseService(IHomeschoolRepository repository)
    {
        this.repository = repository;
    }

    public async Task<OperationResult<StudentCourseDashboard>> ListCoursesAsync(
        UserContext user,
        Guid? studentId = null,
        CancellationToken cancellationToken = default)
    {
        var authorized = RequireStudentReadAccess(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<StudentCourseDashboard>.Failure(authorized.Errors.ToArray());
        }

        var student = await ResolveStudentAsync(studentId, cancellationToken);
        if (student is null)
        {
            return OperationResult<StudentCourseDashboard>.Failure("Student was not found.");
        }

        var schoolYear = await repository.GetSchoolYearAsync(cancellationToken);
        var courses = await repository.GetCoursesAsync(cancellationToken);
        var cards = courses
            .Where(course => course.StudentId == student.Id)
            .Where(course => !course.IsArchived)
            .OrderBy(course => course.Title)
            .Select(course => new StudentCourseCard(
                student.Id,
                course.Id,
                course.Title,
                course.Description.Description,
                course.Duration,
                course.PlannedCreditValue,
                NoGradeYet,
                course.Modules.Count,
                course.Modules.Count(module => module.Status == ModuleStatus.Complete),
                CourseTermNames(course, schoolYear)))
            .ToArray();

        return OperationResult<StudentCourseDashboard>.Success(new StudentCourseDashboard(
            student.Id,
            student.FirstName,
            TermNames(schoolYear),
            cards));
    }

    public async Task<OperationResult<StudentCoursePage>> GetCourseAsync(
        UserContext user,
        Guid courseId,
        Guid? studentId = null,
        CancellationToken cancellationToken = default)
    {
        var authorized = RequireStudentReadAccess(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<StudentCoursePage>.Failure(authorized.Errors.ToArray());
        }

        var course = await repository.GetCourseAsync(courseId, cancellationToken);
        if (course is null || course.IsArchived)
        {
            return OperationResult<StudentCoursePage>.Failure("Course was not found.");
        }

        var student = await ResolveStudentAsync(studentId ?? course.StudentId, cancellationToken);
        if (student is null || course.StudentId != student.Id)
        {
            return OperationResult<StudentCoursePage>.Failure("Course was not found for this student.");
        }

        var schoolYear = await repository.GetSchoolYearAsync(cancellationToken);
        return OperationResult<StudentCoursePage>.Success(new StudentCoursePage(
            student.Id,
            student.FirstName,
            course.Id,
            course.Title,
            course.Description.Description,
            course.Duration,
            course.PlannedCreditValue,
            NoGradeYet,
            TermNames(schoolYear),
            SplitLines(course.CurriculumPlan.LearningObjectives).ToArray(),
            ModuleLinks(course, schoolYear)));
    }

    public async Task<OperationResult<StudentCourseSyllabus>> GetSyllabusAsync(
        UserContext user,
        Guid courseId,
        Guid? studentId = null,
        CancellationToken cancellationToken = default)
    {
        var authorized = RequireStudentReadAccess(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<StudentCourseSyllabus>.Failure(authorized.Errors.ToArray());
        }

        var course = await repository.GetCourseAsync(courseId, cancellationToken);
        if (course is null || course.IsArchived)
        {
            return OperationResult<StudentCourseSyllabus>.Failure("Course was not found.");
        }

        var student = await ResolveStudentAsync(studentId ?? course.StudentId, cancellationToken);
        if (student is null || course.StudentId != student.Id)
        {
            return OperationResult<StudentCourseSyllabus>.Failure("Course was not found for this student.");
        }

        var schoolYear = await repository.GetSchoolYearAsync(cancellationToken);
        return OperationResult<StudentCourseSyllabus>.Success(new StudentCourseSyllabus(
            student.Id,
            course.Id,
            course.Title,
            course.Description.Description,
            course.Duration,
            course.PlannedCreditValue,
            course.Description.InstructionalMethods,
            ParseResources(course.Description.TextsAndResources).ToArray(),
            course.Description.AssessmentMethods,
            course.Description.GradingBasis,
            course.CurriculumPlan.Goals,
            SplitLines(course.CurriculumPlan.LearningObjectives).ToArray(),
            course.CurriculumPlan.PlannedSequence,
            course.CurriculumPlan.ParentNotes,
            TermNames(schoolYear),
            ModuleLinks(course, schoolYear)));
    }

    public async Task<OperationResult<StudentModulePage>> GetModuleAsync(
        UserContext user,
        Guid courseId,
        Guid moduleId,
        Guid? studentId = null,
        CancellationToken cancellationToken = default)
    {
        var authorized = RequireStudentReadAccess(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<StudentModulePage>.Failure(authorized.Errors.ToArray());
        }

        var course = await repository.GetCourseAsync(courseId, cancellationToken);
        if (course is null || course.IsArchived)
        {
            return OperationResult<StudentModulePage>.Failure("Course was not found.");
        }

        var student = await ResolveStudentAsync(studentId ?? course.StudentId, cancellationToken);
        if (student is null || course.StudentId != student.Id)
        {
            return OperationResult<StudentModulePage>.Failure("Course was not found for this student.");
        }

        var modules = course.Modules.OrderBy(module => module.SequenceOrder).ToArray();
        var index = Array.FindIndex(modules, module => module.Id == moduleId);
        if (index < 0)
        {
            return OperationResult<StudentModulePage>.Failure("Module was not found.");
        }

        var schoolYear = await repository.GetSchoolYearAsync(cancellationToken);
        var module = modules[index];
        var submissions = user.Role == UserRole.Student
            ? await repository.GetAssignmentSubmissionsAsync(cancellationToken)
            : Array.Empty<AssignmentSubmission>();
        var assessmentRecords = user.Role == UserRole.Student
            ? await repository.GetAssessmentRecordsAsync(cancellationToken)
            : Array.Empty<AssessmentRecord>();
        return OperationResult<StudentModulePage>.Success(new StudentModulePage(
            student.Id,
            course.Id,
            course.Title,
            module.Id,
            index > 0 ? modules[index - 1].Id : null,
            index < modules.Length - 1 ? modules[index + 1].Id : null,
            module.SequenceOrder,
            module.Title,
            module.Description,
            TermName(module.TermId, schoolYear),
            module.EstimatedLength,
            module.Status,
            module.Instructions,
            module.LearningObjectiveItems
                .Select(objective => new StudentModuleObjectiveView(objective.Text, objective.LinkedCourseObjective))
                .ToArray(),
            module.ResourceItems
                .Select(resource => new StudentModuleResourceView(
                    resource.Name,
                    resource.Link,
                    Path.GetFileName(resource.FilePath),
                    resource.IsPhysicalResource))
                .ToArray(),
            module.Lessons
                .OrderBy(lesson => lesson.SequenceOrder)
                .Select(lesson => new StudentLessonView(
                    lesson.Id,
                    lesson.SequenceOrder,
                    lesson.Title,
                    lesson.IntroductoryText,
                    lesson.LinkedModuleObjective,
                    lesson.LessonType,
                    lesson.EstimatedMinutes,
                    lesson.SuggestedDays,
                    lesson.DifficultyLevel,
                    lesson.SubjectAreas,
                    lesson.Tags,
                    lesson.Prerequisites,
                    lesson.LearningObjectives
                        .Select(objective => new StudentLessonObjectiveView(objective.ObjectiveId, objective.Text, objective.BloomLevel))
                        .ToArray(),
                    lesson.StandardsAlignments
                        .Select(item => new StudentStandardsAlignmentView(item.Framework, item.Code, item.Description))
                        .ToArray(),
                    lesson.SuccessCriteria,
                    lesson.LessonSteps
                        .Select(step => new StudentLessonStepView(step.StepOrder, step.Title, step.StepType, step.Instructions, step.EstimatedMinutes, step.Required))
                        .ToArray(),
                    lesson.Resources
                        .Select(resource => new StudentLessonResourceView(
                            resource.Name,
                            resource.Type,
                            resource.Url,
                            Path.GetFileName(resource.FilePath),
                            resource.IsPhysicalResource,
                            resource.SourceNote,
                            resource.Required,
                            resource.EstimatedMinutes,
                            resource.StudentInstructions,
                            resource.NotesPrompt,
                            resource.Citation is null
                                ? null
                                : new StudentLessonResourceCitationView(resource.Citation.Title, resource.Citation.Publisher, resource.Citation.AccessedAtUtc),
                            resource.OfflineAvailable,
                            resource.License))
                        .ToArray(),
                    lesson.ProblemSets
                        .Select(problemSet => new StudentLessonProblemSetView(
                            problemSet.ProblemSetId,
                            problemSet.Title,
                            problemSet.Instructions,
                            problemSet.EstimatedMinutes,
                            problemSet.Problems
                                .Select(problem => new StudentLessonProblemView(
                                    problem.ProblemId,
                                    problem.Prompt,
                                    problem.ResponseType,
                                    problem.ExpectedAnswer,
                                    problem.Solution,
                                    problem.Skills,
                                    problem.Difficulty))
                                .ToArray()))
                        .ToArray(),
                    lesson.PortfolioConnections
                        .Select(connection => new StudentLessonPortfolioConnectionView(
                            connection.PortfolioSection,
                            connection.ArtifactTitle,
                            connection.ArtifactPurpose,
                            connection.CrossCourseLinks,
                            connection.ReuseInstructions))
                        .ToArray(),
                    lesson.Rubric is null
                        ? null
                        : new StudentLessonRubricView(
                            lesson.Rubric.RubricId,
                            lesson.Rubric.Scale,
                            lesson.Rubric.Criteria
                                .Select(criteria => new StudentLessonRubricCriterionView(
                                    criteria.Criterion,
                                    criteria.Level4,
                                    criteria.Level3,
                                    criteria.Level2,
                                    criteria.Level1))
                                .ToArray()),
                    lesson.ReflectionPrompts,
                    lesson.LinkedAssignmentIds
                        .Select(assignmentId => module.Assignments.FirstOrDefault(assignment => assignment.Id == assignmentId)?.Title ?? "")
                        .Where(title => !string.IsNullOrWhiteSpace(title))
                        .ToArray()))
                .ToArray(),
            module.Assignments
                .OrderBy(assignment => assignment.SequenceOrder)
                .Select(assignment => new StudentAssignmentView(
                    assignment.SequenceOrder,
                    assignment.Title,
                    assignment.Type,
                    assignment.MethodProfile,
                    assignment.Instructions,
                    assignment.EstimatedEffort,
                    assignment.EstimatedMinutesMin,
                    assignment.EstimatedMinutesMax,
                    assignment.DueTimingLabel,
                    assignment.DueDate,
                    assignment.LinkedModuleObjectives,
                    assignment.LinkedLessonIds,
                    assignment.LinkedLessonIds
                        .Select(lessonId => module.Lessons.FirstOrDefault(lesson => lesson.Id == lessonId)?.Title ?? "")
                        .Where(title => !string.IsNullOrWhiteSpace(title))
                        .ToArray(),
                    assignment.RequiredOutput,
                    assignment.IsPortfolioCandidate,
                    assignment.Status,
                    assignment.AssignmentSummary,
                    assignment.StudentFacingGoal,
                    assignment.RequiredDeliverables,
                    assignment.SubmissionFormats,
                    assignment.PortfolioConnection,
                    assignment.Rubric,
                    assignment.AssessmentSkills,
                    assignment.StudentChecklist,
                    assignment.Resources,
                    assignment.AssignmentSteps,
                    assignment.RevisionPolicy,
                    assignment.CompletionCriteria,
                    assignment.ReflectionPrompts,
                    assignment.EvidenceRequirements,
                    assignment.Scoring,
                    assignment.Id,
                    AssignmentSubmissionsFor(submissions, student.Id, course.Id, module.Id, assignment.Id),
                    AssignmentFeedbackFor(assessmentRecords, student.Id, course.Id, module.Id, assignment.Id)))
                .ToArray(),
            module.AssignmentEvidencePlaceholder));
    }

    private static OperationResult RequireStudentReadAccess(UserContext user)
    {
        return user.Role is UserRole.Student or UserRole.ParentAdmin
            ? OperationResult.Success()
            : OperationResult.Failure("Sign in to view student course information.");
    }

    private static IReadOnlyList<StudentModuleLink> ModuleLinks(Course course, SchoolYear? schoolYear)
    {
        return course.Modules
            .OrderBy(module => module.SequenceOrder)
            .Select(module => new StudentModuleLink(
                module.Id,
                module.SequenceOrder,
                module.Title,
                TermName(module.TermId, schoolYear),
                module.Status))
            .ToArray();
    }

    private async Task<Student?> ResolveStudentAsync(Guid? studentId, CancellationToken cancellationToken)
    {
        return studentId.HasValue
            ? await repository.GetStudentAsync(studentId.Value, cancellationToken)
            : await repository.GetStudentAsync(cancellationToken);
    }

    private static IReadOnlyList<string> TermNames(SchoolYear? schoolYear)
    {
        return schoolYear?.Terms
            .OrderBy(term => term.StartDate)
            .Select(term => term.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray() ?? [];
    }

    private static IReadOnlyList<string> CourseTermNames(Course course, SchoolYear? schoolYear)
    {
        var names = course.Modules
            .Select(module => TermName(module.TermId, schoolYear))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return names.Length == 0 && course.Duration == CourseDuration.TwoSemesters
            ? TermNames(schoolYear)
            : names;
    }

    private static string TermName(Guid? termId, SchoolYear? schoolYear)
    {
        return termId.HasValue
            ? schoolYear?.Terms.FirstOrDefault(term => term.Id == termId.Value)?.Name ?? ""
            : "";
    }

    private static IReadOnlyList<StudentAssignmentSubmissionView> AssignmentSubmissionsFor(
        IReadOnlyList<AssignmentSubmission> submissions,
        Guid studentId,
        Guid courseId,
        Guid moduleId,
        Guid assignmentId)
    {
        return submissions
            .Where(submission => submission.StudentId == studentId)
            .Where(submission => submission.CourseId == courseId)
            .Where(submission => submission.ModuleId == moduleId)
            .Where(submission => submission.AssignmentId == assignmentId)
            .OrderByDescending(submission => submission.SubmittedAtUtc)
            .Select(submission => new StudentAssignmentSubmissionView(
                submission.Id,
                submission.AttemptNumber,
                submission.Status,
                submission.SubmittedAtUtc,
                submission.ReturnedAtUtc,
                submission.AcceptedAtUtc,
                submission.ParentReviewNotes,
                submission.PortfolioCandidate,
                submission.Attachments.Count))
            .ToArray();
    }

    private static IReadOnlyList<StudentAssignmentAssessmentFeedbackView> AssignmentFeedbackFor(
        IReadOnlyList<AssessmentRecord> assessmentRecords,
        Guid studentId,
        Guid courseId,
        Guid moduleId,
        Guid assignmentId)
    {
        return assessmentRecords
            .Where(record => !record.IsArchived)
            .Where(record => record.FeedbackVisibleToStudent)
            .Where(record => record.StudentId == studentId)
            .Where(record => record.CourseId == courseId)
            .Where(record => record.ModuleId == moduleId)
            .Where(record => record.AssignmentId == assignmentId)
            .OrderByDescending(record => record.UpdatedAtUtc)
            .Select(record => new StudentAssignmentAssessmentFeedbackView(
                record.Id,
                record.State,
                record.ResultType,
                StudentAssessmentLabel(record),
                record.StudentFeedback,
                record.UpdatedAtUtc))
            .ToArray();
    }

    private static string StudentAssessmentLabel(AssessmentRecord record)
    {
        return record.ResultType switch
        {
            AssessmentResultType.Points when record.PointsEarned.HasValue && record.PointsPossible.HasValue => $"{record.PointsEarned:g} / {record.PointsPossible:g}",
            AssessmentResultType.Percentage when record.Percentage.HasValue => $"{record.Percentage:g}%",
            AssessmentResultType.LetterGrade => record.ResultValue,
            AssessmentResultType.PassFail => record.ResultValue,
            AssessmentResultType.TestScore => !string.IsNullOrWhiteSpace(record.ResultValue) ? record.ResultValue : "Test score recorded",
            AssessmentResultType.Narrative => "Narrative evaluation",
            AssessmentResultType.RubricSummary => "Rubric summary",
            _ => record.State switch
            {
                AssessmentState.Assessed => "Assessed",
                AssessmentState.ReturnedForRevision => "Returned for revision",
                AssessmentState.Incomplete => "Incomplete",
                AssessmentState.Excused => "Excused",
                AssessmentState.NotApplicable => "Not applicable",
                _ => "Feedback recorded"
            }
        };
    }

    private static IEnumerable<StudentResourceView> ParseResources(string value)
    {
        return SplitLines(value)
            .Select(line =>
            {
                var parts = line.Split('|', 2, StringSplitOptions.TrimEntries);
                return parts.Length == 2
                    ? new StudentResourceView(parts[0], parts[1])
                    : new StudentResourceView(line, "");
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Name));
    }

    private static IEnumerable<string> SplitLines(string value)
    {
        return value
            .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !string.IsNullOrWhiteSpace(line));
    }
}
