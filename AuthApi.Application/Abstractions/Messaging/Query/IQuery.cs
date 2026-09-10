using MediatR;

namespace AuthApi.Application.Abstractions.Messaging.Query
{
    public interface IQuery<out TResponse> : IRequest<TResponse>;
}
