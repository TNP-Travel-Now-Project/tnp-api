using AuthApi.Application.Abstractions.Interfaces.Repositories.User;
using AuthApi.Application.Features.Auth.DTOs;
using AuthApi.Application.Features.Users.DTOs;
using Dapper;
using System.Data;
using System.Data.Common;

namespace AuthApi.Infrastructure.Persistence.Dapper.Repositories
{
    public sealed class UserReadRepository : IUserReadRepository
    {
        private readonly DbConnection _conn;
        public UserReadRepository(AppDbContext dbContext) => _conn = dbContext.Connection;

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

            if (_conn.State != ConnectionState.Open)
                await _conn.OpenAsync(cancellationToken);

            var command = new CommandDefinition(
                sql,
                new { UserId = userId },
                cancellationToken: cancellationToken);

            using var multi = await _conn.QueryMultipleAsync(command);

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
                ar.Name AS Role,
                u.CreatedAt
            FROM AspNetUsers u
            LEFT JOIN AspNetUserRoles aur ON aur.UserId = u.Id
            LEFT JOIN AspNetRoles ar ON aur.RoleId = ar.Id
            ORDER BY u.CreatedAt DESC;";

            if (_conn.State != ConnectionState.Open)
                await _conn.OpenAsync();

            var rows = (await _conn.QueryAsync<UserRowsDto>(sql)).ToList();

            var users = rows.GroupBy(p => p.Id)
                            .Select(o =>
                            {
                                var row = o.First();
                                return new UserListItemDto
                                {
                                    Id = o.Key,
                                    Email = row.Email,
                                    UserName = row.UserName,
                                    FirstName = row.FirstName,
                                    LastName = row.LastName,
                                    CreatedAt = row.CreatedAt,
                                    Roles = o.Where(p => p.Role != null).Select(s => s.Role!).ToArray()
                                };
                            }).ToList();
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

            if (_conn.State != ConnectionState.Open)
                await _conn.OpenAsync(cancellationToken);

            var command = new CommandDefinition(
                sql,
                new { UserId = userId },
                cancellationToken: cancellationToken);

            using var multi = await _conn.QueryMultipleAsync(command);

            var row = await multi.ReadSingleOrDefaultAsync<UserDetailResponse>();
            if (row == null) return null;

            var roles = (await multi.ReadAsync<string>()).ToArray() ?? [];

            return row with
            {
                Roles = roles,
                IsLockedOut = row.LockoutEnabled && row.LockoutEnd > DateTimeOffset.UtcNow,
            };
        }
    }
}
