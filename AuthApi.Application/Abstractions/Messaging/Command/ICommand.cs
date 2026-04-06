using MediatR;

namespace AuthApi.Application.Abstractions.Messaging.Command
{
    public interface ICommand : IRequest;

    public interface ICommand<out TResponse> : IRequest<TResponse>;
}
