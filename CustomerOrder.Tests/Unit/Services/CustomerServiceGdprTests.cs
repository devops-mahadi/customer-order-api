using CustomerOrder.Application.Services;
using CustomerOrder.Domain.Constants;
using CustomerOrder.Domain.Entities;
using CustomerOrder.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace CustomerOrder.Tests.Unit.Services;

[Trait("Category", "Unit")]
public class CustomerServiceGdprTests
{
    private readonly Mock<ICustomerRepository> _mockCustomerRepository;
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly Mock<IConsentRepository> _mockConsentRepository;
    private readonly CustomerService _service;

    public CustomerServiceGdprTests()
    {
        _mockCustomerRepository = new Mock<ICustomerRepository>();
        _mockOrderRepository = new Mock<IOrderRepository>();
        _mockConsentRepository = new Mock<IConsentRepository>();
        _service = new CustomerService(
            _mockCustomerRepository.Object,
            _mockOrderRepository.Object,
            _mockConsentRepository.Object);
    }

    #region ExportDataAsync Tests

    [Fact]
    public async Task ExportDataAsync_ValidEmail_ReturnsCompleteDataExport()
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
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            LastUpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        var orders = new List<Order>
        {
            new Order
            {
                OrderId = 1,
                OrderNumber = "ORD-20260408-ABC123",
                CustomerId = 1,
                OrderDate = DateTime.UtcNow.AddDays(-5),
                TotalAmount = 199.99m,
                Status = ApplicationConstants.Order.Status.Pending,
                ShippingAddress = "123 Test St",
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            }
        };

        var consents = new List<CustomerConsent>
        {
            new CustomerConsent
            {
                ConsentId = 1,
                CustomerId = 1,
                ConsentType = ApplicationConstants.Consent.Types.Marketing,
                IsGranted = true,
                ConsentDate = DateTime.UtcNow.AddDays(-10),
                IpAddress = "127.0.0.1",
                UserAgent = "Test Agent",
                ConsentVersion = "1.0"
            }
        };

        _mockCustomerRepository.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync(customer);
        _mockOrderRepository.Setup(x => x.GetByCustomerIdAsync(customer.CustomerId))
            .ReturnsAsync(orders);
        _mockConsentRepository.Setup(x => x.GetByCustomerIdAsync(customer.CustomerId))
            .ReturnsAsync(consents);

        // Act
        var result = await _service.ExportDataAsync(email);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(email);
        result.FirstName.Should().Be("Test");
        result.LastName.Should().Be("User");
        result.Orders.Should().HaveCount(1);
        result.Consents.Should().HaveCount(1);
        result.ExportedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ExportDataAsync_CustomerNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var email = "nonexistent@example.com";
        _mockCustomerRepository.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync((Customer?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ExportDataAsync(email));

        exception.Message.Should().Contain("not found");
        exception.Message.Should().Contain(email);
    }

    [Fact]
    public async Task ExportDataAsync_DeletedCustomer_ThrowsInvalidOperationException()
    {
        // Arrange
        var email = "deleted-guid@anonymized.local";
        var customer = new Customer
        {
            CustomerId = 1,
            Email = email,
            FirstName = "REDACTED",
            LastName = "REDACTED",
            PhoneNumber = string.Empty,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            LastUpdatedAt = DateTime.UtcNow,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow.AddDays(-5),
            DeletedReason = "Customer requested deletion"
        };

        _mockCustomerRepository.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync(customer);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ExportDataAsync(email));

        exception.Message.Should().Contain("deleted");
        exception.Message.Should().Contain(email);
    }

    #endregion

    #region AnonymizeAsync Tests

    [Fact]
    public async Task AnonymizeAsync_ValidEmail_AnonymizesCustomerData()
    {
        // Arrange
        var email = "test@example.com";
        var reason = "Customer requested account deletion";
        var customer = new Customer
        {
            CustomerId = 1,
            Email = email,
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "555-1234",
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            LastUpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        _mockCustomerRepository.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync(customer);
        _mockCustomerRepository.Setup(x => x.UpdateAsync(It.IsAny<Customer>(), It.IsAny<bool>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.AnonymizeAsync(email, reason);

        // Assert
        result.Should().BeTrue();
        customer.FirstName.Should().Be("REDACTED");
        customer.LastName.Should().Be("REDACTED");
        customer.Email.Should().StartWith("deleted-");
        customer.Email.Should().EndWith("@anonymized.local");
        customer.PhoneNumber.Should().BeEmpty();
        customer.IsDeleted.Should().BeTrue();
        customer.DeletedAt.Should().NotBeNull();
        customer.DeletedReason.Should().Be(reason);
        customer.LastUpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task AnonymizeAsync_CustomerNotFound_ReturnsFalse()
    {
        // Arrange
        var email = "nonexistent@example.com";
        _mockCustomerRepository.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync((Customer?)null);

        // Act
        var result = await _service.AnonymizeAsync(email, "Test");

        // Assert
        result.Should().BeFalse();
        _mockCustomerRepository.Verify(x => x.UpdateAsync(It.IsAny<Customer>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task AnonymizeAsync_AlreadyDeleted_ThrowsInvalidOperationException()
    {
        // Arrange
        var email = "deleted@example.com";
        var customer = new Customer
        {
            CustomerId = 1,
            Email = email,
            FirstName = "REDACTED",
            LastName = "REDACTED",
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow.AddDays(-5),
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        _mockCustomerRepository.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync(customer);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AnonymizeAsync(email, "Test"));

        exception.Message.Should().Contain("deleted");
        exception.Message.Should().Contain(email);
    }

    #endregion

    #region GrantConsentAsync Tests

    [Fact]
    public async Task GrantConsentAsync_ValidRequest_CreatesConsent()
    {
        // Arrange
        var email = "test@example.com";
        var consentType = ApplicationConstants.Consent.Types.Marketing;
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";
        var consentVersion = "1.0";

        var customer = new Customer
        {
            CustomerId = 1,
            Email = email,
            FirstName = "Test",
            LastName = "User",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        _mockCustomerRepository.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync(customer);
        _mockConsentRepository.Setup(x => x.GetByCustomerIdAndTypeAsync(customer.CustomerId, consentType))
            .ReturnsAsync((CustomerConsent?)null);
        _mockConsentRepository.Setup(x => x.CreateAsync(It.IsAny<CustomerConsent>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.GrantConsentAsync(email, consentType, ipAddress, userAgent, consentVersion);

        // Assert
        result.Should().BeTrue();
        _mockConsentRepository.Verify(x => x.CreateAsync(It.Is<CustomerConsent>(c =>
            c.CustomerId == customer.CustomerId &&
            c.ConsentType == consentType &&
            c.IsGranted == true &&
            c.IpAddress == ipAddress &&
            c.UserAgent == userAgent &&
            c.ConsentVersion == consentVersion
        )), Times.Once);
    }

    [Fact]
    public async Task GrantConsentAsync_CustomerNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var email = "nonexistent@example.com";
        _mockCustomerRepository.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync((Customer?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GrantConsentAsync(email, "Marketing", "127.0.0.1", "Test", "1.0"));

        exception.Message.Should().Contain("not found");
        exception.Message.Should().Contain(email);
    }

    [Fact]
    public async Task GrantConsentAsync_ConsentAlreadyGranted_ThrowsInvalidOperationException()
    {
        // Arrange
        var email = "test@example.com";
        var consentType = ApplicationConstants.Consent.Types.Marketing;
        var customer = new Customer
        {
            CustomerId = 1,
            Email = email,
            FirstName = "Test",
            LastName = "User",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        var existingConsent = new CustomerConsent
        {
            ConsentId = 1,
            CustomerId = customer.CustomerId,
            ConsentType = consentType,
            IsGranted = true,
            ConsentDate = DateTime.UtcNow.AddDays(-10),
            IpAddress = "127.0.0.1",
            UserAgent = "Old Agent",
            ConsentVersion = "1.0"
        };

        _mockCustomerRepository.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync(customer);
        _mockConsentRepository.Setup(x => x.GetByCustomerIdAndTypeAsync(customer.CustomerId, consentType))
            .ReturnsAsync(existingConsent);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GrantConsentAsync(email, consentType, "127.0.0.1", "New Agent", "2.0"));

        exception.Message.Should().Contain("already been granted");
        exception.Message.Should().Contain(email);
        _mockConsentRepository.Verify(x => x.CreateAsync(It.IsAny<CustomerConsent>()), Times.Never);
    }

    #endregion

    #region RevokeConsentAsync Tests

    [Fact]
    public async Task RevokeConsentAsync_ValidConsent_RevokesConsent()
    {
        // Arrange
        var email = "test@example.com";
        var consentType = ApplicationConstants.Consent.Types.Marketing;
        var customer = new Customer
        {
            CustomerId = 1,
            Email = email,
            FirstName = "Test",
            LastName = "User",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        _mockCustomerRepository.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync(customer);
        _mockConsentRepository.Setup(x => x.RevokeConsentAsync(customer.CustomerId, consentType))
            .ReturnsAsync(true);

        // Act
        var result = await _service.RevokeConsentAsync(email, consentType, "127.0.0.1", "Test Agent");

        // Assert
        result.Should().BeTrue();
        _mockConsentRepository.Verify(x => x.RevokeConsentAsync(customer.CustomerId, consentType), Times.Once);
    }

    [Fact]
    public async Task RevokeConsentAsync_CustomerNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var email = "nonexistent@example.com";
        _mockCustomerRepository.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync((Customer?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RevokeConsentAsync(email, "Marketing", "127.0.0.1", "Test"));

        exception.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task RevokeConsentAsync_ConsentNotFound_ReturnsFalse()
    {
        // Arrange
        var email = "test@example.com";
        var consentType = "NonExistentType";
        var customer = new Customer
        {
            CustomerId = 1,
            Email = email,
            FirstName = "Test",
            LastName = "User",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        _mockCustomerRepository.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync(customer);
        _mockConsentRepository.Setup(x => x.RevokeConsentAsync(customer.CustomerId, consentType))
            .ReturnsAsync(false);

        // Act
        var result = await _service.RevokeConsentAsync(email, consentType, "127.0.0.1", "Test");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetConsentsAsync Tests

    [Fact]
    public async Task GetConsentsAsync_ValidEmail_ReturnsConsents()
    {
        // Arrange
        var email = "test@example.com";
        var customer = new Customer
        {
            CustomerId = 1,
            Email = email,
            FirstName = "Test",
            LastName = "User",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        var consents = new List<CustomerConsent>
        {
            new CustomerConsent
            {
                ConsentId = 1,
                CustomerId = 1,
                ConsentType = ApplicationConstants.Consent.Types.Marketing,
                IsGranted = true,
                ConsentDate = DateTime.UtcNow.AddDays(-10),
                IpAddress = "127.0.0.1",
                UserAgent = "Test",
                ConsentVersion = "1.0"
            },
            new CustomerConsent
            {
                ConsentId = 2,
                CustomerId = 1,
                ConsentType = ApplicationConstants.Consent.Types.DataProcessing,
                IsGranted = false,
                ConsentDate = DateTime.UtcNow.AddDays(-5),
                RevokedDate = DateTime.UtcNow.AddDays(-2),
                IpAddress = "127.0.0.1",
                UserAgent = "Test",
                ConsentVersion = "1.0"
            }
        };

        _mockCustomerRepository.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync(customer);
        _mockConsentRepository.Setup(x => x.GetByCustomerIdAsync(customer.CustomerId))
            .ReturnsAsync(consents);

        // Act
        var result = await _service.GetConsentsAsync(email);

        // Assert
        var consentList = result.ToList();
        consentList.Should().HaveCount(2);
        consentList[0].ConsentType.Should().Be(ApplicationConstants.Consent.Types.Marketing);
        consentList[0].IsGranted.Should().BeTrue();
        consentList[1].ConsentType.Should().Be(ApplicationConstants.Consent.Types.DataProcessing);
        consentList[1].IsGranted.Should().BeFalse();
        consentList[1].RevokedDate.Should().NotBeNull();
    }

    [Fact]
    public async Task GetConsentsAsync_CustomerNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var email = "nonexistent@example.com";
        _mockCustomerRepository.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync((Customer?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetConsentsAsync(email));

        exception.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task GetConsentsAsync_NoConsents_ReturnsEmptyList()
    {
        // Arrange
        var email = "test@example.com";
        var customer = new Customer
        {
            CustomerId = 1,
            Email = email,
            FirstName = "Test",
            LastName = "User",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        _mockCustomerRepository.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync(customer);
        _mockConsentRepository.Setup(x => x.GetByCustomerIdAsync(customer.CustomerId))
            .ReturnsAsync(new List<CustomerConsent>());

        // Act
        var result = await _service.GetConsentsAsync(email);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion
}
