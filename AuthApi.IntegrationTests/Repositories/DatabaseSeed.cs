using System.Data;
using Dapper;

namespace AuthApi.IntegrationTests.Repositories;

/// <summary>
/// Helper seed data vào SQL Server thật cho integration tests.
/// Dùng Dapper để insert trực tiếp vào AspNetUsers/AspNetRoles/AspNetUserRoles.
/// </summary>
internal static class DatabaseSeed
{
    /// <summary>Tạo 1 user + 1 role + gán role cho user. Trả về (userId, roleId).</summary>
    public static async Task<(Guid UserId, Guid RoleId)> SeedUserWithRoleAsync(
        IDbConnection conn,
        string email = "test@example.com",
        string userName = "TestUser",
        string roleName = "User")
    {
        var userId = Guid.CreateVersion7();
        var roleId = Guid.CreateVersion7();

        // IdentityUser fields bắt buộc: Id, UserName, Email, NormalizedUserName, NormalizedEmail, ConcurrencyStamp
        await conn.ExecuteAsync("""
            INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, ConcurrencyStamp, FirstName, LastName, DOB, CreatedAt)
            VALUES (@Id, @UserName, @NormalizedUserName, @Email, @NormalizedEmail, @ConcurrencyStamp, @FirstName, @LastName, @DOB, @CreatedAt)
            """, new
        {
            Id = userId,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            FirstName = "Test",
            LastName = "User",
            DOB = new DateOnly(1990, 1, 1),
            CreatedAt = DateTime.UtcNow
        });

        await conn.ExecuteAsync("""
            INSERT INTO AspNetRoles (Id, Name, NormalizedName)
            VALUES (@Id, @Name, @NormalizedName)
            """, new
        {
            Id = roleId,
            Name = roleName,
            NormalizedName = roleName.ToUpperInvariant()
        });

        await conn.ExecuteAsync("""
            INSERT INTO AspNetUserRoles (UserId, RoleId)
            VALUES (@UserId, @RoleId)
            """, new { UserId = userId, RoleId = roleId });

        return (userId, roleId);
    }

    /// <summary>Tạo thêm role (chưa gán cho user nào).</summary>
    public static async Task<Guid> SeedRoleAsync(IDbConnection conn, string roleName)
    {
        var roleId = Guid.CreateVersion7();
        await conn.ExecuteAsync("""
            INSERT INTO AspNetRoles (Id, Name, NormalizedName)
            VALUES (@Id, @Name, @NormalizedName)
            """, new
        {
            Id = roleId,
            Name = roleName,
            NormalizedName = roleName.ToUpperInvariant()
        });
        return roleId;
    }
}
