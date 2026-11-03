using ECommerce.Domain.Common;
using ECommerce.Domain.Exceptions;

namespace ECommerce.Domain.Entities;

public class Order
{
    private readonly List<OrderItem> _items = [];

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid IdempotencyKey { get; private set; }
    public string RequestFingerprint { get; private set; } = string.Empty;
    public string Currency { get; private set; } = string.Empty;
    public decimal TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public ShippingAddress ShippingAddress { get; private set; } = null!;
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order() { }

    public Order(Guid userId, Guid idempotencyKey, string requestFingerprint, string currency,
        ShippingAddress shippingAddress, IReadOnlyList<OrderLineDraft> lines, DateTimeOffset createdAtUtc)
    {
        if (userId == Guid.Empty || idempotencyKey == Guid.Empty)
            throw new ArgumentException("User and idempotency key are required.");
        ArgumentNullException.ThrowIfNull(shippingAddress);
        ArgumentNullException.ThrowIfNull(lines);
        if (lines.Count is < 1 or > Cart.MaxItems || lines.Any(line => line is null))
            throw new ArgumentException("An order requires between 1 and 100 lines.", nameof(lines));
        if (lines.Select(line => line.ProductVariantId).Distinct().Count() != lines.Count)
            throw new ArgumentException("Order variants cannot repeat.", nameof(lines));
        if (requestFingerprint is null || requestFingerprint.Length != 64 || !requestFingerprint.All(char.IsAsciiHexDigit))
            throw new ArgumentException("A SHA-256 fingerprint is required.", nameof(requestFingerprint));

        Id = Guid.NewGuid();
        UserId = userId;
        IdempotencyKey = idempotencyKey;
        RequestFingerprint = requestFingerprint;
        Currency = CurrencyCode.Normalize(currency);
        ShippingAddress = shippingAddress;
        CreatedAtUtc = createdAtUtc.ToUniversalTime();
        UpdatedAtUtc = CreatedAtUtc;
        Status = OrderStatus.Pending;
        foreach (var line in lines)
            _items.Add(new OrderItem(Id, line));
        TotalAmount = _items.Sum(item => item.LineTotal);
    }

    public void Advance(OrderStatus nextStatus, DateTimeOffset now)
    {
        if (Status == nextStatus)
            return;
        if ((Status, nextStatus) is not (
            (OrderStatus.Pending, OrderStatus.Confirmed) or
            (OrderStatus.Confirmed, OrderStatus.Shipped) or
            (OrderStatus.Shipped, OrderStatus.Delivered)))
            throw new DomainRuleException("This order status transition is not allowed.");
        Status = nextStatus;
        UpdatedAtUtc = now.ToUniversalTime();
    }

    public bool Cancel(DateTimeOffset now)
    {
        if (Status == OrderStatus.Cancelled)
            return false;
        if (Status is not (OrderStatus.Pending or OrderStatus.Confirmed))
            throw new DomainRuleException("Only pending or confirmed orders can be cancelled.");
        Status = OrderStatus.Cancelled;
        UpdatedAtUtc = now.ToUniversalTime();
        return true;
    }
}
