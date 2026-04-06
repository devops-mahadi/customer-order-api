using System.ComponentModel.DataAnnotations;
using CustomerOrder.Domain.Constants;

namespace CustomerOrder.Presentation.DTOs.Requests;

public record UpdateCustomerRequest
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "First Name is required")]
    [StringLength(ApplicationConstants.Customer.FirstNameMaxLength,
        ErrorMessage = "First Name cannot exceed {1} characters")]
    public required string FirstName { get; init; }

    [StringLength(ApplicationConstants.Customer.LastNameMaxLength,
        ErrorMessage = "Last Name cannot exceed {1} characters")]
    public string LastName { get; init; } = string.Empty;

    [Phone(ErrorMessage = "Invalid phone number format")]
    [StringLength(ApplicationConstants.Customer.PhoneNumberMaxLength)]
    public string? PhoneNumber { get; init; }
}
