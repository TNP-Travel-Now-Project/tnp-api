using FluentValidation;

namespace AuthApi.Application.Features.Users.Commands.AssignRoles;

public sealed class AssignRolesCommandValidator : AbstractValidator<AssignRolesCommand>
{
    public AssignRolesCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Roles)
            .NotEmpty()
            .WithMessage("At least one role must be specified");
    }
}
