using FluentValidation;

namespace AuthApi.Application.Features.Users.Commands.RemoveRoles;

public sealed class RemoveRolesCommandValidator : AbstractValidator<RemoveRolesCommand>
{
    public RemoveRolesCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Roles)
            .NotEmpty()
            .WithMessage("At least one role must be specified");
    }
}
