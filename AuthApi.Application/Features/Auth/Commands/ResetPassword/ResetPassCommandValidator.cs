using FluentValidation;

namespace AuthApi.Application.Features.Auth.Commands.ResetPassword
{
    public class ResetPassCommandValidator : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPassCommandValidator()
        {
            RuleFor(p => p.Email)
                .NotEmpty().WithMessage("Email is not empty")
                .EmailAddress().WithMessage("Email wrong format");

            RuleFor(p => p.Otp)
                .NotEmpty().WithMessage("OTP is not empty")
                .Length(4).WithMessage("OTP code must have 4 digits");

            RuleFor(p => p.NewPass)
                .NotEmpty().WithMessage("Password is not empty")
                .Length(8, 20)
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches("[0-9]").WithMessage("Password must contain at least one number")
                .Matches("[~!@#$%^&*()_+=?]").WithMessage("Password must cotain at least one special character");
        }
    }
}
