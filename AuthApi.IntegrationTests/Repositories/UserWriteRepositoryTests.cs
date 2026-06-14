using AuthApi.Application.Features.Users.DTOs;
using AuthApi.Infrastructure.Persistence.Dapper.Repositories;
using AuthApi.IntegrationTests.Fixtures;
using AuthApi.Infrastructure.Persistence.Connection;
using Dapper;
using FluentAssertions;

namespace AuthApi.IntegrationTests.Repositories;

/// <summary>
/// Integration tests cho UserWriteRepository.
/// Test Dapper writes trên SQL Server thật.
/// </summary>
[Collection("SqlServerCollection")]
public sealed class UserWriteRepositoryTests
{
    private readonly UserWriteRepository _repo;
    private readonly IDbConnectionFactory _dbFactory;

    public UserWriteRepositoryTests(SqlServerFixture fixture)
    {
        _repo = new UserWriteRepository(fixture.DbConnectionFactory);
        _dbFactory = fixture.DbConnectionFactory;
    }

    [Fact]
    public async Task UpdateUserAsync_WhenUserExists_ReturnsTrue()
    {
        using var conn = _dbFactory.Create();
        var (userId, _) = await DatabaseSeed.SeedUserWithRoleAsync(conn);

        var updated = await _repo.UpdateUserAsync(userId, new UpdateUserRequest
        {
            FirstName = "Updated",
            LastName = "Name",
            PhoneNumber = "0123456789"
        });

        updated.Should().BeTrue();

        // Verify trực tiếp từ DB
        var row = await conn.QuerySingleOrDefaultAsync(
            "SELECT FirstName, LastName, PhoneNumber FROM AspNetUsers WHERE Id = @Id",
            new { Id = userId });
        row.Should().NotBeNull();
        ((string)row!.FirstName).Should().Be("Updated");
        ((string)row.LastName).Should().Be("Name");
        ((string)row.PhoneNumber).Should().Be("0123456789");
    }

    [Fact]
    public async Task UpdateUserAsync_WhenUserNotExists_ReturnsFalse()
    {
        var result = await _repo.UpdateUserAsync(Guid.NewGuid(), new UpdateUserRequest
        {
            FirstName = "Ghost"
        });

        result.Should().BeFalse();
    }

    [Fact]
    public async Task AssignRolesAsync_WhenRolesDoNotExist_CreatesUserRoles()
    {
        using var conn = _dbFactory.Create();
        var (userId, _) = await DatabaseSeed.SeedUserWithRoleAsync(conn, roleName: "User");

        // Tạo role "Admin" trong DB, sau đó gán cho user
        var adminRoleId = await DatabaseSeed.SeedRoleAsync(conn, "Admin");

        await _repo.AssignRolesAsync(userId, ["Admin"]);

        // Verify: user có 2 roles
        var roles = await conn.QueryAsync<string>("""
            SELECT r.Name FROM AspNetUserRoles ur
            INNER JOIN AspNetRoles r ON r.Id = ur.RoleId
            WHERE ur.UserId = @UserId
            """, new { UserId = userId });
        roles.Should().Contain(["User", "Admin"]);
    }

    [Fact]
    public async Task AssignRolesAsync_WhenRoleAlreadyAssigned_DoesNotDuplicate()
    {
        using var conn = _dbFactory.Create();
        var (userId, roleId) = await DatabaseSeed.SeedUserWithRoleAsync(conn, roleName: "Admin");

        await _repo.AssignRolesAsync(userId, ["Admin"]);

        var count = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM AspNetUserRoles WHERE UserId = @UserId AND RoleId = @RoleId",
            new { UserId = userId, RoleId = roleId });
        count.Should().Be(1);
    }

    [Fact]
    public async Task RemoveRolesAsync_RemovesSpecifiedRolesOnly()
    {
        using var conn = _dbFactory.Create();
        var (userId, _) = await DatabaseSeed.SeedUserWithRoleAsync(conn, roleName: "User");

        var adminRoleId = await DatabaseSeed.SeedRoleAsync(conn, "Admin");
        await conn.ExecuteAsync(
            "INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)",
            new { UserId = userId, RoleId = adminRoleId });

        await _repo.RemoveRolesAsync(userId, ["User"]);

        var remaining = await conn.QueryAsync<string>("""
            SELECT r.Name FROM AspNetUserRoles ur
            INNER JOIN AspNetRoles r ON r.Id = ur.RoleId
            WHERE ur.UserId = @UserId
            """, new { UserId = userId });
        remaining.Should().ContainSingle("Admin");
        remaining.Should().NotContain("User");
    }

    [Fact]
    public async Task SoftDeleteUserAsync_WhenUserExists_SetsLockoutForever()
    {
        using var conn = _dbFactory.Create();
        var (userId, _) = await DatabaseSeed.SeedUserWithRoleAsync(conn);

        var result = await _repo.SoftDeleteUserAsync(userId);
        result.Should().BeTrue();

        var row = await conn.QuerySingleOrDefaultAsync(
            "SELECT LockoutEnabled, LockoutEnd FROM AspNetUsers WHERE Id = @Id",
            new { Id = userId });
        row.Should().NotBeNull();
        ((bool)row!.LockoutEnabled).Should().BeTrue();
        ((DateTimeOffset)row.LockoutEnd).Should().BeCloseTo(DateTimeOffset.MaxValue, TimeSpan.FromDays(365));
    }
}
