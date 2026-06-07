using HomeschoolManager.Application.Access;
using HomeschoolManager.Application.Common;
using HomeschoolManager.Application.Persistence;
using HomeschoolManager.Application.Requirements;
using HomeschoolManager.Domain.Access;
using HomeschoolManager.Domain.Common;
using HomeschoolManager.Domain.Curriculum;
using HomeschoolManager.Domain.LegalRequirements;
using HomeschoolManager.Domain.Students;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace HomeschoolManager.Application.Courses;

public sealed class CourseService
{
    private readonly IHomeschoolRepository repository;
    private const string DefaultPublisherId = "homeschool-manager";
    private const string DefaultPackVersion = "2026.1";
    private static readonly JsonSerializerOptions CoursePackJsonOptions = CreateCoursePackJsonOptions();
    private static readonly LessonPackEnvelope LessonPackTemplate = new(
        "homeschool-manager.lessonpack",
        1,
        DateTimeOffset.UnixEpoch,
        "json",
        "Future lesson packs with attached files should use a zip archive containing this JSON plus files.",
        "Lesson Pack Template",
        "Replace this sample lesson with one or more lessons to add to a learning module.",
        [
            new LessonPackLesson(
                "sample-lesson-1",
                1,
                "Sample Lesson Title",
                "Introduce the lesson topic, explain why it matters, and name what the student should pay attention to while using the resources.",
                "Optional module objective text this lesson supports.",
                LessonType.SelfGuided,
                180,
                3,
                LessonDifficultyLevel.AdvancedHighSchool,
                ["Mathematics", "Agriculture", "Ecology"],
                ["algebra", "rates", "portfolio"],
                ["Algebra II", "Basic spreadsheet use"],
                [new LessonLearningObjective("sample-objective-1", "Use equations, units, rates, ratios, and proportional reasoning to model a practical situation.", BloomLevel.Apply)],
                [new StandardsAlignment("Parent-defined", "MATH-APP-01", "Applies mathematical modeling to real-world scenarios.")],
                ["I can define variables with units.", "I can explain whether my answer is reasonable."],
                [
                    new LessonStep(1, "Read and take notes", LessonStepType.Reading, "Read the assigned resource and take notes on the key variables.", 30, true),
                    new LessonStep(2, "Complete practice", LessonStepType.ProblemSet, "Complete the practice problems and show units in every calculation.", 60, true),
                    new LessonStep(3, "Create evidence", LessonStepType.PortfolioArtifact, "Create a small artifact that shows what you learned.", 90, true)
                ],
                [
                    new LessonPackResource(
                        "Sample article or video",
                        LessonResourceType.Article,
                        "https://example.com/resource",
                        "",
                        false,
                        "Brief note about why this resource belongs in the lesson.",
                        true,
                        25,
                        "Use this resource to identify three concrete facts or examples.",
                        "What measurable variables appear in this resource?",
                        new LessonResourceCitation("Sample Resource", "Example Publisher", null),
                        false,
                        "Check source terms before reuse.")
                ],
                [
                    new LessonProblemSet(
                        "sample-problem-set-1",
                        "Sample Problem Set",
                        "Show all work and include units.",
                        45,
                        [
                            new LessonProblem(
                                "sample-problem-1",
                                "Write a practice problem prompt here.",
                                ProblemResponseType.WorkedSolution,
                                "Expected answer for parent review.",
                                "Optional solution for parent review.",
                                ["modeling", "unit reasoning"],
                                "Medium")
                        ])
                ],
                [
                    new LessonPortfolioConnection(
                        "Portfolio Section",
                        "Sample Artifact",
                        "Explains how the lesson artifact may support the larger portfolio.",
                        ["Related course or project"],
                        "Revise this artifact later with stronger evidence.")
                ],
                new LessonRubric(
                    "sample-rubric",
                    "4-point",
                    [
                        new LessonRubricCriterion(
                            "Evidence quality",
                            "Evidence is accurate, complete, and clearly explained.",
                            "Evidence is mostly accurate and explained.",
                            "Evidence is incomplete or weakly explained.",
                            "Evidence is missing or unclear.")
                    ]),
                ["What assumption mattered most?", "What would improve this work?"],
                new LessonInstructorNotes(
                    "Parent-facing overview of how to guide and evaluate this lesson.",
                    ["Student explains reasoning.", "Student uses evidence from resources."],
                    ["Work lacks units or clear evidence."],
                    ["Ask the student to explain the model or artifact aloud."]),
                ["sample-assignment-1"],
                ["Sample Assignment Title"])
        ],
        TemplateIdentity("lessonpack-template"),
        true);
    private static readonly AssignmentPackEnvelope AssignmentPackTemplate = new(
        "homeschool-manager.assignmentpack",
        1,
        DateTimeOffset.UnixEpoch,
        "json",
        "Future assignment packs with attached files should use a zip archive containing this JSON plus files.",
        "Assignment Pack Template",
        "Replace this sample assignment with one or more assignments to add to a learning module.",
        [
            new AssignmentPackAssignment(
                "sample-assignment-1",
                1,
                "Sample Assignment Title",
                AssignmentType.PortfolioArtifact,
                InstructionalMethodProfile.Hybrid,
                "Describe what the student should do, which resources to use, and how the finished work should be submitted or saved.",
                "60-90 minutes",
                "After completing the related lesson.",
                null,
                ["Optional module objective text this assignment supports."],
                ["sample-lesson-1"],
                ["Sample Lesson Title"],
                "Completed work, notes, project artifact, response, or other evidence the parent can review.",
                "Optional parent notes about adaptation, feedback, or portfolio value.",
                true,
                100m,
                null,
                AssignmentStatus.Planned,
                "Create a short portfolio-ready artifact that shows learning from the related lesson.",
                "Show what you learned by creating evidence that a parent can review and save.",
                60,
                90,
                ["Complete response or artifact", "Evidence from the related lesson", "Brief reflection"],
                [AssignmentSubmissionFormat.PortfolioEntry, AssignmentSubmissionFormat.Reflection],
                new AssignmentPortfolioConnection(
                    true,
                    "Course Portfolio",
                    "Sample Assignment Artifact",
                    "Demonstrates learning from the related module objective.",
                    "Revise after parent feedback before saving in the final portfolio.",
                    ["Related course"]),
                new LessonRubric(
                    "sample-assignment-rubric",
                    "4-point",
                    [
                        new LessonRubricCriterion(
                            "Evidence quality",
                            "Evidence is complete, accurate, and clearly explained.",
                            "Evidence is mostly complete and understandable.",
                            "Evidence is partial or needs more explanation.",
                            "Evidence is missing or unclear.")
                    ]),
                "",
                ["lesson evidence", "clear explanation"],
                ["I included all required parts.", "I checked my work before submitting."],
                [
                    new AssignmentResource(
                        "Related lesson resources",
                        LessonResourceType.Website,
                        "",
                        "",
                        false,
                        true,
                        "Use the resources linked in the related lesson.",
                        "Placeholder resource reference.")
                ],
                [
                    new AssignmentStep(1, "Review lesson material", "Review the related lesson resources and notes.", 20),
                    new AssignmentStep(2, "Create evidence", "Complete the assignment artifact or response.", 45),
                    new AssignmentStep(3, "Reflect and submit", "Check the required parts and write a short reflection.", 15)
                ],
                new AssignmentRevisionPolicy(true, "Revise after parent feedback if the work is portfolio-bound.", 1),
                new AssignmentCompletionCriteria(["All required deliverables are included.", "Work can be understood without verbal explanation."], true, 3),
                ["What did this assignment help you understand?", "What would you improve before saving it?"],
                new AssignmentEvidenceRequirements(true, AssignmentEvidenceType.PortfolioArtifact, ["pdf", "docx", "png"], true, true),
                new AssignmentScoring(100m, null, AssignmentGradingMode.Rubric, true, true))
        ],
        TemplateIdentity("assignmentpack-template"),
        true);
    private static readonly ModulePackEnvelope ModulePackTemplate = new(
        "homeschool-manager.modulepack",
        1,
        DateTimeOffset.UnixEpoch,
        "json",
        "Future module packs with attached files should use a zip archive containing this JSON plus files.",
        "Module Pack Template",
        "Replace this sample module with one module shell. Import lesson and assignment details separately with lessonpack and assignmentpack files.",
        new ModulePackModule(
            "sample-module-1",
            1,
            "Sample Module Title",
            "Brief module description.",
            "",
            "2 weeks",
            "Describe how the student should work through this module.",
            [
                new ModulePackObjective("Sample module objective.", "Optional linked course objective.")
            ],
            [
                new ModulePackResource("Sample module-level resource", "https://example.com", "", false)
            ],
            "Describe assignment or evidence expectations at the module level.",
            ModuleStatus.Planned,
            [
                new ModulePackItemReference("sample-lesson-1", "Sample Lesson Title", 1)
            ],
            [
                new ModulePackItemReference("sample-assignment-1", "Sample Assignment Title", 1)
            ]),
        TemplateIdentity("modulepack-template"),
        true);
    private static readonly SingleCoursePackEnvelope CoursePackTemplate = new(
        "homeschool-manager.coursepack",
        2,
        DateTimeOffset.UnixEpoch,
        "json",
        "Single-course course packs do not include module, lesson, or assignment bodies. Use a course plan bundle when moving a complete plan.",
        new SingleCoursePackCourse(
            "sample-course-1",
            "Sample Course Title",
            ["Sample subject area"],
            CourseDuration.TwoSemesters,
            1.0m,
            new CoursePackDescription(
                "Describe the course purpose, scope, and student expectations.",
                "Describe instructional methods.",
                "",
                "List course-level texts and resources here.",
                "Describe assessment methods.",
                "Describe grading basis."),
            new CoursePackCurriculumPlan(
                "Describe broad course goals.",
                "Write one learning objective per line.",
                "",
                "Describe the planned course sequence.",
                "Add parent planning notes."),
            [
                new SingleCourseRequirementMapping("Statutory", "Sample Requirement Area", CoverageLevel.Primary, "Optional mapping note.")
            ],
            [
                new CourseModuleReference("sample-module-1", "Sample Module Title", 1, "Semester 1")
            ]),
        TemplateIdentity("coursepack-template"),
        true);

    public event Action? CourseNavigationChanged;

    public CourseService(IHomeschoolRepository repository)
    {
        this.repository = repository;
    }

    public async Task<IReadOnlyList<CourseListItem>> ListCoursesAsync(
        Guid? studentId = null,
        CancellationToken cancellationToken = default)
    {
        await RefreshMichiganRequirementSeedAsync(cancellationToken);
        await BackfillImportedCoursePackDetailsAsync(cancellationToken);

        var courses = await repository.GetCoursesAsync(cancellationToken);
        return courses
            .Where(course => studentId is null || course.StudentId == studentId.Value)
            .Where(course => !course.IsArchived)
            .OrderBy(course => course.Title)
            .Select(course => new CourseListItem(
                course.Id,
                course.StudentId,
                course.Title,
                course.Description.Description,
                course.SubjectAreas,
                course.Duration,
                course.PlannedCreditValue,
                course.RequirementMappings.Count))
            .ToArray();
    }

    public async Task<CourseDetail?> GetCourseDetailAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        await RefreshMichiganRequirementSeedAsync(cancellationToken);
        await BackfillImportedCoursePackDetailsAsync(cancellationToken);

        var course = await repository.GetCourseAsync(courseId, cancellationToken);
        if (course is null)
        {
            return null;
        }

        var areas = await repository.GetRequirementAreasAsync(cancellationToken);
        var schoolYear = await repository.GetSchoolYearAsync(cancellationToken);
        var student = await repository.GetStudentAsync(course.StudentId, cancellationToken);
        var availablePacks = await GetAvailableCoursePacksAsync(cancellationToken);
        return ToDetail(course, areas, schoolYear, availablePacks, student);
    }

    public async Task<OperationResult<Guid>> CreateCourseAsync(
        UserContext user,
        CreateCourseCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<Guid>.Failure(authorized.Errors.ToArray());
        }

        var studentResult = await ResolveStudentAsync(command.StudentId, cancellationToken);
        if (!studentResult.Succeeded || studentResult.Value is null)
        {
            return OperationResult<Guid>.Failure(studentResult.Errors.ToArray());
        }
        var student = studentResult.Value;

        var schoolYear = await repository.GetSchoolYearAsync(cancellationToken);
        if (schoolYear is null)
        {
            return OperationResult<Guid>.Failure("Create a school year before adding courses.");
        }

        try
        {
            var course = new Course(
                Guid.NewGuid(),
                student.Id,
                schoolYear.Id,
                command.Title,
                NormalizeSubjectAreas(command.SubjectAreas),
                command.Duration,
                command.PlannedCreditValue,
                null,
                null,
                new CourseDescription(command.Description, "", "", "", "", ""),
                null,
                [],
                []);

            await repository.SaveCourseAsync(course, cancellationToken);
            return OperationResult<Guid>.Success(course.Id);
        }
        catch (DomainException ex)
        {
            return OperationResult<Guid>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> UpdateCourseAsync(
        UserContext user,
        UpdateCourseCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        var existing = await repository.GetCourseAsync(command.CourseId, cancellationToken);
        if (existing is null)
        {
            return OperationResult.Failure("Course was not found.");
        }

        try
        {
            var updated = new Course(
                existing.Id,
                existing.StudentId,
                existing.SchoolYearId,
                command.Title,
                command.SubjectAreas.Count == 0 ? existing.SubjectAreas : NormalizeSubjectAreas(command.SubjectAreas),
                command.Duration,
                command.PlannedCreditValue,
                existing.SourcePackId,
                existing.SourceTemplateId,
                existing.Description,
                existing.CurriculumPlan,
                existing.RequirementMappings,
                existing.Modules);

            await repository.SaveCourseAsync(updated, cancellationToken);
            CourseNavigationChanged?.Invoke();
            return OperationResult.Success();
        }
        catch (DomainException ex)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult<CourseListActionResult>> DeleteCoursesAsync(
        UserContext user,
        CourseListActionCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<CourseListActionResult>.Failure(authorized.Errors.ToArray());
        }

        var courses = await ResolveCourseListActionTargetsAsync(command, cancellationToken);
        var failures = new List<CourseListActionFailure>();
        var successCount = 0;
        foreach (var course in courses)
        {
            if (HasStudentWork(course))
            {
                failures.Add(new CourseListActionFailure(
                    course.Id,
                    course.Title,
                    "This course has student work attached. Archive the course instead so the work and course record are kept together."));
                continue;
            }

            await repository.DeleteCourseAsync(course.Id, cancellationToken);
            successCount++;
        }

        return OperationResult<CourseListActionResult>.Success(new CourseListActionResult(successCount, failures));
    }

    public async Task<OperationResult<CourseListActionResult>> ArchiveCoursesAsync(
        UserContext user,
        CourseListActionCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<CourseListActionResult>.Failure(authorized.Errors.ToArray());
        }

        var courses = await ResolveCourseListActionTargetsAsync(command, cancellationToken);
        var failures = new List<CourseListActionFailure>();
        var successCount = 0;
        var archivedAtUtc = DateTimeOffset.UtcNow;
        foreach (var course in courses)
        {
            try
            {
                await repository.SaveCourseAsync(course.Archive(archivedAtUtc), cancellationToken);
                successCount++;
            }
            catch (DomainException ex)
            {
                failures.Add(new CourseListActionFailure(course.Id, course.Title, ex.Message));
            }
        }

        return OperationResult<CourseListActionResult>.Success(new CourseListActionResult(successCount, failures));
    }

    public async Task<IReadOnlyList<CoursePackSummary>> ListCoursePacksAsync(CancellationToken cancellationToken = default)
    {
        var packs = await GetAvailableCoursePacksAsync(cancellationToken);
        return packs
            .Select(ToCoursePackSummary)
            .ToArray();
    }

    public async Task<CoursePackDetail?> GetCoursePackDetailAsync(string packId, CancellationToken cancellationToken = default)
    {
        var pack = (await GetAvailableCoursePacksAsync(cancellationToken))
            .FirstOrDefault(item => string.Equals(item.Id, packId, StringComparison.OrdinalIgnoreCase));
        if (pack is null)
        {
            return null;
        }

        return new CoursePackDetail(
            pack.Id,
            pack.Name,
            pack.Description,
            pack.Courses.Select(course => new CoursePackCourseView(
                course.TemplateId,
                course.Title,
                course.SubjectAreas,
                course.Duration,
                course.PlannedCreditValue,
                course.Description.Description,
                course.DefaultOptionId,
                course.Options.Select(option => new CoursePackOptionView(
                    option.OptionId,
                    option.Title,
                    option.SubjectAreas,
                    option.Duration,
                    option.PlannedCreditValue,
                    option.Description.Description)).ToArray())).ToArray());
    }

    public async Task<OperationResult<CoursePackDownloadFile>> DownloadCoursePackAsync(string packId, CancellationToken cancellationToken = default)
    {
        var pack = (await GetAvailableCoursePacksAsync(cancellationToken))
            .FirstOrDefault(item => string.Equals(item.Id, packId, StringComparison.OrdinalIgnoreCase));
        if (pack is null)
        {
            return OperationResult<CoursePackDownloadFile>.Failure("Course pack was not found.");
        }

        var download = new CoursePackJsonEnvelope(
            "homeschool-manager.coursepack",
            1,
            DateTimeOffset.UtcNow,
            "json",
            "Future downloads with attached lesson or assignment files should use a zip archive containing this JSON plus files.",
            pack);
        var json = JsonSerializer.Serialize(download, CoursePackJsonOptions);
        var fileName = $"{SafeFileName(pack.Id)}.coursepack";
        return OperationResult<CoursePackDownloadFile>.Success(new CoursePackDownloadFile(
            fileName,
            "application/json",
            Encoding.UTF8.GetBytes(json),
            false));
    }

    public OperationResult<CoursePackDownloadFile> DownloadCoursePackTemplate()
    {
        var template = CoursePackTemplate with { DownloadedAtUtc = DateTimeOffset.UtcNow };
        return OperationResult<CoursePackDownloadFile>.Success(new CoursePackDownloadFile(
            "coursepack-template.coursepack",
            "application/json",
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(template, CoursePackJsonOptions)),
            false));
    }

    public async Task<OperationResult<CoursePackDownloadFile>> DownloadCoursePlanBundleAsync(
        string packId,
        CancellationToken cancellationToken = default)
    {
        var pack = (await GetAvailableCoursePacksAsync(cancellationToken))
            .FirstOrDefault(item => string.Equals(item.Id, packId, StringComparison.OrdinalIgnoreCase));
        if (pack is null)
        {
            return OperationResult<CoursePackDownloadFile>.Failure("Course plan was not found.");
        }

        var schoolYear = await repository.GetSchoolYearAsync(cancellationToken);
        var identity = BuiltInPackIdentity(pack);
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var plan = new CoursePlanPackEnvelope(
                "homeschool-manager.courseplanpack",
                1,
                DateTimeOffset.UtcNow,
                "json",
                "This manifest lists which single-course course packs belong in the course plan bundle.",
                pack.Id,
                pack.Name,
                pack.Description,
                CoursePlanPacingLabel(schoolYear),
                pack.Courses
                    .OrderBy(course => course.TemplateId, StringComparer.OrdinalIgnoreCase)
                    .Select((course, index) =>
                    {
                        var option = course.DefaultOption;
                        var termName = option.Modules
                            .Select(module => ResolveTermName(module.TermNumber, schoolYear))
                            .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)) ?? "";
                        return new CoursePlanOffering(course.TemplateId, option.Title, termName, index + 1);
                    })
                    .ToArray(),
                identity,
                false);
            WriteZipJson(archive, "courseplan.courseplanpack", plan);

            foreach (var template in pack.Courses)
            {
                var option = template.DefaultOption;
                var courseFolder = $"courses/{SafeFileName(option.Title)}";
                WriteZipJson(
                    archive,
                    $"{courseFolder}/course.coursepack",
                    BuildSingleCoursePackEnvelope(option, template.TemplateId, schoolYear, DateTimeOffset.UtcNow, identity));

                foreach (var module in option.Modules.OrderBy(module => module.SequenceOrder))
                {
                    var moduleFolder = $"{courseFolder}/modules/{module.SequenceOrder:00}-{SafeFileName(module.Title)}";
                    WriteZipJson(
                        archive,
                        $"{moduleFolder}/module.modulepack",
                        BuildModulePackEnvelope(option.Title, module, schoolYear, DateTimeOffset.UtcNow, identity));
                    WriteZipJson(
                        archive,
                        $"{moduleFolder}/lessons.lessonpack",
                        BuildLessonPackEnvelope(option.Title, module, DateTimeOffset.UtcNow, identity));
                    WriteZipJson(
                        archive,
                        $"{moduleFolder}/assignments.assignmentpack",
                        BuildAssignmentPackEnvelope(option.Title, module, DateTimeOffset.UtcNow, identity));
                }
            }
        }

        return OperationResult<CoursePackDownloadFile>.Success(new CoursePackDownloadFile(
            $"{SafeFileName(pack.Id)}.zip",
            "application/zip",
            stream.ToArray(),
            true));
    }

    public async Task<OperationResult<CourseImportResult>> ImportCoursePackAsync(
        UserContext user,
        Guid? studentId,
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<CourseImportResult>.Failure(authorized.Errors.ToArray());
        }

        var parsed = ParseSingleCoursePackFile(content);
        if (!parsed.Succeeded || parsed.Value is null)
        {
            return OperationResult<CourseImportResult>.Failure(parsed.Errors.ToArray());
        }

        return await ImportSingleCoursePackEnvelopeAsync(parsed.Value, studentId, cancellationToken);
    }

    public async Task<OperationResult<CoursePlanBundleImportResult>> ImportCoursePlanBundleAsync(
        UserContext user,
        Guid? studentId,
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<CoursePlanBundleImportResult>.Failure(authorized.Errors.ToArray());
        }

        if (content.Length == 0)
        {
            return OperationResult<CoursePlanBundleImportResult>.Failure("Choose a course plan bundle before importing.");
        }

        var totals = new CoursePlanBundleMutableImportResult();
        try
        {
            using var stream = new MemoryStream(content);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            var planId = ReadCoursePlanId(archive) ?? "imported-courseplan";
            var courseEntries = archive.Entries
                .Where(entry => entry.FullName.EndsWith(".coursepack", StringComparison.OrdinalIgnoreCase))
                .OrderBy(entry => entry.FullName, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (courseEntries.Length == 0)
            {
                return OperationResult<CoursePlanBundleImportResult>.Failure("The course plan bundle did not contain any course files.");
            }

            foreach (var courseEntry in courseEntries)
            {
                var coursePack = ParseSingleCoursePackFile(ReadZipEntry(courseEntry));
                if (!coursePack.Succeeded || coursePack.Value is null)
                {
                    return OperationResult<CoursePlanBundleImportResult>.Failure(coursePack.Errors.ToArray());
                }

                var import = await ImportSingleCoursePackEnvelopeAsync(
                    coursePack.Value,
                    studentId,
                    cancellationToken,
                    planId,
                    updateExisting: true);
                if (!import.Succeeded || import.Value is null)
                {
                    return OperationResult<CoursePlanBundleImportResult>.Failure(import.Errors.ToArray());
                }

                totals.CourseCount++;
                var courseFolder = ParentFolder(courseEntry.FullName);
                var course = await repository.GetCourseAsync(import.Value.CourseId, cancellationToken);
                if (course is null)
                {
                    continue;
                }

                var moduleEntries = archive.Entries
                    .Where(entry =>
                        entry.FullName.StartsWith($"{courseFolder}/modules/", StringComparison.OrdinalIgnoreCase) &&
                        entry.FullName.EndsWith(".modulepack", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(entry => entry.FullName, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                foreach (var moduleEntry in moduleEntries)
                {
                    var moduleImport = await UpsertModulePackIntoCourseAsync(import.Value.CourseId, ReadZipEntry(moduleEntry), cancellationToken);
                    if (!moduleImport.Succeeded || moduleImport.Value is null)
                    {
                        return OperationResult<CoursePlanBundleImportResult>.Failure(moduleImport.Errors.ToArray());
                    }

                    totals.ModuleCount++;
                    var moduleFolder = ParentFolder(moduleEntry.FullName);
                    var lessonEntry = archive.Entries.FirstOrDefault(entry =>
                        entry.FullName.StartsWith($"{moduleFolder}/", StringComparison.OrdinalIgnoreCase) &&
                        entry.FullName.EndsWith(".lessonpack", StringComparison.OrdinalIgnoreCase));
                    if (lessonEntry is not null)
                    {
                        var lessonImport = await ImportMissingLessonPackAsync(import.Value.CourseId, moduleImport.Value.ModuleId, ReadZipEntry(lessonEntry), cancellationToken);
                        if (!lessonImport.Succeeded || lessonImport.Value is null)
                        {
                            return OperationResult<CoursePlanBundleImportResult>.Failure(lessonImport.Errors.ToArray());
                        }

                        totals.LessonCount += lessonImport.Value.LessonCount;
                    }

                    var assignmentEntry = archive.Entries.FirstOrDefault(entry =>
                        entry.FullName.StartsWith($"{moduleFolder}/", StringComparison.OrdinalIgnoreCase) &&
                        entry.FullName.EndsWith(".assignmentpack", StringComparison.OrdinalIgnoreCase));
                    if (assignmentEntry is not null)
                    {
                        var assignmentImport = await ImportMissingAssignmentPackAsync(import.Value.CourseId, moduleImport.Value.ModuleId, ReadZipEntry(assignmentEntry), cancellationToken);
                        if (!assignmentImport.Succeeded || assignmentImport.Value is null)
                        {
                            return OperationResult<CoursePlanBundleImportResult>.Failure(assignmentImport.Errors.ToArray());
                        }

                        totals.AssignmentCount += assignmentImport.Value.AssignmentCount;
                    }
                }
            }
        }
        catch (InvalidDataException)
        {
            return OperationResult<CoursePlanBundleImportResult>.Failure("The selected file is not a readable course plan bundle.");
        }

        return OperationResult<CoursePlanBundleImportResult>.Success(new CoursePlanBundleImportResult(
            totals.CourseCount,
            totals.ModuleCount,
            totals.LessonCount,
            totals.AssignmentCount));
    }

    public async Task<OperationResult<LessonPackDownloadFile>> DownloadModuleLessonPackAsync(
        Guid courseId,
        Guid moduleId,
        CancellationToken cancellationToken = default)
    {
        await BackfillImportedCoursePackDetailsAsync(cancellationToken);
        var course = await repository.GetCourseAsync(courseId, cancellationToken);
        var module = course?.Modules.FirstOrDefault(item => item.Id == moduleId);
        if (course is null || module is null)
        {
            return OperationResult<LessonPackDownloadFile>.Failure("Learning module was not found.");
        }

        var envelope = new LessonPackEnvelope(
            "homeschool-manager.lessonpack",
            1,
            DateTimeOffset.UtcNow,
            "json",
            "Future lesson packs with attached files should use a zip archive containing this JSON plus files.",
            $"{course.Title} - {module.Title} Lessons",
            $"Lessons downloaded from the {module.Title} module.",
            module.Lessons
                .OrderBy(lesson => lesson.SequenceOrder)
                .Select(lesson => ToLessonPackLesson(lesson, module.Assignments))
                .ToArray(),
            SourceIdentityForCourse(course),
            false);

        var json = JsonSerializer.Serialize(envelope, CoursePackJsonOptions);
        var fileName = $"{SafeFileName(course.Title)}-{SafeFileName(module.Title)}.lessonpack";
        return OperationResult<LessonPackDownloadFile>.Success(new LessonPackDownloadFile(
            fileName,
            "application/json",
            Encoding.UTF8.GetBytes(json)));
    }

    public OperationResult<LessonPackDownloadFile> DownloadLessonPackTemplate()
    {
        var template = LessonPackTemplate with { DownloadedAtUtc = DateTimeOffset.UtcNow };
        return OperationResult<LessonPackDownloadFile>.Success(new LessonPackDownloadFile(
            "lessonpack-template.lessonpack",
            "application/json",
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(template, CoursePackJsonOptions))));
    }

    public async Task<OperationResult<AssignmentPackDownloadFile>> DownloadModuleAssignmentPackAsync(
        Guid courseId,
        Guid moduleId,
        CancellationToken cancellationToken = default)
    {
        await BackfillImportedCoursePackDetailsAsync(cancellationToken);
        var course = await repository.GetCourseAsync(courseId, cancellationToken);
        var module = course?.Modules.FirstOrDefault(item => item.Id == moduleId);
        if (course is null || module is null)
        {
            return OperationResult<AssignmentPackDownloadFile>.Failure("Learning module was not found.");
        }

        var envelope = new AssignmentPackEnvelope(
            "homeschool-manager.assignmentpack",
            1,
            DateTimeOffset.UtcNow,
            "json",
            "Future assignment packs with attached files should use a zip archive containing this JSON plus files.",
            $"{course.Title} - {module.Title} Assignments",
            $"Assignments downloaded from the {module.Title} module.",
            module.Assignments
                .OrderBy(assignment => assignment.SequenceOrder)
                .Select(assignment => ToAssignmentPackAssignment(assignment, module.Lessons))
                .ToArray(),
            SourceIdentityForCourse(course),
            false);

        var json = JsonSerializer.Serialize(envelope, CoursePackJsonOptions);
        var fileName = $"{SafeFileName(course.Title)}-{SafeFileName(module.Title)}.assignmentpack";
        return OperationResult<AssignmentPackDownloadFile>.Success(new AssignmentPackDownloadFile(
            fileName,
            "application/json",
            Encoding.UTF8.GetBytes(json)));
    }

    public OperationResult<AssignmentPackDownloadFile> DownloadAssignmentPackTemplate()
    {
        var template = AssignmentPackTemplate with { DownloadedAtUtc = DateTimeOffset.UtcNow };
        return OperationResult<AssignmentPackDownloadFile>.Success(new AssignmentPackDownloadFile(
            "assignmentpack-template.assignmentpack",
            "application/json",
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(template, CoursePackJsonOptions))));
    }

    public async Task<OperationResult<ModulePackDownloadFile>> DownloadModulePackAsync(
        Guid courseId,
        Guid moduleId,
        CancellationToken cancellationToken = default)
    {
        await BackfillImportedCoursePackDetailsAsync(cancellationToken);
        var course = await repository.GetCourseAsync(courseId, cancellationToken);
        var module = course?.Modules.FirstOrDefault(item => item.Id == moduleId);
        if (course is null || module is null)
        {
            return OperationResult<ModulePackDownloadFile>.Failure("Learning module was not found.");
        }

        var schoolYear = await repository.GetSchoolYearAsync(cancellationToken);
        var termName = schoolYear?.Terms.FirstOrDefault(term => term.Id == module.TermId)?.Name ?? "";
        var envelope = new ModulePackEnvelope(
            "homeschool-manager.modulepack",
            1,
            DateTimeOffset.UtcNow,
            "json",
            "Future module packs with attached files should use a zip archive containing this JSON plus files.",
            $"{course.Title} - {module.Title} Module",
            $"Module shell downloaded from the {course.Title} course. Lesson and assignment details are not included.",
            ToModulePackModule(module, termName),
            SourceIdentityForCourse(course),
            false);

        var json = JsonSerializer.Serialize(envelope, CoursePackJsonOptions);
        var fileName = $"{SafeFileName(course.Title)}-{SafeFileName(module.Title)}.modulepack";
        return OperationResult<ModulePackDownloadFile>.Success(new ModulePackDownloadFile(
            fileName,
            "application/json",
            Encoding.UTF8.GetBytes(json)));
    }

    public OperationResult<ModulePackDownloadFile> DownloadModulePackTemplate()
    {
        var template = ModulePackTemplate with { DownloadedAtUtc = DateTimeOffset.UtcNow };
        return OperationResult<ModulePackDownloadFile>.Success(new ModulePackDownloadFile(
            "modulepack-template.modulepack",
            "application/json",
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(template, CoursePackJsonOptions))));
    }

    private static CoursePackDownloadFile DownloadCoursePackArchivePlaceholder(CoursePackDefinition pack)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry($"{SafeFileName(pack.Id)}.coursepack");
            using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
            writer.Write(JsonSerializer.Serialize(new CoursePackJsonEnvelope(
                "homeschool-manager.coursepack",
                1,
                DateTimeOffset.UtcNow,
                "zip",
                "Archive download is reserved for course packs with attached lesson or assignment files.",
                pack), CoursePackJsonOptions));
        }

        return new CoursePackDownloadFile($"{SafeFileName(pack.Id)}.coursepack.zip", "application/zip", stream.ToArray(), true);
    }

    private async Task<IReadOnlyList<CoursePackDefinition>> GetAvailableCoursePacksAsync(CancellationToken cancellationToken)
    {
        var installed = await repository.GetInstalledCoursePacksAsync(cancellationToken);
        var builtInIds = DefaultCoursePacks.All
            .Select(pack => pack.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return DefaultCoursePacks.All
            .Concat(installed.Where(pack => !builtInIds.Contains(pack.Id)))
            .OrderBy(pack => pack.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static CoursePackSummary ToCoursePackSummary(CoursePackDefinition pack)
    {
        return new CoursePackSummary(
            pack.Id,
            pack.Name,
            pack.Description,
            pack.Courses.Count,
            pack.Courses.Sum(course => course.DefaultOption.PlannedCreditValue));
    }

    public async Task<OperationResult<CoursePackSummary>> InstallCoursePackFileAsync(
        UserContext user,
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<CoursePackSummary>.Failure(authorized.Errors.ToArray());
        }

        var parseResult = ParseCoursePackFile(content);
        if (!parseResult.Succeeded || parseResult.Value is null)
        {
            return OperationResult<CoursePackSummary>.Failure(parseResult.Errors.ToArray());
        }

        if (DefaultCoursePacks.All.Any(pack => string.Equals(pack.Id, parseResult.Value.Id, StringComparison.OrdinalIgnoreCase)))
        {
            return OperationResult<CoursePackSummary>.Failure("That course pack is already built into the system.");
        }

        await repository.SaveInstalledCoursePackAsync(parseResult.Value, cancellationToken);
        return OperationResult<CoursePackSummary>.Success(ToCoursePackSummary(parseResult.Value));
    }

    public async Task<OperationResult<int>> ImportCoursePackAsync(
        UserContext user,
        ImportCoursePackCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<int>.Failure(authorized.Errors.ToArray());
        }

        var pack = (await GetAvailableCoursePacksAsync(cancellationToken))
            .FirstOrDefault(item => string.Equals(item.Id, command.PackId, StringComparison.OrdinalIgnoreCase));
        if (pack is null)
        {
            return OperationResult<int>.Failure("Course pack was not found.");
        }

        return await ImportCoursePackDefinitionAsync(pack, command.StudentId, command.TemplateIds, command.Selections, cancellationToken);
    }

    private async Task<OperationResult<int>> ImportCoursePackDefinitionAsync(
        CoursePackDefinition pack,
        Guid? studentId,
        IReadOnlyList<string> templateIds,
        IReadOnlyList<CoursePackSelectionCommand> selections,
        CancellationToken cancellationToken)
    {
        var studentResult = await ResolveStudentAsync(studentId, cancellationToken);
        if (!studentResult.Succeeded || studentResult.Value is null)
        {
            return OperationResult<int>.Failure(studentResult.Errors.ToArray());
        }
        var student = studentResult.Value;

        var schoolYear = await repository.GetSchoolYearAsync(cancellationToken);
        if (schoolYear is null)
        {
            return OperationResult<int>.Failure("Create a school year before importing a course pack.");
        }

        var courses = await repository.GetCoursesAsync(cancellationToken);
        var existingTemplateIds = courses
            .Where(course => course.StudentId == student.Id &&
                !course.IsArchived &&
                string.Equals(course.SourcePackId, pack.Id, StringComparison.OrdinalIgnoreCase))
            .Select(course => course.SourceTemplateId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        await RefreshRequirementSeedForPackAsync(pack, cancellationToken);

        var areas = await repository.GetRequirementAreasAsync(cancellationToken);
        if (areas.Count == 0)
        {
            return OperationResult<int>.Failure("Seed Michigan requirement areas before importing this course pack.");
        }

        var importedCount = 0;
        var selectionByTemplateId = selections
            .Where(selection => !string.IsNullOrWhiteSpace(selection.TemplateId))
            .GroupBy(selection => selection.TemplateId.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Last().OptionId.Trim(),
                StringComparer.OrdinalIgnoreCase);

        var selectedTemplateIds = templateIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var templateId in selectionByTemplateId.Keys)
        {
            selectedTemplateIds.Add(templateId);
        }

        var selectedTemplates = selectedTemplateIds.Count == 0
            ? pack.Courses
            : pack.Courses.Where(course => selectedTemplateIds.Contains(course.TemplateId)).ToArray();

        if (selectedTemplates.Count == 0)
        {
            return OperationResult<int>.Failure("Select at least one course to import.");
        }

        var unknownTemplateIds = selectedTemplateIds
            .Except(pack.Courses.Select(course => course.TemplateId), StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (unknownTemplateIds.Length > 0)
        {
            return OperationResult<int>.Failure("One or more selected courses were not found in the course pack.");
        }

        foreach (var template in selectedTemplates.Where(course => !existingTemplateIds.Contains(course.TemplateId)))
        {
            var option = ResolveSelectedOption(template, selectionByTemplateId);
            if (option is null)
            {
                return OperationResult<int>.Failure("One or more selected course options were not found in the course pack.");
            }

            try
            {
                var courseId = Guid.NewGuid();
                var mappings = BuildMappings(courseId, option, areas);
                var modules = BuildModules(courseId, option, schoolYear);
                var course = new Course(
                    courseId,
                    student.Id,
                    schoolYear.Id,
                    option.Title,
                    option.SubjectAreas,
                    option.Duration,
                    option.PlannedCreditValue,
                    pack.Id,
                    template.TemplateId,
                    option.Description,
                    option.CurriculumPlan,
                    mappings,
                    modules);

                await repository.SaveCourseAsync(course, cancellationToken);
                importedCount++;
            }
            catch (DomainException ex)
            {
                return OperationResult<int>.Failure(ex.Message);
            }
        }

        await BackfillImportedCoursePackDetailsAsync(cancellationToken);
        return OperationResult<int>.Success(importedCount);
    }

    public async Task<OperationResult> SaveCourseDescriptionAsync(
        UserContext user,
        SaveCourseDescriptionCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        var course = await repository.GetCourseAsync(command.CourseId, cancellationToken);
        if (course is null)
        {
            return OperationResult.Failure("Course was not found.");
        }

        var description = new CourseDescription(
            command.Description,
            command.InstructionalMethods,
            command.MajorTopics,
            command.TextsAndResources,
            command.AssessmentMethods,
            command.GradingBasis);

        await repository.SaveCourseAsync(course.WithDescription(description), cancellationToken);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SaveCurriculumPlanAsync(
        UserContext user,
        SaveCurriculumPlanCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        var course = await repository.GetCourseAsync(command.CourseId, cancellationToken);
        if (course is null)
        {
            return OperationResult.Failure("Course was not found.");
        }

        var plan = new CurriculumPlan(
            command.Goals,
            command.LearningObjectives,
            command.MajorResources,
            command.PlannedSequence,
            command.ParentNotes);

        await repository.SaveCourseAsync(course.WithCurriculumPlan(plan), cancellationToken);
        return OperationResult.Success();
    }

    public async Task<IReadOnlyList<LearningModuleView>> ListModulesAsync(
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        await BackfillImportedCoursePackDetailsAsync(cancellationToken);
        var course = await repository.GetCourseAsync(courseId, cancellationToken);
        var schoolYear = await repository.GetSchoolYearAsync(cancellationToken);
        var availablePacks = await GetAvailableCoursePacksAsync(cancellationToken);
        return course?.Modules.Select(module => ToModuleView(module, schoolYear, course, availablePacks)).ToArray() ?? [];
    }

    public async Task<LearningModuleView?> GetModuleDetailAsync(
        Guid courseId,
        Guid moduleId,
        CancellationToken cancellationToken = default)
    {
        await BackfillImportedCoursePackDetailsAsync(cancellationToken);
        var course = await repository.GetCourseAsync(courseId, cancellationToken);
        var schoolYear = await repository.GetSchoolYearAsync(cancellationToken);
        var availablePacks = await GetAvailableCoursePacksAsync(cancellationToken);
        return course?.Modules
            .Where(module => module.Id == moduleId)
            .Select(module => ToModuleView(module, schoolYear, course, availablePacks))
            .FirstOrDefault();
    }

    public async Task<OperationResult<Guid>> CreateLearningModuleAsync(
        UserContext user,
        CreateLearningModuleCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<Guid>.Failure(authorized.Errors.ToArray());
        }

        var course = await repository.GetCourseAsync(command.CourseId, cancellationToken);
        if (course is null)
        {
            return OperationResult<Guid>.Failure("Course was not found.");
        }

        try
        {
            var module = new LearningModule(
                Guid.NewGuid(),
                course.Id,
                "",
                course.Modules.Count + 1,
                command.Title,
                command.Description,
                command.EstimatedLength,
                command.Instructions,
                "",
                Lines(command.LearningObjectives.Select(item => item.Text)),
                Lines(command.Resources.Select(item => item.Name)),
                command.AssignmentEvidencePlaceholder,
                command.Status,
                command.TermId,
                BuildObjectiveItems(command.LearningObjectives),
                BuildResourceItems(command.Resources));
            await repository.SaveCourseAsync(course.WithModules(course.Modules.Concat([module]).ToArray()), cancellationToken);
            CourseNavigationChanged?.Invoke();
            return OperationResult<Guid>.Success(module.Id);
        }
        catch (DomainException ex)
        {
            return OperationResult<Guid>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> UpdateLearningModuleAsync(
        UserContext user,
        UpdateLearningModuleCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        var course = await repository.GetCourseAsync(command.CourseId, cancellationToken);
        if (course is null)
        {
            return OperationResult.Failure("Course was not found.");
        }

        var existing = course.Modules.FirstOrDefault(module => module.Id == command.ModuleId);
        if (existing is null)
        {
            return OperationResult.Failure("Learning module was not found.");
        }

        try
        {
            var updatedModule = new LearningModule(
                existing.Id,
                course.Id,
                existing.SourceModuleId,
                existing.SequenceOrder,
                command.Title,
                command.Description,
                command.EstimatedLength,
                command.Instructions,
                "",
                Lines(command.LearningObjectives.Select(item => item.Text)),
                Lines(command.Resources.Select(item => item.Name)),
                command.AssignmentEvidencePlaceholder,
                command.Status,
                command.TermId,
                BuildObjectiveItems(command.LearningObjectives),
                BuildResourceItems(command.Resources),
                existing.Lessons,
                existing.Assignments);
            var modules = course.Modules
                .Select(module => module.Id == command.ModuleId ? updatedModule : module)
                .ToArray();
            await repository.SaveCourseAsync(course.WithModules(modules), cancellationToken);
            CourseNavigationChanged?.Invoke();
            return OperationResult.Success();
        }
        catch (DomainException ex)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> ReorderLearningModulesAsync(
        UserContext user,
        ReorderLearningModulesCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        var course = await repository.GetCourseAsync(command.CourseId, cancellationToken);
        if (course is null)
        {
            return OperationResult.Failure("Course was not found.");
        }

        if (command.ModuleIds.Count != course.Modules.Count ||
            command.ModuleIds.Distinct().Count() != command.ModuleIds.Count ||
            command.ModuleIds.Any(id => course.Modules.All(module => module.Id != id)))
        {
            return OperationResult.Failure("Module order must include each course module exactly once.");
        }

        var modulesById = course.Modules.ToDictionary(module => module.Id);
        var reordered = command.ModuleIds
            .Select((id, index) => modulesById[id] with { SequenceOrder = index + 1 })
            .ToArray();
        await repository.SaveCourseAsync(course.WithModules(reordered), cancellationToken);
        CourseNavigationChanged?.Invoke();
        return OperationResult.Success();
    }

    public async Task<OperationResult> DeleteLearningModuleAsync(
        UserContext user,
        DeleteLearningModuleCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        if (!string.Equals(command.ConfirmationText, "Delete", StringComparison.Ordinal))
        {
            return OperationResult.Failure("Type Delete to confirm module deletion.");
        }

        var course = await repository.GetCourseAsync(command.CourseId, cancellationToken);
        if (course is null)
        {
            return OperationResult.Failure("Course was not found.");
        }

        if (course.Modules.All(module => module.Id != command.ModuleId))
        {
            return OperationResult.Failure("Learning module was not found.");
        }

        var modules = course.Modules
            .Where(module => module.Id != command.ModuleId)
            .ToArray();
        await repository.SaveCourseAsync(course.WithModules(modules), cancellationToken);
        CourseNavigationChanged?.Invoke();
        return OperationResult.Success();
    }

    public async Task<IReadOnlyList<LessonView>> ListLessonsAsync(
        Guid courseId,
        Guid moduleId,
        CancellationToken cancellationToken = default)
    {
        await BackfillImportedCoursePackDetailsAsync(cancellationToken);
        var module = await GetModuleAsync(courseId, moduleId, cancellationToken);
        return module?.Lessons.Select(ToLessonView).ToArray() ?? [];
    }

    public async Task<LessonView?> GetLessonDetailAsync(
        Guid courseId,
        Guid moduleId,
        Guid lessonId,
        CancellationToken cancellationToken = default)
    {
        await BackfillImportedCoursePackDetailsAsync(cancellationToken);
        var module = await GetModuleAsync(courseId, moduleId, cancellationToken);
        return module?.Lessons
            .Where(lesson => lesson.Id == lessonId)
            .Select(ToLessonView)
            .FirstOrDefault();
    }

    public async Task<OperationResult<Guid>> CreateLessonAsync(
        UserContext user,
        CreateLessonCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<Guid>.Failure(authorized.Errors.ToArray());
        }

        var course = await repository.GetCourseAsync(command.CourseId, cancellationToken);
        var module = course?.Modules.FirstOrDefault(item => item.Id == command.ModuleId);
        if (course is null || module is null)
        {
            return OperationResult<Guid>.Failure("Learning module was not found.");
        }

        try
        {
            var lesson = BuildLesson(
                Guid.NewGuid(),
                module.Id,
                "",
                module.Lessons.Count + 1,
                command);
            await SaveModuleAsync(course, module.WithLessons(module.Lessons.Concat([lesson]).ToArray()), cancellationToken);
            CourseNavigationChanged?.Invoke();
            return OperationResult<Guid>.Success(lesson.Id);
        }
        catch (DomainException ex)
        {
            return OperationResult<Guid>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult<LessonPackImportResult>> ImportLessonPackAsync(
        UserContext user,
        Guid courseId,
        Guid moduleId,
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<LessonPackImportResult>.Failure(authorized.Errors.ToArray());
        }

        var parsed = ParseLessonPackFile(content);
        if (!parsed.Succeeded || parsed.Value is null)
        {
            return OperationResult<LessonPackImportResult>.Failure(parsed.Errors.ToArray());
        }

        var course = await repository.GetCourseAsync(courseId, cancellationToken);
        var module = course?.Modules.FirstOrDefault(item => item.Id == moduleId);
        if (course is null || module is null)
        {
            return OperationResult<LessonPackImportResult>.Failure("Learning module was not found.");
        }

        try
        {
            var nextOrder = module.Lessons.Count + 1;
            var existingSourceIds = module.Lessons
                .Select(lesson => lesson.SourceLessonId)
                .Where(sourceId => !string.IsNullOrWhiteSpace(sourceId))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var importedLessons = parsed.Value.Lessons
                .OrderBy(lesson => lesson.SequenceOrder <= 0 ? int.MaxValue : lesson.SequenceOrder)
                .ThenBy(lesson => lesson.Title, StringComparer.OrdinalIgnoreCase)
                .Select(lesson => BuildImportedLesson(module.Id, lesson, nextOrder++, existingSourceIds, module.Assignments))
                .ToArray();

            await SaveModuleAsync(course, module.WithLessons(module.Lessons.Concat(importedLessons).ToArray()), cancellationToken);
            CourseNavigationChanged?.Invoke();
            return OperationResult<LessonPackImportResult>.Success(new LessonPackImportResult(importedLessons.Length));
        }
        catch (DomainException ex)
        {
            return OperationResult<LessonPackImportResult>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> UpdateLessonAsync(
        UserContext user,
        UpdateLessonCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        var course = await repository.GetCourseAsync(command.CourseId, cancellationToken);
        var module = course?.Modules.FirstOrDefault(item => item.Id == command.ModuleId);
        var existing = module?.Lessons.FirstOrDefault(lesson => lesson.Id == command.LessonId);
        if (course is null || module is null || existing is null)
        {
            return OperationResult.Failure("Lesson was not found.");
        }

        try
        {
            var updatedLesson = BuildLesson(
                existing.Id,
                module.Id,
                existing.SourceLessonId,
                existing.SequenceOrder,
                command);
            var lessons = module.Lessons
                .Select(lesson => lesson.Id == command.LessonId ? updatedLesson : lesson)
                .ToArray();
            await SaveModuleAsync(course, module.WithLessons(lessons), cancellationToken);
            CourseNavigationChanged?.Invoke();
            return OperationResult.Success();
        }
        catch (DomainException ex)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> ReorderLessonsAsync(
        UserContext user,
        ReorderLessonsCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        var course = await repository.GetCourseAsync(command.CourseId, cancellationToken);
        var module = course?.Modules.FirstOrDefault(item => item.Id == command.ModuleId);
        if (course is null || module is null)
        {
            return OperationResult.Failure("Learning module was not found.");
        }

        if (command.LessonIds.Count != module.Lessons.Count ||
            command.LessonIds.Distinct().Count() != command.LessonIds.Count ||
            command.LessonIds.Any(id => module.Lessons.All(lesson => lesson.Id != id)))
        {
            return OperationResult.Failure("Lesson order must include each module lesson exactly once.");
        }

        var lessonsById = module.Lessons.ToDictionary(lesson => lesson.Id);
        var reordered = command.LessonIds
            .Select((id, index) => lessonsById[id] with { SequenceOrder = index + 1 })
            .ToArray();
        await SaveModuleAsync(course, module.WithLessons(reordered), cancellationToken);
        CourseNavigationChanged?.Invoke();
        return OperationResult.Success();
    }

    public async Task<OperationResult> DeleteLessonAsync(
        UserContext user,
        DeleteLessonCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        if (!string.Equals(command.ConfirmationText, "Delete", StringComparison.Ordinal))
        {
            return OperationResult.Failure("Type Delete to confirm lesson deletion.");
        }

        var course = await repository.GetCourseAsync(command.CourseId, cancellationToken);
        var module = course?.Modules.FirstOrDefault(item => item.Id == command.ModuleId);
        if (course is null || module is null || module.Lessons.All(lesson => lesson.Id != command.LessonId))
        {
            return OperationResult.Failure("Lesson was not found.");
        }

        var lessons = module.Lessons
            .Where(lesson => lesson.Id != command.LessonId)
            .ToArray();
        await SaveModuleAsync(course, module.WithLessons(lessons), cancellationToken);
        CourseNavigationChanged?.Invoke();
        return OperationResult.Success();
    }

    public async Task<IReadOnlyList<AssignmentView>> ListAssignmentsAsync(
        Guid courseId,
        Guid moduleId,
        CancellationToken cancellationToken = default)
    {
        await BackfillImportedCoursePackDetailsAsync(cancellationToken);
        var module = await GetModuleAsync(courseId, moduleId, cancellationToken);
        return module?.Assignments.Select(ToAssignmentView).ToArray() ?? [];
    }

    public async Task<AssignmentView?> GetAssignmentDetailAsync(
        Guid courseId,
        Guid moduleId,
        Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        await BackfillImportedCoursePackDetailsAsync(cancellationToken);
        var module = await GetModuleAsync(courseId, moduleId, cancellationToken);
        return module?.Assignments
            .Where(assignment => assignment.Id == assignmentId)
            .Select(ToAssignmentView)
            .FirstOrDefault();
    }

    public async Task<OperationResult<Guid>> CreateAssignmentAsync(
        UserContext user,
        CreateAssignmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<Guid>.Failure(authorized.Errors.ToArray());
        }

        var course = await repository.GetCourseAsync(command.CourseId, cancellationToken);
        var module = course?.Modules.FirstOrDefault(item => item.Id == command.ModuleId);
        if (course is null || module is null)
        {
            return OperationResult<Guid>.Failure("Learning module was not found.");
        }

        try
        {
            var assignment = BuildAssignment(
                module.Id,
                "",
                module.Assignments.Count + 1,
                command.Title,
                command.Type,
                command.MethodProfile,
                command.Instructions,
                command.EstimatedEffort,
                command.DueTimingLabel,
                command.DueDate,
                command.LinkedModuleObjectives,
                command.LinkedLessonIds,
                command.RequiredOutput,
                command.ParentNotes,
                command.IsPortfolioCandidate,
                command.PlannedPoints,
                command.PlannedWeight,
                command.Status,
                command.AssignmentSummary,
                command.StudentFacingGoal,
                command.EstimatedMinutesMin,
                command.EstimatedMinutesMax,
                command.RequiredDeliverables,
                command.SubmissionFormats,
                BuildAssignmentPortfolioConnection(command.PortfolioConnection),
                BuildRubric(command.Rubric),
                command.LinkedRubricId,
                command.AssessmentSkills,
                command.StudentChecklist,
                BuildAssignmentResources(command.Resources ?? []),
                BuildAssignmentSteps(command.AssignmentSteps ?? []),
                BuildRevisionPolicy(command.RevisionPolicy),
                BuildCompletionCriteria(command.CompletionCriteria),
                command.ReflectionPrompts,
                BuildEvidenceRequirements(command.EvidenceRequirements),
                BuildScoring(command.Scoring));
            await SaveModuleAsync(course, module.WithAssignments(module.Assignments.Concat([assignment]).ToArray()), cancellationToken);
            CourseNavigationChanged?.Invoke();
            return OperationResult<Guid>.Success(assignment.Id);
        }
        catch (DomainException ex)
        {
            return OperationResult<Guid>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult<AssignmentPackImportResult>> ImportAssignmentPackAsync(
        UserContext user,
        Guid courseId,
        Guid moduleId,
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<AssignmentPackImportResult>.Failure(authorized.Errors.ToArray());
        }

        var parsed = ParseAssignmentPackFile(content);
        if (!parsed.Succeeded || parsed.Value is null)
        {
            return OperationResult<AssignmentPackImportResult>.Failure(parsed.Errors.ToArray());
        }

        var course = await repository.GetCourseAsync(courseId, cancellationToken);
        var module = course?.Modules.FirstOrDefault(item => item.Id == moduleId);
        if (course is null || module is null)
        {
            return OperationResult<AssignmentPackImportResult>.Failure("Learning module was not found.");
        }

        try
        {
            var nextOrder = module.Assignments.Count + 1;
            var existingSourceIds = module.Assignments
                .Select(assignment => assignment.SourceAssignmentId)
                .Where(sourceId => !string.IsNullOrWhiteSpace(sourceId))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var importedAssignments = parsed.Value.Assignments
                .OrderBy(assignment => assignment.SequenceOrder <= 0 ? int.MaxValue : assignment.SequenceOrder)
                .ThenBy(assignment => assignment.Title, StringComparer.OrdinalIgnoreCase)
                .Select(assignment => BuildImportedAssignment(module, assignment, nextOrder++, existingSourceIds))
                .ToArray();

            await SaveModuleAsync(course, module.WithAssignments(module.Assignments.Concat(importedAssignments).ToArray()), cancellationToken);
            CourseNavigationChanged?.Invoke();
            return OperationResult<AssignmentPackImportResult>.Success(new AssignmentPackImportResult(importedAssignments.Length));
        }
        catch (DomainException ex)
        {
            return OperationResult<AssignmentPackImportResult>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult<ModulePackImportResult>> ImportModulePackAsync(
        UserContext user,
        Guid courseId,
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<ModulePackImportResult>.Failure(authorized.Errors.ToArray());
        }

        var parsed = ParseModulePackFile(content);
        if (!parsed.Succeeded || parsed.Value is null)
        {
            return OperationResult<ModulePackImportResult>.Failure(parsed.Errors.ToArray());
        }

        var course = await repository.GetCourseAsync(courseId, cancellationToken);
        if (course is null)
        {
            return OperationResult<ModulePackImportResult>.Failure("Course was not found.");
        }

        var schoolYear = await repository.GetSchoolYearAsync(cancellationToken);
        try
        {
            var module = BuildImportedModule(course.Id, parsed.Value.Module, course.Modules.Count + 1, schoolYear);
            await repository.SaveCourseAsync(course.WithModules(course.Modules.Concat([module]).ToArray()), cancellationToken);
            CourseNavigationChanged?.Invoke();
            return OperationResult<ModulePackImportResult>.Success(new ModulePackImportResult(module.Id, module.Title));
        }
        catch (DomainException ex)
        {
            return OperationResult<ModulePackImportResult>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> UpdateAssignmentAsync(
        UserContext user,
        UpdateAssignmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        var course = await repository.GetCourseAsync(command.CourseId, cancellationToken);
        var module = course?.Modules.FirstOrDefault(item => item.Id == command.ModuleId);
        var existing = module?.Assignments.FirstOrDefault(assignment => assignment.Id == command.AssignmentId);
        if (course is null || module is null || existing is null)
        {
            return OperationResult.Failure("Assignment was not found.");
        }

        try
        {
            var updatedAssignment = BuildAssignment(
                module.Id,
                existing.SourceAssignmentId,
                existing.SequenceOrder,
                command.Title,
                command.Type,
                command.MethodProfile,
                command.Instructions,
                command.EstimatedEffort,
                command.DueTimingLabel,
                command.DueDate,
                command.LinkedModuleObjectives,
                command.LinkedLessonIds,
                command.RequiredOutput,
                command.ParentNotes,
                command.IsPortfolioCandidate,
                command.PlannedPoints,
                command.PlannedWeight,
                command.Status,
                command.AssignmentSummary,
                command.StudentFacingGoal,
                command.EstimatedMinutesMin,
                command.EstimatedMinutesMax,
                command.RequiredDeliverables,
                command.SubmissionFormats,
                BuildAssignmentPortfolioConnection(command.PortfolioConnection),
                BuildRubric(command.Rubric),
                command.LinkedRubricId,
                command.AssessmentSkills,
                command.StudentChecklist,
                BuildAssignmentResources(command.Resources ?? []),
                BuildAssignmentSteps(command.AssignmentSteps ?? []),
                BuildRevisionPolicy(command.RevisionPolicy),
                BuildCompletionCriteria(command.CompletionCriteria),
                command.ReflectionPrompts,
                BuildEvidenceRequirements(command.EvidenceRequirements),
                BuildScoring(command.Scoring)) with { Id = existing.Id };
            var assignments = module.Assignments
                .Select(assignment => assignment.Id == command.AssignmentId ? updatedAssignment : assignment)
                .ToArray();
            await SaveModuleAsync(course, module.WithAssignments(assignments), cancellationToken);
            CourseNavigationChanged?.Invoke();
            return OperationResult.Success();
        }
        catch (DomainException ex)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> ReorderAssignmentsAsync(
        UserContext user,
        ReorderAssignmentsCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        var course = await repository.GetCourseAsync(command.CourseId, cancellationToken);
        var module = course?.Modules.FirstOrDefault(item => item.Id == command.ModuleId);
        if (course is null || module is null)
        {
            return OperationResult.Failure("Learning module was not found.");
        }

        if (command.AssignmentIds.Count != module.Assignments.Count ||
            command.AssignmentIds.Distinct().Count() != command.AssignmentIds.Count ||
            command.AssignmentIds.Any(id => module.Assignments.All(assignment => assignment.Id != id)))
        {
            return OperationResult.Failure("Assignment order must include each module assignment exactly once.");
        }

        var assignmentsById = module.Assignments.ToDictionary(assignment => assignment.Id);
        var reordered = command.AssignmentIds
            .Select((id, index) => assignmentsById[id] with { SequenceOrder = index + 1 })
            .ToArray();
        await SaveModuleAsync(course, module.WithAssignments(reordered), cancellationToken);
        CourseNavigationChanged?.Invoke();
        return OperationResult.Success();
    }

    public async Task<OperationResult> DeleteAssignmentAsync(
        UserContext user,
        DeleteAssignmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        if (!string.Equals(command.ConfirmationText, "Delete", StringComparison.Ordinal))
        {
            return OperationResult.Failure("Type Delete to confirm assignment deletion.");
        }

        var course = await repository.GetCourseAsync(command.CourseId, cancellationToken);
        var module = course?.Modules.FirstOrDefault(item => item.Id == command.ModuleId);
        if (course is null || module is null || module.Assignments.All(assignment => assignment.Id != command.AssignmentId))
        {
            return OperationResult.Failure("Assignment was not found.");
        }

        var assignments = module.Assignments
            .Where(assignment => assignment.Id != command.AssignmentId)
            .ToArray();
        await SaveModuleAsync(course, module.WithAssignments(assignments), cancellationToken);
        CourseNavigationChanged?.Invoke();
        return OperationResult.Success();
    }

    public async Task<OperationResult> SetRequirementMappingsAsync(
        UserContext user,
        SetCourseRequirementMappingsCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        var course = await repository.GetCourseAsync(command.CourseId, cancellationToken);
        if (course is null)
        {
            return OperationResult.Failure("Course was not found.");
        }

        var knownAreaIds = (await repository.GetRequirementAreasAsync(cancellationToken))
            .Select(area => area.Id)
            .ToHashSet();

        if (command.Mappings.Any(mapping => !knownAreaIds.Contains(mapping.RequirementAreaId)))
        {
            return OperationResult.Failure("One or more requirement areas were not found.");
        }

        try
        {
            var mappings = command.Mappings
                .Select(mapping => new RequirementMapping(
                    Guid.NewGuid(),
                    course.Id,
                    mapping.RequirementAreaId,
                    mapping.CoverageLevel,
                    mapping.Notes))
                .ToArray();

            await repository.SaveCourseAsync(course.WithMappings(mappings), cancellationToken);
            return OperationResult.Success();
        }
        catch (DomainException ex)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<IReadOnlyList<CoverageSummaryItem>> GetCoverageSummaryAsync(
        Guid? studentId = null,
        CancellationToken cancellationToken = default)
    {
        await RefreshMichiganRequirementSeedAsync(cancellationToken);
        await BackfillImportedCoursePackDetailsAsync(cancellationToken);

        var courses = (await repository.GetCoursesAsync(cancellationToken))
            .Where(course => studentId is null || course.StudentId == studentId.Value)
            .Where(course => !course.IsArchived)
            .ToArray();
        var areas = await repository.GetRequirementAreasAsync(cancellationToken);
        var schoolYear = await repository.GetSchoolYearAsync(cancellationToken);

        return areas
            .GroupBy(area => area.Name)
            .OrderBy(group => group.Select(area => area.View).Min(SourceOrder))
            .ThenBy(group => group.Key)
            .Select(group =>
            {
                var areaIds = group.Select(area => area.Id).ToHashSet();
                var courseMappings = courses
                    .SelectMany(course => course.RequirementMappings
                        .Where(mapping => areaIds.Contains(mapping.RequirementAreaId))
                        .Select(mapping => new { Course = course, Mapping = mapping }))
                    .Where(item => item.Mapping is not null)
                    .ToArray();

                return new CoverageSummaryItem(
                    group.First().Id,
                    FormatSource(group.Select(area => area.View)),
                    group.Key,
                    string.Join(", ", group.Select(area => area.GradeBand).Distinct().Order()),
                    courseMappings.Length > 0,
                    HighestCoverage(courseMappings.Select(item => item.Mapping.CoverageLevel)),
                    courseMappings.Select(item => item.Course.Title).Distinct().Order().ToArray());
            })
            .ToArray();
    }

    private static CourseDetail ToDetail(
        Course course,
        IReadOnlyList<RequirementArea> areas,
        SchoolYear? schoolYear,
        IReadOnlyList<CoursePackDefinition> availablePacks,
        Student? student = null)
    {
        var mappingViews = course.RequirementMappings
            .Select(mapping =>
            {
                var area = areas.FirstOrDefault(item => item.Id == mapping.RequirementAreaId);
                return new CourseRequirementMappingView(
                    mapping.RequirementAreaId,
                    area?.Name ?? "Unknown requirement area",
                    area?.View ?? "",
                    mapping.CoverageLevel,
                    mapping.Notes);
            })
            .OrderBy(mapping => SourceOrder(mapping.RequirementView))
            .ThenBy(mapping => mapping.RequirementAreaName)
            .ToArray();

        return new CourseDetail(
            course.Id,
            course.StudentId,
            student is null ? "" : $"{student.FirstName} {student.LastName}",
            course.Title,
            course.SubjectAreas,
            course.Duration,
            course.PlannedCreditValue,
            course.Description.Description,
            course.Description.InstructionalMethods,
            course.Description.MajorTopics,
            course.Description.TextsAndResources,
            course.Description.AssessmentMethods,
            course.Description.GradingBasis,
            course.CurriculumPlan.Goals,
            course.CurriculumPlan.LearningObjectives,
            course.CurriculumPlan.MajorResources,
            course.CurriculumPlan.PlannedSequence,
            course.CurriculumPlan.ParentNotes,
            BuildTermViews(schoolYear),
            course.Modules.Select(module => ToModuleView(module, schoolYear, course, availablePacks)).ToArray(),
            mappingViews);
    }

    private async Task<OperationResult<Student>> ResolveStudentAsync(
        Guid? studentId,
        CancellationToken cancellationToken)
    {
        var student = studentId.HasValue
            ? await repository.GetStudentAsync(studentId.Value, cancellationToken)
            : await repository.GetStudentAsync(cancellationToken);

        if (student is null && studentId.HasValue)
        {
            return OperationResult<Student>.Failure("Choose a student before working with courses.");
        }

        if (student is null)
        {
            return OperationResult<Student>.Failure("Create a student before working with courses.");
        }

        return OperationResult<Student>.Success(student);
    }

    private async Task<IReadOnlyList<Course>> ResolveCourseListActionTargetsAsync(
        CourseListActionCommand command,
        CancellationToken cancellationToken)
    {
        var courses = await repository.GetCoursesAsync(cancellationToken);
        var activeStudentCourses = courses
            .Where(course => course.StudentId == command.StudentId && !course.IsArchived)
            .ToArray();

        if (command.ApplyToEntireCourseList)
        {
            return activeStudentCourses;
        }

        var selectedIds = command.CourseIds.ToHashSet();
        return activeStudentCourses
            .Where(course => selectedIds.Contains(course.Id))
            .ToArray();
    }

    private static bool HasStudentWork(Course course)
    {
        return false;
    }

    private async Task<LearningModule?> GetModuleAsync(
        Guid courseId,
        Guid moduleId,
        CancellationToken cancellationToken)
    {
        var course = await repository.GetCourseAsync(courseId, cancellationToken);
        return course?.Modules.FirstOrDefault(module => module.Id == moduleId);
    }

    private async Task SaveModuleAsync(
        Course course,
        LearningModule updatedModule,
        CancellationToken cancellationToken)
    {
        var modules = course.Modules
            .Select(module => module.Id == updatedModule.Id ? updatedModule : module)
            .ToArray();
        await repository.SaveCourseAsync(course.WithModules(modules), cancellationToken);
    }

    private static LearningModuleView ToModuleView(
        LearningModule module,
        SchoolYear? schoolYear = null,
        Course? course = null,
        IReadOnlyList<CoursePackDefinition>? availablePacks = null)
    {
        var termName = schoolYear?.Terms.FirstOrDefault(term => term.Id == module.TermId)?.Name ?? "";
        return new LearningModuleView(
            module.Id,
            module.CourseId,
            module.SourceModuleId,
            module.SequenceOrder,
            module.Title,
            module.Description,
            module.TermId,
            termName,
            module.EstimatedLength,
            module.Instructions,
            module.MajorTopics,
            module.LearningObjectives,
            module.LearningObjectiveItems
                .Select(item => new ModuleLearningObjectiveView(item.Text, item.LinkedCourseObjective))
                .ToArray(),
            module.Resources,
            module.ResourceItems
                .Select(item => new ModuleResourceView(item.Name, item.Link, item.FilePath, item.IsPhysicalResource))
                .ToArray(),
            module.AssignmentEvidencePlaceholder,
            module.Status,
            module.Lessons.Select(ToLessonView).ToArray(),
            module.Assignments.Select(ToAssignmentView).ToArray(),
            AssignmentVariantsFor(course, module, availablePacks ?? DefaultCoursePacks.All));
    }

    private static LessonView ToLessonView(Lesson lesson)
    {
        return new LessonView(
            lesson.Id,
            lesson.ModuleId,
            lesson.SourceLessonId,
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
                .Select(objective => new LessonLearningObjectiveView(objective.ObjectiveId, objective.Text, objective.BloomLevel))
                .ToArray(),
            lesson.StandardsAlignments
                .Select(item => new StandardsAlignmentView(item.Framework, item.Code, item.Description))
                .ToArray(),
            lesson.SuccessCriteria,
            lesson.LessonSteps
                .Select(step => new LessonStepView(step.StepOrder, step.Title, step.StepType, step.Instructions, step.EstimatedMinutes, step.Required))
                .ToArray(),
            lesson.Resources
                .Select(resource => new LessonResourceView(
                    resource.Id,
                    resource.Name,
                    resource.Type,
                    resource.Url,
                    resource.FilePath,
                    resource.IsPhysicalResource,
                    resource.SourceNote,
                    resource.Required,
                    resource.EstimatedMinutes,
                    resource.StudentInstructions,
                    resource.NotesPrompt,
                    resource.Citation is null
                        ? null
                        : new LessonResourceCitationView(resource.Citation.Title, resource.Citation.Publisher, resource.Citation.AccessedAtUtc),
                    resource.OfflineAvailable,
                    resource.License))
                .ToArray(),
            lesson.ProblemSets
                .Select(problemSet => new LessonProblemSetView(
                    problemSet.ProblemSetId,
                    problemSet.Title,
                    problemSet.Instructions,
                    problemSet.EstimatedMinutes,
                    problemSet.Problems
                        .Select(problem => new LessonProblemView(
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
                .Select(connection => new LessonPortfolioConnectionView(
                    connection.PortfolioSection,
                    connection.ArtifactTitle,
                    connection.ArtifactPurpose,
                    connection.CrossCourseLinks,
                    connection.ReuseInstructions))
                .ToArray(),
            lesson.Rubric is null
                ? null
                : new LessonRubricView(
                    lesson.Rubric.RubricId,
                    lesson.Rubric.Scale,
                    lesson.Rubric.Criteria
                        .Select(criteria => new LessonRubricCriterionView(
                            criteria.Criterion,
                            criteria.Level4,
                            criteria.Level3,
                            criteria.Level2,
                            criteria.Level1))
                        .ToArray()),
            lesson.ReflectionPrompts,
            lesson.InstructorNotes is null
                ? null
                : new LessonInstructorNotesView(
                    lesson.InstructorNotes.Overview,
                    lesson.InstructorNotes.LookFors,
                    lesson.InstructorNotes.CommonIssues,
                    lesson.InstructorNotes.SuggestedFeedback),
            lesson.LinkedAssignmentIds);
    }

    private static AssignmentView ToAssignmentView(ModuleAssignment assignment)
    {
        return new AssignmentView(
            assignment.Id,
            assignment.ModuleId,
            assignment.SourceAssignmentId,
            assignment.SequenceOrder,
            assignment.Title,
            assignment.Type,
            assignment.MethodProfile,
            assignment.Instructions,
            assignment.EstimatedEffort,
            assignment.DueTimingLabel,
            assignment.DueDate,
            assignment.LinkedModuleObjectives,
            assignment.LinkedLessonIds,
            assignment.RequiredOutput,
            assignment.ParentNotes,
            assignment.IsPortfolioCandidate,
            assignment.PlannedPoints,
            assignment.PlannedWeight,
            assignment.Status,
            assignment.AssignmentSummary,
            assignment.StudentFacingGoal,
            assignment.EstimatedMinutesMin,
            assignment.EstimatedMinutesMax,
            assignment.RequiredDeliverables,
            assignment.SubmissionFormats,
            assignment.PortfolioConnection,
            assignment.Rubric,
            assignment.LinkedRubricId,
            assignment.AssessmentSkills,
            assignment.StudentChecklist,
            assignment.Resources,
            assignment.AssignmentSteps,
            assignment.RevisionPolicy,
            assignment.CompletionCriteria,
            assignment.ReflectionPrompts,
            assignment.EvidenceRequirements,
            assignment.Scoring);
    }

    private static IReadOnlyList<AssignmentVariantView> AssignmentVariantsFor(
        Course? course,
        LearningModule module,
        IReadOnlyList<CoursePackDefinition> availablePacks)
    {
        if (course is null)
        {
            return [];
        }

        var option = FindSourceOption(course, availablePacks);
        var templateModule = option?.Modules.FirstOrDefault(item =>
            string.Equals(item.ModuleId, module.SourceModuleId, StringComparison.OrdinalIgnoreCase));
        if (templateModule is null)
        {
            return [];
        }

        return templateModule.Assignments
            .SelectMany(assignment => assignment.Variants
                .Select(variant => new AssignmentVariantView(
                    variant.VariantId,
                    assignment.AssignmentId,
                    variant.Title,
                    variant.Type,
                    variant.MethodProfile,
                    variant.Instructions,
                    variant.EstimatedEffort,
                    variant.DueTimingLabel,
                    variant.LinkedModuleObjectives,
                    variant.LinkedLessonIds,
                    variant.RequiredOutput,
                    variant.ParentNotes,
                    variant.IsPortfolioCandidate,
                    variant.PlannedPoints,
                    variant.PlannedWeight,
                    variant.Status,
                    variant.AssignmentSummary,
                    variant.StudentFacingGoal,
                    variant.EstimatedMinutesMin,
                    variant.EstimatedMinutesMax,
                    variant.RequiredDeliverables ?? [],
                    variant.SubmissionFormats ?? [],
                    variant.PortfolioConnection,
                    variant.Rubric,
                    variant.LinkedRubricId,
                    variant.AssessmentSkills ?? [],
                    variant.StudentChecklist ?? [],
                    variant.Resources ?? [],
                    variant.AssignmentSteps ?? [],
                    variant.RevisionPolicy,
                    variant.CompletionCriteria,
                    variant.ReflectionPrompts ?? [],
                    variant.EvidenceRequirements,
                    variant.Scoring)))
            .ToArray();
    }

    private static ModuleAssignment BuildAssignment(
        Guid moduleId,
        string sourceAssignmentId,
        int sequenceOrder,
        string title,
        AssignmentType type,
        InstructionalMethodProfile methodProfile,
        string instructions,
        string estimatedEffort,
        string dueTimingLabel,
        DateOnly? dueDate,
        IReadOnlyList<string> linkedModuleObjectives,
        IReadOnlyList<Guid> linkedLessonIds,
        string requiredOutput,
        string parentNotes,
        bool isPortfolioCandidate,
        decimal? plannedPoints,
        decimal? plannedWeight,
        AssignmentStatus status,
        string assignmentSummary = "",
        string studentFacingGoal = "",
        int? estimatedMinutesMin = null,
        int? estimatedMinutesMax = null,
        IReadOnlyList<string>? requiredDeliverables = null,
        IReadOnlyList<AssignmentSubmissionFormat>? submissionFormats = null,
        AssignmentPortfolioConnection? portfolioConnection = null,
        LessonRubric? rubric = null,
        string linkedRubricId = "",
        IReadOnlyList<string>? assessmentSkills = null,
        IReadOnlyList<string>? studentChecklist = null,
        IReadOnlyList<AssignmentResource>? resources = null,
        IReadOnlyList<AssignmentStep>? assignmentSteps = null,
        AssignmentRevisionPolicy? revisionPolicy = null,
        AssignmentCompletionCriteria? completionCriteria = null,
        IReadOnlyList<string>? reflectionPrompts = null,
        AssignmentEvidenceRequirements? evidenceRequirements = null,
        AssignmentScoring? scoring = null)
    {
        return new ModuleAssignment(
            Guid.NewGuid(),
            moduleId,
            sourceAssignmentId,
            sequenceOrder,
            title,
            type,
            methodProfile,
            instructions,
            estimatedEffort,
            dueTimingLabel,
            dueDate,
            linkedModuleObjectives,
            linkedLessonIds,
            requiredOutput,
            parentNotes,
            isPortfolioCandidate,
            plannedPoints,
            plannedWeight,
            status,
            assignmentSummary,
            studentFacingGoal,
            estimatedMinutesMin,
            estimatedMinutesMax,
            requiredDeliverables,
            submissionFormats,
            portfolioConnection,
            rubric,
            linkedRubricId,
            assessmentSkills,
            studentChecklist,
            resources,
            assignmentSteps,
            revisionPolicy,
            completionCriteria,
            reflectionPrompts,
            evidenceRequirements,
            scoring);
    }

    private static ModuleAssignment BuildImportedAssignment(
        LearningModule module,
        AssignmentPackAssignment assignment,
        int sequenceOrder,
        HashSet<string> existingSourceIds)
    {
        return BuildAssignment(
            module.Id,
            UniqueAssignmentSourceId(RepairSourceId(assignment.SourceAssignmentId, assignment.Title, sequenceOrder), existingSourceIds),
            sequenceOrder,
            assignment.Title,
            assignment.Type,
            assignment.MethodProfile,
            assignment.Instructions,
            assignment.EstimatedEffort,
            assignment.DueTimingLabel,
            assignment.DueDate,
            assignment.LinkedModuleObjectives ?? [],
            ResolveAssignmentPackLessonLinks(module.Lessons, assignment.LinkedLessonSourceIds, assignment.LinkedLessonTitles),
            assignment.RequiredOutput,
            assignment.ParentNotes,
            assignment.IsPortfolioCandidate,
            assignment.PlannedPoints,
            assignment.PlannedWeight,
            assignment.Status,
            assignment.AssignmentSummary,
            assignment.StudentFacingGoal,
            assignment.EstimatedMinutesMin,
            assignment.EstimatedMinutesMax,
            assignment.RequiredDeliverables,
            assignment.SubmissionFormats,
            assignment.PortfolioConnection,
            assignment.Rubric,
            assignment.LinkedRubricId,
            assignment.AssessmentSkills,
            assignment.StudentChecklist,
            assignment.Resources,
            assignment.AssignmentSteps,
            assignment.RevisionPolicy,
            assignment.CompletionCriteria,
            assignment.ReflectionPrompts,
            assignment.EvidenceRequirements,
            assignment.Scoring);
    }

    private static string UniqueAssignmentSourceId(string sourceAssignmentId, HashSet<string> existingSourceIds)
    {
        var normalized = string.IsNullOrWhiteSpace(sourceAssignmentId) ? "" : sourceAssignmentId.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "";
        }

        if (existingSourceIds.Add(normalized))
        {
            return normalized;
        }

        var index = 2;
        string candidate;
        do
        {
            candidate = $"{normalized}-{index++}";
        }
        while (!existingSourceIds.Add(candidate));

        return candidate;
    }

    private static IReadOnlyList<Guid> ResolveAssignmentPackLessonLinks(
        IReadOnlyList<Lesson> lessons,
        IReadOnlyList<string>? sourceLessonIds,
        IReadOnlyList<string>? lessonTitles)
    {
        var linked = new List<Guid>();
        foreach (var sourceLessonId in sourceLessonIds ?? [])
        {
            var match = lessons.FirstOrDefault(lesson =>
                !string.IsNullOrWhiteSpace(lesson.SourceLessonId) &&
                string.Equals(lesson.SourceLessonId, sourceLessonId, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                linked.Add(match.Id);
            }
        }

        foreach (var lessonTitle in lessonTitles ?? [])
        {
            var match = lessons.FirstOrDefault(lesson =>
                string.Equals(lesson.Title, lessonTitle, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                linked.Add(match.Id);
            }
        }

        return linked.Distinct().ToArray();
    }

    private static AssignmentPackAssignment ToAssignmentPackAssignment(
        ModuleAssignment assignment,
        IReadOnlyList<Lesson> lessons)
    {
        var linkedLessons = assignment.LinkedLessonIds
            .Select(id => lessons.FirstOrDefault(lesson => lesson.Id == id))
            .Where(lesson => lesson is not null)
            .Select(lesson => lesson!)
            .ToArray();

        return new AssignmentPackAssignment(
            assignment.SourceAssignmentId,
            assignment.SequenceOrder,
            assignment.Title,
            assignment.Type,
            assignment.MethodProfile,
            assignment.Instructions,
            assignment.EstimatedEffort,
            assignment.DueTimingLabel,
            assignment.DueDate,
            assignment.LinkedModuleObjectives,
            linkedLessons
                .Select(lesson => lesson.SourceLessonId)
                .Where(sourceId => !string.IsNullOrWhiteSpace(sourceId))
                .ToArray(),
            linkedLessons.Select(lesson => lesson.Title).ToArray(),
            assignment.RequiredOutput,
            assignment.ParentNotes,
            assignment.IsPortfolioCandidate,
            assignment.PlannedPoints,
            assignment.PlannedWeight,
            assignment.Status,
            assignment.AssignmentSummary,
            assignment.StudentFacingGoal,
            assignment.EstimatedMinutesMin,
            assignment.EstimatedMinutesMax,
            assignment.RequiredDeliverables,
            assignment.SubmissionFormats,
            assignment.PortfolioConnection,
            assignment.Rubric,
            assignment.LinkedRubricId,
            assignment.AssessmentSkills,
            assignment.StudentChecklist,
            assignment.Resources,
            assignment.AssignmentSteps,
            assignment.RevisionPolicy,
            assignment.CompletionCriteria,
            assignment.ReflectionPrompts,
            assignment.EvidenceRequirements,
            assignment.Scoring);
    }

    private static IReadOnlyList<CourseTermView> BuildTermViews(SchoolYear? schoolYear)
    {
        return schoolYear?.Terms
            .OrderBy(term => term.StartDate)
            .Select(term => new CourseTermView(term.Id, term.Name, term.StartDate, term.EndDate))
            .ToArray() ?? [];
    }

    private static IReadOnlyList<ModuleLearningObjective> BuildObjectiveItems(
        IReadOnlyList<ModuleLearningObjectiveCommand> objectives)
    {
        return objectives
            .Where(item => !string.IsNullOrWhiteSpace(item.Text))
            .Select(item => new ModuleLearningObjective(item.Text, item.LinkedCourseObjective))
            .ToArray();
    }

    private static IReadOnlyList<ModuleResource> BuildResourceItems(
        IReadOnlyList<ModuleResourceCommand> resources)
    {
        return resources
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .Select(item => new ModuleResource(item.Name, item.Link, item.FilePath, item.IsPhysicalResource))
            .ToArray();
    }

    private static IReadOnlyList<LessonResource> BuildLessonResourceItems(
        IReadOnlyList<LessonResourceCommand> resources)
    {
        return resources
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .Select(item => new LessonResource(
                Guid.NewGuid(),
                item.Name,
                item.Type,
                item.Url,
                item.FilePath,
                item.IsPhysicalResource,
                item.SourceNote,
                item.Required,
                item.EstimatedMinutes,
                item.StudentInstructions,
                item.NotesPrompt,
                item.Citation is null
                    ? null
                    : new LessonResourceCitation(item.Citation.Title, item.Citation.Publisher, item.Citation.AccessedAtUtc),
                item.OfflineAvailable,
                item.License))
            .ToArray();
    }

    private static IReadOnlyList<LessonLearningObjective> BuildLessonLearningObjectives(
        IReadOnlyList<LessonLearningObjectiveCommand> objectives)
    {
        return objectives
            .Where(item => !string.IsNullOrWhiteSpace(item.Text))
            .Select(item => new LessonLearningObjective(item.ObjectiveId, item.Text, item.BloomLevel))
            .ToArray();
    }

    private static IReadOnlyList<StandardsAlignment> BuildStandardsAlignments(
        IReadOnlyList<StandardsAlignmentCommand> standards)
    {
        return standards
            .Where(item => !string.IsNullOrWhiteSpace(item.Framework) || !string.IsNullOrWhiteSpace(item.Code) || !string.IsNullOrWhiteSpace(item.Description))
            .Select(item => new StandardsAlignment(item.Framework, item.Code, item.Description))
            .ToArray();
    }

    private static IReadOnlyList<LessonStep> BuildLessonSteps(IReadOnlyList<LessonStepCommand> steps)
    {
        return steps
            .Where(item => !string.IsNullOrWhiteSpace(item.Title))
            .Select(item => new LessonStep(item.StepOrder, item.Title, item.StepType, item.Instructions, item.EstimatedMinutes, item.Required))
            .ToArray();
    }

    private static IReadOnlyList<LessonProblemSet> BuildProblemSets(IReadOnlyList<LessonProblemSetCommand> problemSets)
    {
        return problemSets
            .Where(item => !string.IsNullOrWhiteSpace(item.Title))
            .Select(item => new LessonProblemSet(
                item.ProblemSetId,
                item.Title,
                item.Instructions,
                item.EstimatedMinutes,
                item.Problems
                    .Where(problem => !string.IsNullOrWhiteSpace(problem.Prompt))
                    .Select(problem => new LessonProblem(
                        problem.ProblemId,
                        problem.Prompt,
                        problem.ResponseType,
                        problem.ExpectedAnswer,
                        problem.Solution,
                        problem.Skills,
                        problem.Difficulty))
                    .ToArray()))
            .ToArray();
    }

    private static IReadOnlyList<LessonPortfolioConnection> BuildPortfolioConnections(
        IReadOnlyList<LessonPortfolioConnectionCommand> connections)
    {
        return connections
            .Where(item => !string.IsNullOrWhiteSpace(item.PortfolioSection) || !string.IsNullOrWhiteSpace(item.ArtifactTitle))
            .Select(item => new LessonPortfolioConnection(
                item.PortfolioSection,
                item.ArtifactTitle,
                item.ArtifactPurpose,
                item.CrossCourseLinks,
                item.ReuseInstructions))
            .ToArray();
    }

    private static LessonRubric? BuildRubric(LessonRubricCommand? rubric)
    {
        if (rubric is null)
        {
            return null;
        }

        return new LessonRubric(
            rubric.RubricId,
            rubric.Scale,
            rubric.Criteria
                .Where(item => !string.IsNullOrWhiteSpace(item.Criterion))
                .Select(item => new LessonRubricCriterion(item.Criterion, item.Level4, item.Level3, item.Level2, item.Level1))
                .ToArray());
    }

    private static LessonInstructorNotes? BuildInstructorNotes(LessonInstructorNotesCommand? notes)
    {
        return notes is null
            ? null
            : new LessonInstructorNotes(notes.Overview, notes.LookFors, notes.CommonIssues, notes.SuggestedFeedback);
    }

    private static AssignmentPortfolioConnection? BuildAssignmentPortfolioConnection(AssignmentPortfolioConnectionCommand? connection)
    {
        return connection is null
            ? null
            : new AssignmentPortfolioConnection(
                connection.IsPortfolioCandidate,
                connection.PortfolioSection,
                connection.ArtifactTitle,
                connection.ArtifactPurpose,
                connection.ReuseInstructions,
                connection.CrossCourseLinks);
    }

    private static IReadOnlyList<AssignmentResource> BuildAssignmentResources(IReadOnlyList<AssignmentResourceCommand> resources)
    {
        return resources
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .Select(item => new AssignmentResource(
                item.Name,
                item.Type,
                item.Url,
                item.FilePath,
                item.IsPhysicalResource,
                item.Required,
                item.StudentInstructions,
                item.SourceNote,
                item.Citation is null
                    ? null
                    : new LessonResourceCitation(item.Citation.Title, item.Citation.Publisher, item.Citation.AccessedAtUtc)))
            .ToArray();
    }

    private static IReadOnlyList<AssignmentStep> BuildAssignmentSteps(IReadOnlyList<AssignmentStepCommand> steps)
    {
        return steps
            .Where(item => !string.IsNullOrWhiteSpace(item.Title))
            .Select(item => new AssignmentStep(item.StepOrder, item.Title, item.Instructions, item.EstimatedMinutes))
            .ToArray();
    }

    private static AssignmentRevisionPolicy? BuildRevisionPolicy(AssignmentRevisionPolicyCommand? policy)
    {
        return policy is null
            ? null
            : new AssignmentRevisionPolicy(policy.AllowRevision, policy.RevisionExpectation, policy.MinimumRevisionCount);
    }

    private static AssignmentCompletionCriteria? BuildCompletionCriteria(AssignmentCompletionCriteriaCommand? criteria)
    {
        return criteria is null
            ? null
            : new AssignmentCompletionCriteria(criteria.MinimumRequirements, criteria.RequiresParentReview, criteria.MasteryThreshold);
    }

    private static AssignmentEvidenceRequirements? BuildEvidenceRequirements(AssignmentEvidenceRequirementsCommand? requirements)
    {
        return requirements is null
            ? null
            : new AssignmentEvidenceRequirements(
                requirements.RetainForRecords,
                requirements.EvidenceType,
                requirements.RecommendedFileTypes,
                requirements.RequiresStudentExplanation,
                requirements.RequiresParentEvaluation);
    }

    private static AssignmentScoring? BuildScoring(AssignmentScoringCommand? scoring)
    {
        return scoring is null
            ? null
            : new AssignmentScoring(
                scoring.PlannedPoints,
                scoring.PlannedWeight,
                scoring.GradingMode,
                scoring.CountsTowardGrade,
                scoring.AllowPartialCredit);
    }

    private static Lesson BuildImportedLesson(
        Guid moduleId,
        LessonPackLesson lesson,
        int sequenceOrder,
        HashSet<string> existingSourceIds,
        IReadOnlyList<ModuleAssignment>? assignments = null)
    {
        var sourceLessonId = UniqueLessonSourceId(RepairSourceId(lesson.SourceLessonId, lesson.Title, sequenceOrder), existingSourceIds);
        return new Lesson(
            Guid.NewGuid(),
            moduleId,
            sourceLessonId,
            sequenceOrder,
            lesson.Title,
            lesson.IntroductoryText,
            lesson.LinkedModuleObjective,
            (lesson.Resources ?? [])
                .Where(resource => !string.IsNullOrWhiteSpace(resource.Name))
                .Select(resource => new LessonResource(
                    Guid.NewGuid(),
                    resource.Name,
                    resource.Type,
                    resource.Url,
                    resource.FilePath,
                    resource.IsPhysicalResource,
                    resource.SourceNote,
                    resource.Required,
                    resource.EstimatedMinutes,
                    resource.StudentInstructions,
                    resource.NotesPrompt,
                    resource.Citation,
                    resource.OfflineAvailable,
                    resource.License))
                .ToArray(),
            lesson.LessonType,
            lesson.EstimatedMinutes,
            lesson.SuggestedDays,
            lesson.DifficultyLevel,
            lesson.SubjectAreas,
            lesson.Tags,
            lesson.Prerequisites,
            lesson.LearningObjectives,
            lesson.StandardsAlignments,
            lesson.SuccessCriteria,
            lesson.LessonSteps,
            lesson.ProblemSets,
            lesson.PortfolioConnections,
            lesson.Rubric,
            lesson.ReflectionPrompts,
            lesson.InstructorNotes,
            ResolveLessonPackAssignmentLinks(assignments ?? [], lesson.LinkedAssignmentSourceIds, lesson.LinkedAssignmentTitles));
    }

    private static Lesson BuildLesson(
        Guid lessonId,
        Guid moduleId,
        string sourceLessonId,
        int sequenceOrder,
        CreateLessonCommand command)
    {
        return new Lesson(
            lessonId,
            moduleId,
            sourceLessonId,
            sequenceOrder,
            command.Title,
            command.IntroductoryText,
            command.LinkedModuleObjective,
            BuildLessonResourceItems(command.Resources),
            command.LessonType,
            command.EstimatedMinutes,
            command.SuggestedDays,
            command.DifficultyLevel,
            command.SubjectAreas,
            command.Tags,
            command.Prerequisites,
            BuildLessonLearningObjectives(command.LearningObjectives ?? []),
            BuildStandardsAlignments(command.StandardsAlignments ?? []),
            command.SuccessCriteria,
            BuildLessonSteps(command.LessonSteps ?? []),
            BuildProblemSets(command.ProblemSets ?? []),
            BuildPortfolioConnections(command.PortfolioConnections ?? []),
            BuildRubric(command.Rubric),
            command.ReflectionPrompts,
            BuildInstructorNotes(command.InstructorNotes),
            command.LinkedAssignmentIds);
    }

    private static Lesson BuildLesson(
        Guid lessonId,
        Guid moduleId,
        string sourceLessonId,
        int sequenceOrder,
        UpdateLessonCommand command)
    {
        return new Lesson(
            lessonId,
            moduleId,
            sourceLessonId,
            sequenceOrder,
            command.Title,
            command.IntroductoryText,
            command.LinkedModuleObjective,
            BuildLessonResourceItems(command.Resources),
            command.LessonType,
            command.EstimatedMinutes,
            command.SuggestedDays,
            command.DifficultyLevel,
            command.SubjectAreas,
            command.Tags,
            command.Prerequisites,
            BuildLessonLearningObjectives(command.LearningObjectives ?? []),
            BuildStandardsAlignments(command.StandardsAlignments ?? []),
            command.SuccessCriteria,
            BuildLessonSteps(command.LessonSteps ?? []),
            BuildProblemSets(command.ProblemSets ?? []),
            BuildPortfolioConnections(command.PortfolioConnections ?? []),
            BuildRubric(command.Rubric),
            command.ReflectionPrompts,
            BuildInstructorNotes(command.InstructorNotes),
            command.LinkedAssignmentIds);
    }

    private static string UniqueLessonSourceId(string sourceLessonId, HashSet<string> existingSourceIds)
    {
        var normalized = string.IsNullOrWhiteSpace(sourceLessonId) ? "" : sourceLessonId.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "";
        }

        if (existingSourceIds.Add(normalized))
        {
            return normalized;
        }

        var index = 2;
        string candidate;
        do
        {
            candidate = $"{normalized}-{index++}";
        }
        while (!existingSourceIds.Add(candidate));

        return candidate;
    }

    private static IReadOnlyList<Guid> ResolveLessonPackAssignmentLinks(
        IReadOnlyList<ModuleAssignment> assignments,
        IReadOnlyList<string>? sourceAssignmentIds,
        IReadOnlyList<string>? assignmentTitles)
    {
        var linked = new List<Guid>();
        foreach (var sourceAssignmentId in sourceAssignmentIds ?? [])
        {
            var match = assignments.FirstOrDefault(assignment =>
                !string.IsNullOrWhiteSpace(assignment.SourceAssignmentId) &&
                string.Equals(assignment.SourceAssignmentId, sourceAssignmentId, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                linked.Add(match.Id);
            }
        }

        foreach (var assignmentTitle in assignmentTitles ?? [])
        {
            var match = assignments.FirstOrDefault(assignment =>
                string.Equals(assignment.Title, assignmentTitle, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                linked.Add(match.Id);
            }
        }

        return linked.Distinct().ToArray();
    }

    private static LessonPackLesson ToLessonPackLesson(Lesson lesson, IReadOnlyList<ModuleAssignment> assignments)
    {
        var linkedAssignments = lesson.LinkedAssignmentIds
            .Select(id => assignments.FirstOrDefault(assignment => assignment.Id == id))
            .Where(assignment => assignment is not null)
            .Select(assignment => assignment!)
            .ToArray();

        return new LessonPackLesson(
            lesson.SourceLessonId,
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
            lesson.LearningObjectives,
            lesson.StandardsAlignments,
            lesson.SuccessCriteria,
            lesson.LessonSteps,
            lesson.Resources
                .Select(resource => new LessonPackResource(
                    resource.Name,
                    resource.Type,
                    resource.Url,
                    resource.FilePath,
                    resource.IsPhysicalResource,
                    resource.SourceNote,
                    resource.Required,
                    resource.EstimatedMinutes,
                    resource.StudentInstructions,
                    resource.NotesPrompt,
                    resource.Citation,
                    resource.OfflineAvailable,
                    resource.License))
                .ToArray(),
            lesson.ProblemSets,
            lesson.PortfolioConnections,
            lesson.Rubric,
            lesson.ReflectionPrompts,
            lesson.InstructorNotes,
            linkedAssignments
                .Select(assignment => assignment.SourceAssignmentId)
                .Where(sourceId => !string.IsNullOrWhiteSpace(sourceId))
                .ToArray(),
            linkedAssignments.Select(assignment => assignment.Title).ToArray());
    }

    private static string Lines(IEnumerable<string> values)
    {
        return string.Join(Environment.NewLine, values.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static CoverageLevel? HighestCoverage(IEnumerable<CoverageLevel> levels)
    {
        return levels.OrderBy(level => (int)level).Cast<CoverageLevel?>().FirstOrDefault();
    }

    private static string FormatSource(IEnumerable<string> sources)
    {
        var ordered = sources
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(SourceOrder)
            .ThenBy(source => source)
            .ToArray();

        return ordered.Contains("Statutory", StringComparer.OrdinalIgnoreCase)
            ? "Statutory"
            : ordered.FirstOrDefault() ?? "";
    }

    private static int SourceOrder(string source)
    {
        return source switch
        {
            "Statutory" => 0,
            "MDE Summary" => 1,
            "MMC Reference" => 2,
            _ => 99
        };
    }

    private static IReadOnlyList<RequirementMapping> BuildMappings(
        Guid courseId,
        CourseTemplateOptionDefinition option,
        IReadOnlyList<RequirementArea> areas)
    {
        return option.RequirementMappings
            .Select(mapping => TryBuildMapping(courseId, mapping, areas))
            .Where(mapping => mapping is not null)
            .Select(mapping => mapping!)
            .ToArray();
    }

    private static RequirementMapping? TryBuildMapping(
        Guid courseId,
        CourseTemplateRequirementMapping mapping,
        IReadOnlyList<RequirementArea> areas)
    {
        var area = areas.FirstOrDefault(item =>
            string.Equals(item.View, mapping.RequirementAreaView, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(item.Name, mapping.RequirementAreaName, StringComparison.OrdinalIgnoreCase));

        if (area is null)
        {
            return null;
        }

        return new RequirementMapping(
            Guid.NewGuid(),
            courseId,
            area.Id,
            mapping.CoverageLevel,
            mapping.Notes);
    }

    private static CourseTemplateOptionDefinition? ResolveSelectedOption(
        CourseTemplateDefinition template,
        IReadOnlyDictionary<string, string> selectionByTemplateId)
    {
        var optionId = selectionByTemplateId.TryGetValue(template.TemplateId, out var selectedOptionId)
            ? selectedOptionId
            : template.DefaultOptionId;

        if (string.IsNullOrWhiteSpace(optionId))
        {
            optionId = template.DefaultOptionId;
        }

        return template.Options.FirstOrDefault(option =>
            string.Equals(option.OptionId, optionId, StringComparison.OrdinalIgnoreCase));
    }

    private async Task BackfillImportedCoursePackDetailsAsync(CancellationToken cancellationToken)
    {
        var courses = await repository.GetCoursesAsync(cancellationToken);
        var areas = await repository.GetRequirementAreasAsync(cancellationToken);
        var schoolYear = await repository.GetSchoolYearAsync(cancellationToken);
        var availablePacks = await GetAvailableCoursePacksAsync(cancellationToken);
        foreach (var course in courses)
        {
            var option = FindSourceOption(course, availablePacks);
            if (option is null)
            {
                continue;
            }

            var description = MergeDescription(course.Description, option.Description);
            var curriculumPlan = MergeCurriculumPlan(course.CurriculumPlan, option.CurriculumPlan);
            var mappings = MergeImportedMappings(course, option, areas);
            var modules = MergeImportedModules(course, option, schoolYear);
            if (description == course.Description &&
                curriculumPlan == course.CurriculumPlan &&
                MappingsMatch(course.RequirementMappings, mappings) &&
                ModulesMatch(course.Modules, modules))
            {
                continue;
            }

            var updated = new Course(
                course.Id,
                course.StudentId,
                course.SchoolYearId,
                course.Title,
                course.SubjectAreas,
                course.Duration,
                course.PlannedCreditValue,
                course.SourcePackId,
                course.SourceTemplateId,
                description,
                curriculumPlan,
                mappings,
                modules);

            await repository.SaveCourseAsync(updated, cancellationToken);
        }
    }

    private static IReadOnlyList<LearningModule> BuildModules(
        Guid courseId,
        CourseTemplateOptionDefinition option,
        SchoolYear? schoolYear)
    {
        return option.Modules
            .OrderBy(module => module.SequenceOrder)
            .Select(module =>
            {
                var moduleId = Guid.NewGuid();
                var lessons = BuildLessons(moduleId, module.Lessons);
                var assignments = BuildAssignments(moduleId, module.Assignments, lessons);
                lessons = ApplyCoursePackLessonAssignmentLinks(lessons, module.Lessons, assignments);
                return new LearningModule(
                    moduleId,
                    courseId,
                    module.ModuleId,
                    module.SequenceOrder,
                    module.Title,
                    module.Description,
                    module.EstimatedLength,
                    module.Instructions,
                    "",
                    Lines(module.LearningObjectives.Select(item => item.Text)),
                    Lines(module.Resources.Select(item => item.Name)),
                    module.AssignmentEvidencePlaceholder,
                    module.Status,
                    ResolveTermId(module.TermNumber, schoolYear),
                    module.LearningObjectives
                        .Select(item => new ModuleLearningObjective(item.Text, item.LinkedCourseObjective))
                        .ToArray(),
                    module.Resources
                        .Select(item => new ModuleResource(item.Name, item.Link, "", item.IsPhysicalResource))
                        .ToArray(),
                    lessons,
                    assignments);
            })
            .ToArray();
    }

    private static IReadOnlyList<LearningModule> MergeImportedModules(
        Course course,
        CourseTemplateOptionDefinition option,
        SchoolYear? schoolYear)
    {
        var modules = course.Modules.ToList();
        foreach (var packModule in BuildModules(course.Id, option, schoolYear))
        {
            var existing = modules.FirstOrDefault(module =>
                !string.IsNullOrWhiteSpace(packModule.SourceModuleId) &&
                string.Equals(module.SourceModuleId, packModule.SourceModuleId, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                var mergedExisting = MergeImportedLessons(existing, packModule);
                mergedExisting = MergeImportedAssignments(mergedExisting, packModule);
                var shouldUpgrade =
                    ModuleUsesLegacyText(existing) ||
                    existing.TermId is null ||
                    existing.MajorTopics.Length > 0 ||
                    existing.Lessons.Count == 0 ||
                    existing.Assignments.Count == 0;
                if (shouldUpgrade)
                {
                    modules = modules
                        .Select(module => module.Id == existing.Id
                            ? packModule with { Id = existing.Id, SequenceOrder = existing.SequenceOrder, Lessons = mergedExisting.Lessons, Assignments = mergedExisting.Assignments }
                            : module)
                        .ToList();
                }
                else if (mergedExisting.Lessons.Count != existing.Lessons.Count ||
                    mergedExisting.Assignments.Count != existing.Assignments.Count)
                {
                    modules = modules
                        .Select(module => module.Id == existing.Id
                            ? mergedExisting
                            : module)
                        .ToList();
                }

                continue;
            }

            modules.Add(packModule with { SequenceOrder = modules.Count + 1 });
        }

        return modules
            .OrderBy(module => module.SequenceOrder)
            .ThenBy(module => module.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static LearningModule MergeImportedLessons(LearningModule existing, LearningModule packModule)
    {
        var lessons = existing.Lessons.ToList();
        foreach (var packLesson in packModule.Lessons)
        {
            if (string.IsNullOrWhiteSpace(packLesson.SourceLessonId))
            {
                continue;
            }

            var existingLesson = lessons.FirstOrDefault(lesson =>
                string.Equals(lesson.SourceLessonId, packLesson.SourceLessonId, StringComparison.OrdinalIgnoreCase));
            if (existingLesson is not null)
            {
                continue;
            }

            lessons.Add(packLesson with
            {
                Id = Guid.NewGuid(),
                ModuleId = existing.Id,
                SequenceOrder = lessons.Count + 1
            });
        }

        return existing.WithLessons(lessons);
    }

    private static LearningModule MergeImportedAssignments(LearningModule existing, LearningModule packModule)
    {
        var assignments = existing.Assignments.ToList();
        foreach (var packAssignment in packModule.Assignments)
        {
            if (string.IsNullOrWhiteSpace(packAssignment.SourceAssignmentId))
            {
                continue;
            }

            var existingAssignment = assignments.FirstOrDefault(assignment =>
                string.Equals(assignment.SourceAssignmentId, packAssignment.SourceAssignmentId, StringComparison.OrdinalIgnoreCase));
            if (existingAssignment is not null)
            {
                continue;
            }

            assignments.Add(packAssignment with
            {
                Id = Guid.NewGuid(),
                ModuleId = existing.Id,
                SequenceOrder = assignments.Count + 1,
                LinkedLessonIds = MapAssignmentLessonLinks(packAssignment.LinkedLessonIds, packModule.Lessons, existing.Lessons)
            });
        }

        return existing.WithAssignments(assignments);
    }

    private static IReadOnlyList<Lesson> BuildLessons(
        Guid moduleId,
        IReadOnlyList<CourseTemplateLessonDefinition> lessons)
    {
        return lessons
            .OrderBy(lesson => lesson.SequenceOrder)
            .Select(lesson => new Lesson(
                Guid.NewGuid(),
                moduleId,
                lesson.LessonId,
                lesson.SequenceOrder,
                lesson.Title,
                lesson.IntroductoryText,
                lesson.LinkedModuleObjective,
                lesson.Resources
                    .Select(resource => new LessonResource(
                        Guid.NewGuid(),
                        resource.Name,
                        resource.Type,
                        resource.Url,
                        "",
                        resource.IsPhysicalResource,
                        resource.SourceNote,
                        resource.Required,
                        resource.EstimatedMinutes,
                        resource.StudentInstructions,
                        resource.NotesPrompt,
                        resource.Citation,
                        resource.OfflineAvailable,
                        resource.License))
                    .ToArray(),
                lesson.LessonType,
                lesson.EstimatedMinutes,
                lesson.SuggestedDays,
                lesson.DifficultyLevel,
                lesson.SubjectAreas ?? [],
                lesson.Tags ?? [],
                lesson.Prerequisites ?? [],
                lesson.LearningObjectives ?? [],
                lesson.StandardsAlignments ?? [],
                lesson.SuccessCriteria ?? [],
                lesson.LessonSteps ?? [],
                lesson.ProblemSets ?? [],
                lesson.PortfolioConnections ?? [],
                lesson.Rubric,
                lesson.ReflectionPrompts ?? [],
                lesson.InstructorNotes))
            .ToArray();
    }

    private static IReadOnlyList<ModuleAssignment> BuildAssignments(
        Guid moduleId,
        IReadOnlyList<CourseTemplateAssignmentDefinition> assignments,
        IReadOnlyList<Lesson> lessons)
    {
        return assignments
            .OrderBy(assignment => assignment.SequenceOrder)
            .Select(assignment =>
            {
                var variant = assignment.Variants.FirstOrDefault(item => item.MethodProfile == InstructionalMethodProfile.Hybrid)
                    ?? assignment.Variants.First();
                return BuildAssignment(
                    moduleId,
                    assignment.AssignmentId,
                    assignment.SequenceOrder,
                    variant.Title,
                    variant.Type,
                    variant.MethodProfile,
                    variant.Instructions,
                    variant.EstimatedEffort,
                    variant.DueTimingLabel,
                    null,
                    variant.LinkedModuleObjectives,
                    MapAssignmentLessonLinks(variant.LinkedLessonIds, lessons),
                    variant.RequiredOutput,
                    variant.ParentNotes,
                    variant.IsPortfolioCandidate,
                    variant.PlannedPoints,
                    variant.PlannedWeight,
                    variant.Status,
                    variant.AssignmentSummary,
                    variant.StudentFacingGoal,
                    variant.EstimatedMinutesMin,
                    variant.EstimatedMinutesMax,
                    variant.RequiredDeliverables,
                    variant.SubmissionFormats,
                    variant.PortfolioConnection,
                    variant.Rubric,
                    variant.LinkedRubricId,
                    variant.AssessmentSkills,
                    variant.StudentChecklist,
                    variant.Resources,
                    variant.AssignmentSteps,
                    variant.RevisionPolicy,
                    variant.CompletionCriteria,
                    variant.ReflectionPrompts,
                    variant.EvidenceRequirements,
                    variant.Scoring);
            })
            .ToArray();
    }

    private static IReadOnlyList<Lesson> ApplyCoursePackLessonAssignmentLinks(
        IReadOnlyList<Lesson> lessons,
        IReadOnlyList<CourseTemplateLessonDefinition> lessonDefinitions,
        IReadOnlyList<ModuleAssignment> assignments)
    {
        return lessons
            .Select(lesson =>
            {
                var definition = lessonDefinitions.FirstOrDefault(item =>
                    string.Equals(item.LessonId, lesson.SourceLessonId, StringComparison.OrdinalIgnoreCase));
                if (definition?.LinkedAssignmentIds is null || definition.LinkedAssignmentIds.Count == 0)
                {
                    return lesson;
                }

                var linkedAssignmentIds = definition.LinkedAssignmentIds
                    .Select(sourceAssignmentId => assignments.FirstOrDefault(assignment =>
                        string.Equals(assignment.SourceAssignmentId, sourceAssignmentId, StringComparison.OrdinalIgnoreCase))?.Id ?? Guid.Empty)
                    .Where(id => id != Guid.Empty)
                    .ToArray();

                return lesson with { LinkedAssignmentIds = linkedAssignmentIds };
            })
            .ToArray();
    }

    private static ModulePackModule ToModulePackModule(LearningModule module, string termName)
    {
        return new ModulePackModule(
            module.SourceModuleId,
            module.SequenceOrder,
            module.Title,
            module.Description,
            termName,
            module.EstimatedLength,
            module.Instructions,
            module.LearningObjectiveItems
                .Select(item => new ModulePackObjective(item.Text, item.LinkedCourseObjective))
                .ToArray(),
            module.ResourceItems
                .Select(item => new ModulePackResource(item.Name, item.Link, item.FilePath, item.IsPhysicalResource))
                .ToArray(),
            module.AssignmentEvidencePlaceholder,
            module.Status,
            module.Lessons
                .OrderBy(lesson => lesson.SequenceOrder)
                .Select(lesson => new ModulePackItemReference(lesson.SourceLessonId, lesson.Title, lesson.SequenceOrder))
                .ToArray(),
            module.Assignments
                .OrderBy(assignment => assignment.SequenceOrder)
                .Select(assignment => new ModulePackItemReference(assignment.SourceAssignmentId, assignment.Title, assignment.SequenceOrder))
                .ToArray());
    }

    private static LearningModule BuildImportedModule(
        Guid courseId,
        ModulePackModule module,
        int sequenceOrder,
        SchoolYear? schoolYear)
    {
        return new LearningModule(
            Guid.NewGuid(),
            courseId,
            RepairSourceId(module.SourceModuleId, module.Title, sequenceOrder),
            sequenceOrder,
            module.Title,
            module.Description,
            module.EstimatedLength,
            module.Instructions,
            "",
            Lines((module.LearningObjectives ?? []).Select(item => item.Text)),
            Lines((module.Resources ?? []).Select(item => item.Name)),
            module.AssignmentEvidencePlaceholder,
            module.Status,
            ResolveTermId(module.TermName, schoolYear),
            (module.LearningObjectives ?? [])
                .Select(item => new ModuleLearningObjective(item.Text, item.LinkedCourseObjective))
                .ToArray(),
            (module.Resources ?? [])
                .Select(item => new ModuleResource(item.Name, item.Link, item.FilePath, item.IsPhysicalResource))
                .ToArray());
    }

    private static Guid? ResolveTermId(string termName, SchoolYear? schoolYear)
    {
        if (string.IsNullOrWhiteSpace(termName) || schoolYear is null)
        {
            return null;
        }

        return schoolYear.Terms.FirstOrDefault(term =>
            string.Equals(term.Name, termName, StringComparison.OrdinalIgnoreCase))?.Id;
    }

    private static IReadOnlyList<Guid> MapAssignmentLessonLinks(
        IReadOnlyList<string> sourceLessonIds,
        IReadOnlyList<Lesson> lessons)
    {
        return sourceLessonIds
            .Select(sourceLessonId => lessons.FirstOrDefault(lesson =>
                string.Equals(lesson.SourceLessonId, sourceLessonId, StringComparison.OrdinalIgnoreCase))?.Id ?? Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToArray();
    }

    private static IReadOnlyList<Guid> MapAssignmentLessonLinks(
        IReadOnlyList<Guid> packLessonIds,
        IReadOnlyList<Lesson> packLessons,
        IReadOnlyList<Lesson> existingLessons)
    {
        var sourceIds = packLessonIds
            .Select(packLessonId => packLessons.FirstOrDefault(lesson => lesson.Id == packLessonId)?.SourceLessonId ?? "")
            .Where(sourceId => !string.IsNullOrWhiteSpace(sourceId))
            .ToArray();

        return sourceIds
            .Select(sourceId => existingLessons.FirstOrDefault(lesson =>
                string.Equals(lesson.SourceLessonId, sourceId, StringComparison.OrdinalIgnoreCase))?.Id ?? Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToArray();
    }

    private static Guid? ResolveTermId(int? termNumber, SchoolYear? schoolYear)
    {
        if (termNumber is null || schoolYear is null)
        {
            return null;
        }

        return schoolYear.Terms
            .OrderBy(term => term.StartDate)
            .ElementAtOrDefault(termNumber.Value - 1)
            ?.Id;
    }

    private static bool ModuleUsesLegacyText(LearningModule module)
    {
        return module.LearningObjectiveItems.Count == 0 ||
            module.ResourceItems.Count == 0 ||
            module.LearningObjectiveItems.All(item => string.IsNullOrWhiteSpace(item.LinkedCourseObjective));
    }

    private static IReadOnlyList<RequirementMapping> MergeImportedMappings(
        Course course,
        CourseTemplateOptionDefinition option,
        IReadOnlyList<RequirementArea> areas)
    {
        var knownAreaIds = areas.Select(area => area.Id).ToHashSet();
        var mappings = course.RequirementMappings
            .Where(mapping => knownAreaIds.Contains(mapping.RequirementAreaId))
            .ToList();

        foreach (var packMapping in BuildMappings(course.Id, option, areas))
        {
            if (mappings.Any(mapping => mapping.RequirementAreaId == packMapping.RequirementAreaId))
            {
                continue;
            }

            mappings.Add(packMapping);
        }

        return mappings
            .OrderBy(mapping => AreaOrder(mapping.RequirementAreaId, areas))
            .ThenBy(mapping => mapping.CoverageLevel)
            .ToArray();
    }

    private static bool MappingsMatch(
        IReadOnlyList<RequirementMapping> current,
        IReadOnlyList<RequirementMapping> updated)
    {
        if (current.Count != updated.Count)
        {
            return false;
        }

        var currentKeys = current
            .Select(MappingKey)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var updatedKeys = updated
            .Select(MappingKey)
            .Order(StringComparer.Ordinal)
            .ToArray();

        return currentKeys.SequenceEqual(updatedKeys, StringComparer.Ordinal);
    }

    private static string MappingKey(RequirementMapping mapping)
    {
        return $"{mapping.RequirementAreaId:N}:{mapping.CoverageLevel}:{mapping.Notes}";
    }

    private async Task<OperationResult<CourseImportResult>> ImportSingleCoursePackEnvelopeAsync(
        SingleCoursePackEnvelope envelope,
        Guid? studentId,
        CancellationToken cancellationToken,
        string sourcePackId = "imported-coursepack",
        bool updateExisting = false)
    {
        var studentResult = await ResolveStudentAsync(studentId, cancellationToken);
        if (!studentResult.Succeeded || studentResult.Value is null)
        {
            return OperationResult<CourseImportResult>.Failure(studentResult.Errors.ToArray());
        }

        var schoolYear = await repository.GetSchoolYearAsync(cancellationToken);
        if (schoolYear is null)
        {
            return OperationResult<CourseImportResult>.Failure("Create a school year before importing a course.");
        }

        try
        {
            var effectiveSourcePackId = PackIdentityKey(envelope.SourceIdentity, sourcePackId);
            var existingCourse = updateExisting
                ? (await repository.GetCoursesAsync(cancellationToken)).FirstOrDefault(course =>
                    course.StudentId == studentResult.Value.Id &&
                    !course.IsArchived &&
                    string.Equals(course.SourcePackId, effectiveSourcePackId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(course.SourceTemplateId, RepairSourceId(envelope.Course.SourceCourseId, envelope.Course.Title, 1), StringComparison.OrdinalIgnoreCase))
                : null;
            var courseId = existingCourse?.Id ?? Guid.NewGuid();
            var areas = await repository.GetRequirementAreasAsync(cancellationToken);
            var sourceCourseId = RepairSourceId(envelope.Course.SourceCourseId, envelope.Course.Title, 1);
            var importedDescription = envelope.Course.Description.ToDomain();
            var importedPlan = envelope.Course.CurriculumPlan.ToDomain();
            var course = existingCourse is null
                ? new Course(
                    courseId,
                    studentResult.Value.Id,
                    schoolYear.Id,
                    envelope.Course.Title,
                    NormalizeSubjectAreas(envelope.Course.SubjectAreas),
                    envelope.Course.Duration,
                    envelope.Course.PlannedCreditValue,
                    effectiveSourcePackId,
                    sourceCourseId,
                    importedDescription,
                    importedPlan,
                    BuildSingleCourseMappings(courseId, envelope.Course.RequirementMappings, areas),
                    [])
                : new Course(
                    existingCourse.Id,
                    existingCourse.StudentId,
                    existingCourse.SchoolYearId,
                    string.IsNullOrWhiteSpace(existingCourse.Title) ? envelope.Course.Title : existingCourse.Title,
                    existingCourse.SubjectAreas.Count == 0 ? NormalizeSubjectAreas(envelope.Course.SubjectAreas) : existingCourse.SubjectAreas,
                    existingCourse.Duration,
                    existingCourse.PlannedCreditValue,
                    effectiveSourcePackId,
                    sourceCourseId,
                    MergeDescription(existingCourse.Description, importedDescription),
                    MergeCurriculumPlan(existingCourse.CurriculumPlan, importedPlan),
                    MergeSingleCourseMappings(existingCourse, envelope.Course.RequirementMappings, areas),
                    existingCourse.Modules);

            await repository.SaveCourseAsync(course, cancellationToken);
            CourseNavigationChanged?.Invoke();
            return OperationResult<CourseImportResult>.Success(new CourseImportResult(course.Id, course.Title));
        }
        catch (DomainException ex)
        {
            return OperationResult<CourseImportResult>.Failure(ex.Message);
        }
    }

    private async Task<OperationResult<ModulePackImportResult>> UpsertModulePackIntoCourseAsync(
        Guid courseId,
        byte[] content,
        CancellationToken cancellationToken)
    {
        var parsed = ParseModulePackFile(content);
        if (!parsed.Succeeded || parsed.Value is null)
        {
            return OperationResult<ModulePackImportResult>.Failure(parsed.Errors.ToArray());
        }

        var course = await repository.GetCourseAsync(courseId, cancellationToken);
        if (course is null)
        {
            return OperationResult<ModulePackImportResult>.Failure("Course was not found.");
        }

        var schoolYear = await repository.GetSchoolYearAsync(cancellationToken);
        try
        {
            var existing = course.Modules.FirstOrDefault(module =>
                !string.IsNullOrWhiteSpace(module.SourceModuleId) &&
                string.Equals(
                    module.SourceModuleId,
                    RepairSourceId(parsed.Value.Module.SourceModuleId, parsed.Value.Module.Title, parsed.Value.Module.SequenceOrder),
                    StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                var module = BuildImportedModule(course.Id, parsed.Value.Module, course.Modules.Count + 1, schoolYear);
                await repository.SaveCourseAsync(course.WithModules(course.Modules.Concat([module]).ToArray()), cancellationToken);
                CourseNavigationChanged?.Invoke();
                return OperationResult<ModulePackImportResult>.Success(new ModulePackImportResult(module.Id, module.Title));
            }

            var packModule = BuildImportedModule(course.Id, parsed.Value.Module, existing.SequenceOrder, schoolYear);
            var updated = existing with
            {
                Title = string.IsNullOrWhiteSpace(existing.Title) ? packModule.Title : existing.Title,
                Description = string.IsNullOrWhiteSpace(existing.Description) ? packModule.Description : existing.Description,
                EstimatedLength = string.IsNullOrWhiteSpace(existing.EstimatedLength) ? packModule.EstimatedLength : existing.EstimatedLength,
                Instructions = string.IsNullOrWhiteSpace(existing.Instructions) ? packModule.Instructions : existing.Instructions,
                LearningObjectives = string.IsNullOrWhiteSpace(existing.LearningObjectives) ? packModule.LearningObjectives : existing.LearningObjectives,
                Resources = string.IsNullOrWhiteSpace(existing.Resources) ? packModule.Resources : existing.Resources,
                AssignmentEvidencePlaceholder = string.IsNullOrWhiteSpace(existing.AssignmentEvidencePlaceholder) ? packModule.AssignmentEvidencePlaceholder : existing.AssignmentEvidencePlaceholder,
                TermId = existing.TermId ?? packModule.TermId,
                LearningObjectiveItems = existing.LearningObjectiveItems.Count == 0 ? packModule.LearningObjectiveItems : existing.LearningObjectiveItems,
                ResourceItems = existing.ResourceItems.Count == 0 ? packModule.ResourceItems : existing.ResourceItems
            };
            await SaveModuleAsync(course, updated, cancellationToken);
            CourseNavigationChanged?.Invoke();
            return OperationResult<ModulePackImportResult>.Success(new ModulePackImportResult(updated.Id, updated.Title));
        }
        catch (DomainException ex)
        {
            return OperationResult<ModulePackImportResult>.Failure(ex.Message);
        }
    }

    private async Task<OperationResult<LessonPackImportResult>> ImportMissingLessonPackAsync(
        Guid courseId,
        Guid moduleId,
        byte[] content,
        CancellationToken cancellationToken)
    {
        var parsed = ParseLessonPackFile(content);
        if (!parsed.Succeeded || parsed.Value is null)
        {
            return OperationResult<LessonPackImportResult>.Failure(parsed.Errors.ToArray());
        }

        var course = await repository.GetCourseAsync(courseId, cancellationToken);
        var module = course?.Modules.FirstOrDefault(item => item.Id == moduleId);
        if (course is null || module is null)
        {
            return OperationResult<LessonPackImportResult>.Failure("Learning module was not found.");
        }

        try
        {
            var nextOrder = module.Lessons.Count + 1;
            var existingSourceIds = module.Lessons
                .Select(lesson => lesson.SourceLessonId)
                .Where(sourceId => !string.IsNullOrWhiteSpace(sourceId))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var importedLessons = parsed.Value.Lessons
                .Where(lesson => !existingSourceIds.Contains(RepairSourceId(lesson.SourceLessonId, lesson.Title, lesson.SequenceOrder)))
                .OrderBy(lesson => lesson.SequenceOrder <= 0 ? int.MaxValue : lesson.SequenceOrder)
                .ThenBy(lesson => lesson.Title, StringComparer.OrdinalIgnoreCase)
                .Select(lesson => BuildImportedLesson(module.Id, lesson, nextOrder++, existingSourceIds, module.Assignments))
                .ToArray();

            if (importedLessons.Length > 0)
            {
                await SaveModuleAsync(course, module.WithLessons(module.Lessons.Concat(importedLessons).ToArray()), cancellationToken);
                CourseNavigationChanged?.Invoke();
            }

            return OperationResult<LessonPackImportResult>.Success(new LessonPackImportResult(importedLessons.Length));
        }
        catch (DomainException ex)
        {
            return OperationResult<LessonPackImportResult>.Failure(ex.Message);
        }
    }

    private async Task<OperationResult<AssignmentPackImportResult>> ImportMissingAssignmentPackAsync(
        Guid courseId,
        Guid moduleId,
        byte[] content,
        CancellationToken cancellationToken)
    {
        var parsed = ParseAssignmentPackFile(content);
        if (!parsed.Succeeded || parsed.Value is null)
        {
            return OperationResult<AssignmentPackImportResult>.Failure(parsed.Errors.ToArray());
        }

        var course = await repository.GetCourseAsync(courseId, cancellationToken);
        var module = course?.Modules.FirstOrDefault(item => item.Id == moduleId);
        if (course is null || module is null)
        {
            return OperationResult<AssignmentPackImportResult>.Failure("Learning module was not found.");
        }

        try
        {
            var nextOrder = module.Assignments.Count + 1;
            var existingSourceIds = module.Assignments
                .Select(assignment => assignment.SourceAssignmentId)
                .Where(sourceId => !string.IsNullOrWhiteSpace(sourceId))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var importedAssignments = parsed.Value.Assignments
                .Where(assignment => !existingSourceIds.Contains(RepairSourceId(assignment.SourceAssignmentId, assignment.Title, assignment.SequenceOrder)))
                .OrderBy(assignment => assignment.SequenceOrder <= 0 ? int.MaxValue : assignment.SequenceOrder)
                .ThenBy(assignment => assignment.Title, StringComparer.OrdinalIgnoreCase)
                .Select(assignment => BuildImportedAssignment(module, assignment, nextOrder++, existingSourceIds))
                .ToArray();

            if (importedAssignments.Length > 0)
            {
                await SaveModuleAsync(course, module.WithAssignments(module.Assignments.Concat(importedAssignments).ToArray()), cancellationToken);
                CourseNavigationChanged?.Invoke();
            }

            return OperationResult<AssignmentPackImportResult>.Success(new AssignmentPackImportResult(importedAssignments.Length));
        }
        catch (DomainException ex)
        {
            return OperationResult<AssignmentPackImportResult>.Failure(ex.Message);
        }
    }

    private static IReadOnlyList<RequirementMapping> BuildSingleCourseMappings(
        Guid courseId,
        IReadOnlyList<SingleCourseRequirementMapping> mappings,
        IReadOnlyList<RequirementArea> areas)
    {
        return mappings
            .Select(mapping => TryBuildMapping(
                courseId,
                new CourseTemplateRequirementMapping(
                    mapping.RequirementAreaView,
                    mapping.RequirementAreaName,
                    mapping.CoverageLevel,
                    mapping.Notes),
                areas))
            .Where(mapping => mapping is not null)
            .Select(mapping => mapping!)
            .ToArray();
    }

    private static IReadOnlyList<RequirementMapping> MergeSingleCourseMappings(
        Course course,
        IReadOnlyList<SingleCourseRequirementMapping> mappings,
        IReadOnlyList<RequirementArea> areas)
    {
        var knownAreaIds = areas.Select(area => area.Id).ToHashSet();
        var merged = course.RequirementMappings
            .Where(mapping => knownAreaIds.Contains(mapping.RequirementAreaId))
            .ToList();
        foreach (var mapping in BuildSingleCourseMappings(course.Id, mappings, areas))
        {
            if (merged.Any(existing => existing.RequirementAreaId == mapping.RequirementAreaId))
            {
                continue;
            }

            merged.Add(mapping);
        }

        return merged
            .OrderBy(mapping => AreaOrder(mapping.RequirementAreaId, areas))
            .ThenBy(mapping => mapping.CoverageLevel)
            .ToArray();
    }

    private static SingleCoursePackEnvelope BuildSingleCoursePackEnvelope(
        CourseTemplateOptionDefinition option,
        string sourceCourseId,
        SchoolYear? schoolYear,
        DateTimeOffset downloadedAtUtc,
        PackSourceIdentity sourceIdentity)
    {
        return new SingleCoursePackEnvelope(
            "homeschool-manager.coursepack",
            2,
            downloadedAtUtc,
            "json",
            "Single-course course packs include course details and module references only. Module, lesson, and assignment bodies live beside this file in a course plan bundle.",
            new SingleCoursePackCourse(
                sourceCourseId,
                option.Title,
                option.SubjectAreas,
                option.Duration,
                option.PlannedCreditValue,
                CoursePackDescription.FromDomain(option.Description),
                CoursePackCurriculumPlan.FromDomain(option.CurriculumPlan),
                option.RequirementMappings
                    .Select(mapping => new SingleCourseRequirementMapping(
                        mapping.RequirementAreaView,
                        mapping.RequirementAreaName,
                        mapping.CoverageLevel,
                        mapping.Notes))
                    .ToArray(),
                option.Modules
                    .OrderBy(module => module.SequenceOrder)
                    .Select(module => new CourseModuleReference(
                        module.ModuleId,
                        module.Title,
                        module.SequenceOrder,
                        ResolveTermName(module.TermNumber, schoolYear)))
                    .ToArray()),
            sourceIdentity,
            false);
    }

    private static ModulePackEnvelope BuildModulePackEnvelope(
        string courseTitle,
        CourseTemplateModuleDefinition module,
        SchoolYear? schoolYear,
        DateTimeOffset downloadedAtUtc,
        PackSourceIdentity sourceIdentity)
    {
        return new ModulePackEnvelope(
            "homeschool-manager.modulepack",
            1,
            downloadedAtUtc,
            "json",
            "Future module packs with attached files should use a zip archive containing this JSON plus files.",
            $"{courseTitle} - {module.Title}",
            $"Module shell from {courseTitle}. Import lessonpack and assignmentpack files from this folder to restore details.",
            new ModulePackModule(
                module.ModuleId,
                module.SequenceOrder,
                module.Title,
                module.Description,
                ResolveTermName(module.TermNumber, schoolYear),
                module.EstimatedLength,
                module.Instructions,
                module.LearningObjectives
                    .Select(objective => new ModulePackObjective(objective.Text, objective.LinkedCourseObjective))
                    .ToArray(),
                module.Resources
                    .Select(resource => new ModulePackResource(resource.Name, resource.Link, "", resource.IsPhysicalResource))
                    .ToArray(),
                module.AssignmentEvidencePlaceholder,
                module.Status,
                module.Lessons
                    .OrderBy(lesson => lesson.SequenceOrder)
                    .Select(lesson => new ModulePackItemReference(lesson.LessonId, lesson.Title, lesson.SequenceOrder))
                    .ToArray(),
                module.Assignments
                    .OrderBy(assignment => assignment.SequenceOrder)
                    .Select(assignment => new ModulePackItemReference(assignment.AssignmentId, AssignmentTitleForPack(assignment), assignment.SequenceOrder))
                    .ToArray()),
            sourceIdentity,
            false);
    }

    private static LessonPackEnvelope BuildLessonPackEnvelope(
        string courseTitle,
        CourseTemplateModuleDefinition module,
        DateTimeOffset downloadedAtUtc,
        PackSourceIdentity sourceIdentity)
    {
        return new LessonPackEnvelope(
            "homeschool-manager.lessonpack",
            1,
            downloadedAtUtc,
            "json",
            "Future lesson packs with attached files should use a zip archive containing this JSON plus files.",
            $"{courseTitle} - {module.Title} Lessons",
            $"Lessons from the {module.Title} module.",
            module.Lessons
                .OrderBy(lesson => lesson.SequenceOrder)
                .Select(lesson => new LessonPackLesson(
                    lesson.LessonId,
                    lesson.SequenceOrder,
                    lesson.Title,
                    lesson.IntroductoryText,
                    lesson.LinkedModuleObjective,
                    lesson.LessonType,
                    lesson.EstimatedMinutes,
                    lesson.SuggestedDays,
                    lesson.DifficultyLevel,
                    lesson.SubjectAreas ?? [],
                    lesson.Tags ?? [],
                    lesson.Prerequisites ?? [],
                    lesson.LearningObjectives ?? [],
                    lesson.StandardsAlignments ?? [],
                    lesson.SuccessCriteria ?? [],
                    lesson.LessonSteps ?? [],
                    lesson.Resources
                        .Select(resource => new LessonPackResource(
                            resource.Name,
                            resource.Type,
                            resource.Url,
                            "",
                            resource.IsPhysicalResource,
                            resource.SourceNote,
                            resource.Required,
                            resource.EstimatedMinutes,
                            resource.StudentInstructions,
                            resource.NotesPrompt,
                            resource.Citation,
                            resource.OfflineAvailable,
                            resource.License))
                        .ToArray(),
                    lesson.ProblemSets ?? [],
                    lesson.PortfolioConnections ?? [],
                    lesson.Rubric,
                    lesson.ReflectionPrompts ?? [],
                    lesson.InstructorNotes,
                    lesson.LinkedAssignmentIds ?? [],
                    []))
                .ToArray(),
            sourceIdentity,
            false);
    }

    private static AssignmentPackEnvelope BuildAssignmentPackEnvelope(
        string courseTitle,
        CourseTemplateModuleDefinition module,
        DateTimeOffset downloadedAtUtc,
        PackSourceIdentity sourceIdentity)
    {
        return new AssignmentPackEnvelope(
            "homeschool-manager.assignmentpack",
            1,
            downloadedAtUtc,
            "json",
            "Future assignment packs with attached files should use a zip archive containing this JSON plus files.",
            $"{courseTitle} - {module.Title} Assignments",
            $"Assignments from the {module.Title} module.",
            module.Assignments
                .OrderBy(assignment => assignment.SequenceOrder)
                .Select(assignment =>
                {
                    var variant = assignment.Variants.FirstOrDefault(item => item.MethodProfile == InstructionalMethodProfile.Hybrid)
                        ?? assignment.Variants.First();
                    return new AssignmentPackAssignment(
                        assignment.AssignmentId,
                        assignment.SequenceOrder,
                        variant.Title,
                        variant.Type,
                        variant.MethodProfile,
                        variant.Instructions,
                        variant.EstimatedEffort,
                        variant.DueTimingLabel,
                        null,
                        variant.LinkedModuleObjectives,
                        variant.LinkedLessonIds,
                        [],
                        variant.RequiredOutput,
                        variant.ParentNotes,
                        variant.IsPortfolioCandidate,
                        variant.PlannedPoints,
                        variant.PlannedWeight,
                        variant.Status,
                        variant.AssignmentSummary,
                        variant.StudentFacingGoal,
                        variant.EstimatedMinutesMin,
                        variant.EstimatedMinutesMax,
                        variant.RequiredDeliverables,
                        variant.SubmissionFormats,
                        variant.PortfolioConnection,
                        variant.Rubric,
                        variant.LinkedRubricId,
                        variant.AssessmentSkills,
                        variant.StudentChecklist,
                        variant.Resources,
                        variant.AssignmentSteps,
                        variant.RevisionPolicy,
                        variant.CompletionCriteria,
                        variant.ReflectionPrompts,
                        variant.EvidenceRequirements,
                        variant.Scoring);
                })
                .ToArray(),
            sourceIdentity,
            false);
    }

    private static string AssignmentTitleForPack(CourseTemplateAssignmentDefinition assignment)
    {
        return assignment.Variants.FirstOrDefault(item => item.MethodProfile == InstructionalMethodProfile.Hybrid)?.Title
            ?? assignment.Variants.FirstOrDefault()?.Title
            ?? assignment.AssignmentId;
    }

    private static string RepairSourceId(string sourceId, string title, int sequenceOrder)
    {
        var normalized = string.IsNullOrWhiteSpace(sourceId) ? "" : sourceId.Trim();
        if (!string.IsNullOrWhiteSpace(normalized) &&
            !normalized.StartsWith("sample-", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(normalized, "example", StringComparison.OrdinalIgnoreCase))
        {
            return normalized;
        }

        var titlePart = SafeFileName(title);
        return $"{titlePart}-{Math.Max(sequenceOrder, 1)}";
    }

    private static string ResolveTermName(int? termNumber, SchoolYear? schoolYear)
    {
        if (termNumber is null || schoolYear is null)
        {
            return "";
        }

        return schoolYear.Terms
            .OrderBy(term => term.StartDate)
            .ElementAtOrDefault(termNumber.Value - 1)
            ?.Name ?? "";
    }

    private static string CoursePlanPacingLabel(SchoolYear? schoolYear)
    {
        if (schoolYear is null || schoolYear.Terms.Count <= 1)
        {
            return "Year";
        }

        return schoolYear.Terms.Any(term => term.Name.Contains("semester", StringComparison.OrdinalIgnoreCase))
            ? "Semester"
            : "Term";
    }

    private static void WriteZipJson<T>(ZipArchive archive, string path, T value)
    {
        var entry = archive.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(JsonSerializer.Serialize(value, CoursePackJsonOptions));
    }

    private static byte[] ReadZipEntry(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private static string? ReadCoursePlanId(ZipArchive archive)
    {
        var entry = archive.Entries.FirstOrDefault(item =>
            item.FullName.Equals("courseplan.courseplanpack", StringComparison.OrdinalIgnoreCase));
        if (entry is null)
        {
            return null;
        }

        try
        {
            var plan = JsonSerializer.Deserialize<CoursePlanPackEnvelope>(ReadZipEntry(entry), CoursePackJsonOptions);
            if (plan is null)
            {
                return null;
            }

            return PackIdentityKey(plan.SourceIdentity, plan.PlanId);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string ParentFolder(string path)
    {
        var normalized = path.Replace('\\', '/');
        var index = normalized.LastIndexOf('/');
        return index < 0 ? "" : normalized[..index];
    }

    private static OperationResult<SingleCoursePackEnvelope> ParseSingleCoursePackFile(byte[] content)
    {
        if (content.Length == 0)
        {
            return OperationResult<SingleCoursePackEnvelope>.Failure("Choose a .coursepack file before importing.");
        }

        try
        {
            var envelope = JsonSerializer.Deserialize<SingleCoursePackEnvelope>(content, CoursePackJsonOptions);
            if (envelope is null)
            {
                return OperationResult<SingleCoursePackEnvelope>.Failure("The course pack file could not be read.");
            }

            if (!string.Equals(envelope.Format, "homeschool-manager.coursepack", StringComparison.Ordinal))
            {
                return OperationResult<SingleCoursePackEnvelope>.Failure("The file is not a recognized course pack.");
            }

            if (envelope.FormatVersion != 2)
            {
                return OperationResult<SingleCoursePackEnvelope>.Failure("This course pack format version is not supported for single-course import.");
            }

            if (!string.Equals(envelope.PackageMode, "json", StringComparison.OrdinalIgnoreCase))
            {
                return OperationResult<SingleCoursePackEnvelope>.Failure("Choose a single-course .coursepack JSON file or a full course plan .zip file.");
            }

            var validationErrors = ValidateSingleCoursePack(envelope);
            return validationErrors.Count > 0
                ? OperationResult<SingleCoursePackEnvelope>.Failure(validationErrors.ToArray())
                : OperationResult<SingleCoursePackEnvelope>.Success(envelope);
        }
        catch (JsonException)
        {
            return OperationResult<SingleCoursePackEnvelope>.Failure("The course pack file must contain valid JSON.");
        }
    }

    private static IReadOnlyList<string> ValidateSingleCoursePack(SingleCoursePackEnvelope envelope)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(envelope.Course.Title))
        {
            errors.Add("Course title is required.");
        }

        if (envelope.Course.PlannedCreditValue <= 0)
        {
            errors.Add("Course credit value must be greater than zero.");
        }

        if (envelope.Course.ModuleReferences.Any(module => string.IsNullOrWhiteSpace(module.Title)))
        {
            errors.Add("Every module reference must include a title.");
        }

        return errors;
    }

    private static bool ModulesMatch(
        IReadOnlyList<LearningModule> current,
        IReadOnlyList<LearningModule> updated)
    {
        if (current.Count != updated.Count)
        {
            return false;
        }

        var currentKeys = current.Select(ModuleKey).Order(StringComparer.Ordinal).ToArray();
        var updatedKeys = updated.Select(ModuleKey).Order(StringComparer.Ordinal).ToArray();
        return currentKeys.SequenceEqual(updatedKeys, StringComparer.Ordinal);
    }

    private static string ModuleKey(LearningModule module)
    {
        var objectiveKey = string.Join(";", module.LearningObjectiveItems.Select(item => $"{item.Text}->{item.LinkedCourseObjective}"));
        var resourceKey = string.Join(";", module.ResourceItems.Select(item => $"{item.Name}->{item.Link}->{item.FilePath}->{item.IsPhysicalResource}"));
        var lessonKey = string.Join(";", module.Lessons.Select(LessonKey));
        var assignmentKey = string.Join(";", module.Assignments.Select(AssignmentKey));
        var termKey = module.TermId.HasValue ? module.TermId.Value.ToString("N") : "";
        return $"{module.Id:N}:{module.SourceModuleId}:{module.SequenceOrder}:{module.Title}:{module.Description}:{termKey}:{module.EstimatedLength}:{module.Instructions}:{module.MajorTopics}:{module.LearningObjectives}:{objectiveKey}:{module.Resources}:{resourceKey}:{lessonKey}:{assignmentKey}:{module.AssignmentEvidencePlaceholder}:{module.Status}";
    }

    private static string LessonKey(Lesson lesson)
    {
        var subjectKey = string.Join(";", lesson.SubjectAreas);
        var tagKey = string.Join(";", lesson.Tags);
        var prerequisiteKey = string.Join(";", lesson.Prerequisites);
        var objectiveKey = string.Join(";", lesson.LearningObjectives.Select(objective => $"{objective.ObjectiveId}->{objective.BloomLevel}->{objective.Text}"));
        var standardKey = string.Join(";", lesson.StandardsAlignments.Select(standard => $"{standard.Framework}->{standard.Code}->{standard.Description}"));
        var criteriaKey = string.Join(";", lesson.SuccessCriteria);
        var stepKey = string.Join(";", lesson.LessonSteps.Select(step => $"{step.StepOrder}->{step.Title}->{step.StepType}->{step.Instructions}->{step.EstimatedMinutes}->{step.Required}"));
        var resourceKey = string.Join(";", lesson.Resources.Select(resource => $"{resource.Name}->{resource.Type}->{resource.Url}->{resource.FilePath}->{resource.IsPhysicalResource}->{resource.SourceNote}->{resource.Required}->{resource.EstimatedMinutes}->{resource.StudentInstructions}->{resource.NotesPrompt}->{resource.Citation}->{resource.OfflineAvailable}->{resource.License}"));
        var problemKey = string.Join(";", lesson.ProblemSets.Select(problemSet => $"{problemSet.ProblemSetId}->{problemSet.Title}->{problemSet.Instructions}->{problemSet.EstimatedMinutes}->{string.Join(",", problemSet.Problems)}"));
        var portfolioKey = string.Join(";", lesson.PortfolioConnections.Select(connection => $"{connection.PortfolioSection}->{connection.ArtifactTitle}->{connection.ArtifactPurpose}->{string.Join(",", connection.CrossCourseLinks)}->{connection.ReuseInstructions}"));
        var reflectionKey = string.Join(";", lesson.ReflectionPrompts);
        var assignmentKey = string.Join(";", lesson.LinkedAssignmentIds.Order());
        return $"{lesson.Id:N}:{lesson.SourceLessonId}:{lesson.SequenceOrder}:{lesson.Title}:{lesson.IntroductoryText}:{lesson.LinkedModuleObjective}:{lesson.LessonType}:{lesson.EstimatedMinutes}:{lesson.SuggestedDays}:{lesson.DifficultyLevel}:{subjectKey}:{tagKey}:{prerequisiteKey}:{objectiveKey}:{standardKey}:{criteriaKey}:{stepKey}:{resourceKey}:{problemKey}:{portfolioKey}:{lesson.Rubric}:{reflectionKey}:{lesson.InstructorNotes}:{assignmentKey}";
    }

    private static string AssignmentKey(ModuleAssignment assignment)
    {
        var objectiveKey = string.Join(";", assignment.LinkedModuleObjectives);
        var lessonKey = string.Join(";", assignment.LinkedLessonIds.Order());
        var deliverableKey = string.Join(";", assignment.RequiredDeliverables);
        var formatKey = string.Join(";", assignment.SubmissionFormats);
        var skillKey = string.Join(";", assignment.AssessmentSkills);
        var checklistKey = string.Join(";", assignment.StudentChecklist);
        var resourceKey = string.Join(";", assignment.Resources.Select(resource => $"{resource.Name}->{resource.Type}->{resource.Url}->{resource.FilePath}->{resource.IsPhysicalResource}->{resource.Required}->{resource.StudentInstructions}->{resource.SourceNote}->{resource.Citation}"));
        var stepKey = string.Join(";", assignment.AssignmentSteps.Select(step => $"{step.StepOrder}->{step.Title}->{step.Instructions}->{step.EstimatedMinutes}"));
        var reflectionKey = string.Join(";", assignment.ReflectionPrompts);
        return $"{assignment.Id:N}:{assignment.SourceAssignmentId}:{assignment.SequenceOrder}:{assignment.Title}:{assignment.Type}:{assignment.MethodProfile}:{assignment.AssignmentSummary}:{assignment.StudentFacingGoal}:{assignment.Instructions}:{assignment.EstimatedEffort}:{assignment.EstimatedMinutesMin}:{assignment.EstimatedMinutesMax}:{assignment.DueTimingLabel}:{assignment.DueDate}:{objectiveKey}:{lessonKey}:{assignment.RequiredOutput}:{deliverableKey}:{formatKey}:{assignment.PortfolioConnection}:{assignment.Rubric}:{assignment.LinkedRubricId}:{skillKey}:{checklistKey}:{resourceKey}:{stepKey}:{assignment.RevisionPolicy}:{assignment.CompletionCriteria}:{reflectionKey}:{assignment.EvidenceRequirements}:{assignment.Scoring}:{assignment.ParentNotes}:{assignment.IsPortfolioCandidate}:{assignment.PlannedPoints}:{assignment.PlannedWeight}:{assignment.Status}";
    }

    private static int AreaOrder(Guid requirementAreaId, IReadOnlyList<RequirementArea> areas)
    {
        var area = areas.FirstOrDefault(item => item.Id == requirementAreaId);
        return area is null ? 99 : SourceOrder(area.View);
    }

    private static CourseTemplateOptionDefinition? FindSourceOption(
        Course course,
        IReadOnlyList<CoursePackDefinition> availablePacks)
    {
        if (string.IsNullOrWhiteSpace(course.SourcePackId) || string.IsNullOrWhiteSpace(course.SourceTemplateId))
        {
            return null;
        }

        var pack = availablePacks.FirstOrDefault(item =>
            string.Equals(item.Id, course.SourcePackId, StringComparison.OrdinalIgnoreCase));
        var template = pack?.Courses.FirstOrDefault(item =>
            string.Equals(item.TemplateId, course.SourceTemplateId, StringComparison.OrdinalIgnoreCase));
        if (template is null)
        {
            return null;
        }

        return template.Options.FirstOrDefault(option =>
            string.Equals(option.Title, course.Title, StringComparison.OrdinalIgnoreCase))
            ?? template.DefaultOption;
    }

    private static CourseDescription MergeDescription(CourseDescription current, CourseDescription defaults)
    {
        return new CourseDescription(
            FillBlank(current.Description, defaults.Description),
            FillPackDefault(current.InstructionalMethods, defaults.InstructionalMethods, IsLegacyInstructionalDefault),
            FillBlank(current.MajorTopics, defaults.MajorTopics),
            FillPackDefault(current.TextsAndResources, defaults.TextsAndResources, IsLegacyResourceList),
            FillPackDefault(current.AssessmentMethods, defaults.AssessmentMethods, IsLegacyAssessmentDefault),
            FillPackDefault(current.GradingBasis, defaults.GradingBasis, IsLegacyGradingDefault));
    }

    private static CurriculumPlan MergeCurriculumPlan(CurriculumPlan current, CurriculumPlan defaults)
    {
        return new CurriculumPlan(
            FillBlank(current.Goals, defaults.Goals),
            FillPackDefault(current.LearningObjectives, defaults.LearningObjectives, IsLegacyLearningObjectives),
            current.MajorResources,
            FillBlank(current.PlannedSequence, defaults.PlannedSequence),
            FillBlank(current.ParentNotes, defaults.ParentNotes));
    }

    private static string FillPackDefault(string current, string fallback, Func<string, bool> isLegacyDefault)
    {
        if (string.IsNullOrWhiteSpace(current))
        {
            return fallback;
        }

        return isLegacyDefault(current) ? fallback : current;
    }

    private static string FillBlank(string current, string fallback)
    {
        return string.IsNullOrWhiteSpace(current) ? fallback : current;
    }

    private static IReadOnlyList<string> NormalizeSubjectAreas(IReadOnlyList<string> subjectAreas)
    {
        var normalized = subjectAreas
            .Where(subject => !string.IsNullOrWhiteSpace(subject))
            .Select(subject => subject.Trim())
            .ToArray();

        return normalized.Length == 0 ? ["Course"] : normalized;
    }

    private static bool IsLegacyInstructionalDefault(string current)
    {
        return current.StartsWith("Explicit instruction with guided practice", StringComparison.Ordinal);
    }

    private static bool IsLegacyAssessmentDefault(string current)
    {
        return current.StartsWith("Ongoing formative checks", StringComparison.Ordinal);
    }

    private static bool IsLegacyGradingDefault(string current)
    {
        return current.StartsWith("Mastery-aligned letter grade", StringComparison.Ordinal);
    }

    private static bool IsLegacyResourceList(string current)
    {
        return !current.Contains('|') &&
            !current.Contains('\n') &&
            current.Contains(';');
    }

    private static bool IsLegacyLearningObjectives(string current)
    {
        return IsOriginalPackLearningObjectives(current) ||
            IsGenericMathLearningObjectives(current) ||
            IsGenericScienceLearningObjectives(current) ||
            IsGenericLanguageLearningObjectives(current) ||
            IsGenericArtsLearningObjectives(current) ||
            IsGenericFallbackLearningObjectives(current);
    }

    private static bool IsOriginalPackLearningObjectives(string current)
    {
        return current.Contains("Explain major concepts in", StringComparison.OrdinalIgnoreCase) &&
            current.Contains("produce evidence suitable for course records", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGenericMathLearningObjectives(string current)
    {
        return current.Contains("Solve course-level mathematical problems", StringComparison.OrdinalIgnoreCase) &&
            current.Contains("Model quantitative relationships", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGenericScienceLearningObjectives(string current)
    {
        return current.Contains("Explain course-level scientific concepts", StringComparison.OrdinalIgnoreCase) &&
            current.Contains("Use observation, data, simulations", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGenericLanguageLearningObjectives(string current)
    {
        return current.Contains("Use course-level vocabulary and grammar", StringComparison.OrdinalIgnoreCase) &&
            current.Contains("Compare cultural practices", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGenericArtsLearningObjectives(string current)
    {
        return current.Contains("Apply course-specific techniques", StringComparison.OrdinalIgnoreCase) &&
            current.Contains("Build portfolio or performance records", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGenericFallbackLearningObjectives(string current)
    {
        return current.Contains("Explain important concepts, vocabulary, and methods", StringComparison.OrdinalIgnoreCase) &&
            current.Contains("Prepare portfolio-ready evidence", StringComparison.OrdinalIgnoreCase);
    }

    private async Task RefreshRequirementSeedForPackAsync(
        CoursePackDefinition pack,
        CancellationToken cancellationToken)
    {
        if (string.Equals(pack.RequirementJurisdiction, "Michigan", StringComparison.OrdinalIgnoreCase))
        {
            await RefreshMichiganRequirementSeedAsync(cancellationToken);
        }
    }

    private async Task RefreshMichiganRequirementSeedAsync(CancellationToken cancellationToken)
    {
        await MichiganRequirementSeedRefresh.EnsureCurrentAsync(repository, cancellationToken);
    }

    private static OperationResult<CoursePackDefinition> ParseCoursePackFile(byte[] content)
    {
        if (content.Length == 0)
        {
            return OperationResult<CoursePackDefinition>.Failure("Choose a .coursepack file before installing.");
        }

        try
        {
            var envelope = JsonSerializer.Deserialize<CoursePackJsonEnvelope>(content, CoursePackJsonOptions);
            if (envelope is null)
            {
                return OperationResult<CoursePackDefinition>.Failure("The course pack file could not be read.");
            }

            if (!string.Equals(envelope.Format, "homeschool-manager.coursepack", StringComparison.Ordinal))
            {
                return OperationResult<CoursePackDefinition>.Failure("The file is not a recognized course pack.");
            }

            if (envelope.FormatVersion != 1)
            {
                return OperationResult<CoursePackDefinition>.Failure("This course pack format version is not supported.");
            }

            if (!string.Equals(envelope.PackageMode, "json", StringComparison.OrdinalIgnoreCase))
            {
                return OperationResult<CoursePackDefinition>.Failure("Zipped course pack installs are reserved for a later attachment workflow.");
            }

            var validationErrors = ValidateCoursePack(envelope.Pack);
            return validationErrors.Count > 0
                ? OperationResult<CoursePackDefinition>.Failure(validationErrors.ToArray())
                : OperationResult<CoursePackDefinition>.Success(envelope.Pack);
        }
        catch (JsonException)
        {
            return OperationResult<CoursePackDefinition>.Failure("The course pack file must contain valid JSON.");
        }
    }

    private static OperationResult<LessonPackEnvelope> ParseLessonPackFile(byte[] content)
    {
        if (content.Length == 0)
        {
            return OperationResult<LessonPackEnvelope>.Failure("Choose a .lessonpack file before importing.");
        }

        try
        {
            var envelope = JsonSerializer.Deserialize<LessonPackEnvelope>(content, CoursePackJsonOptions);
            if (envelope is null)
            {
                return OperationResult<LessonPackEnvelope>.Failure("The lesson pack file could not be read.");
            }

            if (!string.Equals(envelope.Format, "homeschool-manager.lessonpack", StringComparison.Ordinal))
            {
                return OperationResult<LessonPackEnvelope>.Failure("The file is not a recognized lesson pack.");
            }

            if (envelope.FormatVersion != 1)
            {
                return OperationResult<LessonPackEnvelope>.Failure("This lesson pack format version is not supported.");
            }

            if (!string.Equals(envelope.PackageMode, "json", StringComparison.OrdinalIgnoreCase))
            {
                return OperationResult<LessonPackEnvelope>.Failure("Zipped lesson pack imports are reserved for a later attachment workflow.");
            }

            var validationErrors = ValidateLessonPack(envelope);
            return validationErrors.Count > 0
                ? OperationResult<LessonPackEnvelope>.Failure(validationErrors.ToArray())
                : OperationResult<LessonPackEnvelope>.Success(envelope);
        }
        catch (JsonException ex)
        {
            return OperationResult<LessonPackEnvelope>.Failure(PackJsonFailureText("lesson pack", ex));
        }
    }

    private static OperationResult<AssignmentPackEnvelope> ParseAssignmentPackFile(byte[] content)
    {
        if (content.Length == 0)
        {
            return OperationResult<AssignmentPackEnvelope>.Failure("Choose a .assignmentpack file before importing.");
        }

        try
        {
            var envelope = JsonSerializer.Deserialize<AssignmentPackEnvelope>(content, CoursePackJsonOptions);
            if (envelope is null)
            {
                return OperationResult<AssignmentPackEnvelope>.Failure("The assignment pack file could not be read.");
            }

            if (!string.Equals(envelope.Format, "homeschool-manager.assignmentpack", StringComparison.Ordinal))
            {
                return OperationResult<AssignmentPackEnvelope>.Failure("The file is not a recognized assignment pack.");
            }

            if (envelope.FormatVersion != 1)
            {
                return OperationResult<AssignmentPackEnvelope>.Failure("This assignment pack format version is not supported.");
            }

            if (!string.Equals(envelope.PackageMode, "json", StringComparison.OrdinalIgnoreCase))
            {
                return OperationResult<AssignmentPackEnvelope>.Failure("Zipped assignment pack imports are reserved for a later attachment workflow.");
            }

            var validationErrors = ValidateAssignmentPack(envelope);
            return validationErrors.Count > 0
                ? OperationResult<AssignmentPackEnvelope>.Failure(validationErrors.ToArray())
                : OperationResult<AssignmentPackEnvelope>.Success(envelope);
        }
        catch (JsonException ex)
        {
            return OperationResult<AssignmentPackEnvelope>.Failure(PackJsonFailureText("assignment pack", ex));
        }
    }

    private static OperationResult<ModulePackEnvelope> ParseModulePackFile(byte[] content)
    {
        if (content.Length == 0)
        {
            return OperationResult<ModulePackEnvelope>.Failure("Choose a .modulepack file before importing.");
        }

        try
        {
            var envelope = JsonSerializer.Deserialize<ModulePackEnvelope>(content, CoursePackJsonOptions);
            if (envelope is null)
            {
                return OperationResult<ModulePackEnvelope>.Failure("The module pack file could not be read.");
            }

            if (!string.Equals(envelope.Format, "homeschool-manager.modulepack", StringComparison.Ordinal))
            {
                return OperationResult<ModulePackEnvelope>.Failure("The file is not a recognized module pack.");
            }

            if (envelope.FormatVersion != 1)
            {
                return OperationResult<ModulePackEnvelope>.Failure("This module pack format version is not supported.");
            }

            if (!string.Equals(envelope.PackageMode, "json", StringComparison.OrdinalIgnoreCase))
            {
                return OperationResult<ModulePackEnvelope>.Failure("Zipped module pack imports are reserved for a later attachment workflow.");
            }

            var validationErrors = ValidateModulePack(envelope);
            return validationErrors.Count > 0
                ? OperationResult<ModulePackEnvelope>.Failure(validationErrors.ToArray())
                : OperationResult<ModulePackEnvelope>.Success(envelope);
        }
        catch (JsonException)
        {
            return OperationResult<ModulePackEnvelope>.Failure("The module pack file must contain valid JSON.");
        }
    }

    private static IReadOnlyList<string> ValidateAssignmentPack(AssignmentPackEnvelope envelope)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(envelope.Name))
        {
            errors.Add("Assignment pack name is required.");
        }

        if (envelope.Assignments is null || envelope.Assignments.Count == 0)
        {
            errors.Add("Assignment pack must include at least one assignment.");
        }

        foreach (var assignment in envelope.Assignments ?? [])
        {
            var label = string.IsNullOrWhiteSpace(assignment.SourceAssignmentId) ? assignment.Title : assignment.SourceAssignmentId;
            if (string.IsNullOrWhiteSpace(assignment.Title))
            {
                errors.Add("Every assignment must include a title.");
            }

            if (!Enum.IsDefined(assignment.Type))
            {
                errors.Add($"Assignment '{label}' has a type this app does not recognize.");
            }

            if (!Enum.IsDefined(assignment.MethodProfile))
            {
                errors.Add($"Assignment '{label}' has an instructional method profile this app does not recognize.");
            }

            if (string.IsNullOrWhiteSpace(assignment.Instructions))
            {
                errors.Add($"Assignment '{label}' must include instructions.");
            }

            if (string.IsNullOrWhiteSpace(assignment.RequiredOutput))
            {
                errors.Add($"Assignment '{label}' must include the expected output.");
            }

            if (assignment.PlannedPoints.HasValue && assignment.PlannedPoints.Value < 0)
            {
                errors.Add($"Assignment '{label}' cannot have negative planned points.");
            }

            if (assignment.PlannedWeight.HasValue && assignment.PlannedWeight.Value < 0)
            {
                errors.Add($"Assignment '{label}' cannot have negative planned weight.");
            }

            if (assignment.EstimatedMinutesMin.HasValue && assignment.EstimatedMinutesMin.Value < 0 ||
                assignment.EstimatedMinutesMax.HasValue && assignment.EstimatedMinutesMax.Value < 0)
            {
                errors.Add($"Assignment '{label}' cannot have negative estimated minutes.");
            }

            if (assignment.EstimatedMinutesMin.HasValue &&
                assignment.EstimatedMinutesMax.HasValue &&
                assignment.EstimatedMinutesMax.Value < assignment.EstimatedMinutesMin.Value)
            {
                errors.Add($"Assignment '{label}' has maximum estimated minutes lower than minimum estimated minutes.");
            }

            if (!Enum.IsDefined(assignment.Status))
            {
                errors.Add($"Assignment '{label}' has a status this app does not recognize.");
            }
        }

        return errors;
    }

    private static IReadOnlyList<string> ValidateModulePack(ModulePackEnvelope envelope)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(envelope.Name))
        {
            errors.Add("Module pack name is required.");
        }

        if (envelope.Module is null)
        {
            errors.Add("Module pack must include one module.");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(envelope.Module.Title))
        {
            errors.Add("Module title is required.");
        }

        if (string.IsNullOrWhiteSpace(envelope.Module.Instructions))
        {
            errors.Add("Module instructions are required.");
        }

        if (envelope.Module.LearningObjectives is null || envelope.Module.LearningObjectives.Count == 0)
        {
            errors.Add("Module pack must include at least one module objective.");
        }

        if (!Enum.IsDefined(envelope.Module.Status))
        {
            errors.Add("Module status is not recognized.");
        }

        return errors;
    }

    private static IReadOnlyList<string> ValidateLessonPack(LessonPackEnvelope envelope)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(envelope.Name))
        {
            errors.Add("Lesson pack name is required.");
        }

        if (envelope.Lessons is null || envelope.Lessons.Count == 0)
        {
            errors.Add("Lesson pack must include at least one lesson.");
        }

        foreach (var lesson in envelope.Lessons ?? [])
        {
            var label = string.IsNullOrWhiteSpace(lesson.SourceLessonId) ? lesson.Title : lesson.SourceLessonId;
            if (string.IsNullOrWhiteSpace(lesson.Title))
            {
                errors.Add("Every lesson must include a title.");
            }

            if (string.IsNullOrWhiteSpace(lesson.IntroductoryText))
            {
                errors.Add($"Lesson '{label}' must include introductory text.");
            }

            if (lesson.Resources is null || lesson.Resources.Count == 0)
            {
                errors.Add($"Lesson '{label}' must include at least one resource.");
            }

            foreach (var resource in lesson.Resources ?? [])
            {
                if (string.IsNullOrWhiteSpace(resource.Name))
                {
                    errors.Add($"Lesson '{label}' has a resource without a name.");
                }

                if (!Enum.IsDefined(resource.Type))
                {
                    errors.Add($"Lesson '{label}' has a resource type this app does not recognize.");
                }
            }
        }

        return errors;
    }

    private static IReadOnlyList<string> ValidateCoursePack(CoursePackDefinition pack)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(pack.Id))
        {
            errors.Add("Course pack id is required.");
        }

        if (string.IsNullOrWhiteSpace(pack.Name))
        {
            errors.Add("Course pack name is required.");
        }

        if (string.IsNullOrWhiteSpace(pack.RequirementJurisdiction))
        {
            errors.Add("Course pack requirement jurisdiction is required.");
        }

        if (pack.Courses.Count == 0)
        {
            errors.Add("Course pack must include at least one course.");
        }

        foreach (var template in pack.Courses)
        {
            if (string.IsNullOrWhiteSpace(template.TemplateId))
            {
                errors.Add("Every course template must include a stable template id.");
            }

            if (template.Options.Count == 0)
            {
                errors.Add($"Course template '{template.TemplateId}' must include at least one option.");
            }

            if (template.Options.Count > 0 && ResolveSelectedOption(template, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)) is null)
            {
                errors.Add($"Course template '{template.TemplateId}' has an invalid default option.");
            }
        }

        return errors;
    }

    private static JsonSerializerOptions CreateCoursePackJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static string PackJsonFailureText(string packKind, JsonException exception)
    {
        return exception.Message.Contains("could not be converted", StringComparison.OrdinalIgnoreCase)
            ? $"One {packKind} item uses a value this version of the app does not recognize. Update the app or revise the pack value and try again."
            : $"The {packKind} file contains unreadable JSON. Choose a downloaded pack file and try again.";
    }

    private static string SafeFileName(string value)
    {
        var normalized = Regex.Replace(value.Trim().ToLowerInvariant(), "[^a-z0-9._-]+", "-");
        normalized = normalized.Trim('-', '.', '_');
        return string.IsNullOrWhiteSpace(normalized) ? "course-pack" : normalized;
    }

    private static PackSourceIdentity BuiltInPackIdentity(CoursePackDefinition pack)
    {
        return new PackSourceIdentity(
            DefaultPublisherId,
            pack.Id,
            DefaultPackVersion,
            $"builtin.{pack.Id}");
    }

    private static PackSourceIdentity TemplateIdentity(string packId)
    {
        return new PackSourceIdentity(
            DefaultPublisherId,
            packId,
            "template",
            $"template.{packId}");
    }

    private static PackSourceIdentity SourceIdentityForCourse(Course course)
    {
        var packId = string.IsNullOrWhiteSpace(course.SourcePackId)
            ? "parent-created"
            : course.SourcePackId;
        return new PackSourceIdentity(
            DefaultPublisherId,
            packId,
            "local",
            packId);
    }

    private static string PackIdentityKey(PackSourceIdentity? identity, string fallbackPackId)
    {
        if (!string.IsNullOrWhiteSpace(identity?.SourceNamespace))
        {
            return identity.SourceNamespace.Trim();
        }

        if (!string.IsNullOrWhiteSpace(identity?.PublisherId) &&
            !string.IsNullOrWhiteSpace(identity.PackId))
        {
            return $"{identity.PublisherId.Trim()}.{identity.PackId.Trim()}";
        }

        return string.IsNullOrWhiteSpace(fallbackPackId) ? "imported-pack" : fallbackPackId.Trim();
    }

    private sealed record CoursePackJsonEnvelope(
        string Format,
        int FormatVersion,
        DateTimeOffset DownloadedAtUtc,
        string PackageMode,
        string ArchiveNote,
        CoursePackDefinition Pack);

    private sealed record SingleCoursePackEnvelope(
        string Format,
        int FormatVersion,
        DateTimeOffset DownloadedAtUtc,
        string PackageMode,
        string ArchiveNote,
        SingleCoursePackCourse Course,
        PackSourceIdentity? SourceIdentity = null,
        bool IsTemplate = false);

    private sealed record SingleCoursePackCourse(
        string SourceCourseId,
        string Title,
        IReadOnlyList<string> SubjectAreas,
        CourseDuration Duration,
        decimal PlannedCreditValue,
        CoursePackDescription Description,
        CoursePackCurriculumPlan CurriculumPlan,
        IReadOnlyList<SingleCourseRequirementMapping> RequirementMappings,
        IReadOnlyList<CourseModuleReference> ModuleReferences);

    private sealed record CoursePackDescription(
        string Description,
        string InstructionalMethods,
        string MajorTopics,
        string TextsAndResources,
        string AssessmentMethods,
        string GradingBasis)
    {
        public static CoursePackDescription FromDomain(CourseDescription description)
        {
            return new CoursePackDescription(
                description.Description,
                description.InstructionalMethods,
                description.MajorTopics,
                description.TextsAndResources,
                description.AssessmentMethods,
                description.GradingBasis);
        }

        public CourseDescription ToDomain()
        {
            return new CourseDescription(
                Description,
                InstructionalMethods,
                MajorTopics,
                TextsAndResources,
                AssessmentMethods,
                GradingBasis);
        }
    }

    private sealed record CoursePackCurriculumPlan(
        string Goals,
        string LearningObjectives,
        string MajorResources,
        string PlannedSequence,
        string ParentNotes)
    {
        public static CoursePackCurriculumPlan FromDomain(CurriculumPlan plan)
        {
            return new CoursePackCurriculumPlan(
                plan.Goals,
                plan.LearningObjectives,
                plan.MajorResources,
                plan.PlannedSequence,
                plan.ParentNotes);
        }

        public CurriculumPlan ToDomain()
        {
            return new CurriculumPlan(
                Goals,
                LearningObjectives,
                MajorResources,
                PlannedSequence,
                ParentNotes);
        }
    }

    private sealed record SingleCourseRequirementMapping(
        string RequirementAreaView,
        string RequirementAreaName,
        CoverageLevel CoverageLevel,
        string Notes);

    private sealed record CourseModuleReference(string SourceModuleId, string Title, int SequenceOrder, string TermName);

    private sealed record CoursePlanPackEnvelope(
        string Format,
        int FormatVersion,
        DateTimeOffset DownloadedAtUtc,
        string PackageMode,
        string ArchiveNote,
        string PlanId,
        string Name,
        string Description,
        string Pacing,
        IReadOnlyList<CoursePlanOffering> Offerings,
        PackSourceIdentity? SourceIdentity = null,
        bool IsTemplate = false);

    private sealed record CoursePlanOffering(string SourceCourseId, string CourseTitle, string TermName, int SequenceOrder);

    private sealed class CoursePlanBundleMutableImportResult
    {
        public int CourseCount { get; set; }
        public int ModuleCount { get; set; }
        public int LessonCount { get; set; }
        public int AssignmentCount { get; set; }
    }

    private sealed record LessonPackEnvelope(
        string Format,
        int FormatVersion,
        DateTimeOffset DownloadedAtUtc,
        string PackageMode,
        string ArchiveNote,
        string Name,
        string Description,
        IReadOnlyList<LessonPackLesson> Lessons,
        PackSourceIdentity? SourceIdentity = null,
        bool IsTemplate = false);

    private sealed record LessonPackLesson(
        string SourceLessonId,
        int SequenceOrder,
        string Title,
        string IntroductoryText,
        string LinkedModuleObjective,
        LessonType LessonType,
        int EstimatedMinutes,
        int SuggestedDays,
        LessonDifficultyLevel DifficultyLevel,
        IReadOnlyList<string> SubjectAreas,
        IReadOnlyList<string> Tags,
        IReadOnlyList<string> Prerequisites,
        IReadOnlyList<LessonLearningObjective> LearningObjectives,
        IReadOnlyList<StandardsAlignment> StandardsAlignments,
        IReadOnlyList<string> SuccessCriteria,
        IReadOnlyList<LessonStep> LessonSteps,
        IReadOnlyList<LessonPackResource> Resources,
        IReadOnlyList<LessonProblemSet> ProblemSets,
        IReadOnlyList<LessonPortfolioConnection> PortfolioConnections,
        LessonRubric? Rubric,
        IReadOnlyList<string> ReflectionPrompts,
        LessonInstructorNotes? InstructorNotes,
        IReadOnlyList<string> LinkedAssignmentSourceIds,
        IReadOnlyList<string> LinkedAssignmentTitles);

    private sealed record LessonPackResource(
        string Name,
        LessonResourceType Type,
        string Url,
        string FilePath,
        bool IsPhysicalResource,
        string SourceNote,
        bool Required,
        int EstimatedMinutes,
        string StudentInstructions,
        string NotesPrompt,
        LessonResourceCitation? Citation,
        bool OfflineAvailable,
        string License);

    private sealed record AssignmentPackEnvelope(
        string Format,
        int FormatVersion,
        DateTimeOffset DownloadedAtUtc,
        string PackageMode,
        string ArchiveNote,
        string Name,
        string Description,
        IReadOnlyList<AssignmentPackAssignment> Assignments,
        PackSourceIdentity? SourceIdentity = null,
        bool IsTemplate = false);

    private sealed record AssignmentPackAssignment(
        string SourceAssignmentId,
        int SequenceOrder,
        string Title,
        AssignmentType Type,
        InstructionalMethodProfile MethodProfile,
        string Instructions,
        string EstimatedEffort,
        string DueTimingLabel,
        DateOnly? DueDate,
        IReadOnlyList<string> LinkedModuleObjectives,
        IReadOnlyList<string> LinkedLessonSourceIds,
        IReadOnlyList<string> LinkedLessonTitles,
        string RequiredOutput,
        string ParentNotes,
        bool IsPortfolioCandidate,
        decimal? PlannedPoints,
        decimal? PlannedWeight,
        AssignmentStatus Status,
        string AssignmentSummary = "",
        string StudentFacingGoal = "",
        int? EstimatedMinutesMin = null,
        int? EstimatedMinutesMax = null,
        IReadOnlyList<string>? RequiredDeliverables = null,
        IReadOnlyList<AssignmentSubmissionFormat>? SubmissionFormats = null,
        AssignmentPortfolioConnection? PortfolioConnection = null,
        LessonRubric? Rubric = null,
        string LinkedRubricId = "",
        IReadOnlyList<string>? AssessmentSkills = null,
        IReadOnlyList<string>? StudentChecklist = null,
        IReadOnlyList<AssignmentResource>? Resources = null,
        IReadOnlyList<AssignmentStep>? AssignmentSteps = null,
        AssignmentRevisionPolicy? RevisionPolicy = null,
        AssignmentCompletionCriteria? CompletionCriteria = null,
        IReadOnlyList<string>? ReflectionPrompts = null,
        AssignmentEvidenceRequirements? EvidenceRequirements = null,
        AssignmentScoring? Scoring = null);

    private sealed record ModulePackEnvelope(
        string Format,
        int FormatVersion,
        DateTimeOffset DownloadedAtUtc,
        string PackageMode,
        string ArchiveNote,
        string Name,
        string Description,
        ModulePackModule Module,
        PackSourceIdentity? SourceIdentity = null,
        bool IsTemplate = false);

    private sealed record PackSourceIdentity(
        string PublisherId,
        string PackId,
        string PackVersion,
        string SourceNamespace);

    private sealed record ModulePackModule(
        string SourceModuleId,
        int SequenceOrder,
        string Title,
        string Description,
        string TermName,
        string EstimatedLength,
        string Instructions,
        IReadOnlyList<ModulePackObjective> LearningObjectives,
        IReadOnlyList<ModulePackResource> Resources,
        string AssignmentEvidencePlaceholder,
        ModuleStatus Status,
        IReadOnlyList<ModulePackItemReference> LessonSequence,
        IReadOnlyList<ModulePackItemReference> AssignmentSequence);

    private sealed record ModulePackObjective(string Text, string LinkedCourseObjective);

    private sealed record ModulePackResource(string Name, string Link, string FilePath, bool IsPhysicalResource);

    private sealed record ModulePackItemReference(string SourceId, string Title, int SequenceOrder);
}
