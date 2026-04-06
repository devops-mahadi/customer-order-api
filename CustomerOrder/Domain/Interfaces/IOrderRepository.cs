using CustomerOrder.Domain.Entities;

namespace CustomerOrder.Domain.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int orderId);
    Task<Order?> GetByOrderNumberAsync(string orderNumber);
    Task<IEnumerable<Order>> GetByCustomerIdAsync(int customerId);
    Task<(IEnumerable<Order> Orders, int TotalCount)> GetFilteredAsync(
        DateTime? startDate,
        DateTime? endDate,
        string? status,
        int pageNumber,
        int pageSize);
    Task<bool> CreateAsync(Order order, bool noTracking = true);
    Task<bool> UpdateAsync(Order order, bool noTracking = true);
    Task<bool> DeleteAsync(int orderId);
}
