using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;

namespace ECommerce.UnitTests;

public class CartTests
{
    [Fact]
    public void Repeated_variant_adds_quantity_to_existing_line()
    {
        var cart = new Cart(Guid.NewGuid(), "try");
        var variantId = Guid.NewGuid();
        cart.AddItem(variantId, 1);
        cart.AddItem(variantId, 2);
        Assert.Equal(3, Assert.Single(cart.Items).Quantity);
        cart.SetQuantity(variantId, 2);
        Assert.Equal(2, Assert.Single(cart.Items).Quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1000)]
    public void Invalid_quantity_does_not_create_a_line(int quantity)
    {
        var cart = new Cart(Guid.NewGuid(), "TRY");
        Assert.Throws<ArgumentOutOfRangeException>(() => cart.AddItem(Guid.NewGuid(), quantity));
        Assert.Empty(cart.Items);
    }

    [Fact]
    public void Quantity_limit_applies_to_merged_items()
    {
        var cart = new Cart(Guid.NewGuid(), "TRY");
        var variantId = Guid.NewGuid();
        cart.AddItem(variantId, 999);
        Assert.Throws<ArgumentOutOfRangeException>(() => cart.AddItem(variantId, 1));
        Assert.Equal(999, Assert.Single(cart.Items).Quantity);
    }

    [Fact]
    public void Currency_change_requires_empty_cart()
    {
        var cart = new Cart(Guid.NewGuid(), "TRY");
        cart.AddItem(Guid.NewGuid(), 1);
        Assert.Throws<DomainRuleException>(() => cart.ChangeCurrency("USD"));
        Assert.Equal("TRY", cart.Currency);
        cart.Clear();
        cart.ChangeCurrency("usd");
        Assert.Equal("USD", cart.Currency);
        Assert.Throws<ArgumentException>(() => cart.ChangeCurrency("EUR"));
    }

    [Fact]
    public void Line_limit_cannot_be_bypassed_but_existing_line_can_increase()
    {
        var cart = new Cart(Guid.NewGuid(), "TRY");
        for (var index = 0; index < Cart.MaxItems; index++)
            cart.AddItem(Guid.NewGuid(), 1);
        Assert.Throws<DomainRuleException>(() => cart.AddItem(Guid.NewGuid(), 1));
        cart.AddItem(cart.Items.First().ProductVariantId, 1);
        Assert.Equal(Cart.MaxItems, cart.Items.Count);
        Assert.Equal(2, cart.Items.First().Quantity);
    }
}
