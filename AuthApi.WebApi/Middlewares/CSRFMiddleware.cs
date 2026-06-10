using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace AuthApi.WebApi.Middlewares
{
    public class CSRFMiddleware(RequestDelegate _next)
    {
        private string apiAuth { get; } = "/api/auth";

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

            if (!string.IsNullOrEmpty(path)
                && path.StartsWith($"{apiAuth}/login")
                || path.StartsWith($"{apiAuth}/register")
                || path.StartsWith($"{apiAuth}/refresh-token")
                || path.StartsWith($"{apiAuth}/logout"))
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
