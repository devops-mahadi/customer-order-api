using CustomerOrder.Domain.Entities;
using CustomerOrder.Infrastructure.Repositories;
using FluentAssertions;

namespace CustomerOrder.Tests.Integration.Container.Repositories;

[Collection("Database")]
public class CustomerRepositoryTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidCustomer_ReturnsTrue()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var repository = new CustomerRepository(context);

        var customer = new Customer
        {
            Email = $"create{Guid.NewGuid()}@test.com",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "123-456-7890",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = await repository.CreateAsync(customer);

        // Assert
        result.Should().BeTrue();
        customer.CustomerId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateAsync_DuplicateEmail_ThrowsException()
    {
        // Arrange
        await using var context1 = fixture.CreateDbContext();
        var repository1 = new CustomerRepository(context1);

        var email = $"duplicate{Guid.NewGuid()}@test.com";
        var customer1 = new Customer
        {
            Email = email,
            FirstName = "First",
            LastName = "Customer",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        await repository1.CreateAsync(customer1);

        // Act & Assert
        await using var context2 = fixture.CreateDbContext();
        var repository2 = new CustomerRepository(context2);

        var customer2 = new Customer
        {
            Email = email,
            FirstName = "Second",
            LastName = "Customer",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        var act = () => repository2.CreateAsync(customer2);
        await act.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingCustomer_ReturnsCustomer()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var repository = new CustomerRepository(context);

        var customer = new Customer
        {
            Email = $"getbyid{Guid.NewGuid()}@test.com",
            FirstName = "Jane",
            LastName = "Smith",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        await repository.CreateAsync(customer);
        var customerId = customer.CustomerId;

        // Act
        var result = await repository.GetByIdAsync(customerId);

        // Assert
        result.Should().NotBeNull();
        result!.CustomerId.Should().Be(customerId);
        result.Email.Should().Be(customer.Email);
        result.FirstName.Should().Be(customer.FirstName);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentCustomer_ReturnsNull()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var repository = new CustomerRepository(context);

        // Act
        var result = await repository.GetByIdAsync(999999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByEmailAsync Tests

    [Fact]
    public async Task GetByEmailAsync_ExistingCustomer_ReturnsCustomer()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var repository = new CustomerRepository(context);

        var email = $"getbyemail{Guid.NewGuid()}@test.com";
        var customer = new Customer
        {
            Email = email,
            FirstName = "Test",
            LastName = "User",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        await repository.CreateAsync(customer);

        // Act
        var result = await repository.GetByEmailAsync(email);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be(email);
        result.FirstName.Should().Be("Test");
        result.LastName.Should().Be("User");
    }

    [Fact]
    public async Task GetByEmailAsync_NonExistentCustomer_ReturnsNull()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var repository = new CustomerRepository(context);

        // Act
        var result = await repository.GetByEmailAsync("nonexistent@test.com");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByEmailAsync_CaseInsensitiveSearch_ReturnsCustomer()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var repository = new CustomerRepository(context);

        var email = $"CaseSensitive{Guid.NewGuid()}@TEST.COM";
        var customer = new Customer
        {
            Email = email,
            FirstName = "Case",
            LastName = "Test",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        await repository.CreateAsync(customer);

        // Act
        var result = await repository.GetByEmailAsync(email.ToLower());

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be(email);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllCustomers()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var repository = new CustomerRepository(context);

        var email1 = $"getall1{Guid.NewGuid()}@test.com";
        var email2 = $"getall2{Guid.NewGuid()}@test.com";

        var customer1 = new Customer
        {
            Email = email1,
            FirstName = "Customer",
            LastName = "One",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        var customer2 = new Customer
        {
            Email = email2,
            FirstName = "Customer",
            LastName = "Two",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        await repository.CreateAsync(customer1);
        await repository.CreateAsync(customer2);

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        var resultList = result.ToList();
        resultList.Should().HaveCountGreaterThanOrEqualTo(2);
        resultList.Should().Contain(c => c.Email == email1);
        resultList.Should().Contain(c => c.Email == email2);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingCustomer_ReturnsTrue()
    {
        // Arrange
        await using var context1 = fixture.CreateDbContext();
        var repository1 = new CustomerRepository(context1);

        var customer = new Customer
        {
            Email = $"update{Guid.NewGuid()}@test.com",
            FirstName = "Original",
            LastName = "Name",
            PhoneNumber = "111-1111",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        await repository1.CreateAsync(customer);
        var customerId = customer.CustomerId;

        // Act
        await using var context2 = fixture.CreateDbContext();
        var repository2 = new CustomerRepository(context2);
        var customerToUpdate = await repository2.GetByIdAsync(customerId);

        customerToUpdate!.FirstName = "Updated";
        customerToUpdate.LastName = "Name";
        customerToUpdate.PhoneNumber = "222-2222";
        customerToUpdate.LastUpdatedAt = DateTime.UtcNow;

        var result = await repository2.UpdateAsync(customerToUpdate);

        // Assert
        result.Should().BeTrue();

        // Verify the update
        await using var context3 = fixture.CreateDbContext();
        var repository3 = new CustomerRepository(context3);
        var updatedCustomer = await repository3.GetByIdAsync(customerId);

        updatedCustomer.Should().NotBeNull();
        updatedCustomer!.FirstName.Should().Be("Updated");
        updatedCustomer.PhoneNumber.Should().Be("222-2222");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingCustomer_ReturnsTrue()
    {
        // Arrange
        await using var context1 = fixture.CreateDbContext();
        var repository1 = new CustomerRepository(context1);

        var customer = new Customer
        {
            Email = $"delete{Guid.NewGuid()}@test.com",
            FirstName = "Delete",
            LastName = "Me",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        await repository1.CreateAsync(customer);
        var customerId = customer.CustomerId;

        // Act
        await using var context2 = fixture.CreateDbContext();
        var repository2 = new CustomerRepository(context2);
        var customerToDelete = await repository2.GetByIdAsync(customerId);

        var result = await repository2.DeleteAsync(customerToDelete!);

        // Assert
        result.Should().BeTrue();

        // Verify deletion
        await using var context3 = fixture.CreateDbContext();
        var repository3 = new CustomerRepository(context3);
        var deletedCustomer = await repository3.GetByIdAsync(customerId);

        deletedCustomer.Should().BeNull();
    }

    #endregion

    #region EmailExistsAsync Tests

    [Fact]
    public async Task EmailExistsAsync_ExistingEmail_ReturnsTrue()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var repository = new CustomerRepository(context);

        var email = $"exists{Guid.NewGuid()}@test.com";
        var customer = new Customer
        {
            Email = email,
            FirstName = "Exists",
            LastName = "Test",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        await repository.CreateAsync(customer);

        // Act
        var result = await repository.EmailExistsAsync(email);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EmailExistsAsync_NonExistentEmail_ReturnsFalse()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var repository = new CustomerRepository(context);

        // Act
        var result = await repository.EmailExistsAsync("doesnotexist@test.com");

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
