namespace CustomerOrder.Presentation.DTOs.Responses;

public record CustomerResponse
{
    public required string FirstName { get; init; }
    public string LastName { get; init; } = string.Empty;
    public required string Email { get; init; }
    public string PhoneNumber { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime LastUpdatedAt { get; init; }
}
