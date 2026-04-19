using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.Commands.Login;
using AuthApi.Application.Features.Auth.Commands.Register;
using AuthApi.Application.Features.Auth.DTOs.Login;
using AuthApi.Application.Features.Auth.DTOs.Register;

namespace AuthApi.Application.Abstractions.Repositories.Auth
{
    public interface IIdentityService
    {
        Task<Result<LoginResponse>> LoginAsync(LoginCommand req);
        Task<Result<RegisterResponse>> RegisterAsync(RegisterCommand req);
        Task<Result<bool>> VerifyEmailAsync(Guid userId, string token);
    }
}
