using AuthApi.Application.Abstractions.Messaging.Command;

namespace AuthApi.Application.Features.Users.Commands.UpdateUser;

public sealed record UpdateUserCommand(
    Guid UserId,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    DateOnly? DateOfBirth) : ICommand;
