namespace CustomerOrder.Domain.Entities;

/// <summary>
/// Tracks customer consent for data processing activities (GDPR Article 7)
/// </summary>
public class CustomerConsent
{
    /// <summary>
    /// Unique identifier for the consent record
    /// </summary>
    public int ConsentId { get; set; }

    /// <summary>
    /// Foreign key to Customer
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// Type of consent (Marketing, DataProcessing, Profiling, ThirdPartySharing)
    /// </summary>
    public required string ConsentType { get; set; }

    /// <summary>
    /// Whether consent is granted or revoked
    /// </summary>
    public bool IsGranted { get; set; }

    /// <summary>
    /// Date when consent was granted (UTC)
    /// </summary>
    public DateTime ConsentDate { get; set; }

    /// <summary>
    /// Date when consent was revoked (UTC)
    /// Null if still granted
    /// </summary>
    public DateTime? RevokedDate { get; set; }

    /// <summary>
    /// IP address when consent was given/revoked
    /// </summary>
    public required string IpAddress { get; set; }

    /// <summary>
    /// User agent when consent was given/revoked
    /// </summary>
    public required string UserAgent { get; set; }

    /// <summary>
    /// Version of the consent form/terms accepted
    /// </summary>
    public required string ConsentVersion { get; set; }

    /// <summary>
    /// Navigation property to Customer
    /// </summary>
    public Customer? Customer { get; set; }
}
