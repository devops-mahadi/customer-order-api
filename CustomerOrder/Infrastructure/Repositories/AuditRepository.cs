using CustomerOrder.Domain.Entities;
using CustomerOrder.Domain.Interfaces;
using CustomerOrder.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustomerOrder.Infrastructure.Repositories;

public class AuditRepository(CustomerOrderDbContext dbContext) : IAuditRepository
{
    public async Task<bool> CreateAsync(AuditLog auditLog)
    {
        dbContext.AuditLogs.Add(auditLog);
        int rowCount = await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        return rowCount > 0;
    }

    public async Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, string entityId)
    {
        return await dbContext.AuditLogs
            .AsNoTracking()
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.ActionDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByUserEmailAsync(string userEmail, int pageNumber = 1, int pageSize = 50)
    {
        return await dbContext.AuditLogs
            .AsNoTracking()
            .Where(a => a.UserEmail == userEmail)
            .OrderByDescending(a => a.ActionDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int pageNumber = 1, int pageSize = 50)
    {
        return await dbContext.AuditLogs
            .AsNoTracking()
            .Where(a => a.ActionDate >= startDate && a.ActionDate <= endDate)
            .OrderByDescending(a => a.ActionDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<bool> DeleteOlderThanAsync(DateTime cutoffDate)
    {
        var oldLogs = await dbContext.AuditLogs
            .Where(a => a.ActionDate < cutoffDate)
            .ToListAsync();

        if (oldLogs.Count == 0)
        {
            return false;
        }

        dbContext.AuditLogs.RemoveRange(oldLogs);
        await dbContext.SaveChangesAsync();
        return true;
    }
}
