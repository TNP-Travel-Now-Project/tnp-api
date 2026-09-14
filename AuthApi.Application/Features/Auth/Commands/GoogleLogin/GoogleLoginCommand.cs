using AuthApi.Application.Abstractions.Messaging.Command;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.DTOs;

namespace AuthApi.Application.Features.Auth.Commands.GoogleLogin;

public sealed record GoogleLoginCommand(string TokenId) : ICommand<Result<LoginResponse>>;
