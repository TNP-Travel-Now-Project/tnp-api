namespace AuthApi.Infrastructure.Common
{
    public class AppSettings
    {
        public string FrontendUrl { get; set; } = string.Empty;
        public string JwtKey { get; set; } = string.Empty;
        public string JwtIssuer { get; set; } = string.Empty;
        public string JwtAudience { get; set; } = string.Empty;
        public string GoogleClientId { get; set; } = string.Empty;
    }
}
