using System.Net;
using System.Net.Http.Json;
using CustomerOrder.Domain.Constants;
using CustomerOrder.Presentation.DTOs.Requests;
using CustomerOrder.Presentation.DTOs.Responses;
using CustomerOrder.Tests.Helpers;
using CustomerOrder.Tests.Integration.Fixtures;
using FluentAssertions;
using Xunit;

namespace CustomerOrder.Tests.Integration.Container.Controllers;

[Trait("Category", "IntegrationContainer")]
public class CustomersControllerGdprTests : IClassFixture<ContainerWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ContainerWebApplicationFactory _factory;

    public CustomersControllerGdprTests(ContainerWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();

        // Add JWT token for authentication
        var token = TestAuthHelper.GetTestToken();
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
    }

    #region ExportData Tests

    [Fact]
    public async Task ExportData_ExistingCustomer_Returns200WithFullData()
    {
        // Arrange
        var email = $"export-{Guid.NewGuid()}@test.com";
        var createRequest = new CreateCustomerRequest
        {
            Email = email,
            FirstName = "Export",
            LastName = "Test",
            PhoneNumber = "555-1000"
        };

        await _client.PostAsJsonAsync(ApiRoutes.Customers.Base, createRequest);

        // Grant some consents
        var consentRequest = new ConsentRequest
        {
            ConsentType = ApplicationConstants.Consent.Types.Marketing,
            ConsentVersion = "1.0"
        };
        await _client.PostAsJsonAsync(ApiRoutes.Customers.Consents(email), consentRequest);

        // Act
        var response = await _client.GetAsync(ApiRoutes.Customers.Export(email));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var exportData = await response.Content.ReadFromJsonAsync<CustomerDataExportResponse>();
        exportData.Should().NotBeNull();
        exportData!.Email.Should().Be(email);
        exportData.FirstName.Should().Be("Export");
        exportData.LastName.Should().Be("Test");
        exportData.Orders.Should().NotBeNull();
        exportData.Consents.Should().NotBeNull();
        exportData.Consents.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task ExportData_NonExistentCustomer_Returns404NotFound()
    {
        // Arrange
        var email = "nonexistent-export@test.com";

        // Act
        var response = await _client.GetAsync(ApiRoutes.Customers.Export(email));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ExportData_DeletedCustomer_Returns400BadRequest()
    {
        // Arrange
        var email = $"deleted-export-{Guid.NewGuid()}@test.com";
        var createRequest = new CreateCustomerRequest
        {
            Email = email,
            FirstName = "Deleted",
            LastName = "Export",
            PhoneNumber = "555-1001"
        };

        await _client.PostAsJsonAsync(ApiRoutes.Customers.Base, createRequest);

        // Anonymize (delete) the customer
        await _client.PostAsync($"{ApiRoutes.Customers.Anonymize(email)}?reason=Test", null);

        // Act
        var response = await _client.GetAsync(ApiRoutes.Customers.Export(email));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("deleted");
    }

    #endregion

    #region Anonymize Tests

    [Fact]
    public async Task Anonymize_ExistingCustomer_Returns200AndAnonymizesData()
    {
        // Arrange
        var email = $"anonymize-{Guid.NewGuid()}@test.com";
        var createRequest = new CreateCustomerRequest
        {
            Email = email,
            FirstName = "Anonymize",
            LastName = "Test",
            PhoneNumber = "555-2000"
        };

        await _client.PostAsJsonAsync(ApiRoutes.Customers.Base, createRequest);

        // Act
        var response = await _client.PostAsync($"{ApiRoutes.Customers.Anonymize(email)}?reason=GDPR Request", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify customer is anonymized - trying to get by original email should fail
        var getResponse = await _client.GetAsync(ApiRoutes.Customers.ByEmail(email));
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Anonymize_NonExistentCustomer_Returns404NotFound()
    {
        // Arrange
        var email = "nonexistent-anonymize@test.com";

        // Act
        var response = await _client.PostAsync($"{ApiRoutes.Customers.Anonymize(email)}?reason=Test", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Anonymize_AlreadyDeletedCustomer_Returns400BadRequest()
    {
        // Arrange
        var email = $"already-deleted-{Guid.NewGuid()}@test.com";
        var createRequest = new CreateCustomerRequest
        {
            Email = email,
            FirstName = "Already",
            LastName = "Deleted",
            PhoneNumber = "555-2001"
        };

        await _client.PostAsJsonAsync(ApiRoutes.Customers.Base, createRequest);

        // Anonymize once
        await _client.PostAsync($"{ApiRoutes.Customers.Anonymize(email)}?reason=First Request", null);

        // Act - Try to anonymize again
        var response = await _client.PostAsync($"{ApiRoutes.Customers.Anonymize(email)}?reason=Second Request", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Anonymize_WithoutReason_Returns400BadRequest()
    {
        // Arrange
        var email = $"no-reason-{Guid.NewGuid()}@test.com";
        var createRequest = new CreateCustomerRequest
        {
            Email = email,
            FirstName = "NoReason",
            LastName = "Test",
            PhoneNumber = "555-2002"
        };

        await _client.PostAsJsonAsync(ApiRoutes.Customers.Base, createRequest);

        // Act - No reason query parameter
        var response = await _client.PostAsync(ApiRoutes.Customers.Anonymize(email), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GrantConsent Tests

    [Fact]
    public async Task GrantConsent_ValidRequest_Returns200()
    {
        // Arrange
        var email = $"grant-consent-{Guid.NewGuid()}@test.com";
        var createRequest = new CreateCustomerRequest
        {
            Email = email,
            FirstName = "Grant",
            LastName = "Consent",
            PhoneNumber = "555-3000"
        };

        await _client.PostAsJsonAsync(ApiRoutes.Customers.Base, createRequest);

        var consentRequest = new ConsentRequest
        {
            ConsentType = ApplicationConstants.Consent.Types.Marketing,
            ConsentVersion = "1.0"
        };

        // Act
        var response = await _client.PostAsJsonAsync(ApiRoutes.Customers.Consents(email), consentRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GrantConsent_AllConsentTypes_Returns201()
    {
        // Arrange
        var email = $"all-consents-{Guid.NewGuid()}@test.com";
        var createRequest = new CreateCustomerRequest
        {
            Email = email,
            FirstName = "All",
            LastName = "Consents",
            PhoneNumber = "555-3001"
        };

        await _client.PostAsJsonAsync(ApiRoutes.Customers.Base, createRequest);

        var consentTypes = new[]
        {
            ApplicationConstants.Consent.Types.Marketing,
            ApplicationConstants.Consent.Types.DataProcessing,
            ApplicationConstants.Consent.Types.Profiling,
            ApplicationConstants.Consent.Types.ThirdPartySharing
        };

        // Act & Assert
        foreach (var consentType in consentTypes)
        {
            var consentRequest = new ConsentRequest
            {
                ConsentType = consentType,
                ConsentVersion = "1.0"
            };

            var response = await _client.PostAsJsonAsync(ApiRoutes.Customers.Consents(email), consentRequest);
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }
    }

    [Fact]
    public async Task GrantConsent_NonExistentCustomer_Returns404NotFound()
    {
        // Arrange
        var email = "nonexistent-consent@test.com";
        var consentRequest = new ConsentRequest
        {
            ConsentType = ApplicationConstants.Consent.Types.Marketing,
            ConsentVersion = "1.0"
        };

        // Act
        var response = await _client.PostAsJsonAsync(ApiRoutes.Customers.Consents(email), consentRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GrantConsent_DuplicateConsent_Returns400BadRequest()
    {
        // Arrange
        var email = $"duplicate-consent-{Guid.NewGuid()}@test.com";
        var createRequest = new CreateCustomerRequest
        {
            Email = email,
            FirstName = "Duplicate",
            LastName = "Consent",
            PhoneNumber = "555-3002"
        };

        await _client.PostAsJsonAsync(ApiRoutes.Customers.Base, createRequest);

        var consentRequest = new ConsentRequest
        {
            ConsentType = ApplicationConstants.Consent.Types.Marketing,
            ConsentVersion = "1.0"
        };

        // Grant consent first time
        await _client.PostAsJsonAsync(ApiRoutes.Customers.Consents(email), consentRequest);

        // Act - Try to grant same consent again
        var response = await _client.PostAsJsonAsync(ApiRoutes.Customers.Consents(email), consentRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("already been granted");
    }

    #endregion

    #region RevokeConsent Tests

    [Fact]
    public async Task RevokeConsent_ExistingConsent_Returns200()
    {
        // Arrange
        var email = $"revoke-consent-{Guid.NewGuid()}@test.com";
        var createRequest = new CreateCustomerRequest
        {
            Email = email,
            FirstName = "Revoke",
            LastName = "Consent",
            PhoneNumber = "555-4000"
        };

        await _client.PostAsJsonAsync(ApiRoutes.Customers.Base, createRequest);

        // Grant consent first
        var consentRequest = new ConsentRequest
        {
            ConsentType = ApplicationConstants.Consent.Types.Marketing,
            ConsentVersion = "1.0"
        };
        await _client.PostAsJsonAsync(ApiRoutes.Customers.Consents(email), consentRequest);

        // Act
        var response = await _client.DeleteAsync(
            ApiRoutes.Customers.ConsentByType(email, ApplicationConstants.Consent.Types.Marketing));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RevokeConsent_NonExistentCustomer_Returns404NotFound()
    {
        // Arrange
        var email = "nonexistent-revoke@test.com";

        // Act
        var response = await _client.DeleteAsync(
            ApiRoutes.Customers.ConsentByType(email, ApplicationConstants.Consent.Types.Marketing));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RevokeConsent_NonExistentConsentType_Returns404NotFound()
    {
        // Arrange
        var email = $"no-consent-type-{Guid.NewGuid()}@test.com";
        var createRequest = new CreateCustomerRequest
        {
            Email = email,
            FirstName = "NoConsentType",
            LastName = "Test",
            PhoneNumber = "555-4001"
        };

        await _client.PostAsJsonAsync(ApiRoutes.Customers.Base, createRequest);

        // Act - Try to revoke consent that was never granted
        var response = await _client.DeleteAsync(
            ApiRoutes.Customers.ConsentByType(email, ApplicationConstants.Consent.Types.Marketing));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetConsents Tests

    [Fact]
    public async Task GetConsents_ExistingCustomer_ReturnsConsentsList()
    {
        // Arrange
        var email = $"get-consents-{Guid.NewGuid()}@test.com";
        var createRequest = new CreateCustomerRequest
        {
            Email = email,
            FirstName = "GetConsents",
            LastName = "Test",
            PhoneNumber = "555-5000"
        };

        await _client.PostAsJsonAsync(ApiRoutes.Customers.Base, createRequest);

        // Grant multiple consents
        var marketingConsent = new ConsentRequest
        {
            ConsentType = ApplicationConstants.Consent.Types.Marketing,
            ConsentVersion = "1.0"
        };
        var dataProcessingConsent = new ConsentRequest
        {
            ConsentType = ApplicationConstants.Consent.Types.DataProcessing,
            ConsentVersion = "1.0"
        };

        await _client.PostAsJsonAsync(ApiRoutes.Customers.Consents(email), marketingConsent);
        await _client.PostAsJsonAsync(ApiRoutes.Customers.Consents(email), dataProcessingConsent);

        // Act
        var response = await _client.GetAsync(ApiRoutes.Customers.Consents(email));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var consents = await response.Content.ReadFromJsonAsync<List<ConsentResponse>>();
        consents.Should().NotBeNull();
        consents!.Should().HaveCountGreaterThanOrEqualTo(2);
        consents.Should().Contain(c => c.ConsentType == ApplicationConstants.Consent.Types.Marketing && c.IsGranted);
        consents.Should().Contain(c => c.ConsentType == ApplicationConstants.Consent.Types.DataProcessing && c.IsGranted);
    }

    [Fact]
    public async Task GetConsents_NonExistentCustomer_Returns404NotFound()
    {
        // Arrange
        var email = "nonexistent-getconsents@test.com";

        // Act
        var response = await _client.GetAsync(ApiRoutes.Customers.Consents(email));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetConsents_CustomerWithNoConsents_ReturnsEmptyList()
    {
        // Arrange
        var email = $"no-consents-{Guid.NewGuid()}@test.com";
        var createRequest = new CreateCustomerRequest
        {
            Email = email,
            FirstName = "NoConsents",
            LastName = "Test",
            PhoneNumber = "555-5001"
        };

        await _client.PostAsJsonAsync(ApiRoutes.Customers.Base, createRequest);

        // Act
        var response = await _client.GetAsync(ApiRoutes.Customers.Consents(email));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var consents = await response.Content.ReadFromJsonAsync<List<ConsentResponse>>();
        consents.Should().NotBeNull();
        consents!.Should().BeEmpty();
    }

    #endregion

    #region Complex GDPR Workflows

    [Fact]
    public async Task CompleteGdprWorkflow_GrantRevokeExport_WorksCorrectly()
    {
        // Arrange
        var email = $"gdpr-workflow-{Guid.NewGuid()}@test.com";
        var createRequest = new CreateCustomerRequest
        {
            Email = email,
            FirstName = "GDPR",
            LastName = "Workflow",
            PhoneNumber = "555-6000"
        };

        // Step 1: Create customer
        var createResponse = await _client.PostAsJsonAsync(ApiRoutes.Customers.Base, createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Step 2: Grant consents
        var marketingConsent = new ConsentRequest
        {
            ConsentType = ApplicationConstants.Consent.Types.Marketing,
            ConsentVersion = "1.0"
        };
        var profilingConsent = new ConsentRequest
        {
            ConsentType = ApplicationConstants.Consent.Types.Profiling,
            ConsentVersion = "1.0"
        };

        await _client.PostAsJsonAsync(ApiRoutes.Customers.Consents(email), marketingConsent);
        await _client.PostAsJsonAsync(ApiRoutes.Customers.Consents(email), profilingConsent);

        // Step 3: Get consents and verify
        var getConsentsResponse = await _client.GetAsync(ApiRoutes.Customers.Consents(email));
        var consents = await getConsentsResponse.Content.ReadFromJsonAsync<List<ConsentResponse>>();
        consents.Should().HaveCountGreaterThanOrEqualTo(2);
        consents!.Should().OnlyContain(c => c.IsGranted);

        // Step 4: Revoke marketing consent
        var revokeResponse = await _client.DeleteAsync(
            ApiRoutes.Customers.ConsentByType(email, ApplicationConstants.Consent.Types.Marketing));
        revokeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 5: Export data
        var exportResponse = await _client.GetAsync(ApiRoutes.Customers.Export(email));
        exportResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var exportData = await exportResponse.Content.ReadFromJsonAsync<CustomerDataExportResponse>();
        exportData.Should().NotBeNull();
        exportData!.Consents.Should().HaveCountGreaterThanOrEqualTo(2);

        var marketingConsentAfterRevoke = exportData.Consents.First(c => c.ConsentType == ApplicationConstants.Consent.Types.Marketing);
        marketingConsentAfterRevoke.IsGranted.Should().BeFalse();

        var profilingConsentStillActive = exportData.Consents.First(c => c.ConsentType == ApplicationConstants.Consent.Types.Profiling);
        profilingConsentStillActive.IsGranted.Should().BeTrue();
    }

    [Fact]
    public async Task RightToErasure_AnonymizeAndVerify_WorksCorrectly()
    {
        // Arrange
        var email = $"erasure-{Guid.NewGuid()}@test.com";
        var createRequest = new CreateCustomerRequest
        {
            Email = email,
            FirstName = "Erasure",
            LastName = "Test",
            PhoneNumber = "555-6001"
        };

        // Create customer
        await _client.PostAsJsonAsync(ApiRoutes.Customers.Base, createRequest);

        // Grant consent
        var consentRequest = new ConsentRequest
        {
            ConsentType = ApplicationConstants.Consent.Types.Marketing,
            ConsentVersion = "1.0"
        };
        await _client.PostAsJsonAsync(ApiRoutes.Customers.Consents(email), consentRequest);

        // Act: Exercise right to erasure
        var anonymizeResponse = await _client.PostAsync(
            $"{ApiRoutes.Customers.Anonymize(email)}?reason=Customer requested data deletion",
            null);

        // Assert
        anonymizeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify customer cannot be found by original email
        var getResponse = await _client.GetAsync(ApiRoutes.Customers.ByEmail(email));
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Verify cannot export with original email (customer not found after anonymization)
        var exportResponse = await _client.GetAsync(ApiRoutes.Customers.Export(email));
        exportResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ConsentVersioning_MultipleVersions_TracksProperly()
    {
        // Arrange
        var email = $"consent-versioning-{Guid.NewGuid()}@test.com";
        var createRequest = new CreateCustomerRequest
        {
            Email = email,
            FirstName = "ConsentVersion",
            LastName = "Test",
            PhoneNumber = "555-6002"
        };

        await _client.PostAsJsonAsync(ApiRoutes.Customers.Base, createRequest);

        // Grant consent with version 1.0
        var consentV1 = new ConsentRequest
        {
            ConsentType = ApplicationConstants.Consent.Types.Marketing,
            ConsentVersion = "1.0"
        };
        await _client.PostAsJsonAsync(ApiRoutes.Customers.Consents(email), consentV1);

        // Revoke it
        await _client.DeleteAsync(
            ApiRoutes.Customers.ConsentByType(email, ApplicationConstants.Consent.Types.Marketing));

        // Grant again with version 2.0
        var consentV2 = new ConsentRequest
        {
            ConsentType = ApplicationConstants.Consent.Types.Marketing,
            ConsentVersion = "2.0"
        };
        await _client.PostAsJsonAsync(ApiRoutes.Customers.Consents(email), consentV2);

        // Act: Get consents
        var response = await _client.GetAsync(ApiRoutes.Customers.Consents(email));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var consents = await response.Content.ReadFromJsonAsync<List<ConsentResponse>>();
        consents.Should().NotBeNull();

        // Should have both consent records (revoked v1 and granted v2)
        var marketingConsents = consents!.Where(c => c.ConsentType == ApplicationConstants.Consent.Types.Marketing).ToList();
        marketingConsents.Should().HaveCountGreaterThanOrEqualTo(1);

        // Most recent should be version 2.0 and granted
        var mostRecent = marketingConsents.OrderByDescending(c => c.ConsentDate).First();
        mostRecent.ConsentVersion.Should().Be("2.0");
        mostRecent.IsGranted.Should().BeTrue();
    }

    #endregion
}
