using AuthApi.Application.Abstractions.Interfaces.Cache;
using AuthApi.Application.Features.Auth.DTOs;
using System.Globalization;

namespace AuthApi.Infrastructure.Services.Cache
{
    public class CallCacheService(ICacheService _cache) : ICallCacheService
    {
        public async Task<MeResponse?> TryGetCachedAsync(string key, CancellationToken ct)
        {
            try
            {
                return await _cache.GetAsync<MeResponse>(key, ct);
            }
            catch { return null; } // Cache failure → fallback to DB
        }

        public async Task TrySetCacheAsync(string key, MeResponse value, TimeSpan CacheDuration, CancellationToken ct)
        {
            try
            {
                await _cache.SetAsync(key, value, CacheDuration, ct);
            }
            catch { }
        }
    }
}
