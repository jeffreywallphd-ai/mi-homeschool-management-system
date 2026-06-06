using HomeschoolManager.Domain.Common;

namespace HomeschoolManager.Domain.Curriculum;

public sealed record Lesson
{
    public Guid Id { get; init; }
    public Guid ModuleId { get; init; }
    public string SourceLessonId { get; init; }
    public int SequenceOrder { get; init; }
    public string Title { get; init; }
    public string IntroductoryText { get; init; }
    public string LinkedModuleObjective { get; init; }
    public IReadOnlyList<LessonResource> Resources { get; init; }

    public Lesson(
        Guid id,
        Guid moduleId,
        string sourceLessonId,
        int sequenceOrder,
        string title,
        string introductoryText,
        string linkedModuleObjective,
        IReadOnlyList<LessonResource>? resources)
    {
        if (moduleId == Guid.Empty)
        {
            throw new DomainException("Module is required for a lesson.");
        }

        if (sequenceOrder < 1)
        {
            throw new DomainException("Lesson sequence order must be one or greater.");
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        ModuleId = moduleId;
        SourceLessonId = string.IsNullOrWhiteSpace(sourceLessonId) ? "" : sourceLessonId.Trim();
        SequenceOrder = sequenceOrder;
        Title = Require.Text(title, nameof(title));
        IntroductoryText = Require.Text(introductoryText, nameof(introductoryText));
        LinkedModuleObjective = string.IsNullOrWhiteSpace(linkedModuleObjective) ? "" : linkedModuleObjective.Trim();
        Resources = NormalizeResources(resources);
    }

    private static IReadOnlyList<LessonResource> NormalizeResources(IReadOnlyList<LessonResource>? resources)
    {
        var normalized = (resources ?? [])
            .Where(resource => !string.IsNullOrWhiteSpace(resource.Name))
            .Select(resource => new LessonResource(
                resource.Id,
                resource.Name,
                resource.Type,
                resource.Url,
                resource.FilePath,
                resource.IsPhysicalResource,
                resource.SourceNote))
            .ToArray();

        if (normalized.Length == 0)
        {
            throw new DomainException("At least one lesson resource is required.");
        }

        return normalized;
    }
}
