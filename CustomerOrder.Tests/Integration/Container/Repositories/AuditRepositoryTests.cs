using CustomerOrder.Domain.Constants;
using CustomerOrder.Domain.Entities;
using CustomerOrder.Infrastructure.Repositories;
using FluentAssertions;

namespace CustomerOrder.Tests.Integration.Container.Repositories;

[Collection("Database")]
public class AuditRepositoryTests(DatabaseFixture fixture)
{
    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidAuditLog_ReturnsTrue()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var repository = new AuditRepository(context);

        var auditLog = new AuditLog
        {
            EntityType = ApplicationConstants.Audit.EntityTypes.Customer,
            EntityId = "test@example.com",
            Action = ApplicationConstants.Audit.Actions.Created,
            UserEmail = "admin@example.com",
            ActionDate = DateTime.UtcNow,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0",
            HttpMethod = "POST",
            Endpoint = "/api/customers"
        };

        // Act
        var result = await repository.CreateAsync(auditLog);

        // Assert
        result.Should().BeTrue();
        auditLog.AuditId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateAsync_WithChanges_StoresChangesCorrectly()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var repository = new AuditRepository(context);

        var entityId = $"changes-{Guid.NewGuid()}@example.com";
        var changes = "{\"oldValue\":\"old\",\"newValue\":\"new\"}";
        var auditLog = new AuditLog
        {
            EntityType = ApplicationConstants.Audit.EntityTypes.Customer,
            EntityId = entityId,
            Action = ApplicationConstants.Audit.Actions.Updated,
            UserEmail = "admin@example.com",
            ActionDate = DateTime.UtcNow,
            Changes = changes,
            IpAddress = "127.0.0.1",
            UserAgent = "Test",
            HttpMethod = "PUT",
            Endpoint = $"/api/customers/{entityId}"
        };

        await repository.CreateAsync(auditLog);

        // Act & Assert
        await using var context2 = fixture.CreateDbContext();
        var repository2 = new AuditRepository(context2);
        var retrieved = await repository2.GetByEntityAsync(auditLog.EntityType, auditLog.EntityId);
        var log = retrieved.First();
        log.Changes.Should().Be(changes);
    }

    #endregion

    #region GetByEntityAsync Tests

    [Fact]
    public async Task GetByEntityAsync_ExistingEntity_ReturnsAuditLogs()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var repository = new AuditRepository(context);

        var entityType = ApplicationConstants.Audit.EntityTypes.Customer;
        var entityId = $"customer-{Guid.NewGuid()}@example.com";

        var log1 = new AuditLog
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = ApplicationConstants.Audit.Actions.Created,
            UserEmail = "admin@example.com",
            ActionDate = DateTime.UtcNow.AddDays(-2),
            IpAddress = "127.0.0.1",
            UserAgent = "Test",
            HttpMethod = "POST",
            Endpoint = "/api/customers"
        };

        var log2 = new AuditLog
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = ApplicationConstants.Audit.Actions.Updated,
            UserEmail = "admin@example.com",
            ActionDate = DateTime.UtcNow.AddDays(-1),
            IpAddress = "127.0.0.1",
            UserAgent = "Test",
            HttpMethod = "PUT",
            Endpoint = $"/api/customers/{entityId}"
        };

        await repository.CreateAsync(log1);
        await repository.CreateAsync(log2);

        // Act
        var result = await repository.GetByEntityAsync(entityType, entityId);

        // Assert
        var logs = result.ToList();
        logs.Should().HaveCountGreaterThanOrEqualTo(2);
        logs.Should().Contain(l => l.Action == ApplicationConstants.Audit.Actions.Created);
        logs.Should().Contain(l => l.Action == ApplicationConstants.Audit.Actions.Updated);
        logs.Should().BeInDescendingOrder(l => l.ActionDate);
    }

    [Fact]
    public async Task GetByEntityAsync_NonExistentEntity_ReturnsEmpty()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var repository = new AuditRepository(context);

        // Act
        var result = await repository.GetByEntityAsync("Customer", "nonexistent@example.com");

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetByUserEmailAsync Tests

    [Fact]
    public async Task GetByUserEmailAsync_ExistingUser_ReturnsUserActivity()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var repository = new AuditRepository(context);

        var userEmail = $"testuser-{Guid.NewGuid()}@example.com";

        var log1 = new AuditLog
        {
            EntityType = ApplicationConstants.Audit.EntityTypes.Customer,
            EntityId = "customer1@example.com",
            Action = ApplicationConstants.Audit.Actions.Viewed,
            UserEmail = userEmail,
            ActionDate = DateTime.UtcNow,
            IpAddress = "127.0.0.1",
            UserAgent = "Test",
            HttpMethod = "GET",
            Endpoint = "/api/customers/customer1@example.com"
        };

        var log2 = new AuditLog
        {
            EntityType = ApplicationConstants.Audit.EntityTypes.Order,
            EntityId = "ORD-123",
            Action = ApplicationConstants.Audit.Actions.Viewed,
            UserEmail = userEmail,
            ActionDate = DateTime.UtcNow.AddMinutes(-5),
            IpAddress = "127.0.0.1",
            UserAgent = "Test",
            HttpMethod = "GET",
            Endpoint = "/api/orders/ORD-123"
        };

        await repository.CreateAsync(log1);
        await repository.CreateAsync(log2);

        // Act
        var result = await repository.GetByUserEmailAsync(userEmail, 1, 50);

        // Assert
        var logs = result.ToList();
        logs.Should().HaveCountGreaterThanOrEqualTo(2);
        logs.Should().OnlyContain(l => l.UserEmail == userEmail);
        logs.Should().BeInDescendingOrder(l => l.ActionDate);
    }

    [Fact]
    public async Task GetByUserEmailAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var repository = new AuditRepository(context);

        var userEmail = $"pagination-{Guid.NewGuid()}@example.com";

        // Create 15 logs
        for (int i = 0; i < 15; i++)
        {
            var log = new AuditLog
            {
                EntityType = ApplicationConstants.Audit.EntityTypes.Customer,
                EntityId = $"customer{i}@example.com",
                Action = ApplicationConstants.Audit.Actions.Viewed,
                UserEmail = userEmail,
                ActionDate = DateTime.UtcNow.AddMinutes(-i),
                IpAddress = "127.0.0.1",
                UserAgent = "Test",
                HttpMethod = "GET",
                Endpoint = $"/api/customers/customer{i}@example.com"
            };
            await repository.CreateAsync(log);
        }

        // Act
        var page1 = await repository.GetByUserEmailAsync(userEmail, 1, 10);
        var page2 = await repository.GetByUserEmailAsync(userEmail, 2, 10);

        // Assert
        page1.Should().HaveCount(10);
        page2.Should().HaveCount(5);
    }

    #endregion

    #region GetByDateRangeAsync Tests

    [Fact]
    public async Task GetByDateRangeAsync_ValidRange_ReturnsLogsInRange()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var repository = new AuditRepository(context);

        var recentId = $"recent-{Guid.NewGuid()}@example.com";
        var oldId = $"old-{Guid.NewGuid()}@example.com";

        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;

        var recentLog = new AuditLog
        {
            EntityType = ApplicationConstants.Audit.EntityTypes.Customer,
            EntityId = recentId,
            Action = ApplicationConstants.Audit.Actions.Created,
            UserEmail = "admin@example.com",
            ActionDate = DateTime.UtcNow.AddDays(-3),
            IpAddress = "127.0.0.1",
            UserAgent = "Test",
            HttpMethod = "POST",
            Endpoint = "/api/customers"
        };

        var oldLog = new AuditLog
        {
            EntityType = ApplicationConstants.Audit.EntityTypes.Customer,
            EntityId = oldId,
            Action = ApplicationConstants.Audit.Actions.Created,
            UserEmail = "admin@example.com",
            ActionDate = DateTime.UtcNow.AddDays(-30),
            IpAddress = "127.0.0.1",
            UserAgent = "Test",
            HttpMethod = "POST",
            Endpoint = "/api/customers"
        };

        await repository.CreateAsync(recentLog);
        await repository.CreateAsync(oldLog);

        // Act
        var result = await repository.GetByDateRangeAsync(startDate, endDate);

        // Assert
        var logs = result.ToList();
        logs.Should().Contain(l => l.EntityId == recentId);
        logs.Should().NotContain(l => l.EntityId == oldId);
        logs.Should().OnlyContain(l => l.ActionDate >= startDate && l.ActionDate <= endDate);
    }

    #endregion

    #region DeleteOlderThanAsync Tests

    [Fact]
    public async Task DeleteOlderThanAsync_OldLogsExist_DeletesAndReturnsTrue()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var repository = new AuditRepository(context);

        var veryOldId = $"veryold-{Guid.NewGuid()}@example.com";
        var recentId = $"recent-{Guid.NewGuid()}@example.com";

        var cutoffDate = DateTime.UtcNow.AddYears(-3);

        var oldLog = new AuditLog
        {
            EntityType = ApplicationConstants.Audit.EntityTypes.Customer,
            EntityId = veryOldId,
            Action = ApplicationConstants.Audit.Actions.Created,
            UserEmail = "admin@example.com",
            ActionDate = DateTime.UtcNow.AddYears(-4),
            IpAddress = "127.0.0.1",
            UserAgent = "Test",
            HttpMethod = "POST",
            Endpoint = "/api/customers"
        };

        var recentLog = new AuditLog
        {
            EntityType = ApplicationConstants.Audit.EntityTypes.Customer,
            EntityId = recentId,
            Action = ApplicationConstants.Audit.Actions.Created,
            UserEmail = "admin@example.com",
            ActionDate = DateTime.UtcNow,
            IpAddress = "127.0.0.1",
            UserAgent = "Test",
            HttpMethod = "POST",
            Endpoint = "/api/customers"
        };

        await repository.CreateAsync(oldLog);
        await repository.CreateAsync(recentLog);

        // Act
        var result = await repository.DeleteOlderThanAsync(cutoffDate);

        // Assert
        result.Should().BeTrue();

        await using var context2 = fixture.CreateDbContext();
        var repository2 = new AuditRepository(context2);
        var allLogs = await repository2.GetByDateRangeAsync(
            DateTime.UtcNow.AddYears(-5),
            DateTime.UtcNow.AddDays(1));

        allLogs.Should().NotContain(l => l.EntityId == veryOldId);
        allLogs.Should().Contain(l => l.EntityId == recentId);
    }

    [Fact]
    public async Task DeleteOlderThanAsync_NoOldLogs_ReturnsFalse()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var repository = new AuditRepository(context);

        var cutoffDate = DateTime.UtcNow.AddYears(-10);

        // Act
        var result = await repository.DeleteOlderThanAsync(cutoffDate);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GDPR Action Tests

    [Fact]
    public async Task CreateAsync_GdprExportAction_LogsCorrectly()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var repository = new AuditRepository(context);

        var entityId = $"gdpr-export-{Guid.NewGuid()}@example.com";
        var auditLog = new AuditLog
        {
            EntityType = ApplicationConstants.Audit.EntityTypes.Customer,
            EntityId = entityId,
            Action = ApplicationConstants.Audit.Actions.Exported,
            UserEmail = entityId,
            ActionDate = DateTime.UtcNow,
            IpAddress = "10.0.0.1",
            UserAgent = "GDPR Export Tool",
            HttpMethod = "GET",
            Endpoint = $"/api/customers/{entityId}/export"
        };

        // Act
        await repository.CreateAsync(auditLog);

        // Assert
        await using var context2 = fixture.CreateDbContext();
        var repository2 = new AuditRepository(context2);
        var logs = await repository2.GetByEntityAsync(auditLog.EntityType, auditLog.EntityId);
        logs.Should().Contain(l => l.Action == ApplicationConstants.Audit.Actions.Exported);
    }

    [Fact]
    public async Task CreateAsync_GdprAnonymizeAction_LogsCorrectly()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var repository = new AuditRepository(context);

        var entityId = $"anonymize-{Guid.NewGuid()}@example.com";
        var auditLog = new AuditLog
        {
            EntityType = ApplicationConstants.Audit.EntityTypes.Customer,
            EntityId = entityId,
            Action = ApplicationConstants.Audit.Actions.Anonymized,
            UserEmail = "admin@example.com",
            ActionDate = DateTime.UtcNow,
            IpAddress = "10.0.0.1",
            UserAgent = "Admin Panel",
            HttpMethod = "POST",
            Endpoint = $"/api/customers/{entityId}/anonymize"
        };

        // Act
        await repository.CreateAsync(auditLog);

        // Assert
        await using var context2 = fixture.CreateDbContext();
        var repository2 = new AuditRepository(context2);
        var logs = await repository2.GetByEntityAsync(auditLog.EntityType, auditLog.EntityId);
        logs.Should().Contain(l => l.Action == ApplicationConstants.Audit.Actions.Anonymized);
    }

    [Fact]
    public async Task CreateAsync_ConsentActions_LogsCorrectly()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var repository = new AuditRepository(context);

        var entityId = $"consent-{Guid.NewGuid()}@example.com";

        var grantLog = new AuditLog
        {
            EntityType = ApplicationConstants.Audit.EntityTypes.Consent,
            EntityId = entityId,
            Action = ApplicationConstants.Audit.Actions.ConsentGranted,
            UserEmail = entityId,
            ActionDate = DateTime.UtcNow,
            IpAddress = "10.0.0.1",
            UserAgent = "Consent Form",
            HttpMethod = "POST",
            Endpoint = $"/api/customers/{entityId}/consents"
        };

        var revokeLog = new AuditLog
        {
            EntityType = ApplicationConstants.Audit.EntityTypes.Consent,
            EntityId = entityId,
            Action = ApplicationConstants.Audit.Actions.ConsentRevoked,
            UserEmail = entityId,
            ActionDate = DateTime.UtcNow.AddMinutes(5),
            IpAddress = "10.0.0.1",
            UserAgent = "Consent Form",
            HttpMethod = "DELETE",
            Endpoint = $"/api/customers/{entityId}/consents/Marketing"
        };

        // Act
        await repository.CreateAsync(grantLog);
        await repository.CreateAsync(revokeLog);

        // Assert
        await using var context2 = fixture.CreateDbContext();
        var repository2 = new AuditRepository(context2);
        var logs = await repository2.GetByEntityAsync(
            ApplicationConstants.Audit.EntityTypes.Consent,
            entityId);

        logs.Should().Contain(l => l.Action == ApplicationConstants.Audit.Actions.ConsentGranted);
        logs.Should().Contain(l => l.Action == ApplicationConstants.Audit.Actions.ConsentRevoked);
    }

    #endregion
}
