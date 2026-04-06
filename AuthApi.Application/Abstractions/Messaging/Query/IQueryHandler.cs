using MediatR;

namespace AuthApi.Application.Abstractions.Messaging.Query
{
    public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
        where TQuery : IQuery<TResponse>;
}
