using System.ComponentModel.DataAnnotations;
using CustomerOrder.Domain.Constants;

namespace CustomerOrder.Presentation.DTOs.Requests;

public record ConsentRequest
{
    [Required]
    [StringLength(ApplicationConstants.Consent.ConsentTypeMaxLength)]
    public required string ConsentType { get; init; }

    [Required]
    [StringLength(ApplicationConstants.Consent.ConsentVersionMaxLength)]
    public required string ConsentVersion { get; init; }
}
