using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;

namespace ECommerce.UnitTests;

public class ProductTests
{
    [Fact]
    public void Constructor_trims_details_and_rejects_empty_category()
    {
        var product = new Product(Guid.NewGuid(), " Shirt ", " Cotton ");
        Assert.Equal("Shirt", product.Name);
        Assert.Equal("Cotton", product.Description);
        Assert.Throws<ArgumentException>(() => new Product(Guid.Empty, "Shirt"));
    }

    [Theory]
    [InlineData("http://example.com/shirt.jpg")]
    [InlineData("javascript:alert(1)")]
    [InlineData("/images/shirt.jpg")]
    [InlineData("not a url")]
    public void Image_url_must_be_absolute_https(string imageUrl)
    {
        var product = new Product(Guid.NewGuid(), "Shirt");
        Assert.Throws<ArgumentException>(() => product.SetImageUrl(imageUrl));
        Assert.Null(product.ImageUrl);
    }

    [Fact]
    public void Image_url_is_trimmed_and_can_be_cleared()
    {
        var product = new Product(Guid.NewGuid(), "Shirt");
        product.SetImageUrl(" https://images.example.com/shirt.jpg ");
        Assert.Equal("https://images.example.com/shirt.jpg", product.ImageUrl);
        product.SetImageUrl("  ");
        Assert.Null(product.ImageUrl);
    }

    [Fact]
    public void Variant_requires_exactly_one_value_from_each_own_option()
    {
        var product = new Product(Guid.NewGuid(), "Shirt");
        var color = product.AddOption("Color");
        var black = product.AddOptionValue(color.Id, "Black");
        var size = product.AddOption("Size");
        var medium = product.AddOptionValue(size.Id, "M");
        Assert.Throws<ArgumentException>(() => product.AddVariant("A", 1, [black.Id]));
        Assert.Throws<ArgumentException>(() => product.AddVariant("A", 1, [black.Id, Guid.NewGuid()]));
        Assert.Throws<ArgumentException>(() => product.AddVariant("A", 1, [black.Id, black.Id]));
        var variant = product.AddVariant("A", 1, [black.Id, medium.Id]);
        Assert.Equal(2, variant.OptionValues.Count);
        Assert.Throws<DomainRuleException>(() => product.AddVariant("B", 1, [medium.Id, black.Id]));
        Assert.Throws<DomainRuleException>(() => product.AddOption("Material"));
    }

    [Fact]
    public void Option_names_and_values_are_case_insensitively_unique()
    {
        var product = new Product(Guid.NewGuid(), "Shirt");
        var option = product.AddOption("Color");
        product.AddOptionValue(option.Id, "Black");
        Assert.Throws<DomainRuleException>(() => product.AddOption(" color "));
        Assert.Throws<DomainRuleException>(() => product.AddOptionValue(option.Id, " black "));
    }

    [Fact]
    public void Optionless_product_has_only_one_default_variant()
    {
        var product = new Product(Guid.NewGuid(), "Book");
        product.AddVariant("BOOK", 1, []);
        Assert.Throws<DomainRuleException>(() => product.AddVariant("BOOK2", 1, []));
    }

    [Theory]
    [InlineData("try", 100.25, "TRY")]
    [InlineData("usd", 2.50, "USD")]
    public void Prices_normalize_currency_and_update_without_duplicate(string currency, decimal amount, string expected)
    {
        var variant = new Product(Guid.NewGuid(), "Book").AddVariant("BOOK", 10, []);
        variant.SetPrice(currency, amount);
        variant.SetPrice(expected, amount + 1);
        var price = Assert.Single(variant.Prices);
        Assert.Equal(expected, price.Currency);
        Assert.Equal(amount + 1, price.Amount);
    }

    [Theory]
    [InlineData("TRY", -1)]
    [InlineData("USD", 1.001)]
    [InlineData("EUR", 1)]
    public void Invalid_prices_are_rejected(string currency, decimal amount)
    {
        var variant = new Product(Guid.NewGuid(), "Book").AddVariant("BOOK", 10, []);
        Assert.ThrowsAny<ArgumentException>(() => variant.SetPrice(currency, amount));
        Assert.Empty(variant.Prices);
    }

    [Fact]
    public void Inventory_deduction_is_checked_and_failed_changes_are_atomic()
    {
        var variant = new Product(Guid.NewGuid(), "Book").AddVariant("BOOK", 10, []);
        Assert.Throws<DomainRuleException>(() => variant.DecreaseStock(11));
        Assert.Equal(10, variant.StockQuantity);
        variant.DecreaseStock(3);
        Assert.Equal(7, variant.StockQuantity);
        variant.IncreaseStock(3);
        Assert.Equal(10, variant.StockQuantity);
        Assert.Throws<ArgumentOutOfRangeException>(() => variant.DecreaseStock(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => variant.UpdateStockQuantity(-1));
        variant.UpdateStockQuantity(int.MaxValue);
        Assert.Throws<DomainRuleException>(() => variant.IncreaseStock(1));
        Assert.Equal(int.MaxValue, variant.StockQuantity);
    }
}
