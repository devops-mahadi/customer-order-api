namespace CustomerOrder.Presentation.DTOs.Responses;

public record OrderResponse
{
    public required string OrderNumber { get; init; }
    public required string CustomerEmail { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public DateTime OrderDate { get; init; }
    public decimal TotalAmount { get; init; }
    public string Status { get; init; } = string.Empty;
    public string ShippingAddress { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
}
