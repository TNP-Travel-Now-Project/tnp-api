namespace AuthApi.Application.Common.Security;

public interface IUserContext
{
    Guid UserId { get; }
    string Email { get; }
    string UserName { get; }
    string[] Roles { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
