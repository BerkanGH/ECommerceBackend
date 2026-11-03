using ECommerce.Domain.Entities;

namespace ECommerce.Application.Orders;

public record AddressDto(string RecipientName, string Phone, string CountryCode, string City, string PostalCode, string AddressLine);
public record OrderLineDto(Guid ProductVariantId, string ProductName, string Sku, string OptionsSummary,
    int Quantity, decimal UnitPrice, decimal LineTotal);
public record OrderSummaryDto(Guid Id, Guid UserId, string Currency, decimal TotalAmount, string Status, DateTimeOffset CreatedAtUtc);
public record OrderDto(Guid Id, Guid UserId, string Currency, decimal TotalAmount, string Status,
    DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, AddressDto ShippingAddress, IReadOnlyList<OrderLineDto> Items)
{
    public static OrderDto From(Order order) => new(order.Id, order.UserId, order.Currency, order.TotalAmount,
        order.Status.ToString(), order.CreatedAtUtc, order.UpdatedAtUtc,
        new AddressDto(order.ShippingAddress.RecipientName, order.ShippingAddress.Phone, order.ShippingAddress.CountryCode,
            order.ShippingAddress.City, order.ShippingAddress.PostalCode, order.ShippingAddress.AddressLine),
        order.Items.OrderBy(item => item.Sku).ThenBy(item => item.Id)
            .Select(item => new OrderLineDto(item.ProductVariantId, item.ProductName, item.Sku,
                item.OptionsSummary, item.Quantity, item.UnitPrice, item.LineTotal)).ToArray());
}
