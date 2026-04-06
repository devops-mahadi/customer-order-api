using CustomerOrder.Application.Services;
using CustomerOrder.Domain.Constants;
using CustomerOrder.Domain.Entities;
using CustomerOrder.Domain.Interfaces;
using CustomerOrder.Presentation.DTOs.Requests;
using FluentAssertions;
using Moq;

namespace CustomerOrder.Tests.Unit.Services;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly Mock<ICustomerRepository> _mockCustomerRepository;
    private readonly OrderService _service;

    public OrderServiceTests()
    {
        _mockOrderRepository = new Mock<IOrderRepository>();
        _mockCustomerRepository = new Mock<ICustomerRepository>();
        _service = new OrderService(_mockOrderRepository.Object, _mockCustomerRepository.Object);
    }

    #region GetByOrderNumberAsync Tests

    [Fact]
    public async Task GetByOrderNumberAsync_ExistingOrder_ReturnsOrderResponse()
    {
        // Arrange
        var orderNumber = "ORD-20260405-ABC123";
        var customer = new Customer
        {
            CustomerId = 1,
            Email = "customer@example.com",
            FirstName = "John",
            LastName = "Doe",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        var order = new Order
        {
            OrderId = 1,
            OrderNumber = orderNumber,
            CustomerId = customer.CustomerId,
            Customer = customer,
            OrderDate = DateTime.UtcNow,
            TotalAmount = 100.50m,
            Status = ApplicationConstants.Order.Status.Pending,
            ShippingAddress = "123 Main St",
            Notes = "Test order",
            CreatedAt = DateTime.UtcNow
        };

        _mockOrderRepository.Setup(x => x.GetByOrderNumberAsync(orderNumber))
            .ReturnsAsync(order);

        // Act
        var result = await _service.GetByOrderNumberAsync(orderNumber);

        // Assert
        result.Should().NotBeNull();
        result!.OrderNumber.Should().Be(orderNumber);
        result.CustomerEmail.Should().Be(customer.Email);
        result.CustomerName.Should().Be("John Doe");
        result.TotalAmount.Should().Be(100.50m);
        result.Status.Should().Be(ApplicationConstants.Order.Status.Pending);
    }

    [Fact]
    public async Task GetByOrderNumberAsync_OrderWithoutCustomerNavigation_LoadsCustomer()
    {
        // Arrange
        var orderNumber = "ORD-20260405-XYZ789";
        var customer = new Customer
        {
            CustomerId = 1,
            Email = "customer@example.com",
            FirstName = "Jane",
            LastName = "Smith",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        var order = new Order
        {
            OrderId = 1,
            OrderNumber = orderNumber,
            CustomerId = customer.CustomerId,
            Customer = null, // Navigation property not loaded
            OrderDate = DateTime.UtcNow,
            TotalAmount = 50.00m,
            Status = ApplicationConstants.Order.Status.Confirmed,
            ShippingAddress = "456 Oak Ave",
            CreatedAt = DateTime.UtcNow
        };

        _mockOrderRepository.Setup(x => x.GetByOrderNumberAsync(orderNumber))
            .ReturnsAsync(order);
        _mockCustomerRepository.Setup(x => x.GetByIdAsync(customer.CustomerId))
            .ReturnsAsync(customer);

        // Act
        var result = await _service.GetByOrderNumberAsync(orderNumber);

        // Assert
        result.Should().NotBeNull();
        result!.CustomerEmail.Should().Be(customer.Email);
        result.CustomerName.Should().Be("Jane Smith");
        _mockCustomerRepository.Verify(x => x.GetByIdAsync(customer.CustomerId), Times.Once);
    }

    [Fact]
    public async Task GetByOrderNumberAsync_NonExistentOrder_ReturnsNull()
    {
        // Arrange
        var orderNumber = "ORD-99999999-NOTFOUND";
        _mockOrderRepository.Setup(x => x.GetByOrderNumberAsync(orderNumber))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _service.GetByOrderNumberAsync(orderNumber);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByOrderNumberAsync_CustomerNotFound_ReturnsNull()
    {
        // Arrange
        var orderNumber = "ORD-20260405-ABC123";
        var order = new Order
        {
            OrderId = 1,
            OrderNumber = orderNumber,
            CustomerId = 999,
            Customer = null,
            OrderDate = DateTime.UtcNow,
            TotalAmount = 100.00m,
            Status = ApplicationConstants.Order.Status.Pending,
            ShippingAddress = "123 Main St",
            CreatedAt = DateTime.UtcNow
        };

        _mockOrderRepository.Setup(x => x.GetByOrderNumberAsync(orderNumber))
            .ReturnsAsync(order);
        _mockCustomerRepository.Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((Customer?)null);

        // Act
        var result = await _service.GetByOrderNumberAsync(orderNumber);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByCustomerEmailAsync Tests

    [Fact]
    public async Task GetByCustomerEmailAsync_ExistingCustomer_ReturnsOrders()
    {
        // Arrange
        var email = "customer@example.com";
        var customer = new Customer
        {
            CustomerId = 1,
            Email = email,
            FirstName = "John",
            LastName = "Doe",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        var orders = new List<Order>
        {
            new Order
            {
                OrderId = 1,
                OrderNumber = "ORD-20260401-ABC123",
                CustomerId = customer.CustomerId,
                OrderDate = DateTime.UtcNow.AddDays(-5),
                TotalAmount = 100.00m,
                Status = ApplicationConstants.Order.Status.Delivered,
                ShippingAddress = "123 Main St",
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new Order
            {
                OrderId = 2,
                OrderNumber = "ORD-20260405-DEF456",
                CustomerId = customer.CustomerId,
                OrderDate = DateTime.UtcNow,
                TotalAmount = 200.00m,
                Status = ApplicationConstants.Order.Status.Pending,
                ShippingAddress = "123 Main St",
                CreatedAt = DateTime.UtcNow
            }
        };

        _mockCustomerRepository.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync(customer);
        _mockOrderRepository.Setup(x => x.GetByCustomerIdAsync(customer.CustomerId))
            .ReturnsAsync(orders);

        // Act
        var result = await _service.GetByCustomerEmailAsync(email);

        // Assert
        var resultList = result.ToList();
        resultList.Should().HaveCount(2);
        resultList[0].OrderNumber.Should().Be("ORD-20260401-ABC123");
        resultList[1].OrderNumber.Should().Be("ORD-20260405-DEF456");
        resultList.All(o => o.CustomerEmail == email).Should().BeTrue();
    }

    [Fact]
    public async Task GetByCustomerEmailAsync_NonExistentCustomer_ReturnsEmptyCollection()
    {
        // Arrange
        var email = "nonexistent@example.com";
        _mockCustomerRepository.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync((Customer?)null);

        // Act
        var result = await _service.GetByCustomerEmailAsync(email);

        // Assert
        result.Should().BeEmpty();
        _mockOrderRepository.Verify(x => x.GetByCustomerIdAsync(It.IsAny<int>()), Times.Never);
    }

    #endregion

    #region GetFilteredAsync Tests

    [Fact]
    public async Task GetFilteredAsync_ValidParameters_ReturnsPagedResults()
    {
        // Arrange
        var customer = new Customer
        {
            CustomerId = 1,
            Email = "customer@example.com",
            FirstName = "John",
            LastName = "Doe",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        var orders = new List<Order>
        {
            new Order
            {
                OrderId = 1,
                OrderNumber = "ORD-20260405-ABC123",
                CustomerId = customer.CustomerId,
                Customer = customer,
                OrderDate = DateTime.UtcNow,
                TotalAmount = 100.00m,
                Status = ApplicationConstants.Order.Status.Pending,
                ShippingAddress = "123 Main St",
                CreatedAt = DateTime.UtcNow
            }
        };

        _mockOrderRepository.Setup(x => x.GetFilteredAsync(
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<string?>(),
            1,
            10))
            .ReturnsAsync((orders, 25));

        // Act
        var result = await _service.GetFilteredAsync(null, null, null, 1, 10);

        // Assert
        result.Data.Should().HaveCount(1);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(25);
    }

    [Fact]
    public async Task GetFilteredAsync_PageSizeZero_UsesDefaultPageSize()
    {
        // Arrange
        _mockOrderRepository.Setup(x => x.GetFilteredAsync(
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<string?>(),
            1,
            ApplicationConstants.Pagination.DefaultPageSize))
            .ReturnsAsync((new List<Order>(), 0));

        // Act
        var result = await _service.GetFilteredAsync(null, null, null, 1, 0);

        // Assert
        result.PageSize.Should().Be(ApplicationConstants.Pagination.DefaultPageSize);
        _mockOrderRepository.Verify(x => x.GetFilteredAsync(
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<string?>(),
            1,
            ApplicationConstants.Pagination.DefaultPageSize), Times.Once);
    }

    [Fact]
    public async Task GetFilteredAsync_PageSizeExceedsMax_EnforcesMaxPageSize()
    {
        // Arrange
        _mockOrderRepository.Setup(x => x.GetFilteredAsync(
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<string?>(),
            1,
            ApplicationConstants.Pagination.MaxPageSize))
            .ReturnsAsync((new List<Order>(), 0));

        // Act
        var result = await _service.GetFilteredAsync(null, null, null, 1, 500);

        // Assert
        result.PageSize.Should().Be(ApplicationConstants.Pagination.MaxPageSize);
        _mockOrderRepository.Verify(x => x.GetFilteredAsync(
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<string?>(),
            1,
            ApplicationConstants.Pagination.MaxPageSize), Times.Once);
    }

    [Fact]
    public async Task GetFilteredAsync_OrderWithoutCustomerNavigation_LoadsCustomer()
    {
        // Arrange
        var customer = new Customer
        {
            CustomerId = 1,
            Email = "customer@example.com",
            FirstName = "Jane",
            LastName = "Smith",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        var orders = new List<Order>
        {
            new Order
            {
                OrderId = 1,
                OrderNumber = "ORD-20260405-ABC123",
                CustomerId = customer.CustomerId,
                Customer = null,
                OrderDate = DateTime.UtcNow,
                TotalAmount = 100.00m,
                Status = ApplicationConstants.Order.Status.Pending,
                ShippingAddress = "123 Main St",
                CreatedAt = DateTime.UtcNow
            }
        };

        _mockOrderRepository.Setup(x => x.GetFilteredAsync(
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<string?>(),
            1,
            10))
            .ReturnsAsync((orders, 1));
        _mockCustomerRepository.Setup(x => x.GetByIdAsync(customer.CustomerId))
            .ReturnsAsync(customer);

        // Act
        var result = await _service.GetFilteredAsync(null, null, null, 1, 10);

        // Assert
        result.Data.Should().HaveCount(1);
        result.Data.First().CustomerEmail.Should().Be(customer.Email);
        _mockCustomerRepository.Verify(x => x.GetByIdAsync(customer.CustomerId), Times.Once);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsGeneratedOrderNumber()
    {
        // Arrange
        var customer = new Customer
        {
            CustomerId = 1,
            Email = "customer@example.com",
            FirstName = "John",
            LastName = "Doe",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        var request = new CreateOrderRequest
        {
            CustomerEmail = customer.Email,
            TotalAmount = 150.00m,
            ShippingAddress = "789 Elm St",
            Notes = "Rush delivery"
        };

        _mockCustomerRepository.Setup(x => x.GetByEmailAsync(request.CustomerEmail))
            .ReturnsAsync(customer);
        _mockOrderRepository.Setup(x => x.CreateAsync(It.IsAny<Order>(), It.IsAny<bool>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().MatchRegex(@"^ORD-\d{8}-[A-Z0-9]{6}$");
        _mockOrderRepository.Verify(x => x.CreateAsync(It.Is<Order>(
            o => o.CustomerId == customer.CustomerId &&
                 o.TotalAmount == request.TotalAmount &&
                 o.Status == ApplicationConstants.Order.Status.Pending), It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NonExistentCustomer_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerEmail = "nonexistent@example.com",
            TotalAmount = 100.00m,
            ShippingAddress = "123 Main St"
        };

        _mockCustomerRepository.Setup(x => x.GetByEmailAsync(request.CustomerEmail))
            .ReturnsAsync((Customer?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(request));

        exception.Message.Should().Contain("not found");
        exception.Message.Should().Contain(request.CustomerEmail);
        _mockOrderRepository.Verify(x => x.CreateAsync(It.IsAny<Order>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_OrderNumberFormat_MatchesExpectedPattern()
    {
        // Arrange
        var customer = new Customer
        {
            CustomerId = 1,
            Email = "customer@example.com",
            FirstName = "Test",
            LastName = "User",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        var request = new CreateOrderRequest
        {
            CustomerEmail = customer.Email,
            TotalAmount = 100.00m,
            ShippingAddress = "123 Main St"
        };

        _mockCustomerRepository.Setup(x => x.GetByEmailAsync(request.CustomerEmail))
            .ReturnsAsync(customer);
        _mockOrderRepository.Setup(x => x.CreateAsync(It.IsAny<Order>(), It.IsAny<bool>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        result.Should().StartWith($"ORD-{today}-");
        result.Split('-').Should().HaveCount(3);
        result.Split('-')[2].Should().HaveLength(6);
    }

    #endregion
}
