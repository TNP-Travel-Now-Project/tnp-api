namespace AuthApi.Application.Common
{
    public static class AuthErrors
    {
        public static readonly Error InvalidCredentials = new(ErrorCodes.InvalidCredentials, "Invalid credentials");
        public static readonly Error UserLockedOut = new(ErrorCodes.UserLockedOut, "User is locked");
        public static readonly Error EmailNotConfirmed = new(ErrorCodes.EmailNotConfirmed, "Email is not confirmed");
        public static readonly Error EmailInvalid = new(ErrorCodes.EmailInvalid, "Email is invalid");
        public static readonly Error RedisFailure = new(ErrorCodes.RedisError, "Redis operation failed");
        public static readonly Error OtpExpired = new(ErrorCodes.OtpExpired, "OTP expired or not found");
        public static readonly Error OtpIncorrect = new(ErrorCodes.OtpIncorrect, "OTP incorrect");
        public static readonly Error UserNotFound = new(ErrorCodes.UserNotFound, "User not found");
        public static readonly Error TokenInvalid = new(ErrorCodes.InvalidToken, "Token is invalid");
        public static readonly Error TokenGenerationFailed = new(ErrorCodes.TokenGenerationError, "Failed to generate email confirmation token");
    }
}