using HomeschoolManager.Domain.Access;

namespace HomeschoolManager.Web.Services;

public sealed class SessionState
{
    public UserContext? CurrentUser { get; private set; }

    public event Action? Changed;

    public bool IsAuthenticated => CurrentUser is not null;

    public bool IsParentAdmin => CurrentUser?.IsParentAdmin == true;

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
