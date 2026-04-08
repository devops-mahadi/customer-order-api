using CustomerOrder.Presentation.DTOs.Requests;
using CustomerOrder.Presentation.DTOs.Responses;

namespace CustomerOrder.Application.Interfaces;

public interface ICustomerService
{
    Task<CustomerResponse?> GetByEmailAsync(string email);
    Task<IEnumerable<CustomerResponse>> GetAllAsync();
    Task<bool> CreateAsync(CreateCustomerRequest request);
    Task<bool> UpdateAsync(string email, UpdateCustomerRequest request);
    Task<bool> DeleteAsync(string email);

    // GDPR Methods
    Task<CustomerDataExportResponse> ExportDataAsync(string email);
    Task<bool> AnonymizeAsync(string email, string reason);
    Task<bool> GrantConsentAsync(string email, string consentType, string ipAddress, string userAgent, string consentVersion);
    Task<bool> RevokeConsentAsync(string email, string consentType, string ipAddress, string userAgent);
    Task<IEnumerable<ConsentResponse>> GetConsentsAsync(string email);
}
