using ECommerce.Api.Contracts;
using ECommerce.Application.Common;
using ECommerce.Application.Identity;
using ECommerce.Application.Products;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Authorize(Roles = AccountRoles.Admin)]
[Route("api/admin/products")]
public sealed class AdminProductsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<ProductSummaryDto>> List(CancellationToken cancellationToken,
        Guid? categoryId = null, string? search = null, int page = 1, int pageSize = 20) =>
        sender.Send(new ListProductsQuery(categoryId, search, page, pageSize, true), cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<ProductDto> Get(Guid id, CancellationToken cancellationToken) =>
        sender.Send(new GetProductQuery(id, true), cancellationToken);

    [HttpPut("{id:guid}/availability")]
    public Task<ProductDto> SetAvailability(Guid id, ProductAvailabilityRequest request, CancellationToken cancellationToken) =>
        sender.Send(new SetProductAvailabilityCommand(id, request.IsActive!.Value), cancellationToken);
}
