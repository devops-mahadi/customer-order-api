using CustomerOrder.Application.Interfaces;
using CustomerOrder.Domain.Constants;
using CustomerOrder.Domain.Entities;
using CustomerOrder.Domain.Interfaces;
using CustomerOrder.Presentation.DTOs.Requests;
using CustomerOrder.Presentation.DTOs.Responses;

namespace CustomerOrder.Application.Services;

public class OrderService(IOrderRepository orderRepository, ICustomerRepository customerRepository) : IOrderService
{
    public async Task<OrderResponse?> GetByOrderNumberAsync(string orderNumber)
    {
        var order = await orderRepository.GetByOrderNumberAsync(orderNumber);
        if (order == null)
            return null;

        var customer = order.Customer ?? await customerRepository.GetByIdAsync(order.CustomerId);
        if (customer == null) return null;

        return MapToResponse(order, customer);
    }

    public async Task<IEnumerable<OrderResponse>> GetByCustomerEmailAsync(string customerEmail)
    {
        var customer = await customerRepository.GetByEmailAsync(customerEmail);
        if (customer == null)
            return Enumerable.Empty<OrderResponse>();

        var orders = await orderRepository.GetByCustomerIdAsync(customer.CustomerId);
        return orders.Select(o => MapToResponse(o, customer));
    }

    public async Task<PagedResponse<OrderResponse>> GetFilteredAsync(
        DateTime? startDate,
        DateTime? endDate,
        string? status,
        int pageNumber = 1,
        int pageSize = 10)
    {
        // Apply default pagination if not specified
        if (pageSize <= 0)
            pageSize = ApplicationConstants.Pagination.DefaultPageSize;

        if (pageSize > ApplicationConstants.Pagination.MaxPageSize)
            pageSize = ApplicationConstants.Pagination.MaxPageSize;

        var (orders, totalCount) = await orderRepository.GetFilteredAsync(
            startDate,
            endDate,
            status,
            pageNumber,
            pageSize);

        var orderResponses = new List<OrderResponse>();
        foreach (var order in orders)
        {
            var customer = order.Customer ?? await customerRepository.GetByIdAsync(order.CustomerId);
            if (customer != null)
            {
                orderResponses.Add(MapToResponse(order, customer));
            }
        }

        return new PagedResponse<OrderResponse>
        {
            Data = orderResponses,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<string> CreateAsync(CreateOrderRequest request)
    {
        // Find customer by email
        var customer = await customerRepository.GetByEmailAsync(request.CustomerEmail);
        if (customer == null)
        {
            throw new InvalidOperationException(
                string.Format(ApplicationConstants.ValidationMessages.CustomerNotFound, request.CustomerEmail));
        }

        // Generate unique order number
        var orderNumber = GenerateOrderNumber();

        var order = new Order
        {
            OrderNumber = orderNumber,
            CustomerId = customer.CustomerId,
            OrderDate = DateTime.UtcNow,
            TotalAmount = request.TotalAmount,
            ShippingAddress = request.ShippingAddress,
            Notes = request.Notes,
            Status = ApplicationConstants.Order.Status.Pending,
            CreatedAt = DateTime.UtcNow
        };

        if (await orderRepository.CreateAsync(order)) return orderNumber;

        throw new InvalidOperationException();
    }

    public async Task<bool> UpdateAsync(string orderNumber, UpdateOrderRequest request)
    {
        var order = await orderRepository.GetByOrderNumberAsync(orderNumber);
        if (order == null)
            return false;

        // Update status
        order.Status = request.Status;

        // Update shipping address if provided
        if (!string.IsNullOrWhiteSpace(request.ShippingAddress))
        {
            order.ShippingAddress = request.ShippingAddress;
        }

        order.UpdatedAt = DateTime.UtcNow;

        return await orderRepository.UpdateAsync(order);
    }

    private static OrderResponse MapToResponse(Order order, Customer customer)
    {
        return new OrderResponse
        {
            OrderNumber = order.OrderNumber,
            CustomerEmail = customer.Email,
            CustomerName = $"{customer.FirstName} {customer.LastName}".Trim(),
            OrderDate = order.OrderDate,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            ShippingAddress = order.ShippingAddress,
            Notes = order.Notes,
            CreatedAt = order.CreatedAt
        };
    }

    private static string GenerateOrderNumber()
    {
        // Generate order number: ORD-YYYYMMDD-XXXXXX (e.g., ORD-20260405-AB12CD)
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        var randomPart = Guid.NewGuid().ToString("N")[..6].ToUpper();
        return $"ORD-{datePart}-{randomPart}";
    }
}
