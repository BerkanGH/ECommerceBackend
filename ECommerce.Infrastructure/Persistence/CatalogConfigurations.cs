using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(category => category.Id);
        builder.Property(category => category.Id).ValueGeneratedNever();
        builder.Property(category => category.Name).HasMaxLength(Category.MaxNameLength).IsRequired();
        builder.Property(category => category.Description).HasMaxLength(Category.MaxDescriptionLength);
        builder.Property<uint>("Version").IsRowVersion();
    }
}

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.Property(product => product.IsActive).HasDefaultValue(true);
        builder.HasKey(product => product.Id);
        builder.Property(product => product.Id).ValueGeneratedNever();
        builder.Property(product => product.Name).HasMaxLength(Product.MaxNameLength).IsRequired();
        builder.Property(product => product.Description).HasMaxLength(Product.MaxDescriptionLength);
        builder.Property(product => product.ImageUrl).HasMaxLength(Product.MaxImageUrlLength);
        builder.Property<uint>("Version").IsRowVersion();
        builder.HasOne<Category>().WithMany().HasForeignKey(product => product.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(product => product.Options).WithOne().HasForeignKey(option => option.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(product => product.Variants).WithOne().HasForeignKey(variant => variant.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(product => product.Options).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(product => product.Variants).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ProductOptionConfiguration : IEntityTypeConfiguration<ProductOption>
{
    public void Configure(EntityTypeBuilder<ProductOption> builder)
    {
        builder.ToTable("ProductOptions");
        builder.HasKey(option => option.Id);
        builder.Property(option => option.Id).ValueGeneratedNever();
        builder.Property(option => option.Name).HasMaxLength(ProductOption.MaxNameLength).IsRequired();
        builder.Property<string>("NormalizedName").HasMaxLength(ProductOption.MaxNameLength)
            .HasComputedColumnSql("upper(btrim(\"Name\"))", stored: true);
        builder.HasIndex("ProductId", "NormalizedName").IsUnique();
        builder.HasMany(option => option.Values).WithOne().HasForeignKey(value => value.ProductOptionId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(option => option.Values).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ProductOptionValueConfiguration : IEntityTypeConfiguration<ProductOptionValue>
{
    public void Configure(EntityTypeBuilder<ProductOptionValue> builder)
    {
        builder.ToTable("ProductOptionValues");
        builder.HasKey(value => value.Id);
        builder.Property(value => value.Id).ValueGeneratedNever();
        builder.Property(value => value.Value).HasMaxLength(ProductOptionValue.MaxValueLength).IsRequired();
        builder.Property<string>("NormalizedValue").HasMaxLength(ProductOptionValue.MaxValueLength)
            .HasComputedColumnSql("upper(btrim(\"Value\"))", stored: true);
        builder.HasIndex("ProductOptionId", "NormalizedValue").IsUnique();
    }
}

public sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants", table => table.HasCheckConstraint("CK_ProductVariants_Stock", "\"StockQuantity\" >= 0"));
        builder.HasKey(variant => variant.Id);
        builder.Property(variant => variant.Id).ValueGeneratedNever();
        builder.Property(variant => variant.Sku).HasMaxLength(ProductVariant.MaxSkuLength).IsRequired();
        builder.Property<string>("NormalizedSku").HasMaxLength(ProductVariant.MaxSkuLength)
            .HasComputedColumnSql("upper(btrim(\"Sku\"))", stored: true);
        builder.HasIndex("NormalizedSku").IsUnique();
        builder.Property<uint>("Version").IsRowVersion();
        builder.Property(variant => variant.CombinationKey).HasMaxLength(329).IsRequired();
        builder.HasIndex(variant => new { variant.ProductId, variant.CombinationKey }).IsUnique();
        builder.HasMany(variant => variant.Prices).WithOne().HasForeignKey(price => price.ProductVariantId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(variant => variant.OptionValues).WithOne().HasForeignKey(value => value.ProductVariantId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(variant => variant.Prices).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(variant => variant.OptionValues).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ProductVariantPriceConfiguration : IEntityTypeConfiguration<ProductVariantPrice>
{
    public void Configure(EntityTypeBuilder<ProductVariantPrice> builder)
    {
        builder.ToTable("ProductVariantPrices", table =>
        {
            table.HasCheckConstraint("CK_ProductVariantPrices_Amount", "\"Amount\" >= 0");
            table.HasCheckConstraint("CK_ProductVariantPrices_Currency", "\"Currency\" IN ('TRY', 'USD')");
        });
        builder.HasKey(price => price.Id);
        builder.Property(price => price.Id).ValueGeneratedNever();
        builder.Property(price => price.Currency).HasMaxLength(3).IsRequired();
        builder.Property(price => price.Amount).HasPrecision(18, 2);
        builder.HasIndex(price => new { price.ProductVariantId, price.Currency }).IsUnique();
    }
}

public sealed class ProductVariantOptionValueConfiguration : IEntityTypeConfiguration<ProductVariantOptionValue>
{
    public void Configure(EntityTypeBuilder<ProductVariantOptionValue> builder)
    {
        builder.ToTable("ProductVariantOptionValues");
        builder.HasKey(value => new { value.ProductVariantId, value.ProductOptionValueId });
        builder.HasOne<ProductOptionValue>().WithMany().HasForeignKey(value => value.ProductOptionValueId).OnDelete(DeleteBehavior.Restrict);
    }
}
