using HomeschoolManager.Application.Common;
using HomeschoolManager.Domain.Access;

namespace HomeschoolManager.Application.Access;

public static class AuthorizationGuard
{
    public static OperationResult RequireParentAdmin(UserContext user)
    {
        return user.IsParentAdmin
            ? OperationResult.Success()
            : OperationResult.Failure("Only the parent/admin can perform this action.");
    }
}
