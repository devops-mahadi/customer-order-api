using CustomerOrder.Presentation.DTOs.Requests;
using CustomerOrder.Presentation.DTOs.Responses;

namespace CustomerOrder.Application.Interfaces;

public interface IOrderService
{
    Task<OrderResponse?> GetByOrderNumberAsync(string orderNumber);
    Task<IEnumerable<OrderResponse>> GetByCustomerEmailAsync(string customerEmail);
    Task<PagedResponse<OrderResponse>> GetFilteredAsync(
        DateTime? startDate,
        DateTime? endDate,
        string? status,
        int pageNumber = 1,
        int pageSize = 10);
    Task<string> CreateAsync(CreateOrderRequest request);
    Task<bool> UpdateAsync(string orderNumber, UpdateOrderRequest request);
}
