using CustomerOrder.Application.Services;
using CustomerOrder.Domain.Constants;
using CustomerOrder.Domain.Entities;
using CustomerOrder.Domain.Interfaces;
using CustomerOrder.Presentation.DTOs.Requests;
using FluentAssertions;
using Moq;

namespace CustomerOrder.Tests.Unit.Services;

public class CustomerServiceTests
{
    private readonly Mock<ICustomerRepository> _mockRepository;
    private readonly CustomerService _service;

    public CustomerServiceTests()
    {
        _mockRepository = new Mock<ICustomerRepository>();
        _service = new CustomerService(_mockRepository.Object);
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsTrue()
    {
        // Arrange
        var request = new CreateCustomerRequest
        {
            Email = "new@example.com",
            FirstName = "Jane",
            LastName = "Smith",
            PhoneNumber = "123-456-7890"
        };

        _mockRepository.Setup(x => x.EmailExistsAsync(request.Email))
            .ReturnsAsync(false);
        _mockRepository.Setup(x => x.CreateAsync(It.IsAny<Customer>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(x => x.CreateAsync(It.Is<Customer>(
            c => c.Email == request.Email &&
                 c.FirstName == request.FirstName &&
                 c.LastName == request.LastName &&
                 c.PhoneNumber == request.PhoneNumber)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_EmailAlreadyExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new CreateCustomerRequest
        {
            Email = "existing@example.com",
            FirstName = "John",
            LastName = "Doe"
        };

        _mockRepository.Setup(x => x.EmailExistsAsync(request.Email))
            .ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(request));

        exception.Message.Should().Contain("already exists");
        exception.Message.Should().Contain(request.Email);
        _mockRepository.Verify(x => x.CreateAsync(It.IsAny<Customer>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NullPhoneNumber_SetsEmptyString()
    {
        // Arrange
        var request = new CreateCustomerRequest
        {
            Email = "nophone@example.com",
            FirstName = "No",
            LastName = "Phone",
            PhoneNumber = null
        };

        _mockRepository.Setup(x => x.EmailExistsAsync(request.Email))
            .ReturnsAsync(false);
        _mockRepository.Setup(x => x.CreateAsync(It.IsAny<Customer>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(x => x.CreateAsync(It.Is<Customer>(
            c => c.PhoneNumber == string.Empty)), Times.Once);
    }

    #endregion

    #region GetByEmailAsync Tests

    [Fact]
    public async Task GetByEmailAsync_ExistingCustomer_ReturnsCustomerResponse()
    {
        // Arrange
        var email = "test@example.com";
        var customer = new Customer
        {
            CustomerId = 1,
            Email = email,
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "555-1234",
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            LastUpdatedAt = DateTime.UtcNow
        };

        _mockRepository.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync(customer);

        // Act
        var result = await _service.GetByEmailAsync(email);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be(customer.Email);
        result.FirstName.Should().Be(customer.FirstName);
        result.LastName.Should().Be(customer.LastName);
        result.CreatedAt.Should().Be(customer.CreatedAt);
        result.LastUpdatedAt.Should().Be(customer.LastUpdatedAt);
    }

    [Fact]
    public async Task GetByEmailAsync_NonExistentCustomer_ReturnsNull()
    {
        // Arrange
        var email = "nonexistent@example.com";
        _mockRepository.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync((Customer?)null);

        // Act
        var result = await _service.GetByEmailAsync(email);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllCustomers()
    {
        // Arrange
        var customers = new List<Customer>
        {
            new Customer
            {
                CustomerId = 1,
                Email = "user1@example.com",
                FirstName = "User",
                LastName = "One",
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            },
            new Customer
            {
                CustomerId = 2,
                Email = "user2@example.com",
                FirstName = "User",
                LastName = "Two",
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            }
        };

        _mockRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(customers);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        var resultList = result.ToList();
        resultList.Should().HaveCount(2);
        resultList[0].Email.Should().Be("user1@example.com");
        resultList[1].Email.Should().Be("user2@example.com");
    }

    [Fact]
    public async Task GetAllAsync_EmptyList_ReturnsEmptyCollection()
    {
        // Arrange
        _mockRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Customer>());

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingCustomer_ReturnsTrue()
    {
        // Arrange
        var email = "update@example.com";
        var existingCustomer = new Customer
        {
            CustomerId = 1,
            Email = email,
            FirstName = "Old",
            LastName = "Name",
            PhoneNumber = "111-1111",
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            LastUpdatedAt = DateTime.UtcNow.AddDays(-30)
        };

        var updateRequest = new UpdateCustomerRequest
        {
            FirstName = "New",
            LastName = "Name",
            PhoneNumber = "222-2222"
        };

        _mockRepository.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync(existingCustomer);
        _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<Customer>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.UpdateAsync(email, updateRequest);

        // Assert
        result.Should().BeTrue();
        existingCustomer.FirstName.Should().Be(updateRequest.FirstName);
        existingCustomer.LastName.Should().Be(updateRequest.LastName);
        existingCustomer.PhoneNumber.Should().Be(updateRequest.PhoneNumber);
        existingCustomer.LastUpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        _mockRepository.Verify(x => x.UpdateAsync(existingCustomer), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentCustomer_ReturnsFalse()
    {
        // Arrange
        var email = "nonexistent@example.com";
        var updateRequest = new UpdateCustomerRequest
        {
            FirstName = "New",
            LastName = "Name"
        };

        _mockRepository.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync((Customer?)null);

        // Act
        var result = await _service.UpdateAsync(email, updateRequest);

        // Assert
        result.Should().BeFalse();
        _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<Customer>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_NullPhoneNumber_SetsEmptyString()
    {
        // Arrange
        var email = "update@example.com";
        var existingCustomer = new Customer
        {
            CustomerId = 1,
            Email = email,
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "555-1234",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        var updateRequest = new UpdateCustomerRequest
        {
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = null
        };

        _mockRepository.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync(existingCustomer);
        _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<Customer>()))
            .ReturnsAsync(true);

        // Act
        await _service.UpdateAsync(email, updateRequest);

        // Assert
        existingCustomer.PhoneNumber.Should().Be(string.Empty);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingCustomer_ReturnsTrue()
    {
        // Arrange
        var email = "delete@example.com";
        var customer = new Customer
        {
            CustomerId = 1,
            Email = email,
            FirstName = "Delete",
            LastName = "Me",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        _mockRepository.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync(customer);
        _mockRepository.Setup(x => x.DeleteAsync(customer))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DeleteAsync(email);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(x => x.DeleteAsync(customer), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentCustomer_ReturnsFalse()
    {
        // Arrange
        var email = "nonexistent@example.com";
        _mockRepository.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync((Customer?)null);

        // Act
        var result = await _service.DeleteAsync(email);

        // Assert
        result.Should().BeFalse();
        _mockRepository.Verify(x => x.DeleteAsync(It.IsAny<Customer>()), Times.Never);
    }

    #endregion
}
