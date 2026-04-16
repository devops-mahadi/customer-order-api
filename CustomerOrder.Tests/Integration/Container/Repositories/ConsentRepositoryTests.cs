using CustomerOrder.Domain.Constants;
using CustomerOrder.Domain.Entities;
using CustomerOrder.Infrastructure.Repositories;
using FluentAssertions;

namespace CustomerOrder.Tests.Integration.Container.Repositories;

[Collection("Database")]
public class ConsentRepositoryTests(DatabaseFixture fixture)
{
    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidConsent_ReturnsTrue()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var customerRepo = new CustomerRepository(context);
        var consentRepo = new ConsentRepository(context);

        var customer = new Customer
        {
            Email = $"consent-{Guid.NewGuid()}@example.com",
            FirstName = "Consent",
            LastName = "Test",
            PhoneNumber = "555-0100",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };
        await customerRepo.CreateAsync(customer);

        var consent = new CustomerConsent
        {
            CustomerId = customer.CustomerId,
            ConsentType = ApplicationConstants.Consent.Types.Marketing,
            IsGranted = true,
            ConsentDate = DateTime.UtcNow,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0",
            ConsentVersion = "1.0"
        };

        // Act
        var result = await consentRepo.CreateAsync(consent);

        // Assert
        result.Should().BeTrue();
        consent.ConsentId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateAsync_MultipleConsentTypes_CreatesAll()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var customerRepo = new CustomerRepository(context);
        var consentRepo = new ConsentRepository(context);

        var customer = new Customer
        {
            Email = $"multi-consent-{Guid.NewGuid()}@example.com",
            FirstName = "Multi",
            LastName = "Consent",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };
        await customerRepo.CreateAsync(customer);

        var marketingConsent = new CustomerConsent
        {
            CustomerId = customer.CustomerId,
            ConsentType = ApplicationConstants.Consent.Types.Marketing,
            IsGranted = true,
            ConsentDate = DateTime.UtcNow,
            IpAddress = "127.0.0.1",
            UserAgent = "Test",
            ConsentVersion = "1.0"
        };

        var dataProcessingConsent = new CustomerConsent
        {
            CustomerId = customer.CustomerId,
            ConsentType = ApplicationConstants.Consent.Types.DataProcessing,
            IsGranted = true,
            ConsentDate = DateTime.UtcNow,
            IpAddress = "127.0.0.1",
            UserAgent = "Test",
            ConsentVersion = "1.0"
        };

        // Act
        await consentRepo.CreateAsync(marketingConsent);
        await consentRepo.CreateAsync(dataProcessingConsent);

        // Assert
        var allConsents = await consentRepo.GetByCustomerIdAsync(customer.CustomerId);
        allConsents.Should().HaveCountGreaterThanOrEqualTo(2);
        allConsents.Should().Contain(c => c.ConsentType == ApplicationConstants.Consent.Types.Marketing);
        allConsents.Should().Contain(c => c.ConsentType == ApplicationConstants.Consent.Types.DataProcessing);
    }

    #endregion

    #region GetByCustomerIdAsync Tests

    [Fact]
    public async Task GetByCustomerIdAsync_ExistingCustomer_ReturnsConsents()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var customerRepo = new CustomerRepository(context);
        var consentRepo = new ConsentRepository(context);

        var customer = new Customer
        {
            Email = $"getconsents-{Guid.NewGuid()}@example.com",
            FirstName = "Get",
            LastName = "Consents",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };
        await customerRepo.CreateAsync(customer);

        var consent = new CustomerConsent
        {
            CustomerId = customer.CustomerId,
            ConsentType = ApplicationConstants.Consent.Types.Profiling,
            IsGranted = true,
            ConsentDate = DateTime.UtcNow,
            IpAddress = "10.0.0.1",
            UserAgent = "Test Agent",
            ConsentVersion = "1.0"
        };
        await consentRepo.CreateAsync(consent);

        // Act
        var result = await consentRepo.GetByCustomerIdAsync(customer.CustomerId);

        // Assert
        var consents = result.ToList();
        consents.Should().HaveCountGreaterThanOrEqualTo(1);
        consents.Should().Contain(c => c.ConsentType == ApplicationConstants.Consent.Types.Profiling);
    }

    [Fact]
    public async Task GetByCustomerIdAsync_NonExistentCustomer_ReturnsEmpty()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var consentRepo = new ConsentRepository(context);

        // Act
        var result = await consentRepo.GetByCustomerIdAsync(999999);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByCustomerIdAsync_OrdersByConsentDateDescending()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var customerRepo = new CustomerRepository(context);
        var consentRepo = new ConsentRepository(context);

        var customer = new Customer
        {
            Email = $"order-test-{Guid.NewGuid()}@example.com",
            FirstName = "Order",
            LastName = "Test",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };
        await customerRepo.CreateAsync(customer);

        var oldConsent = new CustomerConsent
        {
            CustomerId = customer.CustomerId,
            ConsentType = ApplicationConstants.Consent.Types.Marketing,
            IsGranted = true,
            ConsentDate = DateTime.UtcNow.AddDays(-5),
            IpAddress = "127.0.0.1",
            UserAgent = "Test",
            ConsentVersion = "1.0"
        };

        var recentConsent = new CustomerConsent
        {
            CustomerId = customer.CustomerId,
            ConsentType = ApplicationConstants.Consent.Types.DataProcessing,
            IsGranted = true,
            ConsentDate = DateTime.UtcNow,
            IpAddress = "127.0.0.1",
            UserAgent = "Test",
            ConsentVersion = "1.0"
        };

        await consentRepo.CreateAsync(oldConsent);
        await consentRepo.CreateAsync(recentConsent);

        // Act
        var result = await consentRepo.GetByCustomerIdAsync(customer.CustomerId);

        // Assert
        var consents = result.ToList();
        consents.Should().HaveCountGreaterThanOrEqualTo(2);
        consents.Should().BeInDescendingOrder(c => c.ConsentDate);
    }

    #endregion

    #region GetByCustomerIdAndTypeAsync Tests

    [Fact]
    public async Task GetByCustomerIdAndTypeAsync_ExistingConsent_ReturnsConsent()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var customerRepo = new CustomerRepository(context);
        var consentRepo = new ConsentRepository(context);

        var customer = new Customer
        {
            Email = $"gettype-{Guid.NewGuid()}@example.com",
            FirstName = "GetType",
            LastName = "Test",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };
        await customerRepo.CreateAsync(customer);

        var consent = new CustomerConsent
        {
            CustomerId = customer.CustomerId,
            ConsentType = ApplicationConstants.Consent.Types.ThirdPartySharing,
            IsGranted = true,
            ConsentDate = DateTime.UtcNow,
            IpAddress = "192.168.100.1",
            UserAgent = "Consent Form v1.0",
            ConsentVersion = "1.0"
        };
        await consentRepo.CreateAsync(consent);

        // Act
        var result = await consentRepo.GetByCustomerIdAndTypeAsync(
            customer.CustomerId,
            ApplicationConstants.Consent.Types.ThirdPartySharing);

        // Assert
        result.Should().NotBeNull();
        result!.ConsentType.Should().Be(ApplicationConstants.Consent.Types.ThirdPartySharing);
        result.CustomerId.Should().Be(customer.CustomerId);
    }

    [Fact]
    public async Task GetByCustomerIdAndTypeAsync_NonExistentConsent_ReturnsNull()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var customerRepo = new CustomerRepository(context);
        var consentRepo = new ConsentRepository(context);

        var customer = new Customer
        {
            Email = $"noconsenttype-{Guid.NewGuid()}@example.com",
            FirstName = "NoConsentType",
            LastName = "Test",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };
        await customerRepo.CreateAsync(customer);

        // Act
        var result = await consentRepo.GetByCustomerIdAndTypeAsync(
            customer.CustomerId,
            "NonExistentType");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByCustomerIdAndTypeAsync_MultipleVersions_ReturnsMostRecent()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var customerRepo = new CustomerRepository(context);
        var consentRepo = new ConsentRepository(context);

        var customer = new Customer
        {
            Email = $"versions-{Guid.NewGuid()}@example.com",
            FirstName = "Versions",
            LastName = "Test",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };
        await customerRepo.CreateAsync(customer);

        var oldConsent = new CustomerConsent
        {
            CustomerId = customer.CustomerId,
            ConsentType = ApplicationConstants.Consent.Types.Marketing,
            IsGranted = true,
            ConsentDate = DateTime.UtcNow.AddDays(-10),
            IpAddress = "127.0.0.1",
            UserAgent = "Old Version",
            ConsentVersion = "1.0"
        };

        var newConsent = new CustomerConsent
        {
            CustomerId = customer.CustomerId,
            ConsentType = ApplicationConstants.Consent.Types.Marketing,
            IsGranted = true,
            ConsentDate = DateTime.UtcNow,
            IpAddress = "127.0.0.1",
            UserAgent = "New Version",
            ConsentVersion = "2.0"
        };

        await consentRepo.CreateAsync(oldConsent);
        await consentRepo.CreateAsync(newConsent);

        // Act
        var result = await consentRepo.GetByCustomerIdAndTypeAsync(
            customer.CustomerId,
            ApplicationConstants.Consent.Types.Marketing);

        // Assert
        result.Should().NotBeNull();
        result!.ConsentVersion.Should().Be("2.0");
        result.UserAgent.Should().Be("New Version");
    }

    #endregion

    #region RevokeConsentAsync Tests

    [Fact]
    public async Task RevokeConsentAsync_ExistingConsent_RevokesAndReturnsTrue()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var customerRepo = new CustomerRepository(context);
        var consentRepo = new ConsentRepository(context);

        var customer = new Customer
        {
            Email = $"revoke-{Guid.NewGuid()}@example.com",
            FirstName = "Revoke",
            LastName = "Test",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };
        await customerRepo.CreateAsync(customer);

        var consent = new CustomerConsent
        {
            CustomerId = customer.CustomerId,
            ConsentType = ApplicationConstants.Consent.Types.Marketing,
            IsGranted = true,
            ConsentDate = DateTime.UtcNow,
            IpAddress = "127.0.0.1",
            UserAgent = "Test",
            ConsentVersion = "1.0"
        };
        await consentRepo.CreateAsync(consent);

        // Act
        var result = await consentRepo.RevokeConsentAsync(
            customer.CustomerId,
            ApplicationConstants.Consent.Types.Marketing);

        // Assert
        result.Should().BeTrue();

        // Verify consent is revoked
        await using var context2 = fixture.CreateDbContext();
        var consentRepo2 = new ConsentRepository(context2);
        var revokedConsent = await consentRepo2.GetByCustomerIdAndTypeAsync(
            customer.CustomerId,
            ApplicationConstants.Consent.Types.Marketing);

        revokedConsent.Should().NotBeNull();
        revokedConsent!.IsGranted.Should().BeFalse();
        revokedConsent.RevokedDate.Should().NotBeNull();
        revokedConsent.RevokedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RevokeConsentAsync_NonExistentConsent_ReturnsFalse()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var customerRepo = new CustomerRepository(context);
        var consentRepo = new ConsentRepository(context);

        var customer = new Customer
        {
            Email = $"norevoke-{Guid.NewGuid()}@example.com",
            FirstName = "NoRevoke",
            LastName = "Test",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };
        await customerRepo.CreateAsync(customer);

        // Act
        var result = await consentRepo.RevokeConsentAsync(
            customer.CustomerId,
            "NonExistentConsentType");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeConsentAsync_AlreadyRevokedConsent_ReturnsFalse()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var customerRepo = new CustomerRepository(context);
        var consentRepo = new ConsentRepository(context);

        var customer = new Customer
        {
            Email = $"alreadyrevoked-{Guid.NewGuid()}@example.com",
            FirstName = "AlreadyRevoked",
            LastName = "Test",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };
        await customerRepo.CreateAsync(customer);

        var consent = new CustomerConsent
        {
            CustomerId = customer.CustomerId,
            ConsentType = ApplicationConstants.Consent.Types.DataProcessing,
            IsGranted = false, // Already revoked
            ConsentDate = DateTime.UtcNow.AddDays(-5),
            RevokedDate = DateTime.UtcNow.AddDays(-1),
            IpAddress = "127.0.0.1",
            UserAgent = "Test",
            ConsentVersion = "1.0"
        };
        await consentRepo.CreateAsync(consent);

        // Act
        var result = await consentRepo.RevokeConsentAsync(
            customer.CustomerId,
            ApplicationConstants.Consent.Types.DataProcessing);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeConsentAsync_NonExistentCustomer_ReturnsFalse()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var consentRepo = new ConsentRepository(context);

        // Act
        var result = await consentRepo.RevokeConsentAsync(
            999999,
            ApplicationConstants.Consent.Types.Marketing);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingConsent_ReturnsTrue()
    {
        // Arrange
        await using var context1 = fixture.CreateDbContext();
        var customerRepo1 = new CustomerRepository(context1);
        var consentRepo1 = new ConsentRepository(context1);

        var customer = new Customer
        {
            Email = $"update-{Guid.NewGuid()}@example.com",
            FirstName = "Update",
            LastName = "Test",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };
        await customerRepo1.CreateAsync(customer);

        var consent = new CustomerConsent
        {
            CustomerId = customer.CustomerId,
            ConsentType = ApplicationConstants.Consent.Types.Profiling,
            IsGranted = true,
            ConsentDate = DateTime.UtcNow,
            IpAddress = "127.0.0.1",
            UserAgent = "Original Agent",
            ConsentVersion = "1.0"
        };
        await consentRepo1.CreateAsync(consent);

        // Act
        await using var context2 = fixture.CreateDbContext();
        var consentRepo2 = new ConsentRepository(context2);
        var consentToUpdate = await consentRepo2.GetByCustomerIdAndTypeAsync(
            customer.CustomerId,
            ApplicationConstants.Consent.Types.Profiling);

        consentToUpdate!.UserAgent = "Updated Agent";
        consentToUpdate.ConsentVersion = "2.0";

        var result = await consentRepo2.UpdateAsync(consentToUpdate);

        // Assert
        result.Should().BeTrue();

        // Verify update
        await using var context3 = fixture.CreateDbContext();
        var consentRepo3 = new ConsentRepository(context3);
        var updated = await consentRepo3.GetByCustomerIdAndTypeAsync(
            customer.CustomerId,
            ApplicationConstants.Consent.Types.Profiling);

        updated.Should().NotBeNull();
        updated!.UserAgent.Should().Be("Updated Agent");
        updated.ConsentVersion.Should().Be("2.0");
    }

    #endregion

    #region Complex Scenarios

    [Fact]
    public async Task ComplexScenario_GrantRevokeRegrant_WorksCorrectly()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var customerRepo = new CustomerRepository(context);
        var consentRepo = new ConsentRepository(context);

        var customer = new Customer
        {
            Email = $"regrant-{Guid.NewGuid()}@example.com",
            FirstName = "Regrant",
            LastName = "Test",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };
        await customerRepo.CreateAsync(customer);

        // Step 1: Grant consent
        var consent = new CustomerConsent
        {
            CustomerId = customer.CustomerId,
            ConsentType = ApplicationConstants.Consent.Types.Marketing,
            IsGranted = true,
            ConsentDate = DateTime.UtcNow.AddDays(-10),
            IpAddress = "127.0.0.1",
            UserAgent = "Initial Grant",
            ConsentVersion = "1.0"
        };
        await consentRepo.CreateAsync(consent);

        // Step 2: Revoke consent
        var revokeResult = await consentRepo.RevokeConsentAsync(
            customer.CustomerId,
            ApplicationConstants.Consent.Types.Marketing);
        revokeResult.Should().BeTrue();

        // Step 3: Grant again (new consent record)
        var newConsent = new CustomerConsent
        {
            CustomerId = customer.CustomerId,
            ConsentType = ApplicationConstants.Consent.Types.Marketing,
            IsGranted = true,
            ConsentDate = DateTime.UtcNow,
            IpAddress = "127.0.0.1",
            UserAgent = "Re-grant",
            ConsentVersion = "1.0"
        };
        await consentRepo.CreateAsync(newConsent);

        // Assert: Should have 2 consent records for same type
        var allConsents = await consentRepo.GetByCustomerIdAsync(customer.CustomerId);
        var marketingConsents = allConsents.Where(c => c.ConsentType == ApplicationConstants.Consent.Types.Marketing).ToList();

        marketingConsents.Should().HaveCountGreaterThanOrEqualTo(2);

        // Most recent should be granted
        var mostRecent = await consentRepo.GetByCustomerIdAndTypeAsync(
            customer.CustomerId,
            ApplicationConstants.Consent.Types.Marketing);
        mostRecent.Should().NotBeNull();
        mostRecent!.IsGranted.Should().BeTrue();
        mostRecent.UserAgent.Should().Be("Re-grant");
    }

    [Fact]
    public async Task ComplexScenario_MultipleConsentTypesManagement_WorksCorrectly()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var customerRepo = new CustomerRepository(context);
        var consentRepo = new ConsentRepository(context);

        var customer = new Customer
        {
            Email = $"multitypes-{Guid.NewGuid()}@example.com",
            FirstName = "MultiTypes",
            LastName = "Test",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };
        await customerRepo.CreateAsync(customer);

        // Grant all consent types
        var consentTypes = new[]
        {
            ApplicationConstants.Consent.Types.Marketing,
            ApplicationConstants.Consent.Types.DataProcessing,
            ApplicationConstants.Consent.Types.Profiling,
            ApplicationConstants.Consent.Types.ThirdPartySharing
        };

        foreach (var consentType in consentTypes)
        {
            var consent = new CustomerConsent
            {
                CustomerId = customer.CustomerId,
                ConsentType = consentType,
                IsGranted = true,
                ConsentDate = DateTime.UtcNow,
                IpAddress = "127.0.0.1",
                UserAgent = "Test",
                ConsentVersion = "1.0"
            };
            await consentRepo.CreateAsync(consent);
        }

        // Act: Revoke Marketing and Profiling
        await consentRepo.RevokeConsentAsync(customer.CustomerId, ApplicationConstants.Consent.Types.Marketing);
        await consentRepo.RevokeConsentAsync(customer.CustomerId, ApplicationConstants.Consent.Types.Profiling);

        // Assert: Check each consent status
        await using var context2 = fixture.CreateDbContext();
        var consentRepo2 = new ConsentRepository(context2);

        var marketing = await consentRepo2.GetByCustomerIdAndTypeAsync(customer.CustomerId, ApplicationConstants.Consent.Types.Marketing);
        var dataProcessing = await consentRepo2.GetByCustomerIdAndTypeAsync(customer.CustomerId, ApplicationConstants.Consent.Types.DataProcessing);
        var profiling = await consentRepo2.GetByCustomerIdAndTypeAsync(customer.CustomerId, ApplicationConstants.Consent.Types.Profiling);
        var thirdParty = await consentRepo2.GetByCustomerIdAndTypeAsync(customer.CustomerId, ApplicationConstants.Consent.Types.ThirdPartySharing);

        marketing!.IsGranted.Should().BeFalse();
        dataProcessing!.IsGranted.Should().BeTrue();
        profiling!.IsGranted.Should().BeFalse();
        thirdParty!.IsGranted.Should().BeTrue();
    }

    #endregion
}
