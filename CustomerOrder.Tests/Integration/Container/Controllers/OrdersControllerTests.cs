using System.Net;
using System.Net.Http.Json;
using CustomerOrder.Presentation.DTOs.Requests;
using CustomerOrder.Presentation.DTOs.Responses;
using CustomerOrder.Tests.Helpers;
using CustomerOrder.Tests.Integration.Fixtures;
using FluentAssertions;
using Xunit;

namespace CustomerOrder.Tests.Integration.Container.Controllers;

[Trait("Category", "IntegrationContainer")]
public class OrdersControllerTests : IClassFixture<ContainerWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ContainerWebApplicationFactory _factory;

    public OrdersControllerTests(ContainerWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();

        // Add JWT token for authentication using real JwtTokenService
        var token = TestAuthHelper.GetTestToken();
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
    }

    [Fact]
    public async Task CreateOrder_ValidRequest_Returns200OKWithOrderNumber()
    {
        // Arrange - First create a customer
        var customerRequest = new CreateCustomerRequest
        {
            Email = "container-order-customer@test.com",
            FirstName = "Order",
            LastName = "Customer",
            PhoneNumber = "555-1000"
        };
        await _client.PostAsJsonAsync(ApiRoutes.Customers.Base, customerRequest);

        var orderRequest = new CreateOrderRequest
        {
            CustomerEmail = "container-order-customer@test.com",
            TotalAmount = 199.99m,
            ShippingAddress = "123 Container Order St",
            Notes = "Container test order"
        };

        // Act
        var response = await _client.PostAsJsonAsync(ApiRoutes.Orders.Base, orderRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var orderNumber = await response.Content.ReadAsStringAsync();
        orderNumber.Should().NotBeNullOrEmpty();
        orderNumber.Should().StartWith("ORD-");
    }

    [Fact]
    public async Task CreateOrder_NonExistentCustomer_Returns400BadRequest()
    {
        // Arrange
        var orderRequest = new CreateOrderRequest
        {
            CustomerEmail = "container-nonexistent@test.com",
            TotalAmount = 99.99m,
            ShippingAddress = "123 Fake St"
        };

        // Act
        var response = await _client.PostAsJsonAsync(ApiRoutes.Orders.Base, orderRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("not found");
    }

    [Fact]
    public async Task GetOrderByOrderNumber_ExistingOrder_Returns200OK()
    {
        // Arrange - Create customer and order
        var customerRequest = new CreateCustomerRequest
        {
            Email = "container-get-order@test.com",
            FirstName = "Get",
            LastName = "Order",
            PhoneNumber = "555-2000"
        };
        await _client.PostAsJsonAsync(ApiRoutes.Customers.Base, customerRequest);

        var orderRequest = new CreateOrderRequest
        {
            CustomerEmail = "container-get-order@test.com",
            TotalAmount = 299.99m,
            ShippingAddress = "456 Container Get St"
        };
        var createResponse = await _client.PostAsJsonAsync(ApiRoutes.Orders.Base, orderRequest);
        var orderNumber = await createResponse.Content.ReadAsStringAsync();
        orderNumber = orderNumber.Trim('"');

        // Act
        var response = await _client.GetAsync(ApiRoutes.Orders.ByOrderNumber(orderNumber));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        order.Should().NotBeNull();
        order!.OrderNumber.Should().Be(orderNumber);
        order.TotalAmount.Should().Be(299.99m);
        order.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task GetOrdersByCustomerEmail_ReturnsCustomerOrders()
    {
        // Arrange - Create customer
        var customerRequest = new CreateCustomerRequest
        {
            Email = "container-multi-orders@test.com",
            FirstName = "Multi",
            LastName = "Orders",
            PhoneNumber = "555-3000"
        };
        await _client.PostAsJsonAsync(ApiRoutes.Customers.Base, customerRequest);

        // Create multiple orders
        var order1 = new CreateOrderRequest
        {
            CustomerEmail = "container-multi-orders@test.com",
            TotalAmount = 100m,
            ShippingAddress = "Container Order 1 Address"
        };
        var order2 = new CreateOrderRequest
        {
            CustomerEmail = "container-multi-orders@test.com",
            TotalAmount = 200m,
            ShippingAddress = "Container Order 2 Address"
        };

        await _client.PostAsJsonAsync(ApiRoutes.Orders.Base, order1);
        await _client.PostAsJsonAsync(ApiRoutes.Orders.Base, order2);

        // Act
        var response = await _client.GetAsync(ApiRoutes.Orders.ByCustomerEmail("container-multi-orders@test.com"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var orders = await response.Content.ReadFromJsonAsync<List<OrderResponse>>();
        orders.Should().NotBeNull();
        orders!.Should().HaveCountGreaterThanOrEqualTo(2);
        orders.Should().Contain(o => o.TotalAmount == 100m);
        orders.Should().Contain(o => o.TotalAmount == 200m);
    }

    [Fact]
    public async Task GetFilteredOrders_WithPagination_ReturnsPagedResults()
    {
        // Arrange - Create customer and multiple orders
        var customerRequest = new CreateCustomerRequest
        {
            Email = "container-paginated-orders@test.com",
            FirstName = "Paginated",
            LastName = "Orders",
            PhoneNumber = "555-4000"
        };
        await _client.PostAsJsonAsync(ApiRoutes.Customers.Base, customerRequest);

        for (int i = 1; i <= 15; i++)
        {
            var order = new CreateOrderRequest
            {
                CustomerEmail = "container-paginated-orders@test.com",
                TotalAmount = i * 10m,
                ShippingAddress = $"Container Address {i}"
            };
            await _client.PostAsJsonAsync(ApiRoutes.Orders.Base, order);
        }

        // Act - Get first page with page size 10
        var response = await _client.GetAsync($"{ApiRoutes.Orders.Base}?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var pagedResponse = await response.Content.ReadFromJsonAsync<PagedResponse<OrderResponse>>();
        pagedResponse.Should().NotBeNull();
        pagedResponse!.PageNumber.Should().Be(1);
        pagedResponse.PageSize.Should().Be(10);
        pagedResponse.TotalCount.Should().BeGreaterThanOrEqualTo(15);
        pagedResponse.Data.Should().HaveCount(10);
    }

    [Fact]
    public async Task UpdateOrder_ValidRequest_Returns200OK()
    {
        // Arrange - Create customer and order
        var customerRequest = new CreateCustomerRequest
        {
            Email = "container-update-order@test.com",
            FirstName = "Update",
            LastName = "Order",
            PhoneNumber = "555-6000"
        };
        await _client.PostAsJsonAsync(ApiRoutes.Customers.Base, customerRequest);

        var orderRequest = new CreateOrderRequest
        {
            CustomerEmail = "container-update-order@test.com",
            TotalAmount = 500m,
            ShippingAddress = "Container Original Address"
        };
        var createResponse = await _client.PostAsJsonAsync(ApiRoutes.Orders.Base, orderRequest);
        var orderNumber = await createResponse.Content.ReadAsStringAsync();
        orderNumber = orderNumber.Trim('"');

        var updateRequest = new UpdateOrderRequest
        {
            Status = "Shipped",
            ShippingAddress = "Container Updated Address"
        };

        // Act
        var response = await _client.PutAsJsonAsync(ApiRoutes.Orders.ByOrderNumber(orderNumber), updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify update
        var getResponse = await _client.GetAsync(ApiRoutes.Orders.ByOrderNumber(orderNumber));
        var updatedOrder = await getResponse.Content.ReadFromJsonAsync<OrderResponse>();
        updatedOrder.Should().NotBeNull();
        updatedOrder!.Status.Should().Be("Shipped");
        updatedOrder.ShippingAddress.Should().Be("Container Updated Address");
    }

    [Fact]
    public async Task UpdateOrder_NonExistent_Returns404NotFound()
    {
        // Arrange
        var updateRequest = new UpdateOrderRequest
        {
            Status = "Shipped"
        };

        // Act
        var response = await _client.PutAsJsonAsync(ApiRoutes.Orders.ByOrderNumber("ORD-99999999-999999"), updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
