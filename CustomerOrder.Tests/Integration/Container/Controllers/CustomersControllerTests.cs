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
public class CustomersControllerTests : IClassFixture<ContainerWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ContainerWebApplicationFactory _factory;

    public CustomersControllerTests(ContainerWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();

        // Add JWT token for authentication using real JwtTokenService
        var token = TestAuthHelper.GetTestToken();
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
    }

    [Fact]
    public async Task CreateCustomer_ValidRequest_Returns201Created()
    {
        // Arrange
        var request = new CreateCustomerRequest
        {
            Email = "container-integration@test.com",
            FirstName = "Container",
            LastName = "Test",
            PhoneNumber = "555-0100"
        };

        // Act
        var response = await _client.PostAsJsonAsync(ApiRoutes.Customers.Base, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var customer = await response.Content.ReadFromJsonAsync<CustomerResponse>();
        customer.Should().NotBeNull();
        customer!.Email.Should().Be(request.Email);
        customer.FirstName.Should().Be(request.FirstName);
        customer.LastName.Should().Be(request.LastName);
    }

    [Fact]
    public async Task CreateCustomer_DuplicateEmail_Returns400BadRequest()
    {
        // Arrange
        var request = new CreateCustomerRequest
        {
            Email = "container-duplicate@test.com",
            FirstName = "First",
            LastName = "User",
            PhoneNumber = "555-0200"
        };

        await _client.PostAsJsonAsync(ApiRoutes.Customers.Base, request);

        // Act - Try to create duplicate
        var response = await _client.PostAsJsonAsync(ApiRoutes.Customers.Base, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("already exists");
    }

    [Fact]
    public async Task GetAllCustomers_ReturnsCustomersList()
    {
        // Arrange
        var customer1 = new CreateCustomerRequest
        {
            Email = "container-customer1@test.com",
            FirstName = "Customer",
            LastName = "One",
            PhoneNumber = "555-0301"
        };
        var customer2 = new CreateCustomerRequest
        {
            Email = "container-customer2@test.com",
            FirstName = "Customer",
            LastName = "Two",
            PhoneNumber = "555-0302"
        };

        await _client.PostAsJsonAsync(ApiRoutes.Customers.Base, customer1);
        await _client.PostAsJsonAsync(ApiRoutes.Customers.Base, customer2);

        // Act
        var response = await _client.GetAsync(ApiRoutes.Customers.Base);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var customers = await response.Content.ReadFromJsonAsync<List<CustomerResponse>>();
        customers.Should().NotBeNull();
        customers!.Should().HaveCountGreaterThanOrEqualTo(2);
        customers.Should().Contain(c => c.Email == "container-customer1@test.com");
        customers.Should().Contain(c => c.Email == "container-customer2@test.com");
    }

    [Fact]
    public async Task GetCustomerByEmail_ExistingCustomer_Returns200OK()
    {
        // Arrange
        var createRequest = new CreateCustomerRequest
        {
            Email = "container-getbyemail@test.com",
            FirstName = "Get",
            LastName = "ByEmail",
            PhoneNumber = "555-0400"
        };

        await _client.PostAsJsonAsync(ApiRoutes.Customers.Base, createRequest);

        // Act
        var response = await _client.GetAsync(ApiRoutes.Customers.ByEmail(createRequest.Email));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var customer = await response.Content.ReadFromJsonAsync<CustomerResponse>();
        customer.Should().NotBeNull();
        customer!.Email.Should().Be(createRequest.Email);
    }

    [Fact]
    public async Task UpdateCustomer_ExistingCustomer_Returns200OK()
    {
        // Arrange
        var createRequest = new CreateCustomerRequest
        {
            Email = "container-update@test.com",
            FirstName = "Original",
            LastName = "Name",
            PhoneNumber = "555-0500"
        };

        await _client.PostAsJsonAsync(ApiRoutes.Customers.Base, createRequest);

        var updateRequest = new UpdateCustomerRequest
        {
            FirstName = "Updated",
            LastName = "Name",
            PhoneNumber = "555-0599"
        };

        // Act
        var response = await _client.PutAsJsonAsync(ApiRoutes.Customers.ByEmail(createRequest.Email), updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var customer = await response.Content.ReadFromJsonAsync<CustomerResponse>();
        customer.Should().NotBeNull();
        customer!.FirstName.Should().Be("Updated");
        //customer.PhoneNumber.Should().Be("555-0599");
    }

    [Fact]
    public async Task DeleteCustomer_ExistingCustomer_Returns204NoContent()
    {
        // Arrange
        var createRequest = new CreateCustomerRequest
        {
            Email = "container-delete@test.com",
            FirstName = "Delete",
            LastName = "Me",
            PhoneNumber = "555-0600"
        };

        await _client.PostAsJsonAsync(ApiRoutes.Customers.Base, createRequest);

        // Act
        var response = await _client.DeleteAsync(ApiRoutes.Customers.ByEmail(createRequest.Email));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify customer is deleted
        var getResponse = await _client.GetAsync(ApiRoutes.Customers.ByEmail(createRequest.Email));
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
