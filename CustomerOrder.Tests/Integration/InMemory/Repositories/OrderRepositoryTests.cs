using CustomerOrder.Domain.Constants;
using CustomerOrder.Domain.Entities;
using CustomerOrder.Infrastructure.Persistence;
using CustomerOrder.Infrastructure.Repositories;
using FluentAssertions;

namespace CustomerOrder.Tests.Integration.InMemory.Repositories;

public class OrderRepositoryTests(InMemoryDatabaseFixture fixture) : IClassFixture<InMemoryDatabaseFixture>
{
    private async Task<Customer> CreateTestCustomer(CustomerOrderDbContext context, string? email = null)
    {
        var customerRepository = new CustomerRepository(context);

        var customer = new Customer
        {
            Email = email ?? $"customer{Guid.NewGuid()}@test.com",
            FirstName = "Test",
            LastName = "Customer",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        await customerRepository.CreateAsync(customer);
        return customer;
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidOrder_ReturnsOrderWithId()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var customer = await CreateTestCustomer(context);
        var repository = new OrderRepository(context);

        var order = new Order
        {
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            CustomerId = customer.CustomerId,
            OrderDate = DateTime.UtcNow,
            TotalAmount = 150.00m,
            Status = ApplicationConstants.Order.Status.Pending,
            ShippingAddress = "123 Main St",
            Notes = "Test order",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = await repository.CreateAsync(order);

        // Assert
        result.Should().BeTrue();
        order.OrderId.Should().BeGreaterThan(0);
        order.OrderNumber.Should().NotBeNullOrEmpty();
        order.TotalAmount.Should().Be(150.00m);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingOrder_ReturnsOrderWithCustomer()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var customer = await CreateTestCustomer(context);
        var repository = new OrderRepository(context);

        var order = new Order
        {
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            CustomerId = customer.CustomerId,
            OrderDate = DateTime.UtcNow,
            TotalAmount = 75.50m,
            Status = ApplicationConstants.Order.Status.Confirmed,
            ShippingAddress = "789 Elm St",
            CreatedAt = DateTime.UtcNow
        };

        await repository.CreateAsync(order);

        // Act
        var result = await repository.GetByIdAsync(order.OrderId);

        // Assert
        result.Should().NotBeNull();
        result!.OrderId.Should().Be(order.OrderId);
        result.Customer.Should().NotBeNull();
        result.Customer!.Email.Should().Be(customer.Email);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentOrder_ReturnsNull()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var repository = new OrderRepository(context);

        // Act
        var result = await repository.GetByIdAsync(999999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByOrderNumberAsync Tests

    [Fact]
    public async Task GetByOrderNumberAsync_ExistingOrder_ReturnsOrder()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var customer = await CreateTestCustomer(context);
        var repository = new OrderRepository(context);

        var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        var order = new Order
        {
            OrderNumber = orderNumber,
            CustomerId = customer.CustomerId,
            OrderDate = DateTime.UtcNow,
            TotalAmount = 200.00m,
            Status = ApplicationConstants.Order.Status.Shipped,
            ShippingAddress = "321 Pine Rd",
            CreatedAt = DateTime.UtcNow
        };

        await repository.CreateAsync(order);

        // Act
        var result = await repository.GetByOrderNumberAsync(orderNumber);

        // Assert
        result.Should().NotBeNull();
        result!.OrderNumber.Should().Be(orderNumber);
        result.TotalAmount.Should().Be(200.00m);
        result.Customer.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByOrderNumberAsync_NonExistentOrder_ReturnsNull()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var repository = new OrderRepository(context);

        // Act
        var result = await repository.GetByOrderNumberAsync("ORD-99999999-NOTFOUND");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByCustomerIdAsync Tests

    [Fact]
    public async Task GetByCustomerIdAsync_ReturnsCustomerOrders_OrderedByDateDescending()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var customer = await CreateTestCustomer(context);
        var repository = new OrderRepository(context);

        var order1 = new Order
        {
            OrderNumber = $"ORD-20260401-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            CustomerId = customer.CustomerId,
            OrderDate = new DateTime(2026, 4, 1),
            TotalAmount = 100.00m,
            Status = ApplicationConstants.Order.Status.Delivered,
            ShippingAddress = "Address 1",
            CreatedAt = new DateTime(2026, 4, 1)
        };

        var order2 = new Order
        {
            OrderNumber = $"ORD-20260405-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            CustomerId = customer.CustomerId,
            OrderDate = new DateTime(2026, 4, 5),
            TotalAmount = 200.00m,
            Status = ApplicationConstants.Order.Status.Pending,
            ShippingAddress = "Address 2",
            CreatedAt = new DateTime(2026, 4, 5)
        };

        await repository.CreateAsync(order1);
        await repository.CreateAsync(order2);

        // Act
        var result = await repository.GetByCustomerIdAsync(customer.CustomerId);

        // Assert
        var resultList = result.ToList();
        resultList.Should().HaveCountGreaterThanOrEqualTo(2);
        resultList[0].OrderDate.Should().BeAfter(resultList[1].OrderDate);
    }

    [Fact]
    public async Task GetByCustomerIdAsync_NoOrders_ReturnsEmptyCollection()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var customer = await CreateTestCustomer(context);
        var repository = new OrderRepository(context);

        // Act
        var result = await repository.GetByCustomerIdAsync(customer.CustomerId);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetFilteredAsync Tests

    [Fact]
    public async Task GetFilteredAsync_DateRangeFilter_ReturnsOrdersInRange()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var customer = await CreateTestCustomer(context);
        var repository = new OrderRepository(context);

        var order1 = new Order
        {
            OrderNumber = $"ORD-20260401-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            CustomerId = customer.CustomerId,
            OrderDate = new DateTime(2026, 4, 1),
            TotalAmount = 100.00m,
            Status = ApplicationConstants.Order.Status.Pending,
            ShippingAddress = "Address 1",
            CreatedAt = new DateTime(2026, 4, 1)
        };

        var order2 = new Order
        {
            OrderNumber = $"ORD-20260410-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            CustomerId = customer.CustomerId,
            OrderDate = new DateTime(2026, 4, 10),
            TotalAmount = 200.00m,
            Status = ApplicationConstants.Order.Status.Pending,
            ShippingAddress = "Address 2",
            CreatedAt = new DateTime(2026, 4, 10)
        };

        await repository.CreateAsync(order1);
        await repository.CreateAsync(order2);

        // Act
        var (orders, totalCount) = await repository.GetFilteredAsync(
            startDate: new DateTime(2026, 4, 5),
            endDate: new DateTime(2026, 4, 15),
            status: null,
            pageNumber: 1,
            pageSize: 10);

        // Assert
        var resultList = orders.ToList();
        resultList.Should().Contain(o => o.OrderNumber == order2.OrderNumber);
        resultList.Should().NotContain(o => o.OrderNumber == order1.OrderNumber);
    }

    [Fact]
    public async Task GetFilteredAsync_StatusFilter_ReturnsOrdersWithStatus()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var customer = await CreateTestCustomer(context);
        var repository = new OrderRepository(context);

        var order1 = new Order
        {
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            CustomerId = customer.CustomerId,
            OrderDate = DateTime.UtcNow,
            TotalAmount = 100.00m,
            Status = ApplicationConstants.Order.Status.Pending,
            ShippingAddress = "Address 1",
            CreatedAt = DateTime.UtcNow
        };

        var order2 = new Order
        {
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            CustomerId = customer.CustomerId,
            OrderDate = DateTime.UtcNow,
            TotalAmount = 200.00m,
            Status = ApplicationConstants.Order.Status.Shipped,
            ShippingAddress = "Address 2",
            CreatedAt = DateTime.UtcNow
        };

        await repository.CreateAsync(order1);
        await repository.CreateAsync(order2);

        // Act
        var (orders, totalCount) = await repository.GetFilteredAsync(
            startDate: null,
            endDate: null,
            status: ApplicationConstants.Order.Status.Shipped,
            pageNumber: 1,
            pageSize: 10);

        // Assert
        orders.Should().NotBeEmpty();
        orders.Should().OnlyContain(o => o.Status == ApplicationConstants.Order.Status.Shipped);
    }

    [Fact]
    public async Task GetFilteredAsync_Pagination_ReturnsCorrectPage()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var customer = await CreateTestCustomer(context);
        var repository = new OrderRepository(context);

        // Create multiple orders
        for (int i = 0; i < 5; i++)
        {
            var order = new Order
            {
                OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
                CustomerId = customer.CustomerId,
                OrderDate = DateTime.UtcNow.AddDays(-i),
                TotalAmount = 100.00m * (i + 1),
                Status = ApplicationConstants.Order.Status.Pending,
                ShippingAddress = $"Address {i}",
                CreatedAt = DateTime.UtcNow
            };
            await repository.CreateAsync(order);
        }

        // Act
        var (orders, totalCount) = await repository.GetFilteredAsync(
            startDate: null,
            endDate: null,
            status: null,
            pageNumber: 1,
            pageSize: 2);

        // Assert
        orders.Should().HaveCount(2);
        totalCount.Should().BeGreaterThanOrEqualTo(5);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingOrder_UpdatesSuccessfully()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var customer = await CreateTestCustomer(context);
        var repository = new OrderRepository(context);

        var order = new Order
        {
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            CustomerId = customer.CustomerId,
            OrderDate = DateTime.UtcNow,
            TotalAmount = 100.00m,
            Status = ApplicationConstants.Order.Status.Pending,
            ShippingAddress = "Old Address",
            CreatedAt = DateTime.UtcNow
        };

        await repository.CreateAsync(order);

        // Act
        var orderToUpdate = await repository.GetByIdAsync(order.OrderId);

        orderToUpdate!.Status = ApplicationConstants.Order.Status.Shipped;
        orderToUpdate.ShippingAddress = "New Address";
        orderToUpdate.UpdatedAt = DateTime.UtcNow;

        var result = await repository.UpdateAsync(orderToUpdate);

        // Assert
        result.Should().BeTrue();

        // Verify the update
        var updated = await repository.GetByIdAsync(order.OrderId);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(ApplicationConstants.Order.Status.Shipped);
        updated.ShippingAddress.Should().Be("New Address");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingOrder_ReturnsTrue()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var customer = await CreateTestCustomer(context);
        var repository = new OrderRepository(context);

        var order = new Order
        {
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            CustomerId = customer.CustomerId,
            OrderDate = DateTime.UtcNow,
            TotalAmount = 100.00m,
            Status = ApplicationConstants.Order.Status.Pending,
            ShippingAddress = "Address",
            CreatedAt = DateTime.UtcNow
        };

        await repository.CreateAsync(order);

        // Act
        var result = await repository.DeleteAsync(order.OrderId);

        // Assert
        result.Should().BeTrue();

        // Verify deletion
        var deleted = await repository.GetByIdAsync(order.OrderId);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentOrder_ReturnsFalse()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var repository = new OrderRepository(context);

        // Act
        var result = await repository.DeleteAsync(999999);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
