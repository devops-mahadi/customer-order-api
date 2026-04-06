using CustomerOrder.Domain.Entities;
using CustomerOrder.Domain.Interfaces;
using CustomerOrder.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustomerOrder.Infrastructure.Repositories;

public class CustomerRepository(CustomerOrderDbContext dbContext): ICustomerRepository
{
    public async Task<Customer?> GetByIdAsync(int customerId)
    {
        return await dbContext.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);
    }

    public async Task<Customer?> GetByEmailAsync(string email)
    {
        return await dbContext.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Email == email);
    }

    public async Task<IEnumerable<Customer>> GetAllAsync()
    {
        return await  dbContext.Customers
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> CreateAsync(Customer customer, bool noTracking = true)
    {
        dbContext.Customers.Add(customer);
        int rowCount = await dbContext.SaveChangesAsync();
        if (noTracking) dbContext.ChangeTracker.Clear();
        return rowCount > 0;
        
    }

    public async Task<bool> UpdateAsync(Customer customer, bool noTracking = true)
    {
        dbContext.Customers.Update(customer);
        int rowCount = await dbContext.SaveChangesAsync();
        if (noTracking) dbContext.ChangeTracker.Clear();
        return rowCount > 0;
    }

    public async Task<bool> DeleteAsync(Customer customer)
    {
        dbContext.Customers.Remove(customer);
        return await dbContext.SaveChangesAsync() > 0;
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await dbContext.Customers
            .AnyAsync(c => c.Email == email);
    }
}