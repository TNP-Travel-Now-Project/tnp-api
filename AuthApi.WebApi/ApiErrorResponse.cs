using System.Text.Json.Serialization;
using AuthApi.Application.Common;

namespace AuthApi.WebApi
{
    public class ApiErrorResponse
    {
        public string Code { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IDictionary<string, string[]>? Errors { get; init; }

        public ApiErrorResponse() { }

        public ApiErrorResponse(Error error)
        {
            Code = error.Code;
            Message = error.Message;
        }
    }
}
