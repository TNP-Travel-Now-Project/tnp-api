using AuthApi.Application.Abstractions.Messaging;
using AuthApi.Application.Features.Users.DTOs;

namespace AuthApi.Application.Features.Users.Queries.GetUsers
{
    public sealed record GetAllUserQuery : IQuery<List<UserDto>>;
}
