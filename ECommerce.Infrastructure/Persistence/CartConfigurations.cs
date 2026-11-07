using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence;

public sealed class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("Carts", table =>
            table.HasCheckConstraint("CK_Carts_Currency", "\"Currency\" IN ('TRY', 'USD')"));
        builder.HasKey(cart => cart.Id);
        builder.Property(cart => cart.Id).ValueGeneratedNever();
        builder.Property(cart => cart.Currency).HasMaxLength(3).IsRequired();
        builder.Property<uint>("Version").IsRowVersion();
        builder.HasIndex(cart => cart.UserId).IsUnique();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(cart => cart.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(cart => cart.Items).WithOne().HasForeignKey(item => item.CartId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(cart => cart.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("CartItems", table =>
            table.HasCheckConstraint("CK_CartItems_Quantity", "\"Quantity\" BETWEEN 1 AND 999"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property<uint>("Version").IsRowVersion();
        builder.HasIndex(item => new { item.CartId, item.ProductVariantId }).IsUnique();
        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(item => item.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
    }
}
