using CustomerOrder.Application.Interfaces;
using CustomerOrder.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CustomerOrder.Application.Services;

/// <summary>
/// Background service for GDPR-compliant data retention
/// Automatically purges old data based on retention policies
/// </summary>
public class DataRetentionService : BackgroundService
{
    private readonly ILogger<DataRetentionService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IConfiguration _configuration;
    private readonly int _deletedCustomersRetentionDays;
    private readonly int _auditLogRetentionYears;
    private readonly int _runCleanupAtHour;
    private readonly bool _enableAutomaticCleanup;

    public DataRetentionService(
        ILogger<DataRetentionService> logger,
        IServiceScopeFactory serviceScopeFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _configuration = configuration;

        // Load retention settings from configuration
        _deletedCustomersRetentionDays = _configuration.GetValue<int>("DataRetention:DeletedCustomersRetentionDays", 90);
        _auditLogRetentionYears = _configuration.GetValue<int>("DataRetention:AuditLogRetentionYears", 3);
        _runCleanupAtHour = _configuration.GetValue<int>("DataRetention:RunCleanupAtHour", 2);
        _enableAutomaticCleanup = _configuration.GetValue<bool>("DataRetention:EnableAutomaticCleanup", true);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enableAutomaticCleanup)
        {
            _logger.LogInformation("Data retention service is disabled in configuration");
            return;
        }

        _logger.LogInformation("Data retention service started. Cleanup runs daily at {Hour}:00 UTC", _runCleanupAtHour);

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextRun = GetNextRunTime(now, _runCleanupAtHour);
            var delay = nextRun - now;

            _logger.LogInformation("Next data retention cleanup scheduled for {NextRun} UTC", nextRun);

            // Wait until the next scheduled run
            await Task.Delay(delay, stoppingToken);

            if (!stoppingToken.IsCancellationRequested)
            {
                await PerformCleanupAsync();
            }
        }
    }

    private async Task PerformCleanupAsync()
    {
        _logger.LogInformation("Starting data retention cleanup process");

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
            var customerRepository = scope.ServiceProvider.GetRequiredService<ICustomerRepository>();

            // 1. Purge old audit logs
            _logger.LogInformation("Purging audit logs older than {Years} years", _auditLogRetentionYears);
            var auditLogsPurged = await auditService.PurgeLogsOlderThanAsync(_auditLogRetentionYears);
            _logger.LogInformation("Audit logs purge completed. Success: {Success}", auditLogsPurged);

            // 2. Permanently delete anonymized customers older than retention period
            _logger.LogInformation("Deleting anonymized customers older than {Days} days", _deletedCustomersRetentionDays);
            var deletedCount = await PurgeAnonymizedCustomersAsync(customerRepository);
            _logger.LogInformation("Purged {Count} anonymized customers", deletedCount);

            _logger.LogInformation("Data retention cleanup completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during data retention cleanup");
        }
    }

    private async Task<int> PurgeAnonymizedCustomersAsync(ICustomerRepository customerRepository)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-_deletedCustomersRetentionDays);
        var allCustomers = await customerRepository.GetAllAsync();
        var customersToDelete = allCustomers
            .Where(c => c.IsDeleted && c.DeletedAt.HasValue && c.DeletedAt.Value < cutoffDate)
            .ToList();

        int count = 0;
        foreach (var customer in customersToDelete)
        {
            // Permanently delete from database
            var deleted = await customerRepository.DeleteAsync(customer);
            if (deleted)
            {
                count++;
            }
        }

        return count;
    }

    private static DateTime GetNextRunTime(DateTime now, int targetHour)
    {
        var nextRun = new DateTime(now.Year, now.Month, now.Day, targetHour, 0, 0, DateTimeKind.Utc);

        if (now >= nextRun)
        {
            // If we've passed today's run time, schedule for tomorrow
            nextRun = nextRun.AddDays(1);
        }

        return nextRun;
    }
}
