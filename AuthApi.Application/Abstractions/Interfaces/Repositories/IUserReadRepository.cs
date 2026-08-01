using AuthApi.Application.Features.Auth.DTOs;
using AuthApi.Application.Features.Users.DTOs;

namespace AuthApi.Application.Abstractions.Interfaces.Repositories;

public interface IUserReadRepository
{
    Task<MeResponse?> GetMeAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<UserListItemDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<UserDetailResponse?> GetUserDetailAsync(Guid userId, CancellationToken cancellationToken = default);
}
