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
    private static readonly JsonSerializerOptions CoursePackJsonOptions = CreateCoursePackJsonOptions();

    public event Action? CourseNavigationChanged;

    public CourseService(IHomeschoolRepository repository)
    {
        this.repository = repository;
    }

    public async Task<IReadOnlyList<CourseListItem>> ListCoursesAsync(CancellationToken cancellationToken = default)
    {
        await RefreshMichiganRequirementSeedAsync(cancellationToken);
        await BackfillImportedCoursePackDetailsAsync(cancellationToken);

        var courses = await repository.GetCoursesAsync(cancellationToken);
        return courses
            .OrderBy(course => course.Title)
            .Select(course => new CourseListItem(
                course.Id,
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
        return ToDetail(course, areas, schoolYear);
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

        var student = await repository.GetStudentAsync(cancellationToken);
        if (student is null)
        {
            return OperationResult<Guid>.Failure("Create a student before adding courses.");
        }

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

    public IReadOnlyList<CoursePackSummary> ListCoursePacks()
    {
        return DefaultCoursePacks.All
            .Select(pack => new CoursePackSummary(
                pack.Id,
                pack.Name,
                pack.Description,
                pack.Courses.Count,
                pack.Courses.Sum(course => course.DefaultOption.PlannedCreditValue)))
            .ToArray();
    }

    public CoursePackDetail? GetCoursePackDetail(string packId)
    {
        var pack = DefaultCoursePacks.All.FirstOrDefault(item => item.Id == packId);
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

    public OperationResult<CoursePackExportFile> ExportCoursePack(string packId)
    {
        var pack = DefaultCoursePacks.All.FirstOrDefault(item => item.Id == packId);
        if (pack is null)
        {
            return OperationResult<CoursePackExportFile>.Failure("Course pack was not found.");
        }

        var export = new CoursePackJsonEnvelope(
            "homeschool-manager.coursepack",
            1,
            DateTimeOffset.UtcNow,
            "json",
            "Future exports with attached lesson or assignment files should use a zip archive containing this JSON plus files.",
            pack);
        var json = JsonSerializer.Serialize(export, CoursePackJsonOptions);
        var fileName = $"{SafeFileName(pack.Id)}.coursepack";
        return OperationResult<CoursePackExportFile>.Success(new CoursePackExportFile(
            fileName,
            "application/json",
            Encoding.UTF8.GetBytes(json),
            false));
    }

    private static CoursePackExportFile ExportCoursePackArchivePlaceholder(CoursePackDefinition pack)
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
                "Archive export is reserved for course packs with attached lesson or assignment files.",
                pack), CoursePackJsonOptions));
        }

        return new CoursePackExportFile($"{SafeFileName(pack.Id)}.coursepack.zip", "application/zip", stream.ToArray(), true);
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

        var pack = DefaultCoursePacks.All.FirstOrDefault(item => item.Id == command.PackId);
        if (pack is null)
        {
            return OperationResult<int>.Failure("Course pack was not found.");
        }

        var student = await repository.GetStudentAsync(cancellationToken);
        if (student is null)
        {
            return OperationResult<int>.Failure("Create a student before importing a course pack.");
        }

        var schoolYear = await repository.GetSchoolYearAsync(cancellationToken);
        if (schoolYear is null)
        {
            return OperationResult<int>.Failure("Create a school year before importing a course pack.");
        }

        var courses = await repository.GetCoursesAsync(cancellationToken);
        var existingTemplateIds = courses
            .Where(course => string.Equals(course.SourcePackId, pack.Id, StringComparison.OrdinalIgnoreCase))
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
        var selectionByTemplateId = command.Selections
            .Where(selection => !string.IsNullOrWhiteSpace(selection.TemplateId))
            .GroupBy(selection => selection.TemplateId.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Last().OptionId.Trim(),
                StringComparer.OrdinalIgnoreCase);

        var selectedTemplateIds = command.TemplateIds
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
        return course?.Modules.Select(module => ToModuleView(module, schoolYear, course)).ToArray() ?? [];
    }

    public async Task<LearningModuleView?> GetModuleDetailAsync(
        Guid courseId,
        Guid moduleId,
        CancellationToken cancellationToken = default)
    {
        await BackfillImportedCoursePackDetailsAsync(cancellationToken);
        var course = await repository.GetCourseAsync(courseId, cancellationToken);
        var schoolYear = await repository.GetSchoolYearAsync(cancellationToken);
        return course?.Modules
            .Where(module => module.Id == moduleId)
            .Select(module => ToModuleView(module, schoolYear, course))
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
            var lesson = new Lesson(
                Guid.NewGuid(),
                module.Id,
                "",
                module.Lessons.Count + 1,
                command.Title,
                command.IntroductoryText,
                command.LinkedModuleObjective,
                BuildLessonResourceItems(command.Resources));
            await SaveModuleAsync(course, module.WithLessons(module.Lessons.Concat([lesson]).ToArray()), cancellationToken);
            CourseNavigationChanged?.Invoke();
            return OperationResult<Guid>.Success(lesson.Id);
        }
        catch (DomainException ex)
        {
            return OperationResult<Guid>.Failure(ex.Message);
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
            var updatedLesson = new Lesson(
                existing.Id,
                module.Id,
                existing.SourceLessonId,
                existing.SequenceOrder,
                command.Title,
                command.IntroductoryText,
                command.LinkedModuleObjective,
                BuildLessonResourceItems(command.Resources));
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
                command.Status);
            await SaveModuleAsync(course, module.WithAssignments(module.Assignments.Concat([assignment]).ToArray()), cancellationToken);
            CourseNavigationChanged?.Invoke();
            return OperationResult<Guid>.Success(assignment.Id);
        }
        catch (DomainException ex)
        {
            return OperationResult<Guid>.Failure(ex.Message);
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
                command.Status) with { Id = existing.Id };
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

    public async Task<IReadOnlyList<CoverageSummaryItem>> GetCoverageSummaryAsync(CancellationToken cancellationToken = default)
    {
        await RefreshMichiganRequirementSeedAsync(cancellationToken);
        await BackfillImportedCoursePackDetailsAsync(cancellationToken);

        var courses = await repository.GetCoursesAsync(cancellationToken);
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
        SchoolYear? schoolYear)
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
            course.Modules.Select(module => ToModuleView(module, schoolYear, course)).ToArray(),
            mappingViews);
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
        Course? course = null)
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
            AssignmentVariantsFor(course, module));
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
            lesson.Resources
                .Select(resource => new LessonResourceView(
                    resource.Id,
                    resource.Name,
                    resource.Type,
                    resource.Url,
                    resource.FilePath,
                    resource.IsPhysicalResource,
                    resource.SourceNote))
                .ToArray());
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
            assignment.Status);
    }

    private static IReadOnlyList<AssignmentVariantView> AssignmentVariantsFor(
        Course? course,
        LearningModule module)
    {
        if (course is null)
        {
            return [];
        }

        var option = FindSourceOption(course);
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
                    variant.Status)))
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
        AssignmentStatus status)
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
            status);
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
                item.SourceNote))
            .ToArray();
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
            .Select(mapping =>
            {
                var area = areas.FirstOrDefault(item =>
                    string.Equals(item.View, mapping.RequirementAreaView, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(item.Name, mapping.RequirementAreaName, StringComparison.OrdinalIgnoreCase));

                if (area is null)
                {
                    throw new DomainException($"Requirement area '{mapping.RequirementAreaName}' was not found.");
                }

                return new RequirementMapping(
                    Guid.NewGuid(),
                    courseId,
                    area.Id,
                    mapping.CoverageLevel,
                    mapping.Notes);
            })
            .ToArray();
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
        foreach (var course in courses)
        {
            var option = FindSourceOption(course);
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
                    BuildAssignments(moduleId, module.Assignments, lessons));
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
                        resource.SourceNote))
                    .ToArray()))
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
                    variant.Status);
            })
            .ToArray();
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
        var resourceKey = string.Join(";", lesson.Resources.Select(resource => $"{resource.Name}->{resource.Type}->{resource.Url}->{resource.FilePath}->{resource.IsPhysicalResource}->{resource.SourceNote}"));
        return $"{lesson.Id:N}:{lesson.SourceLessonId}:{lesson.SequenceOrder}:{lesson.Title}:{lesson.IntroductoryText}:{lesson.LinkedModuleObjective}:{resourceKey}";
    }

    private static string AssignmentKey(ModuleAssignment assignment)
    {
        var objectiveKey = string.Join(";", assignment.LinkedModuleObjectives);
        var lessonKey = string.Join(";", assignment.LinkedLessonIds.Order());
        return $"{assignment.Id:N}:{assignment.SourceAssignmentId}:{assignment.SequenceOrder}:{assignment.Title}:{assignment.Type}:{assignment.MethodProfile}:{assignment.Instructions}:{assignment.EstimatedEffort}:{assignment.DueTimingLabel}:{assignment.DueDate}:{objectiveKey}:{lessonKey}:{assignment.RequiredOutput}:{assignment.ParentNotes}:{assignment.IsPortfolioCandidate}:{assignment.PlannedPoints}:{assignment.PlannedWeight}:{assignment.Status}";
    }

    private static int AreaOrder(Guid requirementAreaId, IReadOnlyList<RequirementArea> areas)
    {
        var area = areas.FirstOrDefault(item => item.Id == requirementAreaId);
        return area is null ? 99 : SourceOrder(area.View);
    }

    private static CourseTemplateOptionDefinition? FindSourceOption(Course course)
    {
        if (string.IsNullOrWhiteSpace(course.SourcePackId) || string.IsNullOrWhiteSpace(course.SourceTemplateId))
        {
            return null;
        }

        var pack = DefaultCoursePacks.All.FirstOrDefault(item =>
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

    private static JsonSerializerOptions CreateCoursePackJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static string SafeFileName(string value)
    {
        var normalized = Regex.Replace(value.Trim().ToLowerInvariant(), "[^a-z0-9._-]+", "-");
        normalized = normalized.Trim('-', '.', '_');
        return string.IsNullOrWhiteSpace(normalized) ? "course-pack" : normalized;
    }

    private sealed record CoursePackJsonEnvelope(
        string Format,
        int FormatVersion,
        DateTimeOffset ExportedAtUtc,
        string PackageMode,
        string ArchiveNote,
        CoursePackDefinition Pack);
}
