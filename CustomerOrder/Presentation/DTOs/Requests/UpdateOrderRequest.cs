using System.ComponentModel.DataAnnotations;
using CustomerOrder.Domain.Constants;

namespace CustomerOrder.Presentation.DTOs.Requests;

public record UpdateOrderRequest
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "Status is required")]
    [StringLength(ApplicationConstants.Order.StatusMaxLength, ErrorMessage = "Status cannot exceed {1} characters")]
    public required string Status { get; init; }

    [StringLength(ApplicationConstants.Order.ShippingAddressMaxLength,
        ErrorMessage = "Shipping Address cannot exceed {1} characters")]
    public string? ShippingAddress { get; init; }
}
