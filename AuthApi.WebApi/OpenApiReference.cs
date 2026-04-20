using Microsoft.OpenApi;

namespace AuthApi.WebApi
{
    internal class OpenApiReference
    {
        public ReferenceType Type { get; set; }
        public string Id { get; set; }
    }
}