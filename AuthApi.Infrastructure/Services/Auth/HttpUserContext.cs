using System.Security.Claims;
using AuthApi.Application.Common.Security;
using Microsoft.AspNetCore.Http;

namespace AuthApi.Infrastructure.Services.Auth;

public sealed class HttpUserContext(IHttpContextAccessor _httpContextAccessor) : IUserContext
{
    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public Guid UserId
    {
        get
        {
            var userIdString = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                throw new UnauthorizedException("User is not authenticated or user ID not found in claims");

            return userId;
        }
    }

    public string Email => Principal?.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

    public string UserName => Principal?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

    public string[] Roles => Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray() ?? [];

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;
}
