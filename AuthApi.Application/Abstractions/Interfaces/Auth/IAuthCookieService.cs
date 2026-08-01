namespace AuthApi.Application.Abstractions.Interfaces.Auth
{
    public interface IAuthCookieService
    {
        string? GetAccessToken();
        string? GetRefreshTokenCookie();
        string? GetCSRFTokenCookie();

        void SetAccessToken(string token, int minutes);
        void SetRefreshTokenCookie(string token, int days);
        void SetCSRFTokenCookie(int days);

        void ClearTokenCookies();
    }
}