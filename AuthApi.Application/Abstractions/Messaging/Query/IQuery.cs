using MediatR;

namespace AuthApi.Application.Abstractions.Messaging.Query
{
    public interface IQuery<out TReponse> : IRequest<TReponse>;
}
