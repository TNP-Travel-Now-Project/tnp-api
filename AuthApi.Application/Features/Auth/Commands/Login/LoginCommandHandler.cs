using AuthApi.Domain.Interfaces;
using AuthApi.Application.Abstractions.Messaging;
using User = AuthApi.Domain.Entities.Users;

namespace AuthApi.Application.Features.Auth.Commands.CreateUser
{
    public class LoginCommandHandler(IUserRepository repoUser) : ICommandHandler<LoginCommand, Guid>
    {
        public async Task<Guid> Handle(LoginCommand req, CancellationToken cancellationToken)
        {
            var user = User.Create(req.Age, req.Role, req.Name);

            await repoUser.AddAsync(user);

            await repoUser.CommitAsync();

            return user.Id;
        }
    }
}
