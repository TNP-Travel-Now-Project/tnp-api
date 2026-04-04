using MediatR;

namespace AuthApi.Application.Abstractions.Messaging
{
    public interface IQuery<out TReponse> : IRequest<TReponse>;
}
