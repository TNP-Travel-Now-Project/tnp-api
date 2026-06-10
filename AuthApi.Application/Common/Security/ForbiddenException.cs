namespace AuthApi.Application.Common.Security;

public sealed class ForbiddenException(string message) : Exception(message);
