using AuthApi.Application.Abstractions.Interfaces.Cache;
using Microsoft.Extensions.Logging;

namespace AuthApi.Infrastructure.Services.Cache;

public class CallCacheService<T>(ICacheService _cache, ILogger<CallCacheService<T>> _logger) : ICallCacheService<T>
    where T : class
{
    public async Task<T?> TryGetCachedAsync(string key, CancellationToken ct)
    {
        try
        {
            return await _cache.GetAsync<T>(key, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get cache for key {Key}", key);
            return null;
        }
    }

    public async Task TrySetCacheAsync(string key, T value, TimeSpan cacheDuration, CancellationToken ct)
    {
        try
        {
            await _cache.SetAsync(key, value, cacheDuration, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set cache for key {Key}", key);
        }
    }
}
