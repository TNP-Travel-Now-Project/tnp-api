using AuthApi.Application.Abstractions.Interfaces.Cache;
using AuthApi.Application.Abstractions.Interfaces.Repositories;
using AuthApi.Application.Common;
using AuthApi.Application.Common.Security;
using AuthApi.Application.Features.Users.DTOs;
using AuthApi.Application.Features.Users.Queries.Me;
using FluentAssertions;
using Moq;

namespace AuthApi.Tests.Features.Users.Queries.Me;

public sealed class MeQueryHandlerTests
{
    private readonly Mock<IUserReadRepository> _userRepoMock;
    private readonly Mock<IUserContext> _contextMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly MeQueryHandler _handler;
    private readonly MeQuery _query = new();

    public MeQueryHandlerTests()
    {
        _userRepoMock = new Mock<IUserReadRepository>();
        _contextMock = new Mock<IUserContext>();
        _cacheMock = new Mock<ICacheService>();

        _cacheMock
            .Setup(c => c.GetAsync<MeResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MeResponse?)null);

        _handler = new MeQueryHandler(_userRepoMock.Object, _contextMock.Object, _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ReturnsFail()
    {
        _contextMock.Setup(c => c.IsAuthenticated).Returns(false);

        var result = await _handler.Handle(_query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Message.Should().Be("User not authenticated");
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsFail()
    {
        var userId = Guid.NewGuid();
        _contextMock.Setup(c => c.IsAuthenticated).Returns(true);
        _contextMock.Setup(c => c.UserId).Returns(userId);
        _userRepoMock
            .Setup(r => r.GetMeAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MeResponse?)null);

        var result = await _handler.Handle(_query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Message.Should().Be("User not found");
    }

    [Fact]
    public async Task Handle_WhenUserExists_ReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        var meResponse = new MeResponse
        {
            Id = userId,
            Email = "test@test.com",
            UserName = "TestUser",
            FirstName = "Test",
            LastName = "User",
            Roles = ["User"],
            CreatedAt = DateTime.UtcNow
        };

        _contextMock.Setup(c => c.IsAuthenticated).Returns(true);
        _contextMock.Setup(c => c.UserId).Returns(userId);
        _userRepoMock
            .Setup(r => r.GetMeAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(meResponse);

        var result = await _handler.Handle(_query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(meResponse);
    }

    [Fact]
    public async Task Handle_WhenCacheHit_DoesNotCallRepository()
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
        _cacheMock
            .Setup(c => c.GetAsync<MeResponse>($"cache:me:{userId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResponse);

        var result = await _handler.Handle(_query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(cachedResponse);
        _userRepoMock.Verify(r => r.GetMeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCacheFallsBackToDb_SetsCache()
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
        _userRepoMock
            .Setup(r => r.GetMeAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbResponse);

        var result = await _handler.Handle(_query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _cacheMock.Verify(
            c => c.SetAsync($"cache:me:{userId}", dbResponse, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
