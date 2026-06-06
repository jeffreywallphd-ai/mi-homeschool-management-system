using HomeschoolManager.Domain.Access;
using HomeschoolManager.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace HomeschoolManager.Infrastructure.Access;

public sealed class LocalAccessService
{
    private readonly HomeschoolManagerOptions options;

    public LocalAccessService(IOptions<HomeschoolManagerOptions> options)
    {
        this.options = options.Value;
    }

    public UserContext CreateParentSession()
    {
        var windowsName = Environment.UserName;
        return UserContext.ParentAdmin(string.IsNullOrWhiteSpace(windowsName) ? "Parent/Admin" : windowsName);
    }

    public UserContext? CreateStudentSession(string pin)
    {
        return string.Equals(pin, options.StudentPin, StringComparison.Ordinal)
            ? UserContext.Student("Student")
            : null;
    }
}
