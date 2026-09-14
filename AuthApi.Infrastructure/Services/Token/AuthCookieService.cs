using AuthApi.Application.Abstractions.Interfaces.Auth;
using Microsoft.AspNetCore.Http;

namespace AuthApi.Infrastructure.Services.Token
{
    public class AuthCookieService(IHttpContextAccessor _httpContextAccessor) : IAuthCookieService
    {
        private static readonly CookieOptions _refreshTokenOptions = new()
        {
            Secure = true,
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.None,
            Path = "/"
        };

        private static readonly CookieOptions _csrfTokenOptions = new()
        {
            Secure = true,
            HttpOnly = false,
            IsEssential = true,
            SameSite = SameSiteMode.None,
            Path = "/"
        };

        public void ClearTokenCookies()
        {
            var context = _httpContextAccessor.HttpContext;

            if (context == null) return;

            context?.Response.Cookies.Delete("refreshToken", _refreshTokenOptions);
            context?.Response.Cookies.Delete("CSRF-TOKEN", _csrfTokenOptions);
        }

        public string? GetRefreshTokenCookie()
                => _httpContextAccessor.HttpContext?.Request.Cookies["refreshToken"];

        public string? GetCSRFTokenCookie()
                => _httpContextAccessor.HttpContext?.Request.Cookies["CSRF-TOKEN"];

        public void SetRefreshTokenCookie(string token, int days)
        {
            var options = CloneOptions(_refreshTokenOptions, DateTime.UtcNow.AddDays(days));
            _httpContextAccessor.HttpContext?.Response.Cookies.Append("refreshToken", token, options);
        }

        public void SetCSRFTokenCookie(int days)
        {
            var token = Convert.ToString(Guid.CreateVersion7())!;

            var options = CloneOptions(_csrfTokenOptions, DateTime.UtcNow.AddDays(days));
            _httpContextAccessor.HttpContext?.Response.Cookies.Append("CSRF-TOKEN", token, options);
        }

        private static CookieOptions CloneOptions(CookieOptions baseOptions, DateTimeOffset expires)
        {
            return new CookieOptions
            {
                Path = baseOptions.Path,
                Secure = baseOptions.Secure,
                HttpOnly = baseOptions.HttpOnly,
                SameSite = baseOptions.SameSite,
                IsEssential = baseOptions.IsEssential,
                Expires = expires
            };
        }
    }
}