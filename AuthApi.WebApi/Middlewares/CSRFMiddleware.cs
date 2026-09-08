using Microsoft.OpenApi;

namespace AuthApi.WebApi.Middlewares
{
    public class CSRFMiddleware(RequestDelegate _next)
    {
        private const string apiAuth = "/api/auth";
        private string[] bypassPaths { get; } = { $"{apiAuth}/login", $"{apiAuth}/register", $"{apiAuth}/refresh-token", $"{apiAuth}/google-login" };

        private readonly string[] SafeMethods = {
            HttpMethods.Get,
            HttpMethods.Head,
            HttpMethods.Options,
        };

        private readonly string[] ValidMethods = {
            HttpMethods.Post,
            HttpMethods.Put,
            HttpMethods.Patch,
            HttpMethods.Delete,
        };

        public async Task InvokeAsync(HttpContext _context)
        {
            var method = _context.Request.Method;

            if (SafeMethods.Contains(method))
            {
                await _next(_context);
                return;
            }

            var path = _context.Request.Path.Value?.ToLower();

            if (path == null)
            {
                _context.Response.StatusCode = 400;
                await _context.Response.WriteAsync("Path requested not found");
                return;
            }

            if (bypassPaths.Contains(path))
            {
                await _next(_context);
                return;
            }

            if (ValidMethods.Contains(method))
            {
                var cookieToken = _context.Request.Cookies["CSRF-TOKEN"];
                var headerToken = _context.Request.Headers["X-CSRF-TOKEN"].FirstOrDefault();

                if (string.IsNullOrEmpty(cookieToken)
                    || string.IsNullOrEmpty(headerToken)
                    || cookieToken != headerToken)
                {
                    _context.Response.StatusCode = 403;
                    await _context.Response.WriteAsync("CSRF validation failed");
                    return;
                }
            }

            await _next(_context);
        }
    }
}
