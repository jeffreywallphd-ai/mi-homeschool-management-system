namespace HomeschoolManager.Domain.Access;

public sealed record UserContext(UserRole Role, string DisplayName)
{
    public bool IsParentAdmin => Role == UserRole.ParentAdmin;

    public static UserContext ParentAdmin(string displayName) => new(UserRole.ParentAdmin, displayName);

    public static UserContext Student(string displayName) => new(UserRole.Student, displayName);
}
