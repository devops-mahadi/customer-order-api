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
}
