using CustomerOrder.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace CustomerOrder.Tests.Integration.Container.Repositories;

public class DatabaseFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _msSqlContainer;

    public string ConnectionString { get; private set; } = string.Empty;

    public DatabaseFixture()
    {
        _msSqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("YourStrong!Passw0rd")
            .Build();
    }

    public async Task InitializeAsync()
    {
        // Start the SQL Server container
        await _msSqlContainer.StartAsync();

        // Get the connection string
        ConnectionString = _msSqlContainer.GetConnectionString();

        // Create and migrate the database
        await using var context = CreateDbContext();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        // Stop and dispose the container
        await _msSqlContainer.DisposeAsync();
    }

    public CustomerOrderDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CustomerOrderDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new CustomerOrderDbContext(options);
    }
}
