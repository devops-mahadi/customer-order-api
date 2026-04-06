namespace CustomerOrder.Domain.Entities;

public class Order
{
    public int OrderId { get; set; } // Primary key
    public required string OrderNumber { get; set; } // Unique public identifier
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Pending"; // e.g., Pending, Confirmed, Shipped, Delivered, Cancelled
    public required string ShippingAddress { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
