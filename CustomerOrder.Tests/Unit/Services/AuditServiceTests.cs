using CustomerOrder.Application.Services;
using CustomerOrder.Domain.Constants;
using CustomerOrder.Domain.Entities;
using CustomerOrder.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace CustomerOrder.Tests.Unit.Services;

[Trait("Category", "Unit")]
public class AuditServiceTests
{
    private readonly Mock<IAuditRepository> _mockRepository;
    private readonly AuditService _service;

    public AuditServiceTests()
    {
        _mockRepository = new Mock<IAuditRepository>();
        _service = new AuditService(_mockRepository.Object);
    }

    #region LogAsync Tests

    [Fact]
    public async Task LogAsync_ValidData_CreatesAuditLog()
    {
        // Arrange
        var entityType = ApplicationConstants.Audit.EntityTypes.Customer;
        var entityId = "john.doe@example.com";
        var action = ApplicationConstants.Audit.Actions.Created;
        var userEmail = "jane.smith@example.com";
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";
        var httpMethod = "POST";
        var endpoint = "/api/customers";

        _mockRepository.Setup(x => x.CreateAsync(It.IsAny<AuditLog>()))
            .ReturnsAsync(true);

        // Act
        await _service.LogAsync(entityType, entityId, action, userEmail, ipAddress, userAgent, httpMethod, endpoint);

        // Assert
        _mockRepository.Verify(x => x.CreateAsync(It.Is<AuditLog>(log =>
            log.EntityType == entityType &&
            log.EntityId == entityId &&
            log.Action == action &&
            log.UserEmail == userEmail &&
            log.IpAddress == ipAddress &&
            log.UserAgent == userAgent &&
            log.HttpMethod == httpMethod &&
            log.Endpoint == endpoint &&
            log.Changes == null
        )), Times.Once);
    }

    [Fact]
    public async Task LogAsync_WithChanges_StoresChanges()
    {
        // Arrange
        var changes = "{\"oldEmail\":\"old@test.com\",\"newEmail\":\"new@test.com\"}";

        _mockRepository.Setup(x => x.CreateAsync(It.IsAny<AuditLog>()))
            .ReturnsAsync(true);

        // Act
        await _service.LogAsync(
            ApplicationConstants.Audit.EntityTypes.Customer,
            "test@example.com",
            ApplicationConstants.Audit.Actions.Updated,
            "admin@example.com",
            "127.0.0.1",
            "Test Agent",
            "PUT",
            "/api/customers/test@example.com",
            changes);

        // Assert
        _mockRepository.Verify(x => x.CreateAsync(It.Is<AuditLog>(log =>
            log.Changes == changes
        )), Times.Once);
    }

    [Fact]
    public async Task LogAsync_ExportAction_LogsCorrectAction()
    {
        // Arrange
        _mockRepository.Setup(x => x.CreateAsync(It.IsAny<AuditLog>()))
            .ReturnsAsync(true);

        // Act
        await _service.LogAsync(
            ApplicationConstants.Audit.EntityTypes.Customer,
            "test@example.com",
            ApplicationConstants.Audit.Actions.Exported,
            "admin@example.com",
            "127.0.0.1",
            "Test Agent",
            "GET",
            "/api/customers/test@example.com/export");

        // Assert
        _mockRepository.Verify(x => x.CreateAsync(It.Is<AuditLog>(log =>
            log.Action == ApplicationConstants.Audit.Actions.Exported
        )), Times.Once);
    }

    [Fact]
    public async Task LogAsync_AnonymizeAction_LogsCorrectAction()
    {
        // Arrange
        _mockRepository.Setup(x => x.CreateAsync(It.IsAny<AuditLog>()))
            .ReturnsAsync(true);

        // Act
        await _service.LogAsync(
            ApplicationConstants.Audit.EntityTypes.Customer,
            "test@example.com",
            ApplicationConstants.Audit.Actions.Anonymized,
            "admin@example.com",
            "127.0.0.1",
            "Test Agent",
            "POST",
            "/api/customers/test@example.com/anonymize");

        // Assert
        _mockRepository.Verify(x => x.CreateAsync(It.Is<AuditLog>(log =>
            log.Action == ApplicationConstants.Audit.Actions.Anonymized
        )), Times.Once);
    }

    [Fact]
    public async Task LogAsync_ConsentGranted_LogsCorrectAction()
    {
        // Arrange
        _mockRepository.Setup(x => x.CreateAsync(It.IsAny<AuditLog>()))
            .ReturnsAsync(true);

        // Act
        await _service.LogAsync(
            ApplicationConstants.Audit.EntityTypes.Consent,
            "test@example.com",
            ApplicationConstants.Audit.Actions.ConsentGranted,
            "test@example.com",
            "127.0.0.1",
            "Test Agent",
            "POST",
            "/api/customers/test@example.com/consents");

        // Assert
        _mockRepository.Verify(x => x.CreateAsync(It.Is<AuditLog>(log =>
            log.Action == ApplicationConstants.Audit.Actions.ConsentGranted &&
            log.EntityType == ApplicationConstants.Audit.EntityTypes.Consent
        )), Times.Once);
    }

    #endregion

    #region GetAuditTrailAsync Tests

    [Fact]
    public async Task GetAuditTrailAsync_ValidEntityTypeAndId_ReturnsAuditLogs()
    {
        // Arrange
        var entityType = ApplicationConstants.Audit.EntityTypes.Customer;
        var entityId = "john.doe@example.com";
        var expectedLogs = new List<AuditLog>
        {
            new AuditLog
            {
                AuditId = 1,
                EntityType = entityType,
                EntityId = entityId,
                Action = ApplicationConstants.Audit.Actions.Created,
                UserEmail = "admin@example.com",
                ActionDate = DateTime.UtcNow.AddDays(-2),
                IpAddress = "127.0.0.1",
                UserAgent = "Test",
                HttpMethod = "POST",
                Endpoint = "/api/customers"
            },
            new AuditLog
            {
                AuditId = 2,
                EntityType = entityType,
                EntityId = entityId,
                Action = ApplicationConstants.Audit.Actions.Updated,
                UserEmail = "admin@example.com",
                ActionDate = DateTime.UtcNow.AddDays(-1),
                IpAddress = "127.0.0.1",
                UserAgent = "Test",
                HttpMethod = "PUT",
                Endpoint = "/api/customers/john.doe@example.com"
            }
        };

        _mockRepository.Setup(x => x.GetByEntityAsync(entityType, entityId))
            .ReturnsAsync(expectedLogs);

        // Act
        var result = await _service.GetAuditTrailAsync(entityType, entityId);

        // Assert
        var logs = result.ToList();
        logs.Should().HaveCount(2);
        logs.Should().BeEquivalentTo(expectedLogs);
    }

    [Fact]
    public async Task GetAuditTrailAsync_NoLogs_ReturnsEmptyList()
    {
        // Arrange
        _mockRepository.Setup(x => x.GetByEntityAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<AuditLog>());

        // Act
        var result = await _service.GetAuditTrailAsync("Customer", "nonexistent@example.com");

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetUserActivityAsync Tests

    [Fact]
    public async Task GetUserActivityAsync_ValidUser_ReturnsUserActivity()
    {
        // Arrange
        var userEmail = "admin@example.com";
        var expectedLogs = new List<AuditLog>
        {
            new AuditLog
            {
                AuditId = 1,
                EntityType = ApplicationConstants.Audit.EntityTypes.Customer,
                EntityId = "customer1@example.com",
                Action = ApplicationConstants.Audit.Actions.Viewed,
                UserEmail = userEmail,
                ActionDate = DateTime.UtcNow,
                IpAddress = "127.0.0.1",
                UserAgent = "Test",
                HttpMethod = "GET",
                Endpoint = "/api/customers/customer1@example.com"
            }
        };

        _mockRepository.Setup(x => x.GetByUserEmailAsync(userEmail, 1, 50))
            .ReturnsAsync(expectedLogs);

        // Act
        var result = await _service.GetUserActivityAsync(userEmail);

        // Assert
        var logs = result.ToList();
        logs.Should().HaveCount(1);
        logs.First().UserEmail.Should().Be(userEmail);
    }

    [Fact]
    public async Task GetUserActivityAsync_WithPagination_PassesCorrectParameters()
    {
        // Arrange
        var userEmail = "admin@example.com";
        var pageNumber = 2;
        var pageSize = 25;

        _mockRepository.Setup(x => x.GetByUserEmailAsync(userEmail, pageNumber, pageSize))
            .ReturnsAsync(new List<AuditLog>());

        // Act
        await _service.GetUserActivityAsync(userEmail, pageNumber, pageSize);

        // Assert
        _mockRepository.Verify(x => x.GetByUserEmailAsync(userEmail, pageNumber, pageSize), Times.Once);
    }

    #endregion

    #region PurgeLogsOlderThanAsync Tests

    [Fact]
    public async Task PurgeLogsOlderThanAsync_ValidRetentionYears_DeletesOldLogs()
    {
        // Arrange
        var retentionYears = 3;
        _mockRepository.Setup(x => x.DeleteOlderThanAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.PurgeLogsOlderThanAsync(retentionYears);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(x => x.DeleteOlderThanAsync(It.Is<DateTime>(date =>
            date <= DateTime.UtcNow.AddYears(-retentionYears)
        )), Times.Once);
    }

    [Fact]
    public async Task PurgeLogsOlderThanAsync_NoOldLogs_ReturnsFalse()
    {
        // Arrange
        _mockRepository.Setup(x => x.DeleteOlderThanAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.PurgeLogsOlderThanAsync(3);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task PurgeLogsOlderThanAsync_OneYearRetention_CalculatesCorrectCutoffDate()
    {
        // Arrange
        var retentionYears = 1;
        _mockRepository.Setup(x => x.DeleteOlderThanAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(true);

        // Act
        await _service.PurgeLogsOlderThanAsync(retentionYears);

        // Assert
        _mockRepository.Verify(x => x.DeleteOlderThanAsync(It.Is<DateTime>(date =>
            date <= DateTime.UtcNow.AddYears(-1) &&
            date >= DateTime.UtcNow.AddYears(-1).AddMinutes(-1)
        )), Times.Once);
    }

    #endregion
}
