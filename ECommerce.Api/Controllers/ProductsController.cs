using ECommerce.Api.Contracts;
using ECommerce.Api.Filters;
using ECommerce.Application.Common;
using ECommerce.Application.Products;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<ProductSummaryDto>> List(CancellationToken cancellationToken,
        Guid? categoryId = null, string? search = null, int page = 1, int pageSize = 20) =>
        sender.Send(new ListProductsQuery(categoryId, search, page, pageSize), cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<ProductDto> Get(Guid id, CancellationToken cancellationToken) =>
        sender.Send(new GetProductQuery(id), cancellationToken);

    [HttpPost]
    [DevelopmentCatalogWrite]
    public async Task<ActionResult<ProductDto>> Create(ProductRequest request, CancellationToken cancellationToken)
    {
        var product = await sender.Send(new CreateProductCommand(request.CategoryId, request.Name, request.Description), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
    }

    [HttpPut("{id:guid}")]
    [DevelopmentCatalogWrite]
    public Task<ProductDto> Update(Guid id, ProductRequest request, CancellationToken cancellationToken) =>
        sender.Send(new UpdateProductCommand(id, request.CategoryId, request.Name, request.Description), cancellationToken);

    [HttpPost("{id:guid}/options")]
    [DevelopmentCatalogWrite]
    public Task<ProductDto> AddOption(Guid id, OptionRequest request, CancellationToken cancellationToken) =>
        sender.Send(new AddProductOptionCommand(id, request.Name, request.Values), cancellationToken);

    [HttpPost("{id:guid}/options/{optionId:guid}/values")]
    [DevelopmentCatalogWrite]
    public Task<ProductDto> AddValue(Guid id, Guid optionId, OptionValueRequest request, CancellationToken cancellationToken) =>
        sender.Send(new AddProductOptionValueCommand(id, optionId, request.Value), cancellationToken);

    [HttpPost("{id:guid}/variants")]
    [DevelopmentCatalogWrite]
    public Task<ProductDto> AddVariant(Guid id, VariantRequest request, CancellationToken cancellationToken) =>
        sender.Send(new AddProductVariantCommand(id, request.Sku, request.StockQuantity, request.OptionValueIds, request.Prices), cancellationToken);

    [HttpPut("{id:guid}/variants/{variantId:guid}/price")]
    [DevelopmentCatalogWrite]
    public Task<ProductDto> SetPrice(Guid id, Guid variantId, PriceRequest request, CancellationToken cancellationToken) =>
        sender.Send(new SetVariantPriceCommand(id, variantId, request.Currency, request.Amount), cancellationToken);

    [HttpPut("{id:guid}/variants/{variantId:guid}/stock")]
    [DevelopmentCatalogWrite]
    public Task<ProductDto> SetStock(Guid id, Guid variantId, StockRequest request, CancellationToken cancellationToken) =>
        sender.Send(new SetVariantStockCommand(id, variantId, request.StockQuantity), cancellationToken);
}
