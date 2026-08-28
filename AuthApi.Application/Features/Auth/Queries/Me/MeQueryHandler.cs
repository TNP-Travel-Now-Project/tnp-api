using AuthApi.Application.Abstractions.Interfaces.Cache;
using AuthApi.Application.Abstractions.Interfaces.Repositories.User;
using AuthApi.Application.Abstractions.Messaging.Query;
using AuthApi.Application.Common;
using AuthApi.Application.Common.Security;
using AuthApi.Application.Features.Auth.DTOs;

namespace AuthApi.Application.Features.Auth.Queries.Me
{
    public class MeQueryHandler(
        IUserReadRepository _userRepo,
        IUserContext _context,
        ICallCacheService _callCache) : IQueryHandler<MeQuery, Result<MeResponse>>
    {
        private const string CacheKeyPrefix = "cache:me:";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        public async Task<Result<MeResponse>> Handle(MeQuery request, CancellationToken cancellationToken)
        {
            if (!_context.IsAuthenticated)
                return Result<MeResponse>.Fail(new Error(ErrorCodes.UserNotFound, "User not authenticated"));

            var userId = _context.UserId;
            var cacheKey = $"{CacheKeyPrefix}{userId}";

            // 1. Try cache first
            var cached = await _callCache.TryGetCachedAsync(cacheKey, cancellationToken);
            if (cached != null)
                return Result<MeResponse>.Success(cached);

            // 2. Cache miss → query DB
            var user = await _userRepo.GetMeAsync(userId, cancellationToken);
            if (user == null)
                return Result<MeResponse>.Fail(new Error(ErrorCodes.UserNotFound, "User not found"));

            // 3. set cache
            await _callCache.TrySetCacheAsync(cacheKey, user, CacheDuration, cancellationToken);

            return Result<MeResponse>.Success(user);
        }
    }
}
