using HomeschoolManager.Domain.Access;

namespace HomeschoolManager.StudentPortal.Web.Services;

public sealed class SessionState
{
    public UserContext? CurrentUser { get; private set; }

    public event Action? Changed;

    public bool IsAuthenticated => CurrentUser is not null;

    public bool IsStudent => CurrentUser?.Role == UserRole.Student;

    public void SignIn(UserContext user)
    {
        CurrentUser = user;
        Changed?.Invoke();
    }

    public void SignOut()
    {
        CurrentUser = null;
        Changed?.Invoke();
    }
}
