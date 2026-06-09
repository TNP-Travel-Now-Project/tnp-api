using AuthApi.Application.Abstractions.Interfaces.Repositories;
using AuthApi.Application.Features.Users.DTOs;
using AuthApi.Infrastructure.Persistence.Connection;
using Dapper;

namespace AuthApi.Infrastructure.Persistence.Dapper.Repositories
{
    internal sealed class UserWriteRepository : IUserWriteRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public UserWriteRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<bool> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default)
        {
            const string sql = @"
                UPDATE AspNetUsers
                SET
                    FirstName = COALESCE(@FirstName, FirstName),
                    LastName = COALESCE(@LastName, LastName),
                    PhoneNumber = COALESCE(@PhoneNumber, PhoneNumber),
                    DOB = COALESCE(@DateOfBirth, DOB),
                    UpdatedAt = @Now
                WHERE Id = @UserId";

            using var conn = _connectionFactory.Create();

            var rows = await conn.ExecuteAsync(
                sql,
                new
                {
                    UserId = userId,
                    request.FirstName,
                    request.LastName,
                    request.PhoneNumber,
                    request.DateOfBirth,
                    Now = DateTime.UtcNow
                });

            return rows > 0;
        }

        public async Task AssignRolesAsync(Guid userId, string[] roles, CancellationToken cancellationToken = default)
        {
            const string sql = @"
                INSERT INTO AspNetUserRoles (UserId, RoleId)
                SELECT @UserId, r.Id
                FROM AspNetRoles r
                WHERE r.Name IN @Roles
                  AND NOT EXISTS (
                      SELECT 1 FROM AspNetUserRoles ur
                      WHERE ur.UserId = @UserId AND ur.RoleId = r.Id
                  )";

            using var conn = _connectionFactory.Create();
            await conn.ExecuteAsync(sql, new { UserId = userId, Roles = roles });
        }

        public async Task RemoveRolesAsync(Guid userId, string[] roles, CancellationToken cancellationToken = default)
        {
            const string sql = @"
                DELETE ur
                FROM AspNetUserRoles ur
                INNER JOIN AspNetRoles r ON r.Id = ur.RoleId
                WHERE ur.UserId = @UserId
                  AND r.Name IN @Roles";

            using var conn = _connectionFactory.Create();
            await conn.ExecuteAsync(sql, new { UserId = userId, Roles = roles });
        }

        public async Task<bool> SoftDeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            const string sql = @"
                UPDATE AspNetUsers
                SET
                    LockoutEnabled = 1,
                    LockoutEnd = @LockoutEnd,
                    UpdatedAt = @Now
                WHERE Id = @UserId";

            using var conn = _connectionFactory.Create();

            var rows = await conn.ExecuteAsync(
                sql,
                new
                {
                    UserId = userId,
                    LockoutEnd = DateTimeOffset.MaxValue,
                    Now = DateTime.UtcNow
                });

            return rows > 0;
        }
    }
}
