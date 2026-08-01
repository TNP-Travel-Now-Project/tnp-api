using AuthApi.Application.Abstractions.Interfaces.Cache;
using AuthApi.Application.Abstractions.Interfaces.Repositories;
using AuthApi.Application.Common;
using AuthApi.Application.Common.Security;
using AuthApi.Application.Features.Auth.DTOs;
using AuthApi.Application.Features.Auth.Queries.Me;
using FluentAssertions;
using Moq;

namespace AuthApi.Tests.Features.Users.Queries.Me;

public sealed class MeQueryHandlerTests
{
    private readonly Mock<IUserReadRepository> _userRepoMock;
    private readonly Mock<IUserContext> _contextMock;
    private readonly Mock<ICallCacheService> _callCacheMock;
    private readonly MeQueryHandler _handler;
    private readonly MeQuery _query = new();

    public MeQueryHandlerTests()
    {
        _userRepoMock = new Mock<IUserReadRepository>();
        _contextMock = new Mock<IUserContext>();
        _callCacheMock = new Mock<ICallCacheService>();

        _handler = new MeQueryHandler(_userRepoMock.Object, _contextMock.Object, _callCacheMock.Object);
    }

    /*
     * Code path: !_context.IsAuthenticated ──▶ return Fail
     */
    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ReturnsUnauthorized()
    {
        _contextMock.Setup(c => c.IsAuthenticated).Returns(false);

        var result = await _handler.Handle(_query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(ErrorCodes.UserNotFound);
        result.Error!.Message.Should().Be("User not authenticated");
    }

    /*
     * Code path: cache hit ──▶ return Success (không gọi DB)
     */
    [Fact]
    public async Task Handle_WhenCacheHit_ReturnsCachedResponseAndDoesNotCallDb()
    {
        var userId = Guid.NewGuid();
        var cachedResponse = new MeResponse
        {
            Id = userId,
            Email = "cached@test.com",
            UserName = "CachedUser",
            Roles = ["User"],
            CreatedAt = DateTime.UtcNow
        };

        _contextMock.Setup(c => c.IsAuthenticated).Returns(true);
        _contextMock.Setup(c => c.UserId).Returns(userId);
        _callCacheMock
            .Setup(c => c.TryGetCachedAsync($"cache:me:{userId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResponse);

        var result = await _handler.Handle(_query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(cachedResponse);
        _userRepoMock.Verify(r => r.GetMeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /*
     * Code path: cache miss + user not found in DB ──▶ return Fail
     */
    [Fact]
    public async Task Handle_WhenCacheMissAndUserNotFound_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();

        _contextMock.Setup(c => c.IsAuthenticated).Returns(true);
        _contextMock.Setup(c => c.UserId).Returns(userId);
        _callCacheMock
            .Setup(c => c.TryGetCachedAsync($"cache:me:{userId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync((MeResponse?)null);
        _userRepoMock
            .Setup(r => r.GetMeAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MeResponse?)null);

        var result = await _handler.Handle(_query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(ErrorCodes.UserNotFound);
        result.Error!.Message.Should().Be("User not found");
    }

    /*
     * Code path: cache miss + user exists ──▶ set cache + return Success
     */
    [Fact]
    public async Task Handle_WhenCacheMissAndUserExists_ReturnsSuccessAndSetsCache()
    {
        var userId = Guid.NewGuid();
        var dbResponse = new MeResponse
        {
            Id = userId,
            Email = "db@test.com",
            UserName = "DbUser",
            Roles = ["User"],
            CreatedAt = DateTime.UtcNow
        };

        _contextMock.Setup(c => c.IsAuthenticated).Returns(true);
        _contextMock.Setup(c => c.UserId).Returns(userId);
        _callCacheMock
            .Setup(c => c.TryGetCachedAsync($"cache:me:{userId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync((MeResponse?)null);
        _userRepoMock
            .Setup(r => r.GetMeAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbResponse);

        var result = await _handler.Handle(_query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(dbResponse);
        _callCacheMock.Verify(
            c => c.TrySetCacheAsync($"cache:me:{userId}", dbResponse, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /*
     * Code path: cache miss + user exists ──▶ returns response with correct roles
     */
    [Fact]
    public async Task Handle_WhenUserHasMultipleRoles_ReturnsAllRoles()
    {
        var userId = Guid.NewGuid();
        var meResponse = new MeResponse
        {
            Id = userId,
            Email = "admin@test.com",
            UserName = "AdminUser",
            Roles = ["User", "Admin"],
            CreatedAt = DateTime.UtcNow
        };

        _contextMock.Setup(c => c.IsAuthenticated).Returns(true);
        _contextMock.Setup(c => c.UserId).Returns(userId);
        _callCacheMock
            .Setup(c => c.TryGetCachedAsync($"cache:me:{userId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync((MeResponse?)null);
        _userRepoMock
            .Setup(r => r.GetMeAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(meResponse);

        var result = await _handler.Handle(_query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Roles.Should().HaveCount(2);
        result.Value!.Roles.Should().Contain("User");
        result.Value!.Roles.Should().Contain("Admin");
    }
}
