using Microsoft.AspNetCore.Identity;

namespace AuthApi.Infrastructure.Identities;

public class CustomIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError PasswordTooShort(int length)
        => new()
        {
            Code = nameof(PasswordTooShort),
            Description = $"Password must be at least {length} characters"
        };

    public override IdentityError PasswordRequiresDigit()
        => new()
        {
            Code = nameof(PasswordRequiresDigit),
            Description = "Password must contain at least one digit"
        };

    public override IdentityError PasswordRequiresLower()
        => new()
        {
            Code = nameof(PasswordRequiresLower),
            Description = "Password must contain at least one lowercase letter"
        };

    public override IdentityError PasswordRequiresUpper()
        => new()
        {
            Code = nameof(PasswordRequiresUpper),
            Description = "Password must contain at least one uppercase letter"
        };

    public override IdentityError PasswordRequiresNonAlphanumeric()
        => new()
        {
            Code = nameof(PasswordRequiresNonAlphanumeric),
            Description = "Password must contain at least one special character (~!@#$%^&*()_+=?)"
        };

    public override IdentityError DuplicateEmail(string email)
        => new()
        {
            Code = nameof(DuplicateEmail),
            Description = $"Email '{email}' is already registered"
        };

    public override IdentityError DuplicateUserName(string userName)
        => new()
        {
            Code = nameof(DuplicateUserName),
            Description = $"Username '{userName}' is already taken"
        };

    public override IdentityError InvalidEmail(string? email)
        => new()
        {
            Code = nameof(InvalidEmail),
            Description = "Email is not in a valid format"
        };

    public override IdentityError InvalidUserName(string? userName)
        => new()
        {
            Code = nameof(InvalidUserName),
            Description = "Username is not in a valid format"
        };

    public override IdentityError PasswordMismatch()
        => new()
        {
            Code = nameof(PasswordMismatch),
            Description = "Incorrect password"
        };

    public override IdentityError LoginAlreadyAssociated()
        => new()
        {
            Code = nameof(LoginAlreadyAssociated),
            Description = "This login is already associated with another account"
        };

    public override IdentityError UserLockoutNotEnabled()
        => new()
        {
            Code = nameof(UserLockoutNotEnabled),
            Description = "Lockout is not enabled for this user"
        };

    public override IdentityError UserAlreadyHasPassword()
        => new()
        {
            Code = nameof(UserAlreadyHasPassword),
            Description = "User already has a password set"
        };

    public override IdentityError UserNotInRole(string role)
        => new()
        {
            Code = nameof(UserNotInRole),
            Description = $"User is not in role '{role}'"
        };
}