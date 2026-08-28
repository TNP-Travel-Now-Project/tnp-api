using AuthApi.Application.Abstractions.Interfaces.Repositories.User;
using AuthApi.Application.Abstractions.Messaging.Query;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Users.DTOs;
using MediatR;

namespace AuthApi.Application.Features.Users.Queries.GetUsers
{
    public class GetAllUserQueryHandler(IUserReadRepository _userRepo) : IQueryHandler<GetAllUserQuery, Result<List<UserListItemDto>>>
    {
        public async Task<Result<List<UserListItemDto>>> Handle(GetAllUserQuery request, CancellationToken cancellationToken)
        {
            var users = await _userRepo.GetAllUsersAsync(cancellationToken);
            return Result<List<UserListItemDto>>.Success(users);
        }
    }
}
