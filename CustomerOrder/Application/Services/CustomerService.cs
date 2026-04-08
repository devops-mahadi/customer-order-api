using CustomerOrder.Application.Interfaces;
using CustomerOrder.Domain.Constants;
using CustomerOrder.Domain.Entities;
using CustomerOrder.Domain.Interfaces;
using CustomerOrder.Presentation.DTOs.Requests;
using CustomerOrder.Presentation.DTOs.Responses;

namespace CustomerOrder.Application.Services;

public class CustomerService(
    ICustomerRepository customerRepository,
    IOrderRepository orderRepository,
    IConsentRepository consentRepository) : ICustomerService
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
        {
            return false;
        }

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
        {
            return false;
        }

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

    // GDPR Implementation

    /// <summary>
    /// GDPR Article 20: Right to Data Portability
    /// Export all customer data in machine-readable format
    /// </summary>
    public async Task<CustomerDataExportResponse> ExportDataAsync(string email)
    {
        var customer = await customerRepository.GetByEmailAsync(email);
        if (customer == null)
        {
            throw new InvalidOperationException(
                string.Format(ApplicationConstants.ValidationMessages.CustomerNotFound, email));
        }

        // Check if customer is deleted
        if (customer.IsDeleted)
        {
            throw new InvalidOperationException(
                $"Cannot export data for deleted customer '{email}'");
        }

        // Get all orders for this customer
        var orders = await orderRepository.GetByCustomerIdAsync(customer.CustomerId);
        var orderResponses = orders.Select(o => new OrderResponse
        {
            OrderNumber = o.OrderNumber,
            CustomerEmail = email,
            CustomerName = $"{customer.FirstName} {customer.LastName}".Trim(),
            OrderDate = o.OrderDate,
            TotalAmount = o.TotalAmount,
            Status = o.Status,
            ShippingAddress = o.ShippingAddress,
            Notes = o.Notes,
            CreatedAt = o.CreatedAt
        });

        // Get all consents for this customer
        var consents = await consentRepository.GetByCustomerIdAsync(customer.CustomerId);
        var consentResponses = consents.Select(c => new ConsentResponse
        {
            ConsentType = c.ConsentType,
            IsGranted = c.IsGranted,
            ConsentDate = c.ConsentDate,
            RevokedDate = c.RevokedDate,
            IpAddress = c.IpAddress,
            ConsentVersion = c.ConsentVersion
        });

        return new CustomerDataExportResponse
        {
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Email = customer.Email,
            PhoneNumber = customer.PhoneNumber,
            CreatedAt = customer.CreatedAt,
            LastUpdatedAt = customer.LastUpdatedAt,
            IsDeleted = customer.IsDeleted,
            DeletedAt = customer.DeletedAt,
            DeletedReason = customer.DeletedReason,
            Orders = orderResponses,
            Consents = consentResponses,
            ExportedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// GDPR Article 17: Right to Erasure (Right to be Forgotten)
    /// Anonymizes customer data while maintaining referential integrity
    /// </summary>
    public async Task<bool> AnonymizeAsync(string email, string reason)
    {
        var customer = await customerRepository.GetByEmailAsync(email);
        if (customer == null)
        {
            return false;
        }

        if (customer.IsDeleted)
        {
            throw new InvalidOperationException(
                string.Format(ApplicationConstants.ValidationMessages.CustomerAlreadyDeleted, email));
        }

        // Anonymize customer data
        customer.FirstName = "REDACTED";
        customer.LastName = "REDACTED";
        customer.Email = $"deleted-{Guid.NewGuid()}@anonymized.local";
        customer.PhoneNumber = string.Empty;
        customer.IsDeleted = true;
        customer.DeletedAt = DateTime.UtcNow;
        customer.DeletedReason = reason;
        customer.LastUpdatedAt = DateTime.UtcNow;

        return await customerRepository.UpdateAsync(customer);
    }

    /// <summary>
    /// GDPR Article 7: Consent - Grant consent for data processing
    /// </summary>
    public async Task<bool> GrantConsentAsync(
        string email,
        string consentType,
        string ipAddress,
        string userAgent,
        string consentVersion)
    {
        var customer = await customerRepository.GetByEmailAsync(email);
        if (customer == null)
        {
            throw new InvalidOperationException(
                string.Format(ApplicationConstants.ValidationMessages.CustomerNotFound, email));
        }

        // Check if consent already exists and is active
        var existingConsent = await consentRepository.GetByCustomerIdAndTypeAsync(customer.CustomerId, consentType);
        if (existingConsent != null && existingConsent.IsGranted)
        {
            throw new InvalidOperationException(
                $"Consent '{consentType}' has already been granted for customer '{email}'");
        }

        var consent = new CustomerConsent
        {
            CustomerId = customer.CustomerId,
            ConsentType = consentType,
            IsGranted = true,
            ConsentDate = DateTime.UtcNow,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            ConsentVersion = consentVersion
        };

        return await consentRepository.CreateAsync(consent);
    }

    /// <summary>
    /// GDPR Article 7: Consent - Revoke consent for data processing
    /// </summary>
    public async Task<bool> RevokeConsentAsync(
        string email,
        string consentType,
        string ipAddress,
        string userAgent)
    {
        var customer = await customerRepository.GetByEmailAsync(email);
        if (customer == null)
        {
            throw new InvalidOperationException(
                string.Format(ApplicationConstants.ValidationMessages.CustomerNotFound, email));
        }

        return await consentRepository.RevokeConsentAsync(customer.CustomerId, consentType);
    }

    /// <summary>
    /// Get all consents for a customer
    /// </summary>
    public async Task<IEnumerable<ConsentResponse>> GetConsentsAsync(string email)
    {
        var customer = await customerRepository.GetByEmailAsync(email);
        if (customer == null)
        {
            throw new InvalidOperationException(
                string.Format(ApplicationConstants.ValidationMessages.CustomerNotFound, email));
        }

        var consents = await consentRepository.GetByCustomerIdAsync(customer.CustomerId);
        return consents.Select(c => new ConsentResponse
        {
            ConsentType = c.ConsentType,
            IsGranted = c.IsGranted,
            ConsentDate = c.ConsentDate,
            RevokedDate = c.RevokedDate,
            IpAddress = c.IpAddress,
            ConsentVersion = c.ConsentVersion
        });
    }
}
