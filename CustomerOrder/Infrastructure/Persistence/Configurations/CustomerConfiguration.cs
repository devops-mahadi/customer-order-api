using CustomerOrder.Domain.Constants;
using CustomerOrder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerOrder.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        // Primary Key
        builder.HasKey(c => c.CustomerId);

        builder.Property(c => c.CustomerId)
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(c => c.FirstName)
            .IsRequired()
            .HasMaxLength(ApplicationConstants.Customer.FirstNameMaxLength);

        builder.Property(c => c.LastName)
            .IsRequired()
            .HasMaxLength(ApplicationConstants.Customer.LastNameMaxLength);

        builder.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(ApplicationConstants.Customer.EmailMaxLength);

        builder.Property(c => c.PhoneNumber)
            .HasMaxLength(ApplicationConstants.Customer.PhoneNumberMaxLength);

        builder.Property(c => c.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(c => c.LastUpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // GDPR: Soft delete fields
        builder.Property(c => c.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(c => c.DeletedAt);

        builder.Property(c => c.DeletedReason)
            .HasMaxLength(ApplicationConstants.Consent.DeletedReasonMaxLength);

        // Indexes for performance
        builder.HasIndex(c => c.Email)
            .IsUnique()
            .HasDatabaseName("IX_Customers_Email");

        builder.HasIndex(c => c.IsDeleted)
            .HasDatabaseName("IX_Customers_IsDeleted");

        // Relationships
        builder.HasMany(c => c.Orders)
            .WithOne(o => o.Customer)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

        builder.HasMany(c => c.Consents)
            .WithOne(cs => cs.Customer)
            .HasForeignKey(cs => cs.CustomerId)
            .OnDelete(DeleteBehavior.Cascade); // When customer deleted, delete consents
    }
}
