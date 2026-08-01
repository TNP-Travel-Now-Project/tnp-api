using AuthApi.Application.Abstractions.Interfaces.Repositories;
using AuthApi.Application.Features.Auth.DTOs;
using AuthApi.Application.Features.Users.DTOs;
using AuthApi.Infrastructure.Persistence.Connection;
using Dapper;

namespace AuthApi.Infrastructure.Persistence.Dapper.Repositories
{
    internal sealed class UserReadRepository(IDbConnectionFactory _connectionFactory) : IUserReadRepository
    {
        public async Task<MeResponse?> GetMeAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            const string sql = @"
                SELECT
                    u.Id,
                    u.Email,
                    u.UserName,
                    u.FirstName,
                    u.LastName,
                    u.DOB AS DateOfBirth,
                    u.PhoneNumber,
                    u.EmailConfirmed,
                    u.CreatedAt,
                    u.UpdatedAt
                FROM AspNetUsers u
                WHERE u.Id = @UserId;

                SELECT r.Name
                FROM AspNetRoles r
                INNER JOIN AspNetUserRoles ur ON r.Id = ur.RoleId
                WHERE ur.UserId = @UserId;";

            using var conn = _connectionFactory.Create();

            var command = new CommandDefinition(
                sql,
                new { Userid = userId },
                cancellationToken: cancellationToken);

            using var multi = await conn.QueryMultipleAsync(command);

            var user = await multi.ReadSingleOrDefaultAsync<MeResponse>();
            if (user is null) return null;

            var roles = (await multi.ReadAsync<string>()).ToArray() ?? [];

            return user with { Roles = roles };
        }

        public async Task<List<UserListItemDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
        {
            const string sql = @"
                SELECT
                    u.Id,
                    u.Email,
                    u.UserName,
                    u.FirstName,
                    u.LastName,
                    u.CreatedAt
                FROM AspNetUsers u
                ORDER BY u.CreatedAt DESC";

            const string rolesSql = @"
                SELECT ur.UserId, r.Name AS Role
                FROM AspNetUserRoles ur
                INNER JOIN AspNetRoles r ON r.Id = ur.RoleId";

            using var conn = _connectionFactory.Create();

            var users = (await conn.QueryAsync<UserListItemDto>(sql)).ToList();
            if (users.Count == 0) return users;

            var roleRows = await conn.QueryAsync<roleRowsDto>(rolesSql);
            var roleLookup = roleRows
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Role).ToArray());

            for (var i = 0; i < users.Count; i++)
            {
                var user = users[i];
                users[i] = user with { Roles = roleLookup.GetValueOrDefault(user.Id, []) };
            }

            return users;
        }

        public async Task<UserDetailResponse?> GetUserDetailAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            const string sql = @"
                SELECT
                    u.Id,
                    u.Email,
                    u.UserName,
                    u.FirstName,
                    u.LastName,
                    u.DOB AS DateOfBirth,
                    u.PhoneNumber,
                    u.EmailConfirmed,
                    u.LockoutEnabled,
                    u.LockoutEnd,
                    u.AccessFailedCount,
                    u.CreatedAt,
                    u.UpdatedAt
                FROM AspNetUsers u
                WHERE u.Id = @UserId;

                SELECT r.Name
                FROM AspNetRoles r
                INNER JOIN AspNetUserRoles ur ON r.Id = ur.RoleId
                WHERE ur.UserId = @UserId;";

            using var conn = _connectionFactory.Create();

            var command = new CommandDefinition(
                sql,
                new { UserId = userId },
                cancellationToken: cancellationToken);

            using var multi = await conn.QueryMultipleAsync(command);

            var row = await multi.ReadSingleOrDefaultAsync<UserDetailResponse>();
            if (row == null) return null;

            var roles = (await multi.ReadAsync<string>()).ToArray() ?? [];

            return new UserDetailResponse
            {
                Id = row.Id,
                Email = row.Email ?? "",
                UserName = row.UserName ?? "",
                FirstName = row.FirstName ?? "",
                LastName = row.LastName ?? "",
                DateOfBirth = row.DateOfBirth,
                PhoneNumber = row.PhoneNumber,
                EmailConfirmed = row.EmailConfirmed,
                IsLockedOut = row.LockoutEnabled && row.LockoutEnd > DateTimeOffset.UtcNow,
                Roles = roles,
                CreatedAt = row.CreatedAt,
                UpdatedAt = row.UpdatedAt
            };
        }
    }
}
