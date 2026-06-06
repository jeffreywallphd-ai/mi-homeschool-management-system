using HomeschoolManager.Domain.Common;

namespace HomeschoolManager.Domain.Curriculum;

public sealed record ModuleLearningObjective
{
    public string Text { get; init; }
    public string LinkedCourseObjective { get; init; }

    public ModuleLearningObjective(string text, string linkedCourseObjective)
    {
        Text = Require.Text(text, nameof(text));
        LinkedCourseObjective = string.IsNullOrWhiteSpace(linkedCourseObjective) ? "" : linkedCourseObjective.Trim();
    }
}
