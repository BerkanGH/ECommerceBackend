using ECommerce.Domain.Entities;

namespace ECommerce.Application.Products;

public record ProductSummaryDto(Guid Id, Guid CategoryId, string Name, string? Description);
public record OptionValueDto(Guid Id, string Value);
public record OptionDto(Guid Id, string Name, IReadOnlyList<OptionValueDto> Values);
public record PriceDto(string Currency, decimal Amount);
public record VariantDto(Guid Id, string Sku, int StockQuantity, IReadOnlyList<Guid> OptionValueIds, IReadOnlyList<PriceDto> Prices);
public record ProductDto(Guid Id, Guid CategoryId, string Name, string? Description,
    IReadOnlyList<OptionDto> Options, IReadOnlyList<VariantDto> Variants)
{
    public static ProductDto From(Product product) => new(
        product.Id, product.CategoryId, product.Name, product.Description,
        product.Options.OrderBy(option => option.Name).ThenBy(option => option.Id)
            .Select(option => new OptionDto(option.Id, option.Name,
                option.Values.OrderBy(value => value.Value).ThenBy(value => value.Id)
                    .Select(value => new OptionValueDto(value.Id, value.Value)).ToArray())).ToArray(),
        product.Variants.OrderBy(variant => variant.Sku).ThenBy(variant => variant.Id)
            .Select(variant => new VariantDto(variant.Id, variant.Sku, variant.StockQuantity,
                variant.OptionValues.Select(value => value.ProductOptionValueId).Order().ToArray(),
                variant.Prices.OrderBy(price => price.Currency)
                    .Select(price => new PriceDto(price.Currency, price.Amount)).ToArray())).ToArray());
}
