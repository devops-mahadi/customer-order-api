namespace CustomerOrder.Presentation.DTOs.Responses;

/// <summary>
/// GDPR Article 7: Consent response
/// Shows customer consent status for data processing activities
/// </summary>
public record ConsentResponse
{
    public required string ConsentType { get; init; }
    public bool IsGranted { get; init; }
    public DateTime ConsentDate { get; init; }
    public DateTime? RevokedDate { get; init; }
    public required string IpAddress { get; init; }
    public required string ConsentVersion { get; init; }
}
