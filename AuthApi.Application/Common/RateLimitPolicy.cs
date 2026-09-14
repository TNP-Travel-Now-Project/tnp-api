namespace AuthApi.Application.Common
{
    public class RateLimitPolicy
    {
        public int PermitLimit { get; set; } = 10;
        public int WindowMinutes { get; set; } = 1;
        public int QueueLimit { get; set; } = 0;
    }
}
