using AuthApi.Infrastructure.Persistence;
using AuthApi.Infrastructure.Persistence.Connection;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace AuthApi.IntegrationTests.Fixtures;

/// <summary>
/// Quản lý vòng đời container SQL Server.
/// Implement IAsyncLifetime để xUnit tự động gọi InitializeAsync/DisposeAsync.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container;

    public SqlServerFixture()
    {
        _container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("IntegrationTest_Pass123!")
            .Build();
    }

    /// <summary>Connection string từ container đang chạy.</summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <summary>Factory tạo Dapper connection, dùng trong các Repository tests.</summary>
    public IDbConnectionFactory DbConnectionFactory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // 1. Start container SQL Server trong Docker
        await _container.StartAsync();

        // 2. Tạo AppDbContext với connection string của container
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        // 3. Chạy tất cả EF Core migrations để tạo schema
        using var ctx = new AppDbContext(options);
        await ctx.Database.MigrateAsync();

        // 4. Tạo DbConnectionFactory cho Dapper tests
        DbConnectionFactory = new DbConnectionFactory(ConnectionString);
    }

    public async Task DisposeAsync()
    {
        await _container.StopAsync();
        await _container.DisposeAsync();
    }
}

/// <summary>
/// Collection fixture: đảm bảo SqlServerFixture dùng chung 1 container
/// cho tất cả tests trong cùng collection.
/// </summary>
[CollectionDefinition("SqlServerCollection")]
public class SqlServerCollection : ICollectionFixture<SqlServerFixture>;
