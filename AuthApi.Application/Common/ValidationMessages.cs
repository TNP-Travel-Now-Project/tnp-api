namespace AuthApi.Application.Common
{
    public static class ValidationMessages
    {
        public const string FieldRequired = "{PropertyName} cannot be empty";
        public const string EmailInvalid = "Email is not in a valid format";
        public const string EmailMaxLength = "Email cannot exceed {MaxLength} characters";

        public const string PasswordMinLength = "Password must be at least {MinLength} characters";
        public const string PasswordMaxLength = "Password cannot exceed {MaxLength} characters";
        public const string PasswordLengthRange = "Password must be between {MinLength} and {MaxLength} characters";
        public const string PasswordRequiresUppercase = "Password must contain at least one uppercase letter";
        public const string PasswordRequiresLowercase = "Password must contain at least one lowercase letter";
        public const string PasswordRequiresDigit = "Password must contain at least one digit";
        public const string PasswordRequiresSpecial = "Password must contain at least one special character (~!@#$%^&*()_+=?)";
        public const string PasswordsMustMatch = "Confirmation password must match the password";

        public const string OtpRequired = "OTP code cannot be empty";
        public const string OtpLength = "OTP code must be exactly {ExpectedLength} digits";
        public const string OtpDigits = "OTP code can only contain digits";

        public const string FullNameRequired = "Full name cannot be empty";
        public const string FullNameMaxLength = "Full name cannot exceed {MaxLength} characters";

        public const string UserNameRequired = "Username cannot be empty";
        public const string UserNameMaxLength = "Username cannot exceed {MaxLength} characters";

        public const string PhoneNumberRequired = "Phone number cannot be empty";
        public const string PhoneNumberLength = "Phone number must be exactly {ExpectedLength} digits";
        public const string PhoneNumberDigits = "Phone number can only contain digits (0-9)";

        public const string DateOfBirthRequired = "Date of birth cannot be empty";
        public const string DateOfBirthRange = "Age must be between {From} and {To} years";

        public const string ValidationFailed = "Invalid request data";
        public const string InternalServerError = "An error occurred on the server. Please try again later";
    }
}