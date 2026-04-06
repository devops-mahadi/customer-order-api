using CustomerOrder.Domain.Entities;
using CustomerOrder.Domain.Interfaces;
using CustomerOrder.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustomerOrder.Infrastructure.Repositories;

public class OrderRepository(CustomerOrderDbContext dbContext) : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(int orderId)
    {
        return await dbContext.Orders
            .Include(o => o.Customer)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderId == orderId);
    }

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber)
    {
        return await dbContext.Orders
            .Include(o => o.Customer)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
    }

    public async Task<IEnumerable<Order>> GetByCustomerIdAsync(int customerId)
    {
        return await dbContext.Orders
            .Where(o => o.CustomerId == customerId)
            .AsNoTracking()
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<(IEnumerable<Order> Orders, int TotalCount)> GetFilteredAsync(
        DateTime? startDate,
        DateTime? endDate,
        string? status,
        int pageNumber,
        int pageSize)
    {
        var query = dbContext.Orders
            .Include(o => o.Customer)
            .AsQueryable();

        // Apply filters
        if (startDate.HasValue)
        {
            query = query.Where(o => o.OrderDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(o => o.OrderDate <= endDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(o => o.Status == status);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply pagination
        var orders = await query
            .OrderByDescending(o => o.OrderDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();

        return (orders, totalCount);
    }

    public async Task<bool> CreateAsync(Order order, bool noTracking = true)
    {
        dbContext.Orders.Add(order);
        int rowCount = await dbContext.SaveChangesAsync();
        if (noTracking) dbContext.ChangeTracker.Clear();
        return rowCount > 0;
    }

    public async Task<bool> UpdateAsync(Order order, bool noTracking = true)
    {
        dbContext.Orders.Update(order);
        int rowCount = await dbContext.SaveChangesAsync();
        if (noTracking) dbContext.ChangeTracker.Clear();
        return rowCount > 0;
    }

    public async Task<bool> DeleteAsync(int orderId)
    {
        var order = await dbContext.Orders.FindAsync(orderId);
        if (order == null)
            return false;

        dbContext.Orders.Remove(order);
        return await dbContext.SaveChangesAsync() > 0;
    }
}
