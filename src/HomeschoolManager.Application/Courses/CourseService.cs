using HomeschoolManager.Application.Access;
using HomeschoolManager.Application.Common;
using HomeschoolManager.Application.Persistence;
using HomeschoolManager.Application.Requirements;
using HomeschoolManager.Domain.Access;
using HomeschoolManager.Domain.Common;
using HomeschoolManager.Domain.Curriculum;
using HomeschoolManager.Domain.LegalRequirements;

namespace HomeschoolManager.Application.Courses;

public sealed class CourseService
{
    private readonly IHomeschoolRepository repository;

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
        return ToDetail(course, areas);
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
                existing.RequirementMappings);

            await repository.SaveCourseAsync(updated, cancellationToken);
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
                    mappings);

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

    private static CourseDetail ToDetail(Course course, IReadOnlyList<RequirementArea> areas)
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
            mappingViews);
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
            if (description == course.Description &&
                curriculumPlan == course.CurriculumPlan &&
                MappingsMatch(course.RequirementMappings, mappings))
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
                mappings);

            await repository.SaveCourseAsync(updated, cancellationToken);
        }
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
}
