namespace AuthApi.Application.Abstractions.Interfaces.Auth
{
    public interface IAuthCookieService
    {
        string? GetRefreshTokenCookie();
        string? GetCSRFTokenCookie();

        void SetRefreshTokenCookie(string token, int days);
        void SetCSRFTokenCookie(int days);

        void ClearTokenCookies();
    }
}