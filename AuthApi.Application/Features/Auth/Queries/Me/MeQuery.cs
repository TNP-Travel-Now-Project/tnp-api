using AuthApi.Application.Abstractions.Messaging.Query;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.DTOs;

namespace AuthApi.Application.Features.Auth.Queries.Me
{
    public sealed record MeQuery : IQuery<Result<MeResponse>>;
}
