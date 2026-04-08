using CustomerOrder.Domain.Entities;

namespace CustomerOrder.Domain.Interfaces;

public interface IAuditRepository
{
    Task<bool> CreateAsync(AuditLog auditLog);
    Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, string entityId);
    Task<IEnumerable<AuditLog>> GetByUserEmailAsync(string userEmail, int pageNumber = 1, int pageSize = 50);
    Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int pageNumber = 1, int pageSize = 50);
    Task<bool> DeleteOlderThanAsync(DateTime cutoffDate);
}
