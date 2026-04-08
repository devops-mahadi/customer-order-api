namespace CustomerOrder.Presentation.DTOs.Responses;

/// <summary>
/// GDPR Article 20: Right to Data Portability
/// Contains all personal data held about a customer in machine-readable format
/// </summary>
public record CustomerDataExportResponse
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public string? PhoneNumber { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastUpdatedAt { get; init; }
    public bool IsDeleted { get; init; }
    public DateTime? DeletedAt { get; init; }
    public string? DeletedReason { get; init; }
    public IEnumerable<OrderResponse> Orders { get; init; } = new List<OrderResponse>();
    public IEnumerable<ConsentResponse> Consents { get; init; } = new List<ConsentResponse>();
    public DateTime ExportedAt { get; init; }
}
