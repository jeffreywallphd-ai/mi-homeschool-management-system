using HomeschoolManager.Application.Access;
using HomeschoolManager.Application.Common;
using HomeschoolManager.Application.Persistence;
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
        var courses = await repository.GetCoursesAsync(cancellationToken);
        return courses
            .OrderBy(course => course.Title)
            .Select(course => new CourseListItem(
                course.Id,
                course.Title,
                course.SubjectArea,
                course.PlannedCreditValue,
                course.RequirementMappings.Count))
            .ToArray();
    }

    public async Task<CourseDetail?> GetCourseDetailAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
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
                command.SubjectArea,
                command.PlannedCreditValue,
                null,
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
                command.SubjectArea,
                command.PlannedCreditValue,
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
        var courses = await repository.GetCoursesAsync(cancellationToken);
        var areas = await repository.GetRequirementAreasAsync(cancellationToken);

        return areas
            .OrderBy(area => area.View)
            .ThenBy(area => area.Name)
            .Select(area =>
            {
                var courseMappings = courses
                    .Select(course => new
                    {
                        Course = course,
                        Mapping = course.RequirementMappings.FirstOrDefault(mapping => mapping.RequirementAreaId == area.Id)
                    })
                    .Where(item => item.Mapping is not null)
                    .ToArray();

                return new CoverageSummaryItem(
                    area.Id,
                    area.View,
                    area.Name,
                    area.GradeBand,
                    courseMappings.Length > 0,
                    HighestCoverage(courseMappings.Select(item => item.Mapping!.CoverageLevel)),
                    courseMappings.Select(item => item.Course.Title).Order().ToArray());
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
            .OrderBy(mapping => mapping.RequirementView)
            .ThenBy(mapping => mapping.RequirementAreaName)
            .ToArray();

        return new CourseDetail(
            course.Id,
            course.Title,
            course.SubjectArea,
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
}
