using AuthApi.Application.Abstractions.Interfaces.Repositories;
using AuthApi.Application.Features.Users.DTOs;
using SqlKata.Execution;

namespace AuthApi.Infrastructure.Persistence.Repositories.Users
{
    internal class UserQueryRepository : IUserQueryRepository
    {
        private readonly QueryFactory _queryFactory;

        public UserQueryRepository(QueryFactory queryFactory)
        {
            _queryFactory = queryFactory;
        }

        public async Task<List<UserDto>> GetAllUserAsync(CancellationToken cancellationToken)
        {
            var query = _queryFactory.Query("AspNetUsers")
                                    .Select("Id", "Age", "Role", "FullName", "Email", "CreatedAt");

            var user = await query.GetAsync<UserDto>(cancellationToken: cancellationToken);

            return user.ToList();
        }
    }
}
