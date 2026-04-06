using System.ComponentModel.DataAnnotations;
using CustomerOrder.Domain.Constants;

namespace CustomerOrder.Presentation.DTOs.Requests;

public record CreateOrderRequest
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "Customer Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(ApplicationConstants.Customer.EmailMaxLength)]
    public required string CustomerEmail { get; init; }

    [Required(ErrorMessage = "Total Amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Total Amount must be greater than 0")]
    public decimal TotalAmount { get; init; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "Shipping Address is required")]
    [StringLength(ApplicationConstants.Order.ShippingAddressMaxLength,
        ErrorMessage = "Shipping Address cannot exceed {1} characters")]
    public required string ShippingAddress { get; init; }

    [StringLength(ApplicationConstants.Order.NotesMaxLength,
        ErrorMessage = "Notes cannot exceed {1} characters")]
    public string? Notes { get; init; }
}
