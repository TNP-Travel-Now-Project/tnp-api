using AuthApi.Application.Abstractions.Interfaces.Auth;
using AuthApi.Application.Abstractions.Interfaces.Repositories;
using AuthApi.Application.Abstractions.Messaging.Query;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Users.DTOs;

namespace AuthApi.Application.Features.Users.Queries.Me
{
    public class MeQueryHandler(
        IUserQueryRepository _userQuery,
        ICurrentUserService _currentUser) : IQueryHandler<MeQuery, Result<MeResponse>>
    {
        public async Task<Result<MeResponse>> Handle(MeQuery request, CancellationToken cancellationToken)
        {
            if (!_currentUser.IsAuthenticated())
                return Result<MeResponse>.Fail(new Error(ErrorCodes.UserNotFound, "User not authenticated"));

            var userId = _currentUser.GetUserId();
            if (userId == null)
                return Result<MeResponse>.Fail(new Error(ErrorCodes.UserNotFound, "User id not found in claims"));

            var user = await _userQuery.GetUserByIdAsync(userId.Value, cancellationToken);
            if (user == null)
                return Result<MeResponse>.Fail(new Error(ErrorCodes.UserNotFound, "User not found"));

            return Result<MeResponse>.Success(user);
        }
    }
}
