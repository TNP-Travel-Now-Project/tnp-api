using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace AuthApi.WebApi.HealthChecks;

/// <summary>
/// Kiểm tra Redis còn hoạt động bằng cách gửi lệnh PING.
/// Dùng IConnectionMultiplexer có sẵn trong DI (registered từ Infrastructure).
/// </summary>
public sealed class RedisHealthCheck(IConnectionMultiplexer _redis) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.PingAsync();

            return HealthCheckResult.Healthy("Redis is reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis is unreachable", ex);
        }
    }
}
