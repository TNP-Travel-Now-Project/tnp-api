using AuthApi.Application.Features.Auth.DTOs;

namespace AuthApi.Application.Abstractions.Interfaces.Cache
{
    public interface ICallCacheService
    {
        Task<MeResponse?> TryGetCachedAsync(string key, CancellationToken ct);
        Task TrySetCacheAsync(string key, MeResponse value, TimeSpan CacheDuration, CancellationToken ct);
    }
}
