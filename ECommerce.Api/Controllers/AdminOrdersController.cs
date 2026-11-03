using ECommerce.Application.Common;
using ECommerce.Application.Identity;
using ECommerce.Application.Orders;
using ECommerce.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Authorize(Roles = AccountRoles.Admin)]
[Route("api/admin/orders")]
public sealed class AdminOrdersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<OrderSummaryDto>> List(CancellationToken cancellationToken,
        OrderStatus? status = null, int page = 1, int pageSize = 20) =>
        sender.Send(new ListOrdersQuery(true, status, page, pageSize), cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<OrderDto> Get(Guid id, CancellationToken cancellationToken) =>
        sender.Send(new GetOrderQuery(id, true), cancellationToken);

    [HttpPost("{id:guid}/confirm")]
    public Task<OrderDto> Confirm(Guid id, CancellationToken cancellationToken) =>
        sender.Send(new AdvanceOrderCommand(id, OrderStatus.Confirmed), cancellationToken);

    [HttpPost("{id:guid}/ship")]
    public Task<OrderDto> Ship(Guid id, CancellationToken cancellationToken) =>
        sender.Send(new AdvanceOrderCommand(id, OrderStatus.Shipped), cancellationToken);

    [HttpPost("{id:guid}/deliver")]
    public Task<OrderDto> Deliver(Guid id, CancellationToken cancellationToken) =>
        sender.Send(new AdvanceOrderCommand(id, OrderStatus.Delivered), cancellationToken);

    [HttpPost("{id:guid}/cancel")]
    public Task<OrderDto> Cancel(Guid id, CancellationToken cancellationToken) =>
        sender.Send(new CancelOrderCommand(id, true), cancellationToken);
}
