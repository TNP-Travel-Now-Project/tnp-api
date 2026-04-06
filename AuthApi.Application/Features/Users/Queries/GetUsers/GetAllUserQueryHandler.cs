using AuthApi.Application.Abstractions.Messaging.Query;
using AuthApi.Application.Abstractions.Repositories;
using AuthApi.Application.Features.Users.DTOs;

namespace AuthApi.Application.Features.Users.Queries.GetUsers
{
    public class GetAllUserQueryHandler(IUserQueryRepository repoUserQuery) : IQueryHandler<GetAllUserQuery, List<UserDto>>
    {
        public async Task<List<UserDto>> Handle(GetAllUserQuery req, CancellationToken cancellationToken)
        {
            var users = await repoUserQuery.GetAllUserAsync(cancellationToken);

            return users;
        }
    }
}
