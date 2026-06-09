using AuthApi.Application.Features.Users.DTOs;

namespace AuthApi.Application.Abstractions.Interfaces.Repositories;

public interface IUserWriteRepository
{
    Task<bool> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task AssignRolesAsync(Guid userId, string[] roles, CancellationToken cancellationToken = default);
    Task RemoveRolesAsync(Guid userId, string[] roles, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
