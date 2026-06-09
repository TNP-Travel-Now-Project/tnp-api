using AuthApi.Application.Abstractions.Interfaces.Repositories;
using AuthApi.Application.Abstractions.Messaging.Query;
using AuthApi.Application.Common;
using AuthApi.Application.Common.Security;
using AuthApi.Application.Features.Users.DTOs;

namespace AuthApi.Application.Features.Users.Queries.GetUserById;

public class GetUserByIdQueryHandler(
    IUserReadRepository _userRepo,
    IUserContext _context) : IQueryHandler<GetUserByIdQuery, Result<UserDetailResponse>>
{
    public async Task<Result<UserDetailResponse>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        if (!_context.IsAuthenticated)
            return Result<UserDetailResponse>.Fail(new Error(ErrorCodes.Unauthorized, "User not authenticated"));

        if (!_context.IsInRole("Admin"))
            return Result<UserDetailResponse>.Fail(new Error(ErrorCodes.Forbidden, "Only admins can view user details"));

        var user = await _userRepo.GetUserDetailAsync(request.UserId, cancellationToken);
        if (user == null)
            return Result<UserDetailResponse>.Fail(new Error(ErrorCodes.UserNotFound, "User not found"));

        return Result<UserDetailResponse>.Success(user);
    }
}
