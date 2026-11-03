using ECommerce.Api.Contracts;
using ECommerce.Application.Identity;
using ECommerce.Application.Common;
using ECommerce.Application.Products;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

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
    [Authorize(Roles = AccountRoles.Admin)]
    public async Task<ActionResult<ProductDto>> Create(ProductRequest request, CancellationToken cancellationToken)
    {
        var product = await sender.Send(new CreateProductCommand(request.CategoryId, request.Name, request.Description), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = AccountRoles.Admin)]
    public Task<ProductDto> Update(Guid id, ProductRequest request, CancellationToken cancellationToken) =>
        sender.Send(new UpdateProductCommand(id, request.CategoryId, request.Name, request.Description), cancellationToken);

    [HttpPost("{id:guid}/options")]
    [Authorize(Roles = AccountRoles.Admin)]
    public Task<ProductDto> AddOption(Guid id, OptionRequest request, CancellationToken cancellationToken) =>
        sender.Send(new AddProductOptionCommand(id, request.Name, request.Values), cancellationToken);

    [HttpPost("{id:guid}/options/{optionId:guid}/values")]
    [Authorize(Roles = AccountRoles.Admin)]
    public Task<ProductDto> AddValue(Guid id, Guid optionId, OptionValueRequest request, CancellationToken cancellationToken) =>
        sender.Send(new AddProductOptionValueCommand(id, optionId, request.Value), cancellationToken);

    [HttpPost("{id:guid}/variants")]
    [Authorize(Roles = AccountRoles.Admin)]
    public Task<ProductDto> AddVariant(Guid id, VariantRequest request, CancellationToken cancellationToken) =>
        sender.Send(new AddProductVariantCommand(id, request.Sku, request.StockQuantity, request.OptionValueIds, request.Prices), cancellationToken);

    [HttpPut("{id:guid}/variants/{variantId:guid}/price")]
    [Authorize(Roles = AccountRoles.Admin)]
    public Task<ProductDto> SetPrice(Guid id, Guid variantId, PriceRequest request, CancellationToken cancellationToken) =>
        sender.Send(new SetVariantPriceCommand(id, variantId, request.Currency, request.Amount), cancellationToken);

    [HttpPut("{id:guid}/variants/{variantId:guid}/stock")]
    [Authorize(Roles = AccountRoles.Admin)]
    public Task<ProductDto> SetStock(Guid id, Guid variantId, StockRequest request, CancellationToken cancellationToken) =>
        sender.Send(new SetVariantStockCommand(id, variantId, request.StockQuantity), cancellationToken);
}
