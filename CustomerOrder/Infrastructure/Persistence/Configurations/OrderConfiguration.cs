using CustomerOrder.Domain.Constants;
using CustomerOrder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerOrder.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        // Primary Key
        builder.HasKey(o => o.OrderId);

        builder.Property(o => o.OrderId)
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(o => o.OrderNumber)
            .IsRequired()
            .HasMaxLength(ApplicationConstants.Order.OrderNumberMaxLength);

        builder.Property(o => o.CustomerId)
            .IsRequired();

        builder.Property(o => o.OrderDate)
            .IsRequired();

        builder.Property(o => o.TotalAmount)
            .IsRequired()
            .HasPrecision(18, 2); // 18 digits total, 2 after decimal point

        builder.Property(o => o.Status)
            .IsRequired()
            .HasMaxLength(ApplicationConstants.Order.StatusMaxLength);

        builder.Property(o => o.ShippingAddress)
            .IsRequired()
            .HasMaxLength(ApplicationConstants.Order.ShippingAddressMaxLength);

        builder.Property(o => o.Notes)
            .HasMaxLength(ApplicationConstants.Order.NotesMaxLength);

        builder.Property(o => o.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(o => o.UpdatedAt);

        // Unique index on OrderNumber for lookups
        builder.HasIndex(o => o.OrderNumber)
            .IsUnique()
            .HasDatabaseName("IX_Orders_OrderNumber");

        // Index on CustomerId for faster joins and customer order queries
        builder.HasIndex(o => o.CustomerId)
            .HasDatabaseName("IX_Orders_CustomerId");

        // Index on OrderDate for date range filtering
        builder.HasIndex(o => o.OrderDate)
            .HasDatabaseName("IX_Orders_OrderDate");

        // Index on Status for status filtering
        builder.HasIndex(o => o.Status)
            .HasDatabaseName("IX_Orders_Status");

        // Composite index for combined date range + status filtering
        // This prevents N+1 queries when filtering by both date and status
        builder.HasIndex(o => new { o.OrderDate, o.Status })
            .HasDatabaseName("IX_Orders_OrderDate_Status");
    }
}
