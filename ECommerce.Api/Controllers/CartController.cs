using ECommerce.Api.Contracts;
using ECommerce.Application.Carts;
using ECommerce.Application.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Authorize(Roles = AccountRoles.Customer)]
[Route("api/cart")]
public sealed class CartController(ISender sender) : ControllerBase
{
    [HttpGet]
    public Task<CartDto> Get(CancellationToken cancellationToken) =>
        sender.Send(new GetCartQuery(), cancellationToken);

    [HttpPost("items")]
    public Task<CartDto> AddItem(AddCartItemRequest request, CancellationToken cancellationToken) =>
        sender.Send(new AddCartItemCommand(request.ProductVariantId, request.Quantity, request.Currency), cancellationToken);

    [HttpPut("items/{variantId:guid}")]
    public Task<CartDto> SetQuantity(Guid variantId, CartQuantityRequest request, CancellationToken cancellationToken) =>
        sender.Send(new SetCartItemQuantityCommand(variantId, request.Quantity), cancellationToken);

    [HttpPut("currency")]
    public Task<CartDto> SetCurrency(CartCurrencyRequest request, CancellationToken cancellationToken) =>
        sender.Send(new SetCartCurrencyCommand(request.Currency), cancellationToken);

    [HttpDelete("items/{variantId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid variantId, CancellationToken cancellationToken)
    {
        await sender.Send(new RemoveCartItemCommand(variantId), cancellationToken);
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> Clear(CancellationToken cancellationToken)
    {
        await sender.Send(new ClearCartCommand(), cancellationToken);
        return NoContent();
    }
}
