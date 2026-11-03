namespace ECommerce.Application.Carts;

public record CartSelectionDto(string Name, string Value);
public record CartPriceSnapshot(string Currency, decimal Amount);
public record CartVariantSnapshot(Guid Id, Guid ProductId, string ProductName, string Sku,
    int StockQuantity, IReadOnlyList<CartSelectionDto> Options, IReadOnlyList<CartPriceSnapshot> Prices);
public record CartItemDto(Guid ProductVariantId, Guid? ProductId, string? ProductName, string? Sku,
    int Quantity, decimal? UnitPrice, decimal? LineTotal, int AvailableStock,
    IReadOnlyList<CartSelectionDto> Options, bool IsAvailable, string? Issue);
public record CartDto(Guid? Id, string Currency, IReadOnlyList<CartItemDto> Items, decimal? TotalAmount, bool CanCheckout);
