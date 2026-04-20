using AuthApi.Application.Configuration;
using AuthApi.Infrastructure.Common;
using AuthApi.Infrastructure.Configuration;
using AuthApi.Infrastructure.Identities;
using AuthApi.Infrastructure.Identities.Seeds;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using AuthApi.Infrastructure.Services.Auth;
using Microsoft.OpenApi;


namespace AuthApi.WebApi
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

            builder.Services.AddHttpContextAccessor();

            var connectionString = builder.Configuration.GetConnectionString("Default");
            if (string.IsNullOrEmpty(connectionString))
                throw new Exception("Connectstring invalid");

            builder.Services.Configure<AppSettings>(
                 builder.Configuration.GetSection("AppSettings")
            );

            builder.Services.AddApplication()
                            .AddInfrastructure(builder.Configuration, connectionString);

            builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
            {
                options.TokenLifespan = TimeSpan.FromMinutes(30);
            });

            #region Config CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowNextJS",
                    policy =>
                    {
                        policy.WithOrigins("http://localhost:3000")
                              .AllowAnyMethod()
                              .AllowAnyHeader();
                    });
            });
            #endregion

            #region config JWT
            var jwtSettings = builder.Configuration.GetSection("AppSettings");
            var key = Encoding.UTF8.GetBytes(jwtSettings["JwtKey"]!);

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })

            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["JwtIssuer"],
                    ValidAudience = jwtSettings["JwtAudience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero   // Không cho phép lệch giờ (production yêu cầu strict)
                };

                // Xử lý sự kiện (custom response)
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        // Log lỗi (không lộ chi tiết cho client)
                        Console.WriteLine($"Authentication failed: {context.Exception.Message}");

                        if (context.Exception is SecurityTokenExpiredException)
                        {
                            context.Response.Headers.Append("Token-Expired", "true");
                        }

                        return Task.CompletedTask;
                    },

                    OnChallenge = context =>
                    {
                        // Tùy chỉnh response khi không có token hoặc token invalid
                        context.HandleResponse(); // Ngăn chặn response mặc định
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";

                        var result = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            error = "unauthorized",
                            message = "You must provide a valid access token."
                        });

                        return context.Response.WriteAsync(result);
                    }
                };
            });
            #endregion

            #region Add Authorize for swagger
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Your API Title",
                    Version = "v1",
                    Description = "ASP.NET Core Web API với Google OAuth + JWT"
                });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Nhập JWT token theo định dạng: Bearer {token}\nVí dụ: Bearer eyJhbGciOiJIUzI1NiIs..."
                });

                options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });
            });
            #endregion

            var app = builder.Build();

            #region Seeds role
            using (var scope = app.Services.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                await RoleSeeder.SeedAsync(roleManager);
                await RoleSeeder.SeedAdminAsync(userManager, roleManager);
            }
            #endregion

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                app.UseExceptionHandler("/error"); //global error handler
            }

            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = new[] { new HangfireAuthFilter() },
                DashboardTitle = "Hangfire – Admin"
            });

            app.UseCors("AllowNextJS");

            app.UseHttpsRedirection();

            app.UseAuthentication();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
