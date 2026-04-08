using CustomerOrder.Domain.Constants;
using CustomerOrder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerOrder.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        // Primary Key
        builder.HasKey(a => a.AuditId);

        builder.Property(a => a.AuditId)
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(a => a.EntityType)
            .IsRequired()
            .HasMaxLength(ApplicationConstants.Audit.EntityTypeMaxLength);

        builder.Property(a => a.EntityId)
            .IsRequired()
            .HasMaxLength(ApplicationConstants.Audit.EntityIdMaxLength);

        builder.Property(a => a.Action)
            .IsRequired()
            .HasMaxLength(ApplicationConstants.Audit.ActionMaxLength);

        builder.Property(a => a.UserEmail)
            .IsRequired()
            .HasMaxLength(ApplicationConstants.Audit.UserEmailMaxLength);

        builder.Property(a => a.ActionDate)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(a => a.Changes)
            .HasMaxLength(4000); // Allow large JSON changes

        builder.Property(a => a.IpAddress)
            .IsRequired()
            .HasMaxLength(ApplicationConstants.Audit.IpAddressMaxLength);

        builder.Property(a => a.UserAgent)
            .IsRequired()
            .HasMaxLength(ApplicationConstants.Audit.UserAgentMaxLength);

        builder.Property(a => a.HttpMethod)
            .IsRequired()
            .HasMaxLength(ApplicationConstants.Audit.HttpMethodMaxLength);

        builder.Property(a => a.Endpoint)
            .IsRequired()
            .HasMaxLength(ApplicationConstants.Audit.EndpointMaxLength);

        // Indexes for performance
        builder.HasIndex(a => new { a.EntityType, a.EntityId })
            .HasDatabaseName("IX_AuditLogs_EntityType_EntityId");

        builder.HasIndex(a => a.UserEmail)
            .HasDatabaseName("IX_AuditLogs_UserEmail");

        builder.HasIndex(a => a.ActionDate)
            .HasDatabaseName("IX_AuditLogs_ActionDate");
    }
}
