using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;

namespace ECommerce.UnitTests;

public class OrderTests
{
    private static Order CreateOrder() => new(Guid.NewGuid(), Guid.NewGuid(), new string('A', 64), "TRY",
        new ShippingAddress("Recipient", "555", "TR", "Istanbul", "34000", "Street 1"),
        [new OrderLineDraft(Guid.NewGuid(), "Book", "BOOK", "", 2, 10.25m)], DateTimeOffset.UtcNow);

    [Fact]
    public void Order_copies_lines_and_calculates_decimal_total()
    {
        var order = CreateOrder();
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(20.50m, order.TotalAmount);
        Assert.Equal(20.50m, Assert.Single(order.Items).LineTotal);
        Assert.Throws<NotSupportedException>(() => ((ICollection<OrderItem>)order.Items).Clear());
    }

    [Fact]
    public void Cancellation_signals_stock_return_only_once()
    {
        var order = CreateOrder();
        order.Advance(OrderStatus.Confirmed, DateTimeOffset.UtcNow);
        Assert.True(order.Cancel(DateTimeOffset.UtcNow));
        Assert.False(order.Cancel(DateTimeOffset.UtcNow));
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Throws<DomainRuleException>(() => order.Advance(OrderStatus.Confirmed, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Delivery_requires_confirmation_and_shipping()
    {
        var order = CreateOrder();
        Assert.Throws<DomainRuleException>(() => order.Advance(OrderStatus.Shipped, DateTimeOffset.UtcNow));
        order.Advance(OrderStatus.Confirmed, DateTimeOffset.UtcNow);
        order.Advance(OrderStatus.Shipped, DateTimeOffset.UtcNow);
        Assert.Throws<DomainRuleException>(() => order.Cancel(DateTimeOffset.UtcNow));
        order.Advance(OrderStatus.Delivered, DateTimeOffset.UtcNow);
        Assert.Throws<DomainRuleException>(() => order.Cancel(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Order_requires_nonempty_lines_and_idempotency_key()
    {
        var address = new ShippingAddress("Recipient", "555", "TR", "City", "34000", "Street");
        var line = new OrderLineDraft(Guid.NewGuid(), "Book", "BOOK", "", 1, 10m);
        Assert.Throws<ArgumentException>(() => new Order(Guid.NewGuid(), Guid.Empty, new string('A', 64), "TRY", address, [line], DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => new Order(Guid.NewGuid(), Guid.NewGuid(), new string('A', 64), "TRY", address, [], DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => new Order(Guid.NewGuid(), Guid.NewGuid(), new string('A', 64), "TRY", address, [line, line], DateTimeOffset.UtcNow));
    }
}
