using AuthApi.Application.Abstractions.Repositories;
using AuthApi.Application.Abstractions.Repositories.Auth;
using AuthApi.Application.Abstractions.Repositories.Email;
using AuthApi.Application.Features.Auth.Commands.Register;
using AuthApi.Domain.Interfaces;
using AuthApi.Infrastructure.Identities;
using AuthApi.Infrastructure.Identities.Seeds;
using AuthApi.Infrastructure.Persistence;
using AuthApi.Infrastructure.Persistence.Connection;
using AuthApi.Infrastructure.Persistence.Repositories.Users;
using AuthApi.Infrastructure.Services.Email;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SqlKata.Compilers;
using SqlKata.Execution;
using System.Data;

namespace AuthApi.Infrastructure.Configuration
{
    public static class DependencyInjection
    {
        // Config DI Infrastructure Layer
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
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
                 options.UseSqlServer(config.GetConnectionString("Default"))
             );

            services.Configure<IdentityOptions>(options =>
            {
                // Password
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 6;

                // Lockout
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.AllowedForNewUsers = true;

                // User
                options.User.RequireUniqueEmail = true;

                // Signin
                options.SignIn.RequireConfirmedEmail = true;

            });
            #endregion

            #region Service DI
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserQueryRepository, UserQueryRepository>();

            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IIdentityService, IdentityService>();

            services.AddScoped<IEmailService, EmailService>();
            #endregion

            #region FluentValidation DI
            services.AddValidatorsFromAssemblyContaining<RegisterCommandValidator>();
            #endregion

            return services;
        }
    }
}
