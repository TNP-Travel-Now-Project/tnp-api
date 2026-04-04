using AuthApi.Application.Abstractions.Repositories;
using AuthApi.Domain.Interfaces;
using AuthApi.Infrastructure.Persistence;
using AuthApi.Infrastructure.Persistence.Connection;
using AuthApi.Infrastructure.Persistence.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
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
            #endregion

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserQueryRepository, UserQueryRepository>();

            return services;
        }
    }
}
