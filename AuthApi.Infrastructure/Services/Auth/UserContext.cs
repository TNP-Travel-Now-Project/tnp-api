using AuthApi.Application.Common.Security;

namespace AuthApi.Infrastructure.Services.Auth;

public sealed class UserContext : IUserContext
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string[] Roles { get; init; } = [];
    public bool IsAuthenticated { get; init; }
    public bool IsInRole(string role) => Roles.Contains(role);

    public static UserContext System => new()
    {
        UserId = Guid.Empty,
        Email = "system@background.job",
        UserName = "System",
        Roles = ["System"],
        IsAuthenticated = true
    };

    public static UserContext TestUser => new()
    {
        UserId = Guid.NewGuid(),
        Email = "test@test.com",
        UserName = "TestUser",
        Roles = ["User"],
        IsAuthenticated = true
    };

    public static UserContext TestAdmin => new()
    {
        UserId = Guid.NewGuid(),
        Email = "admin@test.com",
        UserName = "TestAdmin",
        Roles = ["User", "Admin"],
        IsAuthenticated = true
    };
}
