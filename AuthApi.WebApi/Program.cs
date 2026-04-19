using AuthApi.Application.Configuration;
using AuthApi.Infrastructure.Common;
using AuthApi.Infrastructure.Configuration;
using AuthApi.Infrastructure.Identities;
using AuthApi.Infrastructure.Identities.Seeds;
using AuthApi.Infrastructure.Persistence;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Identity;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;


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

            builder.Services.Configure<AppSettings>(
                 builder.Configuration.GetSection("AppSettings")
            );

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

            builder.Services.AddApplication()
                            .AddInfrastructure(builder.Configuration);

            builder.Services.AddIdentityCore<ApplicationUser>(ops =>
                            {
                                ops.SignIn.RequireConfirmedEmail = true; // bat buoc verify email
                            })
                            .AddRoles<IdentityRole<Guid>>()
                            .AddEntityFrameworkStores<AppDbContext>()
                            .AddDefaultTokenProviders();


            builder.Services.AddHangfire(config =>
            {
                config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                      .UseSimpleAssemblyNameTypeSerializer()
                      .UseRecommendedSerializerSettings()
                      .UsePostgreSqlStorage(options =>
                      {
                          options.UseNpgsqlConnection(connectionString); 
                      });
            });

            builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
            {
                options.TokenLifespan = TimeSpan.FromHours(2);
            });

            builder.Services.AddHangfireServer();

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Bearer";
                options.DefaultChallengeScheme = "Bearer";
            });


            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

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
            }

            app.UseHangfireDashboard("/hangfire");

            app.UseHttpsRedirection();

            app.UseAuthentication();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
