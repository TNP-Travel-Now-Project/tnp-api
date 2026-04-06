using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.Commands.Register;
using AuthApi.Application.Features.Auth.DTOs.Register;
using System.Security.Authentication.ExtendedProtection;

namespace AuthApi.Application.Abstractions.Repositories.Auth
{
    public interface IIdentityService
    {
        //Task<Result<RegisterResponse>> GetUsersAsync(RegisterCommand req);
        Task<Result<RegisterResponse>> RegisterAsync(RegisterCommand req);
        Task<Result<bool>> VerifyEmailAsync(Guid userId, string token);
    }
}
