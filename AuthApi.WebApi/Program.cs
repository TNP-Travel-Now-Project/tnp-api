using AuthApi.Application.Configuration;
using AuthApi.Infrastructure.Common;
using AuthApi.Infrastructure.Configuration;
using AuthApi.Infrastructure.Identities;
using AuthApi.Infrastructure.Identities.Seeds;
using AuthApi.Infrastructure.Services.Auth;
using AuthApi.WebApi.Middlewares;
using Hangfire;
using Humanizer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

namespace AuthApi.WebApi
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

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
                        Console.WriteLine($"Authentication failed: {context.Exception.Message}");

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

            #region config Authorization Policies
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAdmin", policy =>
                    policy.RequireRole("Admin"));

                options.AddPolicy("RequireUser", policy =>
                    policy.RequireRole("User"));

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

            app.MapControllers();

            app.Run();
        }
    }
}
