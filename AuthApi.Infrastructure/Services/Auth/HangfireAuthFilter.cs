using Hangfire.Dashboard;

namespace AuthApi.Infrastructure.Services.Auth
{
    public class HangfireAuthFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            // Cho phép chỉ khi:
            // 1. Đã đăng nhập
            // 2. Có role là Admin (hoặc thêm role khác nếu muốn)
            return httpContext.User.Identity?.IsAuthenticated == true
                   && httpContext.User.IsInRole("Admin");
        }
    }
}
