using AuthApi.Application.Abstractions.Interfaces.Repositories;
using AuthApi.Application.Abstractions.Messaging.Query;
using AuthApi.Application.Features.Users.DTOs;

namespace AuthApi.Application.Features.Users.Queries.GetUsers
{
    public class GetAllUserQueryHandler(IUserReadRepository _userRepo) : IQueryHandler<GetAllUserQuery, List<UserListItemDto>>
    {
        public async Task<List<UserListItemDto>> Handle(GetAllUserQuery req, CancellationToken cancellationToken)
        {
            return await _userRepo.GetAllUsersAsync(cancellationToken);
        }
    }
}
