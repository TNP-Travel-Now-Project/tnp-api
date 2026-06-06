namespace AuthApi.Application.Abstractions.Interfaces.Auth
{
    public interface ICurrentUserService
    {
        Guid? GetUserId();
        bool IsAuthenticated();
        string? GetEmail();
        IReadOnlyList<string> GetRoles();
    }
}
