using AuthApi.Application.Features.Auth.DTOs;
using AuthApi.Application.Features.Users.DTOs;

namespace AuthApi.Application.Abstractions.Repositories
{
    public interface IUserQueryRepository
    {
        Task<List<UserDto>> GetAllUserAsync(CancellationToken cancellationToken = default);
    }
}
