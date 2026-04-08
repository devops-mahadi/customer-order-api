using CustomerOrder.Domain.Constants;
using CustomerOrder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerOrder.Infrastructure.Persistence.Configurations;

public class CustomerConsentConfiguration : IEntityTypeConfiguration<CustomerConsent>
{
    public void Configure(EntityTypeBuilder<CustomerConsent> builder)
    {
        builder.ToTable("CustomerConsents");

        // Primary Key
        builder.HasKey(c => c.ConsentId);

        builder.Property(c => c.ConsentId)
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(c => c.CustomerId)
            .IsRequired();

        builder.Property(c => c.ConsentType)
            .IsRequired()
            .HasMaxLength(ApplicationConstants.Consent.ConsentTypeMaxLength);

        builder.Property(c => c.IsGranted)
            .IsRequired();

        builder.Property(c => c.ConsentDate)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(c => c.RevokedDate);

        builder.Property(c => c.IpAddress)
            .IsRequired()
            .HasMaxLength(ApplicationConstants.Consent.IpAddressMaxLength);

        builder.Property(c => c.UserAgent)
            .IsRequired()
            .HasMaxLength(ApplicationConstants.Consent.UserAgentMaxLength);

        builder.Property(c => c.ConsentVersion)
            .IsRequired()
            .HasMaxLength(ApplicationConstants.Consent.ConsentVersionMaxLength);

        // Indexes for performance
        builder.HasIndex(c => c.CustomerId)
            .HasDatabaseName("IX_CustomerConsents_CustomerId");

        builder.HasIndex(c => new { c.CustomerId, c.ConsentType })
            .HasDatabaseName("IX_CustomerConsents_CustomerId_ConsentType");

        // Relationships
        builder.HasOne(c => c.Customer)
            .WithMany(cu => cu.Consents)
            .HasForeignKey(c => c.CustomerId)
            .OnDelete(DeleteBehavior.Cascade); // When customer deleted, delete consents
    }
}
