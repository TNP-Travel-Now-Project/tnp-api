using System.Security.Claims;
using AuthApi.Application.Abstractions.Interfaces.Auth;
using Microsoft.AspNetCore.Http;

namespace AuthApi.Infrastructure.Services.Auth
{
    public class CurrentUserService(IHttpContextAccessor _httpContextAccessor) : ICurrentUserService
    {
        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public Guid? GetUserId()
        {
            var userIdString = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                return null;
            return userId;
        }

        public bool IsAuthenticated() => User?.Identity?.IsAuthenticated ?? false;

        public string? GetEmail() => User?.FindFirstValue(ClaimTypes.Email);

        public IReadOnlyList<string> GetRoles()
        {
            if (User == null) return Array.Empty<string>();
            return User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        }
    }
}
