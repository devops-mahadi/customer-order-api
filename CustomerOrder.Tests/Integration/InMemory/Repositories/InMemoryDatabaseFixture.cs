using CustomerOrder.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustomerOrder.Tests.Integration.InMemory.Repositories;

public class InMemoryDatabaseFixture
{
    public CustomerOrderDbContext CreateDbContext()
    {
        // Use provided name or generate unique one
        var dbName = "MyTestDb ";

        var options = new DbContextOptionsBuilder<CustomerOrderDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var context = new CustomerOrderDbContext(options);

        // Ensure database is created
        context.Database.EnsureCreated();

        return context;
    }
}
