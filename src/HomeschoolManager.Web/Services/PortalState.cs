namespace HomeschoolManager.Web.Services;

public sealed class PortalState
{
    private readonly string studentPortalBaseUrl;

    public PortalState(IConfiguration configuration)
    {
        studentPortalBaseUrl = configuration["HomeschoolManager:StudentPortalBaseUrl"]?.TrimEnd('/')
            ?? "http://localhost:5172";
    }

    public string StudentPortalBaseUrl => studentPortalBaseUrl;

    public string StudentPortalPath(Guid? studentId = null)
    {
        return studentId.HasValue
            ? $"{StudentPortalBaseUrl}/student/{studentId.Value}"
            : $"{StudentPortalBaseUrl}/student";
    }
}
