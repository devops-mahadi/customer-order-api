using CustomerOrder.Application.Interfaces;
using CustomerOrder.Domain.Entities;
using CustomerOrder.Domain.Interfaces;

namespace CustomerOrder.Application.Services;

public class AuditService(IAuditRepository auditRepository) : IAuditService
{
    public async Task LogAsync(
        string entityType,
        string entityId,
        string action,
        string userEmail,
        string ipAddress,
        string userAgent,
        string httpMethod,
        string endpoint,
        string? changes = null)
    {
        var auditLog = new AuditLog
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            UserEmail = userEmail,
            ActionDate = DateTime.UtcNow,
            Changes = changes,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            HttpMethod = httpMethod,
            Endpoint = endpoint
        };

        await auditRepository.CreateAsync(auditLog);
    }

    public async Task<IEnumerable<AuditLog>> GetAuditTrailAsync(string entityType, string entityId)
    {
        return await auditRepository.GetByEntityAsync(entityType, entityId);
    }

    public async Task<IEnumerable<AuditLog>> GetUserActivityAsync(string userEmail, int pageNumber = 1, int pageSize = 50)
    {
        return await auditRepository.GetByUserEmailAsync(userEmail, pageNumber, pageSize);
    }

    public async Task<bool> PurgeLogsOlderThanAsync(int retentionYears)
    {
        var cutoffDate = DateTime.UtcNow.AddYears(-retentionYears);
        return await auditRepository.DeleteOlderThanAsync(cutoffDate);
    }
}
