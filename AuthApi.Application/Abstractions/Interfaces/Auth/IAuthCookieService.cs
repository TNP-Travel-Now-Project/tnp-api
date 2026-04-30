namespace AuthApi.Application.Abstractions.Interfaces.Auth
{
    public interface IAuthCookieService
    {
        string? GetAccessToken();
        string? GetRefreshToken();
        string? GetCSRFToken();

        void SetAccessToken(string token, int minutes);
        void SetRefreshToken(string token, int days);
        void SetCSRFToken(int days);

        void ClearTokens();
    }
}
