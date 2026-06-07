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
    public bool Required { get; init; }
    public int EstimatedMinutes { get; init; }
    public string StudentInstructions { get; init; }
    public string NotesPrompt { get; init; }
    public LessonResourceCitation? Citation { get; init; }
    public bool OfflineAvailable { get; init; }
    public string License { get; init; }

    public LessonResource(
        Guid id,
        string name,
        LessonResourceType type,
        string url,
        string filePath,
        bool isPhysicalResource,
        string sourceNote,
        bool required = true,
        int estimatedMinutes = 0,
        string studentInstructions = "",
        string notesPrompt = "",
        LessonResourceCitation? citation = null,
        bool offlineAvailable = false,
        string license = "")
    {
        if (!Enum.IsDefined(type))
        {
            throw new DomainException("Lesson resource type is not recognized.");
        }

        if (estimatedMinutes < 0)
        {
            throw new DomainException("Lesson resource estimated minutes cannot be negative.");
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Name = Require.Text(name, nameof(name));
        Type = type;
        Url = string.IsNullOrWhiteSpace(url) ? "" : url.Trim();
        FilePath = string.IsNullOrWhiteSpace(filePath) ? "" : filePath.Trim();
        IsPhysicalResource = isPhysicalResource || type == LessonResourceType.PhysicalResource;
        SourceNote = string.IsNullOrWhiteSpace(sourceNote) ? "" : sourceNote.Trim();
        Required = required;
        EstimatedMinutes = estimatedMinutes;
        StudentInstructions = string.IsNullOrWhiteSpace(studentInstructions) ? "" : studentInstructions.Trim();
        NotesPrompt = string.IsNullOrWhiteSpace(notesPrompt) ? "" : notesPrompt.Trim();
        Citation = citation;
        OfflineAvailable = offlineAvailable;
        License = string.IsNullOrWhiteSpace(license) ? "" : license.Trim();
    }
}
