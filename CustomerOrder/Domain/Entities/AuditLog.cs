namespace CustomerOrder.Domain.Entities;

/// <summary>
/// Tracks all data access and modification activities for GDPR compliance
/// Implements Article 30 (Records of Processing Activities)
/// </summary>
public class AuditLog
{
    /// <summary>
    /// Unique identifier for the audit log entry
    /// </summary>
    public long AuditId { get; set; }

    /// <summary>
    /// Type of entity being audited (Customer, Order, etc.)
    /// </summary>
    public required string EntityType { get; set; }

    /// <summary>
    /// Identifier of the entity (email, order number, etc.)
    /// </summary>
    public required string EntityId { get; set; }

    /// <summary>
    /// Action performed (Created, Updated, Deleted, Viewed, Exported, Anonymized)
    /// </summary>
    public required string Action { get; set; }

    /// <summary>
    /// Email of the user who performed the action
    /// </summary>
    public required string UserEmail { get; set; }

    /// <summary>
    /// Timestamp of the action (UTC)
    /// </summary>
    public DateTime ActionDate { get; set; }

    /// <summary>
    /// JSON representation of changes made (before/after values)
    /// Null for read operations
    /// </summary>
    public string? Changes { get; set; }

    /// <summary>
    /// IP address of the user who performed the action
    /// </summary>
    public required string IpAddress { get; set; }

    /// <summary>
    /// User agent (browser/client information)
    /// </summary>
    public required string UserAgent { get; set; }

    /// <summary>
    /// HTTP method used (GET, POST, PUT, DELETE)
    /// </summary>
    public required string HttpMethod { get; set; }

    /// <summary>
    /// API endpoint that was called
    /// </summary>
    public required string Endpoint { get; set; }
}
