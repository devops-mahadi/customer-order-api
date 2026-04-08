using CustomerOrder.Domain.Entities;

namespace CustomerOrder.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(string entityType, string entityId, string action, string userEmail, string ipAddress, string userAgent, string httpMethod, string endpoint, string? changes = null);
    Task<IEnumerable<AuditLog>> GetAuditTrailAsync(string entityType, string entityId);
    Task<IEnumerable<AuditLog>> GetUserActivityAsync(string userEmail, int pageNumber = 1, int pageSize = 50);
    Task<bool> PurgeLogsOlderThanAsync(int retentionYears);
}
