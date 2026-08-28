namespace AuthApi.Application.Common
{
    public record Error(
        string Code,
        string Message,
        IDictionary<string, FieldError[]>? Errors = null);
}
