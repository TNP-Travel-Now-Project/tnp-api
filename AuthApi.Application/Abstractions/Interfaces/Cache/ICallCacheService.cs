namespace AuthApi.Application.Abstractions.Interfaces.Cache;

public interface ICallCacheService<T> where T : class
{
    Task<T?> TryGetCachedAsync(string key, CancellationToken ct);
    Task TrySetCacheAsync(string key, T value, TimeSpan cacheDuration, CancellationToken ct);
}
