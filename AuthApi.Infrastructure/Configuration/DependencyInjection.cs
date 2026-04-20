using AuthApi.Application.Abstractions.Interfaces.Email;
using AuthApi.Application.Abstractions.Interfaces.Repositories;
using AuthApi.Application.Abstractions.Repositories.Auth;
using AuthApi.Application.Abstractions.Repositories.Email;
using AuthApi.Application.Features.Auth.Commands.Register;
using AuthApi.Domain.Interfaces;
using AuthApi.Infrastructure.Identities;
using AuthApi.Infrastructure.Persistence;
using AuthApi.Infrastructure.Persistence.Connection;
using AuthApi.Infrastructure.Persistence.Repositories.Users;
using AuthApi.Infrastructure.Services.Auth;
using AuthApi.Infrastructure.Services.Email;
using AuthApi.Infrastructure.Services.Token;
using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;
using Hangfire.Redis.StackExchange;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SqlKata.Compilers;
using SqlKata.Execution;
using StackExchange.Redis;
using System.Data;

namespace AuthApi.Infrastructure.Configuration
{
    public static class DependencyInjection
    {
        // Config DI Infrastructure Layer
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config, string connectionString)
        {
            #region SqlKata DI
            services.AddScoped<IDbConnection>(sp =>
                new SqlConnection(config.GetConnectionString("Default")));

            services.AddScoped<QueryFactory>(sp =>
            {
                var connection = sp.GetRequiredService<IDbConnection>();
                var compiler = new SqlServerCompiler();

                return new QueryFactory(connection, compiler);
            });
            #endregion

            #region Dapper DI
            services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();
            #endregion

            #region EFCore DI
            services.AddDbContext<AppDbContext>(options =>
                 options.UseNpgsql(config.GetConnectionString("Default"))
             );

            services.Configure<IdentityOptions>(options =>
            {
                // Password
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 6;

                // Lockout
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);

                // User
                options.User.RequireUniqueEmail = true;

                // Signin
                options.SignIn.RequireConfirmedEmail = true;
            });

            services.AddIdentityCore<ApplicationUser>(ops =>
            {
                ops.SignIn.RequireConfirmedEmail = true;

            }).AddRoles<IdentityRole<Guid>>()
              .AddEntityFrameworkStores<AppDbContext>()
              .AddDefaultTokenProviders();
            #endregion

            #region Service DI
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserQueryRepository, UserQueryRepository>();

            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IIdentityService, IdentityService>();

            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IEmailChecker, EmailChecker>();
            #endregion

            #region FluentValidation DI
            services.AddValidatorsFromAssemblyContaining<RegisterCommandValidator>();
            #endregion

            #region Redis Config
            var redisConnectionString = config.GetConnectionString("Redis");
            if (redisConnectionString == null)
                throw new InvalidOperationException("Redis connection string is not configured.");

            var redisOptions = ConfigurationOptions.Parse(redisConnectionString);

            // config performance
            redisOptions.SyncTimeout = 10000;
            redisOptions.ConnectTimeout = 10000;

            // nếu Redis cloud
            redisOptions.SslProtocols =
                System.Security.Authentication.SslProtocols.Tls12 |
                System.Security.Authentication.SslProtocols.Tls13;

            // tạo connection singleton
            var redis = ConnectionMultiplexer.Connect(redisOptions);

            // đăng ký Redis dùng chung toàn hệ thống
            services.AddSingleton<IConnectionMultiplexer>(redis);

            // REDIS CACHE
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
            });

            // SIGNALR REDIS BACKPLANE
            services.AddSignalR()
                .AddStackExchangeRedis(redisConnectionString);

            // HANGFIRE REDIS STORAGE
            services.AddHangfire(config =>
            {
                config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                         .UseSimpleAssemblyNameTypeSerializer()
                         .UseRecommendedSerializerSettings()
                         .UsePostgreSqlStorage(options =>
                         {
                             options.UseNpgsqlConnection(connectionString);

                         }).UseRedisStorage(redisConnectionString);
            });

            services.AddHangfireServer();
            #endregion

            return services;
        }
    }
}
