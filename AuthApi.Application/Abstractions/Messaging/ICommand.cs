using MediatR;

namespace AuthApi.Application.Abstractions.Messaging
{
    public interface ICommand : IRequest;

    public interface ICommand<out TResponse> : IRequest<TResponse>;
}
