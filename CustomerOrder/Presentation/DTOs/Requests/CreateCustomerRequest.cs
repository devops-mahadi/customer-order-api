using System.ComponentModel.DataAnnotations;
using CustomerOrder.Domain.Constants;

namespace CustomerOrder.Presentation.DTOs.Requests;

public record CreateCustomerRequest
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "First Name is required")]
    [StringLength(ApplicationConstants.Customer.FirstNameMaxLength,
        ErrorMessage = "First Name cannot exceed {1} characters")]
    public required string FirstName { get; init; }

    [StringLength(ApplicationConstants.Customer.LastNameMaxLength,
        ErrorMessage = "Last Name cannot exceed {1} characters")]
    public string LastName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(ApplicationConstants.Customer.EmailMaxLength)]
    public required string Email { get; init; }

    [Phone(ErrorMessage = "Invalid phone number format")]
    [StringLength(ApplicationConstants.Customer.PhoneNumberMaxLength)]
    public string? PhoneNumber { get; init; }
}
