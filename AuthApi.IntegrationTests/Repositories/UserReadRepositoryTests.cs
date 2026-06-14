using AuthApi.Application.Features.Users.DTOs;
using AuthApi.Infrastructure.Persistence.Dapper.Repositories;
using AuthApi.IntegrationTests.Fixtures;
using AuthApi.Infrastructure.Persistence.Connection;
using Dapper;
using FluentAssertions;

namespace AuthApi.IntegrationTests.Repositories;

/// <summary>
/// Integration tests cho UserReadRepository.
/// Mỗi test chạy trên SQL Server thật (trong container Docker).
/// </summary>
[Collection("SqlServerCollection")]
public sealed class UserReadRepositoryTests
{
    private readonly UserReadRepository _repo;
    private readonly IDbConnectionFactory _dbFactory;

    public UserReadRepositoryTests(SqlServerFixture fixture)
    {
        _repo = new UserReadRepository(fixture.DbConnectionFactory);
        _dbFactory = fixture.DbConnectionFactory;
    }

    [Fact]
    public async Task GetMeAsync_WithSeedData_ReturnsCorrectUser()
    {
        using var conn = _dbFactory.Create();
        var (userId, _) = await DatabaseSeed.SeedUserWithRoleAsync(
            conn,
            email: "getme@test.com",
            userName: "GetMeUser");

        var result = await _repo.GetMeAsync(userId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.Email.Should().Be("getme@test.com");
        result.UserName.Should().Be("GetMeUser");
        result.FirstName.Should().Be("Test");
        result.LastName.Should().Be("User");
        result.EmailConfirmed.Should().BeFalse();
        result.Roles.Should().ContainSingle("User");
    }

    [Fact]
    public async Task GetMeAsync_WhenUserHasMultipleRoles_ReturnsAllRoles()
    {
        using var conn = _dbFactory.Create();
        var (userId, _) = await DatabaseSeed.SeedUserWithRoleAsync(conn, roleName: "User");

        var adminRoleId = await DatabaseSeed.SeedRoleAsync(conn, "Admin");
        await conn.ExecuteAsync(
            "INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)",
            new { UserId = userId, RoleId = adminRoleId });

        var result = await _repo.GetMeAsync(userId);

        result.Should().NotBeNull();
        result!.Roles.Should().HaveCount(2);
        result.Roles.Should().Contain(["User", "Admin"]);
    }

    [Fact]
    public async Task GetMeAsync_WhenUserNotFound_ReturnsNull()
    {
        var result = await _repo.GetMeAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllUsersAsync_WithSeedData_ReturnsAllUsers()
    {
        using var conn = _dbFactory.Create();
        await DatabaseSeed.SeedUserWithRoleAsync(conn, email: "user1@test.com");
        await DatabaseSeed.SeedUserWithRoleAsync(conn, email: "user2@test.com");

        var result = await _repo.GetAllUsersAsync();

        result.Should().NotBeNull();
        result.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetUserDetailAsync_WithSeedData_ReturnsFullDetail()
    {
        using var conn = _dbFactory.Create();
        var (userId, _) = await DatabaseSeed.SeedUserWithRoleAsync(
            conn,
            email: "detail@test.com",
            roleName: "Admin");

        var result = await _repo.GetUserDetailAsync(userId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.Email.Should().Be("detail@test.com");
        result.Roles.Should().ContainSingle("Admin");
        result.IsLockedOut.Should().BeFalse();
        result.EmailConfirmed.Should().BeFalse();
    }
}

