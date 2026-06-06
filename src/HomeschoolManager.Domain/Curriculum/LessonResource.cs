using HomeschoolManager.Domain.Common;

namespace HomeschoolManager.Domain.Curriculum;

public sealed record LessonResource
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public LessonResourceType Type { get; init; }
    public string Url { get; init; }
    public string FilePath { get; init; }
    public bool IsPhysicalResource { get; init; }
    public string SourceNote { get; init; }

    public LessonResource(
        Guid id,
        string name,
        LessonResourceType type,
        string url,
        string filePath,
        bool isPhysicalResource,
        string sourceNote)
    {
        if (!Enum.IsDefined(type))
        {
            throw new DomainException("Lesson resource type is not recognized.");
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Name = Require.Text(name, nameof(name));
        Type = type;
        Url = string.IsNullOrWhiteSpace(url) ? "" : url.Trim();
        FilePath = string.IsNullOrWhiteSpace(filePath) ? "" : filePath.Trim();
        IsPhysicalResource = isPhysicalResource || type == LessonResourceType.PhysicalResource;
        SourceNote = string.IsNullOrWhiteSpace(sourceNote) ? "" : sourceNote.Trim();
    }
}
