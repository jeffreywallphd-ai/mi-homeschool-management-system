using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using HomeschoolManager.Application.Access;
using HomeschoolManager.Application.Common;
using HomeschoolManager.Application.Persistence;
using HomeschoolManager.Domain.Access;
using HomeschoolManager.Domain.Common;
using HomeschoolManager.Domain.Curriculum;
using HomeschoolManager.Domain.Household;
using HomeschoolManager.Domain.Records;
using HomeschoolManager.Domain.Students;

namespace HomeschoolManager.Application.Records;

public sealed class TranscriptService
{
    private const string ContentTypeZip = "application/zip";
    private const string ContentTypePdf = "application/pdf";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly IHomeschoolRepository repository;

    public TranscriptService(IHomeschoolRepository repository)
    {
        this.repository = repository;
    }

    public async Task<OperationResult<TranscriptPreview>> GetTranscriptAsync(
        UserContext user,
        Guid? studentId = null,
        TranscriptSpan span = TranscriptSpan.HighSchool,
        CancellationToken cancellationToken = default)
    {
        var authorized = RequireReadAccess(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<TranscriptPreview>.Failure(authorized.Errors.ToArray());
        }

        var context = await LoadContextAsync(studentId, span, cancellationToken);
        if (!context.Succeeded)
        {
            return OperationResult<TranscriptPreview>.Failure(context.Errors.ToArray());
        }

        return OperationResult<TranscriptPreview>.Success(BuildPreview(context.Value!));
    }

    public async Task<OperationResult> SaveCourseRecordAsync(
        UserContext user,
        SaveTranscriptCourseRecordCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        var student = await repository.GetStudentAsync(command.StudentId, cancellationToken);
        if (student is null)
        {
            return OperationResult.Failure("Student was not found.");
        }

        var course = await repository.GetCourseAsync(command.CourseId, cancellationToken);
        if (course is null || course.StudentId != student.Id)
        {
            return OperationResult.Failure("Course was not found for this student.");
        }

        var existing = await repository.GetTranscriptCourseRecordAsync(course.Id, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        try
        {
            var record = new TranscriptCourseRecord(
                existing?.Id ?? Guid.NewGuid(),
                student.Id,
                course.Id,
                command.FinalGrade,
                command.EarnedCreditValue,
                command.CompletionDate,
                command.CreditBasis,
                command.ParentNotes,
                command.IncludeInTranscript,
                user.DisplayName,
                existing?.RecordedAtUtc ?? now,
                now);

            await repository.SaveTranscriptCourseRecordAsync(record, cancellationToken);
            return OperationResult.Success();
        }
        catch (DomainException ex)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult<TranscriptExportDownloadFile>> CreateTranscriptPacketAsync(
        UserContext user,
        CreateTranscriptExportCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<TranscriptExportDownloadFile>.Failure(authorized.Errors.ToArray());
        }

        var context = await LoadContextAsync(command.StudentId, command.Span, cancellationToken);
        if (!context.Succeeded)
        {
            return OperationResult<TranscriptExportDownloadFile>.Failure(context.Errors.ToArray());
        }

        var preview = BuildPreview(context.Value!);
        var manifest = BuildManifest(preview);
        var html = BuildTranscriptHtml(preview);
        var markdown = BuildManifestMarkdown(manifest);

        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteZipText(archive, "transcript.html", html);
            WriteZipJson(archive, "manifest.json", manifest);
            WriteZipText(archive, "manifest.md", markdown);
        }

        return OperationResult<TranscriptExportDownloadFile>.Success(new TranscriptExportDownloadFile(
            $"{SafeFileName(preview.StudentName)}-{SafeFileName(preview.SpanLabel)}-transcript.zip",
            ContentTypeZip,
            stream.ToArray(),
            manifest));
    }

    public async Task<OperationResult<TranscriptExportDownloadFile>> CreateTranscriptPdfPacketAsync(
        UserContext user,
        CreateTranscriptExportCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<TranscriptExportDownloadFile>.Failure(authorized.Errors.ToArray());
        }

        var context = await LoadContextAsync(command.StudentId, command.Span, cancellationToken);
        if (!context.Succeeded)
        {
            return OperationResult<TranscriptExportDownloadFile>.Failure(context.Errors.ToArray());
        }

        var preview = BuildPreview(context.Value!);
        var manifest = BuildManifest(preview);
        var pdf = TranscriptPdfRenderer.Create(preview, manifest);

        return OperationResult<TranscriptExportDownloadFile>.Success(new TranscriptExportDownloadFile(
            $"{SafeFileName(preview.StudentName)}-{SafeFileName(preview.SpanLabel)}-transcript-packet.pdf",
            ContentTypePdf,
            pdf,
            manifest));
    }

    private async Task<OperationResult<TranscriptContext>> LoadContextAsync(
        Guid? studentId,
        TranscriptSpan span,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(span))
        {
            return OperationResult<TranscriptContext>.Failure("Transcript span is not recognized.");
        }

        var students = await repository.GetStudentsAsync(cancellationToken);
        var student = studentId.HasValue
            ? students.FirstOrDefault(candidate => candidate.Id == studentId.Value)
            : students.FirstOrDefault();
        if (student is null)
        {
            return OperationResult<TranscriptContext>.Failure("Student was not found.");
        }

        var profile = await repository.GetSchoolProfileAsync(cancellationToken);
        var schoolYears = (await repository.GetSchoolYearsAsync(cancellationToken))
            .Where(year => year.StudentId == student.Id)
            .ToArray();
        var courses = (await repository.GetCoursesAsync(cancellationToken))
            .Where(course => course.StudentId == student.Id)
            .ToArray();
        var records = (await repository.GetTranscriptCourseRecordsAsync(cancellationToken))
            .Where(record => record.StudentId == student.Id)
            .ToArray();

        return OperationResult<TranscriptContext>.Success(new TranscriptContext(
            student,
            students,
            profile,
            schoolYears,
            courses,
            records,
            span));
    }

    private static TranscriptPreview BuildPreview(TranscriptContext context)
    {
        var warnings = new List<string>();
        var years = YearSections(context, warnings).ToArray();
        var includedCourses = years.SelectMany(year => year.Courses).ToArray();
        var descriptions = DescriptionViews(context, includedCourses.Select(course => course.CourseId).ToHashSet()).ToArray();
        var summary = new TranscriptSummary(
            includedCourses.Length,
            includedCourses.Count(course => course.CompletionStatus == CompletionStatus.Completed),
            includedCourses.Count(IsInProgress),
            includedCourses.Sum(course => course.PlannedCreditValue),
            includedCourses.Sum(course => course.EarnedCreditValue ?? 0),
            "Not calculated",
            "Earned credits come only from parent-recorded transcript course records.");

        if (includedCourses.Length == 0)
        {
            warnings.Add($"No {RequestedSpanLabel(context.Span).ToLowerInvariant()} courses are recorded for this student yet.");
        }

        var gradeCoverage = CalculateGradeCoverage(years);
        var spanLabel = SpanLabel(context.Span, gradeCoverage);
        var coverageNote = CoverageNote(context.Span, gradeCoverage);
        return new TranscriptPreview(
            context.Student.Id,
            StudentName(context.Student),
            context.Student.GradeLevel,
            context.Students.Select(student => new TranscriptStudentOption(student.Id, StudentName(student), student.GradeLevel)).ToArray(),
            context.Span,
            spanLabel,
            gradeCoverage.Label,
            coverageNote,
            context.SchoolProfile?.SchoolName ?? "Family Homeschool",
            context.SchoolProfile?.AdministratorParentName ?? "",
            context.SchoolProfile?.Jurisdiction ?? "",
            DateTimeOffset.UtcNow,
            summary,
            years,
            descriptions,
            warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private static IEnumerable<TranscriptYearSection> YearSections(TranscriptContext context, List<string> warnings)
    {
        var latestYear = context.SchoolYears
            .OrderByDescending(year => year.EndYear)
            .ThenByDescending(year => year.StartYear)
            .FirstOrDefault();
        var recordsByCourse = context.CourseRecords
            .GroupBy(record => record.CourseId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(record => record.UpdatedAtUtc).First());
        var yearById = context.SchoolYears.ToDictionary(year => year.Id);
        var fallbackYear = latestYear ?? context.SchoolYears.FirstOrDefault();

        var rows = new List<(SchoolYear? Year, int? Grade, TranscriptCourseRow Row)>();
        foreach (var course in context.Courses.OrderBy(course => course.Title, StringComparer.OrdinalIgnoreCase))
        {
            recordsByCourse.TryGetValue(course.Id, out var record);
            if (record?.IncludeInTranscript == false || course.CompletionStatus == CompletionStatus.Skipped)
            {
                continue;
            }

            yearById.TryGetValue(course.SchoolYearId, out var year);
            year ??= fallbackYear;
            var grade = EstimatedGradeForYear(context.Student, latestYear, year);
            if (!IncludedInSpan(context.Span, grade))
            {
                continue;
            }

            if (year is null)
            {
                warnings.Add($"School year was not found for \"{course.Title}\".");
            }

            if (course.CompletionStatus == CompletionStatus.Completed && record is null)
            {
                warnings.Add($"\"{course.Title}\" is completed but has no parent-recorded final transcript line yet.");
            }

            rows.Add((year, grade, ToCourseRow(course, record, year)));
        }

        return rows
            .GroupBy(item => item.Year?.Id ?? Guid.Empty)
            .OrderBy(group => group.Min(item => item.Year?.StartYear ?? int.MaxValue))
            .ThenBy(group => group.Key)
            .Select(group =>
            {
                var year = group.First().Year;
                var courses = group
                    .OrderBy(item => item.Row.CourseTitle, StringComparer.OrdinalIgnoreCase)
                    .Select(item => item.Row)
                    .ToArray();
                var grade = group.Select(item => item.Grade).FirstOrDefault(item => item.HasValue);
                return new TranscriptYearSection(
                    year?.Id,
                    year?.Name ?? "School year not recorded",
                    grade,
                    grade.HasValue ? $"Grade {grade.Value}" : "Grade not recorded",
                    year?.StartYear ?? 0,
                    year?.EndYear ?? 0,
                    courses,
                    courses.Sum(course => course.PlannedCreditValue),
                    courses.Sum(course => course.EarnedCreditValue ?? 0));
            });
    }

    private static TranscriptCourseRow ToCourseRow(Course course, TranscriptCourseRecord? record, SchoolYear? year)
    {
            var termNames = course.Modules
            .Select(module => year?.Terms.FirstOrDefault(term => term.Id == module.TermId)?.Name ?? "")
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new TranscriptCourseRow(
            course.Id,
            record?.Id,
            course.Title,
            course.SubjectArea,
            termNames.Length == 0 ? DurationLabel(course.Duration) : string.Join(", ", termNames),
            course.Duration,
            course.PlannedCreditValue,
            record?.EarnedCreditValue,
            string.IsNullOrWhiteSpace(record?.FinalGrade) ? "Not recorded" : record.FinalGrade,
            StatusLabel(course, record),
            course.CompletionStatus,
            record?.CompletionDate,
            record?.IncludeInTranscript ?? true,
            record?.CreditBasis ?? "",
            record?.ParentNotes ?? "",
            SourceReferences(course, record));
    }

    private static IEnumerable<TranscriptCourseDescriptionView> DescriptionViews(
        TranscriptContext context,
        HashSet<Guid> includedCourseIds)
    {
        var yearById = context.SchoolYears.ToDictionary(year => year.Id);
        return context.Courses
            .Where(course => includedCourseIds.Contains(course.Id))
            .OrderBy(course => course.Title, StringComparer.OrdinalIgnoreCase)
            .Select(course => new TranscriptCourseDescriptionView(
                course.Id,
                course.Title,
                course.SubjectArea,
                course.Description.Description,
                course.Description.InstructionalMethods,
                course.Description.MajorTopics,
                course.Description.TextsAndResources,
                course.Description.AssessmentMethods,
                course.Description.GradingBasis,
                course.Modules
                    .OrderBy(module => module.SequenceOrder)
                    .Select(module => new TranscriptModuleDescriptionView(
                        module.Id,
                        module.SequenceOrder,
                        module.Title,
                        module.Description,
                        TermLabelForModule(course, module, yearById),
                        module.LearningObjectiveItems.Select(objective => objective.Text).ToArray(),
                        module.ResourceItems.Select(resource => ResourceLabel(resource.Name, resource.Link, resource.FilePath)).ToArray(),
                        module.Assignments
                            .OrderBy(assignment => assignment.SequenceOrder)
                            .Select(assignment => new TranscriptAssignmentDescriptionView(
                                assignment.Id,
                                assignment.SequenceOrder,
                                assignment.Title,
                                assignment.AssignmentSummary,
                                assignment.StudentFacingGoal,
                                assignment.IsPortfolioCandidate))
                            .ToArray()))
                    .ToArray()));
    }

    private static string TermLabelForModule(
        Course course,
        LearningModule module,
        IReadOnlyDictionary<Guid, SchoolYear> yearById)
    {
        return yearById.TryGetValue(course.SchoolYearId, out var year)
            ? year.Terms.FirstOrDefault(term => term.Id == module.TermId)?.Name ?? ""
            : "";
    }

    private static TranscriptArchiveManifest BuildManifest(TranscriptPreview preview)
    {
        var courseSources = preview.Years
            .SelectMany(year => year.Courses.Select(course => new TranscriptArchiveCourseSource(
                course.CourseId,
                course.TranscriptCourseRecordId,
                course.CourseTitle,
                year.SchoolYearName,
                course.FinalGrade,
                course.EarnedCreditValue,
                course.IncludeInTranscript)))
            .ToArray();

        return new TranscriptArchiveManifest(
            "homeschool-manager.transcript-export",
            1,
            preview.GeneratedAtUtc,
            preview.StudentId,
            preview.StudentName,
            preview.Span,
            preview.SpanLabel,
            preview.SchoolName,
            preview.AdministratorParentName,
            preview.CoverageNote,
            preview.Summary.CourseCount,
            preview.Summary.PlannedCredits,
            preview.Summary.EarnedCredits,
            courseSources,
            preview.Warnings);
    }

    private static string BuildTranscriptHtml(TranscriptPreview preview)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\">");
        builder.AppendLine($"<title>{Html(preview.StudentName)} - {Html(preview.SpanLabel)}</title>");
        builder.AppendLine("<style>");
        builder.AppendLine("body{font-family:Georgia,'Times New Roman',serif;color:#111827;margin:0;background:#f3f4f6;}main{max-width:960px;margin:0 auto;background:#fff;min-height:100vh;padding:42px;}h1{text-align:center;letter-spacing:.04em;text-transform:uppercase;font-size:26px;margin:0 0 8px;}h2{font-size:18px;border-bottom:2px solid #111827;padding-bottom:5px;margin:28px 0 10px;}h3{font-size:15px;margin:20px 0 6px;}p{line-height:1.45}.muted,small{color:#4b5563}.identity{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:8px;margin:22px 0}.identity div,.summary div{border:1px solid #9ca3af;padding:8px}.summary{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:8px;margin:12px 0 22px}table{width:100%;border-collapse:collapse;margin-bottom:18px}th,td{border:1px solid #9ca3af;padding:7px;text-align:left;vertical-align:top}th{background:#f3f4f6;text-transform:uppercase;font-size:11px;letter-spacing:.04em}.number{text-align:right;white-space:nowrap}.note{border:1px solid #9ca3af;padding:10px;background:#fafafa}.signature{display:grid;grid-template-columns:1fr 12rem;gap:24px;margin-top:32px}.signature div{border-top:1px solid #111827;padding-top:6px}.appendix{break-before:page}.course-description{break-inside:avoid;border-top:1px solid #d1d5db;padding-top:12px;margin-top:12px}@media print{body{background:#fff}main{padding:0;max-width:none}.appendix{break-before:page}}</style>");
        builder.AppendLine("</head><body><main>");
        builder.AppendLine($"<h1>{Html(preview.SpanLabel)}</h1>");
        builder.AppendLine("<p style=\"text-align:center\" class=\"muted\">Family-issued homeschool academic record</p>");
        if (!string.IsNullOrWhiteSpace(preview.CoverageNote))
        {
            builder.AppendLine($"<p class=\"note\"><strong>Transcript coverage:</strong> {Html(preview.CoverageNote)}</p>");
        }

        builder.AppendLine("<section class=\"identity\">");
        AppendMeta(builder, "Student", preview.StudentName);
        AppendMeta(builder, "School", preview.SchoolName);
        AppendMeta(builder, "Grade span", preview.TypicalGradeRange);
        AppendMeta(builder, "Jurisdiction", preview.Jurisdiction);
        AppendMeta(builder, "Administrator", preview.AdministratorParentName);
        AppendMeta(builder, "Generated", preview.GeneratedAtUtc.ToLocalTime().ToString("d"));
        builder.AppendLine("</section>");

        builder.AppendLine("<section class=\"summary\">");
        AppendMeta(builder, "Courses", preview.Summary.CourseCount.ToString());
        AppendMeta(builder, "Completed", preview.Summary.CompletedCourseCount.ToString());
        AppendMeta(builder, "Earned credits", Credit(preview.Summary.EarnedCredits));
        AppendMeta(builder, "GPA", preview.Summary.GpaDisplay);
        builder.AppendLine("</section>");

        foreach (var year in preview.Years)
        {
            builder.AppendLine($"<h2>{Html(year.SchoolYearName)} - {Html(year.GradeLabel)}</h2>");
            builder.AppendLine("<table><thead><tr><th>Course</th><th>Subject</th><th>Term</th><th>Planned Credit</th><th>Earned Credit</th><th>Final Grade</th><th>Status</th></tr></thead><tbody>");
            foreach (var course in year.Courses)
            {
                builder.AppendLine("<tr>");
                builder.AppendLine($"<td>{Html(course.CourseTitle)}</td>");
                builder.AppendLine($"<td>{Html(course.SubjectArea)}</td>");
                builder.AppendLine($"<td>{Html(course.TermLabel)}</td>");
                builder.AppendLine($"<td class=\"number\">{Credit(course.PlannedCreditValue)}</td>");
                builder.AppendLine($"<td class=\"number\">{(course.EarnedCreditValue.HasValue ? Credit(course.EarnedCreditValue.Value) : "Not recorded")}</td>");
                builder.AppendLine($"<td>{Html(course.FinalGrade)}</td>");
                builder.AppendLine($"<td>{Html(course.Status)}</td>");
                builder.AppendLine("</tr>");
            }

            builder.AppendLine("</tbody></table>");
        }

        if (preview.Warnings.Count > 0)
        {
            builder.AppendLine("<section class=\"note\"><strong>Record notes</strong><ul>");
            foreach (var warning in preview.Warnings)
            {
                builder.AppendLine($"<li>{Html(warning)}</li>");
            }

            builder.AppendLine("</ul></section>");
        }

        builder.AppendLine("<section class=\"signature\"><div>Parent/Admin Signature</div><div>Date</div></section>");
        builder.AppendLine("<section class=\"appendix\"><h2>Course Descriptions</h2>");
        foreach (var course in preview.CourseDescriptions)
        {
            builder.AppendLine("<article class=\"course-description\">");
            builder.AppendLine($"<h3>{Html(course.CourseTitle)}</h3>");
            AppendParagraph(builder, "Description", course.Description);
            AppendParagraph(builder, "Major topics", course.MajorTopics);
            AppendParagraph(builder, "Texts and resources", course.TextsAndResources);
            AppendParagraph(builder, "Assessment methods", course.AssessmentMethods);
            if (course.Modules.Count > 0)
            {
                builder.AppendLine("<p><strong>Modules and assignments:</strong></p><ul>");
                foreach (var module in course.Modules)
                {
                    builder.AppendLine($"<li>{Html(module.SequenceOrder.ToString())}. {Html(module.Title)}");
                    if (module.Assignments.Count > 0)
                    {
                        builder.AppendLine("<ul>");
                        foreach (var assignment in module.Assignments)
                        {
                            builder.AppendLine($"<li>{Html(assignment.Title)}</li>");
                        }

                        builder.AppendLine("</ul>");
                    }

                    builder.AppendLine("</li>");
                }

                builder.AppendLine("</ul>");
            }

            builder.AppendLine("</article>");
        }

        builder.AppendLine("</section>");
        builder.AppendLine("</main></body></html>");
        return builder.ToString();
    }

    private static string BuildManifestMarkdown(TranscriptArchiveManifest manifest)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# {manifest.SpanLabel}");
        builder.AppendLine();
        builder.AppendLine($"- Student: {manifest.StudentName}");
        builder.AppendLine($"- School: {manifest.SchoolName}");
        builder.AppendLine($"- Administrator: {manifest.AdministratorParentName}");
        builder.AppendLine($"- Generated: {manifest.GeneratedAtUtc.ToLocalTime():g}");
        builder.AppendLine($"- Coverage: {manifest.CoverageNote}");
        builder.AppendLine($"- Courses: {manifest.CourseCount}");
        builder.AppendLine($"- Planned credits: {Credit(manifest.PlannedCredits)}");
        builder.AppendLine($"- Earned credits: {Credit(manifest.EarnedCredits)}");
        builder.AppendLine();
        builder.AppendLine("This packet is a family-issued homeschool transcript archive generated from parent-owned source records.");
        builder.AppendLine();
        builder.AppendLine("## Source Courses");
        foreach (var course in manifest.CourseSources)
        {
            builder.AppendLine($"- {course.CourseTitle}: {course.SchoolYearName}; final grade {course.FinalGrade}; earned credit {(course.EarnedCreditValue.HasValue ? Credit(course.EarnedCreditValue.Value) : "not recorded")}");
        }

        if (manifest.Warnings.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Record Notes");
            foreach (var warning in manifest.Warnings)
            {
                builder.AppendLine($"- {warning}");
            }
        }

        return builder.ToString();
    }

    private static OperationResult RequireReadAccess(UserContext user)
    {
        return user.Role is UserRole.ParentAdmin or UserRole.Student
            ? OperationResult.Success()
            : OperationResult.Failure("Sign in to view transcript information.");
    }

    private static bool IncludedInSpan(TranscriptSpan span, int? grade)
    {
        if (span == TranscriptSpan.AllCourses)
        {
            return true;
        }

        if (!grade.HasValue)
        {
            return true;
        }

        return span switch
        {
            TranscriptSpan.MiddleSchool => grade.Value is >= 6 and <= 8,
            TranscriptSpan.HighSchool => grade.Value is >= 9 and <= 12,
            _ => true
        };
    }

    private static int? EstimatedGradeForYear(Student student, SchoolYear? latestYear, SchoolYear? year)
    {
        if (year is null)
        {
            return null;
        }

        if (latestYear is null)
        {
            return student.GradeLevel;
        }

        var offset = latestYear.EndYear - year.EndYear;
        return Math.Clamp(student.GradeLevel - offset, 0, 12);
    }

    private static bool IsInProgress(TranscriptCourseRow course)
    {
        return course.CompletionStatus is CompletionStatus.InProgress or CompletionStatus.NeedsReview or CompletionStatus.NotStarted;
    }

    private static string StatusLabel(Course course, TranscriptCourseRecord? record)
    {
        return course.CompletionStatus switch
        {
            CompletionStatus.Completed when record?.EarnedCreditValue is not null || !string.IsNullOrWhiteSpace(record?.FinalGrade) => "Completed",
            CompletionStatus.Completed => "Completed, final record needed",
            CompletionStatus.NeedsReview => "Needs parent review",
            CompletionStatus.InProgress => "In progress",
            CompletionStatus.NotStarted => "Planned",
            CompletionStatus.Skipped => "Not included",
            _ => "Recorded"
        };
    }

    private static IReadOnlyList<string> SourceReferences(Course course, TranscriptCourseRecord? record)
    {
        var references = new List<string> { $"Course:{course.Id}" };
        if (record is not null)
        {
            references.Add($"TranscriptCourseRecord:{record.Id}");
        }

        if (!string.IsNullOrWhiteSpace(course.SourcePackId))
        {
            references.Add($"SourcePack:{course.SourcePackId}");
        }

        return references;
    }

    private static string DurationLabel(CourseDuration duration)
    {
        return duration switch
        {
            CourseDuration.OneSemester => "One semester",
            CourseDuration.TwoSemesters => "Full year",
            _ => "Course"
        };
    }

    private static string RequestedSpanLabel(TranscriptSpan span)
    {
        return span switch
        {
            TranscriptSpan.MiddleSchool => "Middle School Transcript",
            TranscriptSpan.AllCourses => "Academic Transcript",
            _ => "High School Transcript"
        };
    }

    private static string SpanLabel(TranscriptSpan span, GradeCoverageInfo coverage)
    {
        if (coverage.Grades.Count == 0)
        {
            return RequestedSpanLabel(span);
        }

        if (span == TranscriptSpan.AllCourses)
        {
            return $"{coverage.Label} Academic Transcript";
        }

        var conventionalGrades = span == TranscriptSpan.MiddleSchool
            ? Enumerable.Range(6, 3).ToArray()
            : Enumerable.Range(9, 4).ToArray();
        var coversConventionalSpan = conventionalGrades.All(coverage.Grades.Contains);
        if (coversConventionalSpan)
        {
            return RequestedSpanLabel(span);
        }

        return $"{coverage.Label} Homeschool Transcript";
    }

    private static GradeCoverageInfo CalculateGradeCoverage(IReadOnlyList<TranscriptYearSection> years)
    {
        var grades = years
            .Select(year => year.EstimatedGradeLevel)
            .Where(grade => grade.HasValue)
            .Select(grade => grade!.Value)
            .Distinct()
            .Order()
            .ToArray();
        return new GradeCoverageInfo(grades, GradeCoverageLabel(grades));
    }

    private static string GradeCoverageLabel(IReadOnlyList<int> grades)
    {
        if (grades.Count == 0)
        {
            return "Recorded courses only";
        }

        var ranges = new List<string>();
        var start = grades[0];
        var end = grades[0];
        for (var index = 1; index < grades.Count; index++)
        {
            if (grades[index] == end + 1)
            {
                end = grades[index];
                continue;
            }

            ranges.Add(GradeRangeLabel(start, end));
            start = grades[index];
            end = grades[index];
        }

        ranges.Add(GradeRangeLabel(start, end));
        return string.Join(", ", ranges);
    }

    private static string GradeRangeLabel(int start, int end)
    {
        return start == end ? $"Grade {start}" : $"Grades {start}-{end}";
    }

    private static string CoverageNote(TranscriptSpan span, GradeCoverageInfo coverage)
    {
        if (coverage.Grades.Count == 0)
        {
            return "This transcript includes only course records available in this system.";
        }

        if (span == TranscriptSpan.AllCourses)
        {
            return $"This transcript includes {coverage.Label.ToLowerInvariant()} course records available in this system.";
        }

        var conventionalGrades = span == TranscriptSpan.MiddleSchool
            ? Enumerable.Range(6, 3).ToArray()
            : Enumerable.Range(9, 4).ToArray();
        var missing = conventionalGrades.Except(coverage.Grades).ToArray();
        if (missing.Length == 0)
        {
            return $"This transcript includes {coverage.Label.ToLowerInvariant()} course records available in this system.";
        }

        return $"This transcript includes only {coverage.Label.ToLowerInvariant()} course records available in this system. Other transcripts may be available for {GradeCoverageLabel(missing).ToLowerInvariant()} not included here.";
    }

    private static string ResourceLabel(string name, string link, string filePath)
    {
        if (!string.IsNullOrWhiteSpace(link))
        {
            return $"{name} ({link})";
        }

        if (!string.IsNullOrWhiteSpace(filePath))
        {
            return $"{name} ({filePath})";
        }

        return name;
    }

    private static void AppendMeta(StringBuilder builder, string label, string value)
    {
        builder.AppendLine($"<div><small>{Html(label)}</small><br>{Html(value)}</div>");
    }

    private static void AppendParagraph(StringBuilder builder, string label, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            builder.AppendLine($"<p><strong>{Html(label)}:</strong> {Html(value)}</p>");
        }
    }

    private static void WriteZipJson<T>(ZipArchive archive, string path, T value)
    {
        var entry = archive.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(JsonSerializer.Serialize(value, JsonOptions));
    }

    private static void WriteZipText(ZipArchive archive, string path, string value)
    {
        var entry = archive.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(value);
    }

    private static string Credit(decimal value)
    {
        return value.ToString("0.0##");
    }

    private static string SafeFileName(string value)
    {
        var safe = new string((value ?? "")
            .Trim()
            .Select(character => char.IsAsciiLetterOrDigit(character) ? char.ToLowerInvariant(character) : '-')
            .ToArray());
        while (safe.Contains("--", StringComparison.Ordinal))
        {
            safe = safe.Replace("--", "-", StringComparison.Ordinal);
        }

        return string.IsNullOrWhiteSpace(safe.Trim('-')) ? "transcript" : safe.Trim('-');
    }

    private static string Html(string value)
    {
        return WebUtility.HtmlEncode(value ?? "");
    }

    private static string Blank(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Not recorded" : value;
    }

    private static string StudentName(Student student)
    {
        return $"{student.FirstName} {student.LastName}".Trim();
    }

    private sealed record TranscriptContext(
        Student Student,
        IReadOnlyList<Student> Students,
        SchoolProfile? SchoolProfile,
        IReadOnlyList<SchoolYear> SchoolYears,
        IReadOnlyList<Course> Courses,
        IReadOnlyList<TranscriptCourseRecord> CourseRecords,
        TranscriptSpan Span);

    private sealed record GradeCoverageInfo(IReadOnlyList<int> Grades, string Label);
}
