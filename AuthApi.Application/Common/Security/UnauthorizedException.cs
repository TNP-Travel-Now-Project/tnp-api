namespace AuthApi.Application.Common.Security;

public sealed class UnauthorizedException(string message) : Exception(message);
