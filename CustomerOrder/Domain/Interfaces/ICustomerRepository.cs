using CustomerOrder.Domain.Entities;

namespace CustomerOrder.Domain.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(int customerId);
    Task<Customer?> GetByEmailAsync(string email);
    Task<IEnumerable<Customer>> GetAllAsync();
    Task<bool> CreateAsync(Customer customer, bool noTracking = true);
    Task<bool> UpdateAsync(Customer customer, bool noTracking = true);
    Task<bool> DeleteAsync(Customer customer);
    Task<bool> EmailExistsAsync(string email);
}
