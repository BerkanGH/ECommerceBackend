using System.ComponentModel.DataAnnotations;

namespace ECommerce.Api.Contracts;

public record CategoryRequest(
    [Required, StringLength(100)] string Name,
    [StringLength(1000)] string? Description);

public record ProductRequest(
    Guid CategoryId,
    [Required, StringLength(100)] string Name,
    [StringLength(1000)] string? Description,
    [StringLength(2048)] string? ImageUrl = null);

public record OptionRequest(
    [Required, StringLength(100)] string Name,
    [Required, MinLength(1), MaxLength(100)] string[] Values);

public record OptionValueRequest([Required, StringLength(100)] string Value);

public record VariantRequest(
    [Required, StringLength(100)] string Sku,
    [Range(0, int.MaxValue)] int StockQuantity,
    [Required, MaxLength(10)] Guid[] OptionValueIds,
    [Required, MinLength(1), MaxLength(2)] PriceRequest[] Prices);

public record PriceRequest([Required, StringLength(3)] string Currency, decimal Amount);
public record StockRequest([Range(0, int.MaxValue)] int StockQuantity);
public record ProductAvailabilityRequest([Required] bool? IsActive);
