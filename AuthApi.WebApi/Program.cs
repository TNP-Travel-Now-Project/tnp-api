using AuthApi.Application.Configuration;
using AuthApi.Infrastructure.Common;
using AuthApi.Infrastructure.Configuration;
using AuthApi.Infrastructure.Identities;
using AuthApi.Infrastructure.Identities.Seeds;
using AuthApi.Infrastructure.Persistence;
using AuthApi.Infrastructure.Services.Auth;
using AuthApi.WebApi.HealthChecks;
using AuthApi.Infrastructure.Services.ConvertType;
using AuthApi.WebApi.Middlewares;
using Dapper;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using HealthChecks.UI.Client;
using Microsoft.OpenApi;
using Serilog;
using System.Text;

namespace AuthApi.WebApi
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Serilog: cấu hình từ appsettings.json + environment variables
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false)
                    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                    .AddEnvironmentVariables()
                    .Build())
                .CreateLogger();

            try
            {
                Log.Information("Starting application");
                await BuildAndRun(args);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }

        private static async Task BuildAndRun(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Serilog cho toàn bộ host
            builder.Host.UseSerilog();

            builder.Services.AddControllers();

            builder.Services.AddHttpContextAccessor();

            // Nao day len prod thi them "!" cho bien isDev
            var isDev = builder.Environment.IsDevelopment();
            var feUrl = builder.Configuration["Frontend:Url"];

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
                        policy.WithOrigins(feUrl!)
                              .AllowAnyMethod()
                              .AllowAnyHeader()
                              .AllowCredentials();
                    });
            });
            #endregion

            #region Config JWT
            var jwtSettings = builder.Configuration.GetSection("AppSettings");
            var key = Encoding.UTF8.GetBytes(jwtSettings["JwtKey"]!);

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })

            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["JwtIssuer"],
                    ValidAudience = jwtSettings["JwtAudience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        //var token = context.Request.Cookies["accessToken"]; cach luu acesstoken len cookie

                        var authorization = context.Request.Headers.Authorization.FirstOrDefault();
                        if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer "))
                        {
                            context.Token = authorization.Substring("Bearer ".Length).Trim();
                        }

                        return Task.CompletedTask;
                    },

                    OnAuthenticationFailed = context =>
                    {
                        Log.Warning(context.Exception, "Authentication failed");

                        if (context.Exception is SecurityTokenExpiredException)
                        {
                            context.Response.Headers.Append("Token-Expired", "true");
                        }

                        return Task.CompletedTask;
                    },

                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";

                        var result = System.Text.Json.JsonSerializer.Serialize(
                            new ApiErrorResponse
                            {
                                Code = "UNAUTHORIZED",
                                Message = "You must provide a valid access token."
                            });

                        return context.Response.WriteAsync(result);
                    }
                };
            });
            #endregion

            #region Health Checks
            builder.Services.AddHealthChecks()
                .AddDbContextCheck<AppDbContext>(
                    name: "sqlserver",
                    tags: ["db", "sql"])
                .AddCheck<RedisHealthCheck>(
                    name: "redis",
                    tags: ["cache", "redis"]);
            #endregion

            #region config Authorization Policies
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));

                options.AddPolicy("RequireUser", policy => policy.RequireRole("User"));

                options.AddPolicy("RequireAdminOrUser", policy =>
                    policy.RequireAssertion(ctx =>
                        ctx.User.IsInRole("Admin") || ctx.User.IsInRole("User")));
            });
            #endregion

            #region Setup CookiePolicyOptions
            builder.Services.Configure<CookiePolicyOptions>(options =>
            {
                options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
                options.HttpOnly = HttpOnlyPolicy.None;
                options.OnAppendCookie = ctx =>
                {
                    ctx.CookieOptions.SameSite = SameSiteMode.None;
                    ctx.CookieOptions.Secure = isDev;
                    //ctx.CookieOptions.HttpOnly = true;
                    ctx.CookieOptions.IsEssential = true;
                    ctx.CookieOptions.Path = "/";
                };

                options.OnDeleteCookie = ctx =>
                {
                    ctx.CookieOptions.SameSite = SameSiteMode.None;
                    ctx.CookieOptions.Secure = true;
                };
            });
            #endregion 

            #region Add Authorize for swagger
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo

                {
                    Title = "Travel Now API",
                    Version = "v1",
                    Description = "ASP.NET Core Web API với Google OAuth + JWT Bearer"
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

            #region Implement Dapper Convert from DateTime to DateOnly
            SqlMapper.AddTypeHandler(new TypeSafeDapperDateOnly());
            #endregion

            var app = builder.Build();

            #region Auto migrate (Docker)
            if (args.Contains("--migrate"))
            {
                using var scope = app.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await db.Database.MigrateAsync();
            }
            #endregion

            #region Seeds role
            using (var scope = app.Services.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                await RoleSeeder.SeedAsync(roleManager);
                await RoleSeeder.SeedAdminAsync(userManager, roleManager);
            }
            #endregion

            // OpenAPI FE can UseSwagger o tat ca moi truong de bat lay schema
            app.UseSwagger();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwaggerUI();
            }

            app.UseMiddleware<ExceptionMiddleware>();

            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = new[] { new HangfireAuthFilter() },
                DashboardTitle = "Hangfire – Admin"
            });

            app.UseCors("AllowNextJS");

            app.UseHttpsRedirection();

            app.UseMiddleware<SecureHeadersMiddleware>();

            app.UseCookiePolicy();

            app.UseAuthentication();

            app.UseMiddleware<CSRFMiddleware>();

            app.UseAuthorization();

            // Health check endpoint — Docker + Load Balancer dùng để biết app còn sống
            app.MapHealthChecks("/health", new()
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            app.MapControllers();

            app.Run();
        }
    }
}
