namespace AuthApi.Application.Common
{
    public static class ErrorCodes
    {
        public const string InvalidCredentials = "INVALID_CREDENTIALS";
        public const string UserLockedOut = "USER_LOCKED_OUT";
        public const string EmailNotConfirmed = "EMAIL_NOT_CONFIRMED";
        public const string UserNotFound = "USER_NOT_FOUND";
        public const string InvalidToken = "INVALID_TOKEN";
        public const string OtpExpired = "OTP_EXPIRED";
        public const string OtpIncorrect = "OTP_INCORRECT";
        public const string EmailInvalid = "EMAIL_INVALID";
        public const string RedisError = "REDIS_ERROR";
        public const string TokenRefreshError = "TOKEN_REFRESH_ERROR";
        public const string RegistrationError = "REGISTRATION_ERROR";
        public const string TokenGenerationError = "TOKEN_GENERATION_ERROR";
        public const string ValidationError = "VALIDATION_ERROR";
        public const string GeneralError = "GENERAL_ERROR";
        public const string InternalError = "INTERNAL_ERROR";
    }
}
