using AuthApi.Application.Abstractions.Interfaces.Cache;
using AuthApi.Application.Abstractions.Interfaces.Repositories;
using AuthApi.Application.Abstractions.Messaging.Query;
using AuthApi.Application.Common;
using AuthApi.Application.Common.Security;
using AuthApi.Application.Features.Users.DTOs;

namespace AuthApi.Application.Features.Users.Queries.Me
{
    public class MeQueryHandler(
        IUserReadRepository _userRepo,
        IUserContext _context,
        ICacheService _cache) : IQueryHandler<MeQuery, Result<MeResponse>>
    {
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
        private const string CacheKeyPrefix = "cache:me:";

        public async Task<Result<MeResponse>> Handle(MeQuery request, CancellationToken cancellationToken)
        {
            if (!_context.IsAuthenticated)
                return Result<MeResponse>.Fail(new Error(ErrorCodes.UserNotFound, "User not authenticated"));

            var userId = _context.UserId;
            var cacheKey = $"{CacheKeyPrefix}{userId}";

            // 1. Try cache first
            var cached = await TryGetCachedAsync(cacheKey, cancellationToken);
            if (cached != null)
                return Result<MeResponse>.Success(cached);

            // 2. Cache miss → query DB
            var user = await _userRepo.GetMeAsync(userId, cancellationToken);
            if (user == null)
                return Result<MeResponse>.Fail(new Error(ErrorCodes.UserNotFound, "User not found"));

            // 3. Store in cache (fire-and-forget, không block response)
            await TrySetCacheAsync(cacheKey, user, cancellationToken);

            return Result<MeResponse>.Success(user);
        }

        private async Task<MeResponse?> TryGetCachedAsync(string key, CancellationToken ct)
        {
            try
            {
                return await _cache.GetAsync<MeResponse>(key, ct);
            }
            catch
            {
                return null; // Cache failure → fallback to DB
            }
        }

        private async Task TrySetCacheAsync(string key, MeResponse value, CancellationToken ct)
        {
            try
            {
                await _cache.SetAsync(key, value, CacheDuration, ct);
            }
            catch
            {
                // Silent fail — cache không quan trọng bằng response
            }
        }
    }
}
