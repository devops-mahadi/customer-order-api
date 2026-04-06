using CustomerOrder.Application.Interfaces;
using CustomerOrder.Domain.Constants;
using CustomerOrder.Domain.Entities;
using CustomerOrder.Domain.Interfaces;
using CustomerOrder.Presentation.DTOs.Requests;
using CustomerOrder.Presentation.DTOs.Responses;

namespace CustomerOrder.Application.Services;

public class CustomerService(ICustomerRepository customerRepository) : ICustomerService
{
    public async Task<CustomerResponse?> GetByEmailAsync(string email)
    {
        var customer = await customerRepository.GetByEmailAsync(email);
        return customer == null ? null : MapToResponse(customer);
    }

    public async Task<IEnumerable<CustomerResponse>> GetAllAsync()
    {
        var customers = await customerRepository.GetAllAsync();
        return customers.Select(MapToResponse);
    }

    public async Task<bool> CreateAsync(CreateCustomerRequest request)
    {
        // Check if email already exists
        if (await customerRepository.EmailExistsAsync(request.Email))
        {
            throw new InvalidOperationException(
                string.Format(ApplicationConstants.ValidationMessages.CustomerAlreadyExists, request.Email));
        }

        var customer = new Customer
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        return await customerRepository.CreateAsync(customer);
    }

    public async Task<bool> UpdateAsync(string email, UpdateCustomerRequest request)
    {
        var customer = await customerRepository.GetByEmailAsync(email);
        if (customer == null)
            return false;

        customer.FirstName = request.FirstName;
        customer.LastName = request.LastName;
        customer.PhoneNumber = request.PhoneNumber ?? string.Empty;
        customer.LastUpdatedAt = DateTime.UtcNow;

        return await customerRepository.UpdateAsync(customer);
    }

    public async Task<bool> DeleteAsync(string email)
    {
        var customer = await customerRepository.GetByEmailAsync(email);
        if (customer == null)
            return false;

        return await customerRepository.DeleteAsync(customer);
    }

    private static CustomerResponse MapToResponse(Customer customer)
    {
        return new CustomerResponse
        {
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Email = customer.Email,
            CreatedAt = customer.CreatedAt,
            LastUpdatedAt = customer.LastUpdatedAt
        };
    }
}
