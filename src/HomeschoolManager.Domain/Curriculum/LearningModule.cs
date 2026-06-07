using HomeschoolManager.Domain.Common;

namespace HomeschoolManager.Domain.Curriculum;

public sealed record LearningModule
{
    public Guid Id { get; init; }
    public Guid CourseId { get; init; }
    public string SourceModuleId { get; init; }
    public int SequenceOrder { get; init; }
    public string Title { get; init; }
    public string Description { get; init; }
    public Guid? TermId { get; init; }
    public string EstimatedLength { get; init; }
    public string Instructions { get; init; }
    public string MajorTopics { get; init; }
    public string LearningObjectives { get; init; }
    public IReadOnlyList<ModuleLearningObjective> LearningObjectiveItems { get; init; }
    public string Resources { get; init; }
    public IReadOnlyList<ModuleResource> ResourceItems { get; init; }
    public IReadOnlyList<Lesson> Lessons { get; init; }
    public IReadOnlyList<ModuleAssignment> Assignments { get; init; }
    public string AssignmentEvidencePlaceholder { get; init; }
    public ModuleStatus Status { get; init; }

    public LearningModule(
        Guid id,
        Guid courseId,
        string sourceModuleId,
        int sequenceOrder,
        string title,
        string description,
        string estimatedLength,
        string instructions,
        string majorTopics,
        string learningObjectives,
        string resources,
        string assignmentEvidencePlaceholder,
        ModuleStatus status,
        Guid? termId = null,
        IReadOnlyList<ModuleLearningObjective>? learningObjectiveItems = null,
        IReadOnlyList<ModuleResource>? resourceItems = null,
        IReadOnlyList<Lesson>? lessons = null,
        IReadOnlyList<ModuleAssignment>? assignments = null)
    {
        if (courseId == Guid.Empty)
        {
            throw new DomainException("Course is required for a learning module.");
        }

        if (sequenceOrder < 1)
        {
            throw new DomainException("Module sequence order must be one or greater.");
        }

        if (!Enum.IsDefined(status))
        {
            throw new DomainException("Module status is not recognized.");
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        CourseId = courseId;
        SourceModuleId = string.IsNullOrWhiteSpace(sourceModuleId) ? "" : sourceModuleId.Trim();
        SequenceOrder = sequenceOrder;
        Title = Require.Text(title, nameof(title));
        Description = description.Trim();
        TermId = termId == Guid.Empty ? null : termId;
        EstimatedLength = estimatedLength.Trim();
        Instructions = Require.Text(instructions, nameof(instructions));
        MajorTopics = majorTopics.Trim();
        LearningObjectiveItems = NormalizeLearningObjectives(learningObjectiveItems, learningObjectives);
        LearningObjectives = Lines(LearningObjectiveItems.Select(item => item.Text));
        ResourceItems = NormalizeResources(resourceItems, resources);
        Resources = Lines(ResourceItems.Select(SerializeResource));
        var normalizedLessons = NormalizeLessons(LessonsForModule(lessons ?? [], Id));
        Assignments = NormalizeAssignments(AssignmentsForModule(assignments ?? [], Id, normalizedLessons));
        Lessons = NormalizeLessonAssignmentLinks(normalizedLessons, Assignments);
        AssignmentEvidencePlaceholder = assignmentEvidencePlaceholder.Trim();
        Status = status;
    }

    public LearningModule WithLessons(IReadOnlyList<Lesson> lessons)
    {
        var normalizedLessons = NormalizeLessons(LessonsForModule(lessons, Id));
        var validLessonIds = normalizedLessons.Select(lesson => lesson.Id).ToHashSet();
        var assignments = Assignments
            .Select(assignment => assignment with
            {
                LinkedLessonIds = assignment.LinkedLessonIds
                    .Where(validLessonIds.Contains)
                    .ToArray()
            })
            .ToArray();
        return this with
        {
            Lessons = NormalizeLessonAssignmentLinks(normalizedLessons, assignments),
            Assignments = NormalizeAssignments(AssignmentsForModule(assignments, Id, normalizedLessons))
        };
    }

    public LearningModule WithAssignments(IReadOnlyList<ModuleAssignment> assignments)
    {
        var normalizedAssignments = NormalizeAssignments(AssignmentsForModule(assignments, Id, Lessons));
        return this with
        {
            Assignments = normalizedAssignments,
            Lessons = NormalizeLessonAssignmentLinks(Lessons, normalizedAssignments)
        };
    }

    private static IReadOnlyList<ModuleLearningObjective> NormalizeLearningObjectives(
        IReadOnlyList<ModuleLearningObjective>? items,
        string fallbackText)
    {
        var normalized = (items ?? ParseLearningObjectives(fallbackText))
            .Where(item => !string.IsNullOrWhiteSpace(item.Text))
            .Select(item => new ModuleLearningObjective(item.Text, item.LinkedCourseObjective))
            .ToArray();

        if (normalized.Length == 0)
        {
            throw new DomainException("Learning objectives are required.");
        }

        return normalized;
    }

    private static IReadOnlyList<ModuleResource> NormalizeResources(
        IReadOnlyList<ModuleResource>? items,
        string fallbackText)
    {
        return (items ?? ParseResources(fallbackText))
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .Select(item => new ModuleResource(item.Name, item.Link, item.FilePath, item.IsPhysicalResource))
            .ToArray();
    }

    private static IReadOnlyList<ModuleLearningObjective> ParseLearningObjectives(string value)
    {
        return SplitLines(value)
            .Select(line =>
            {
                var parts = line.Split("||", 2, StringSplitOptions.TrimEntries);
                return new ModuleLearningObjective(parts[0], parts.Length == 2 ? parts[1] : "");
            })
            .ToArray();
    }

    private static IReadOnlyList<ModuleResource> ParseResources(string value)
    {
        return SplitLines(value)
            .Select(line =>
            {
                var parts = line.Split('|', 4, StringSplitOptions.TrimEntries);
                return new ModuleResource(
                    parts[0],
                    parts.Length > 1 ? parts[1] : "",
                    parts.Length > 2 ? parts[2] : "",
                    parts.Length > 3 && bool.TryParse(parts[3], out var isPhysical) && isPhysical);
            })
            .ToArray();
    }

    private static IReadOnlyList<Lesson> LessonsForModule(IReadOnlyList<Lesson> lessons, Guid moduleId)
    {
        var invalidLesson = lessons.FirstOrDefault(lesson => lesson.ModuleId != moduleId);
        if (invalidLesson is not null)
        {
            throw new DomainException("Lessons must belong to the module being updated.");
        }

        var duplicateSourceId = lessons
            .Where(lesson => !string.IsNullOrWhiteSpace(lesson.SourceLessonId))
            .GroupBy(lesson => lesson.SourceLessonId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateSourceId is not null)
        {
            throw new DomainException("A module can only have one lesson per source lesson id.");
        }

        return lessons;
    }

    private static IReadOnlyList<Lesson> NormalizeLessons(IReadOnlyList<Lesson> lessons)
    {
        return lessons
            .OrderBy(lesson => lesson.SequenceOrder)
            .ThenBy(lesson => lesson.Title, StringComparer.OrdinalIgnoreCase)
            .Select((lesson, index) => lesson with { SequenceOrder = index + 1 })
            .ToArray();
    }

    private static IReadOnlyList<ModuleAssignment> AssignmentsForModule(
        IReadOnlyList<ModuleAssignment> assignments,
        Guid moduleId,
        IReadOnlyList<Lesson> lessons)
    {
        var invalidAssignment = assignments.FirstOrDefault(assignment => assignment.ModuleId != moduleId);
        if (invalidAssignment is not null)
        {
            throw new DomainException("Assignments must belong to the module being updated.");
        }

        var duplicateSourceId = assignments
            .Where(assignment => !string.IsNullOrWhiteSpace(assignment.SourceAssignmentId))
            .GroupBy(assignment => assignment.SourceAssignmentId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateSourceId is not null)
        {
            throw new DomainException("A module can only have one assignment per source assignment id.");
        }

        var knownLessonIds = lessons.Select(lesson => lesson.Id).ToHashSet();
        var invalidLinkedLesson = assignments
            .SelectMany(assignment => assignment.LinkedLessonIds)
            .FirstOrDefault(lessonId => !knownLessonIds.Contains(lessonId));
        if (invalidLinkedLesson != Guid.Empty)
        {
            throw new DomainException("Assignment lesson links must belong to the same module.");
        }

        return assignments;
    }

    private static IReadOnlyList<ModuleAssignment> NormalizeAssignments(IReadOnlyList<ModuleAssignment> assignments)
    {
        return assignments
            .OrderBy(assignment => assignment.SequenceOrder)
            .ThenBy(assignment => assignment.Title, StringComparer.OrdinalIgnoreCase)
            .Select((assignment, index) => assignment with { SequenceOrder = index + 1 })
            .ToArray();
    }

    private static IReadOnlyList<Lesson> NormalizeLessonAssignmentLinks(
        IReadOnlyList<Lesson> lessons,
        IReadOnlyList<ModuleAssignment> assignments)
    {
        var knownAssignmentIds = assignments.Select(assignment => assignment.Id).ToHashSet();
        return lessons
            .Select(lesson => lesson with
            {
                LinkedAssignmentIds = lesson.LinkedAssignmentIds
                    .Where(knownAssignmentIds.Contains)
                    .Distinct()
                    .ToArray()
            })
            .ToArray();
    }

    private static string SerializeResource(ModuleResource resource)
    {
        if (string.IsNullOrWhiteSpace(resource.Link) &&
            string.IsNullOrWhiteSpace(resource.FilePath) &&
            !resource.IsPhysicalResource)
        {
            return resource.Name;
        }

        return $"{resource.Name} | {resource.Link} | {resource.FilePath} | {resource.IsPhysicalResource}";
    }

    private static IEnumerable<string> SplitLines(string value)
    {
        return (value ?? "")
            .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !string.IsNullOrWhiteSpace(line));
    }

    private static string Lines(IEnumerable<string> values)
    {
        return string.Join(Environment.NewLine, values.Where(value => !string.IsNullOrWhiteSpace(value)));
    }
}
