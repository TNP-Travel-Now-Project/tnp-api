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

        public async Task<MeResponse?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var query = _queryFactory.Query("AspNetUsers as u")
                .LeftJoin("AspNetUserRoles as ur", "u.Id", "ur.UserId")
                .LeftJoin("AspNetRoles as r", "ur.RoleId", "r.Id")
                .Where("u.Id", userId)
                .Select(
                    "u.Id",
                    "u.Email",
                    "u.UserName",
                    "u.FirstName",
                    "u.LastName",
                    "u.DOB as DateOfBirth",
                    "u.PhoneNumber",
                    "u.EmailConfirmed",
                    "u.CreatedAt",
                    "u.UpdatedAt",
                    "r.Name as Role"
                )
                .GroupBy("u.Id", "u.Email", "u.UserName", "u.FirstName", "u.LastName",
                         "u.DOB", "u.PhoneNumber", "u.EmailConfirmed", "u.CreatedAt", "u.UpdatedAt", "r.Name");

            var result = await query.GetAsync<MeResponse>(cancellationToken: cancellationToken);
            return result.FirstOrDefault();
        }
    }
}
