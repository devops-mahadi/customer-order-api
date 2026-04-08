using CustomerOrder.Domain.Entities;

namespace CustomerOrder.Domain.Interfaces;

public interface IConsentRepository
{
    Task<bool> CreateAsync(CustomerConsent consent);
    Task<bool> UpdateAsync(CustomerConsent consent);
    Task<IEnumerable<CustomerConsent>> GetByCustomerIdAsync(int customerId);
    Task<CustomerConsent?> GetByCustomerIdAndTypeAsync(int customerId, string consentType);
    Task<bool> RevokeConsentAsync(int customerId, string consentType);
}
