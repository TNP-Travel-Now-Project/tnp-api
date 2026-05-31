using System.Net.NetworkInformation;
using System.Security.Cryptography;

namespace AuthApi.WebApi.Middlewares
{
    public class SecureHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _env;

        public SecureHeadersMiddleware(RequestDelegate next, IWebHostEnvironment env)
        {
            _next = next;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var isdev = _env.IsDevelopment();

            var nonceBytes = new byte[32];
            RandomNumberGenerator.Fill(nonceBytes);
            var nonce = Convert.ToBase64String(nonceBytes);

            context.Items["CSPNonce"] = nonce;

            string csp;

            csp = isdev ? ConfigCSPForDev() : ConfigCSPForProd(nonce);

            context.Response.Headers.Append("Content-Security-Policy",
                csp.Replace("\r\n", " ").Replace("\n", " ").Trim());

            // Các header bảo mật chung
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

            //if (!isdev) 
            if (isdev)
            {
                context.Response.Headers.Append("Strict-Transport-Security",
                    "max-age=31536000; includeSubDomains; preload");
            }

            await _next(context);
        }

        // CSP: Context Security Polly
        private string ConfigCSPForDev()
            => $@"
                default-src 'self';
                script-src 
                    'self' 
                    'unsafe-inline' 
                    'unsafe-eval' 
                    https://your-nextjs-domain.com 
                    https://localhost:* 
                    ws://localhost:* 
                    wss://localhost:*
                style-src 
                    'self' 
                    'unsafe-inline';
                img-src 'self' data: https: blob:;
                font-src 'self' https: data:;
                connect-src 
                    'self' 
                    https://localhost:*;
                    https://your-api-domain.com 
                    https://your-nextjs-domain.com 
                    ws://localhost:* 
                    wss://localhost:* 
                frame-ancestors 'self';
                object-src 'none';
                base-uri 'self';
                form-action 'self';
            ";

        private string ConfigCSPForProd(string nonce)
            => $@"
                default-src 'self';
                script-src 
                    'self' 
                    'nonce-{nonce}' 
                    'strict-dynamic' 
                    https://your-nextjs-domain.com;
                style-src 
                    'self' 
                    'unsafe-inline';
                img-src 'self' data: https:;
                font-src 'self' https: data:;
                connect-src 
                    'self' 
                    https://your-api-domain.com 
                    https://your-nextjs-domain.com 
                    wss://your-nextjs-domain.com;
                frame-ancestors 'none';
                object-src 'none';
                base-uri 'self';
                form-action 'self';
                upgrade-insecure-requests;
                block-all-mixed-content;
            ";
    }
}
