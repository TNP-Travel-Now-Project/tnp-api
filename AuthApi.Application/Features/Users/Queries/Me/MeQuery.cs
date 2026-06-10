using AuthApi.Application.Abstractions.Messaging.Query;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Users.DTOs;

namespace AuthApi.Application.Features.Users.Queries.Me
{
    public sealed record MeQuery : IQuery<Result<MeResponse>>;
}
