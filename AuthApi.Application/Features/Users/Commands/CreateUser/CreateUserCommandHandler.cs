using AuthApi.Domain.Interfaces;
using AuthApi.Application.Abstractions.Messaging;
using User = AuthApi.Domain.Entities.Users;

namespace AuthApi.Application.Features.Users.Commands.CreateUser
{
    public class CreateUserCommandHandler(IUserRepository repoUser) : ICommandHandler<CreateUserCommand, Guid>
    {
        public async Task<Guid> Handle(CreateUserCommand req, CancellationToken cancellationToken)
        {
            var user = User.Create(req.Age, req.Role, req.Name);

            await repoUser.AddAsync(user);

            await repoUser.CommitAsync();

            return user.Id;
        }
    }
}
