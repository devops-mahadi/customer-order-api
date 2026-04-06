namespace CustomerOrder.Domain.Entities;

public class Customer
{
    public int CustomerId { get; set; } // primary Key, unique per customer
    public required string FirstName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public required string Email { get; set; } // unique identifier per customer
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
