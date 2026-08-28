namespace AuthApi.Application.Common;

public class RateLimitingOptions
{
    public RateLimitPolicy Auth { get; set; } = new();
    public RateLimitPolicy Refresh { get; set; } = new();
}