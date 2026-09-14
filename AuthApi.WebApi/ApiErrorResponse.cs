using AuthApi.Application.Common;
using System.Text.Json.Serialization;


namespace AuthApi.WebApi
{
    public class ApiErrorResponse
    {
        public string Code { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IDictionary<string, FieldError[]>? Errors { get; init; }

        public ApiErrorResponse() { }

        public ApiErrorResponse(Error error)
        {
            Code = error.Code;
            Message = error.Message;
            Errors = error.Errors;
        }
    }
}