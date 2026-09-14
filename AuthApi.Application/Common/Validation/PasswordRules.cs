using FluentValidation;

namespace AuthApi.Application.Common.Validation
{
    public static class PasswordRules
    {
        public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder
                .NotEmpty().WithMessage(ValidationMessages.FieldRequired)
                .Length(8, 20).WithMessage(ValidationMessages.PasswordLengthRange)
                .Matches("[A-Z]").WithMessage(ValidationMessages.PasswordRequiresUppercase)
                .Matches("[a-z]").WithMessage(ValidationMessages.PasswordRequiresLowercase)
                .Matches("[0-9]").WithMessage(ValidationMessages.PasswordRequiresDigit)
                .Matches("[~!@#$%^&*()_+=?]").WithMessage(ValidationMessages.PasswordRequiresSpecial);
        }
    }
}
