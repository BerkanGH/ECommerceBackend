using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders", table =>
        {
            table.HasCheckConstraint("CK_Orders_Currency", "\"Currency\" IN ('TRY', 'USD')");
            table.HasCheckConstraint("CK_Orders_Total", "\"TotalAmount\" >= 0");
            table.HasCheckConstraint("CK_Orders_Status", "\"Status\" IN ('Pending','Confirmed','Shipped','Delivered','Cancelled')");
        });
        builder.HasKey(order => order.Id);
        builder.Property(order => order.Id).ValueGeneratedNever();
        builder.Property(order => order.Currency).HasMaxLength(3).IsRequired();
        builder.Property(order => order.TotalAmount).HasPrecision(24, 2);
        builder.Property(order => order.RequestFingerprint).HasMaxLength(64).IsRequired();
        builder.Property(order => order.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property<Guid>("Revision").IsConcurrencyToken();
        builder.HasIndex(order => new { order.UserId, order.IdempotencyKey }).IsUnique();
        builder.HasIndex(order => new { order.UserId, order.CreatedAtUtc });
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(order => order.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(order => order.Items).WithOne().HasForeignKey(item => item.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(order => order.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.OwnsOne(order => order.ShippingAddress, address =>
        {
            address.Property(value => value.RecipientName).HasMaxLength(100).IsRequired();
            address.Property(value => value.Phone).HasMaxLength(30).IsRequired();
            address.Property(value => value.CountryCode).HasMaxLength(2).IsRequired();
            address.Property(value => value.City).HasMaxLength(100).IsRequired();
            address.Property(value => value.PostalCode).HasMaxLength(20).IsRequired();
            address.Property(value => value.AddressLine).HasMaxLength(500).IsRequired();
        });
        builder.Navigation(order => order.ShippingAddress).IsRequired();
    }
}

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems", table =>
        {
            table.HasCheckConstraint("CK_OrderItems_Quantity", "\"Quantity\" BETWEEN 1 AND 999");
            table.HasCheckConstraint("CK_OrderItems_Price", "\"UnitPrice\" >= 0");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.ProductName).HasMaxLength(100).IsRequired();
        builder.Property(item => item.Sku).HasMaxLength(100).IsRequired();
        builder.Property(item => item.OptionsSummary).HasMaxLength(2048).IsRequired();
        builder.Property(item => item.UnitPrice).HasPrecision(18, 2);
        builder.Ignore(item => item.LineTotal);
        builder.HasIndex(item => new { item.OrderId, item.ProductVariantId }).IsUnique();
        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(item => item.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
    }
}
