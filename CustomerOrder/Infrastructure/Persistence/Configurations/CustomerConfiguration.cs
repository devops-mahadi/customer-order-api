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

        // Indexes for performance
        builder.HasIndex(c => c.Email)
            .IsUnique()
            .HasDatabaseName("IX_Customers_Email");

        // Relationships
        builder.HasMany(c => c.Orders)
            .WithOne(o => o.Customer)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete
    }
}
