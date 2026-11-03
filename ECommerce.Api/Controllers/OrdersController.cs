using ECommerce.Api.Contracts;
using ECommerce.Application.Common;
using ECommerce.Application.Identity;
using ECommerce.Application.Orders;
using ECommerce.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Authorize(Roles = AccountRoles.Customer)]
[Route("api/orders")]
public sealed class OrdersController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<OrderDto>> Checkout(CheckoutRequest request,
        [FromHeader(Name = "Idempotency-Key")] Guid idempotencyKey, CancellationToken cancellationToken)
    {
        var address = request.ShippingAddress;
        var order = await sender.Send(new CheckoutCommand(idempotencyKey, request.ExpectedCurrency, request.ExpectedTotal,
            new AddressDto(address.RecipientName, address.Phone, address.CountryCode,
                address.City, address.PostalCode, address.AddressLine)), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = order.Id }, order);
    }

    [HttpGet]
    public Task<PagedResult<OrderSummaryDto>> List(CancellationToken cancellationToken,
        OrderStatus? status = null, int page = 1, int pageSize = 20) =>
        sender.Send(new ListOrdersQuery(Status: status, Page: page, PageSize: pageSize), cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<OrderDto> Get(Guid id, CancellationToken cancellationToken) =>
        sender.Send(new GetOrderQuery(id), cancellationToken);

    [HttpPost("{id:guid}/cancel")]
    public Task<OrderDto> Cancel(Guid id, CancellationToken cancellationToken) =>
        sender.Send(new CancelOrderCommand(id), cancellationToken);
}
