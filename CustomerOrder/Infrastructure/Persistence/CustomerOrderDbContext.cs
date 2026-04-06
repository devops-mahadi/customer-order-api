using Microsoft.EntityFrameworkCore;
using CustomerOrder.Domain.Entities;
using CustomerOrder.Infrastructure.Persistence.Configurations;
using CustomerOrder.Domain.Constants;

namespace CustomerOrder.Infrastructure.Persistence;

public class CustomerOrderDbContext: DbContext
{
    public CustomerOrderDbContext(DbContextOptions<CustomerOrderDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(options =>
            options.MigrationsHistoryTable(ApplicationConstants.MigrationHistoryTable,
                ApplicationConstants.ApplicationSchema));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply entity configurations
        modelBuilder.HasDefaultSchema(ApplicationConstants.ApplicationSchema);
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new OrderConfiguration());
    }
}