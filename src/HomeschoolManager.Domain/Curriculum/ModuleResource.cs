using HomeschoolManager.Domain.Common;

namespace HomeschoolManager.Domain.Curriculum;

public sealed record ModuleResource
{
    public string Name { get; init; }
    public string Link { get; init; }
    public string FilePath { get; init; }
    public bool IsPhysicalResource { get; init; }

    public ModuleResource(string name, string link, string filePath, bool isPhysicalResource)
    {
        Name = Require.Text(name, nameof(name));
        Link = string.IsNullOrWhiteSpace(link) ? "" : link.Trim();
        FilePath = string.IsNullOrWhiteSpace(filePath) ? "" : filePath.Trim();
        IsPhysicalResource = isPhysicalResource;
    }
}
