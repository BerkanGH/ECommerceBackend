using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.Carts;

public record GetCartQuery : IRequest<CartDto>;
public record AddCartItemCommand(Guid ProductVariantId, int Quantity, string Currency) : IRequest<CartDto>;
public record SetCartItemQuantityCommand(Guid ProductVariantId, int Quantity) : IRequest<CartDto>;
public record SetCartCurrencyCommand(string Currency) : IRequest<CartDto>;
public record RemoveCartItemCommand(Guid ProductVariantId) : IRequest;
public record ClearCartCommand : IRequest;

public sealed class GetCartHandler(ICartRepository carts, ICurrentUser currentUser, CartViewBuilder views)
    : IRequestHandler<GetCartQuery, CartDto>
{
    public async Task<CartDto> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var cart = await carts.GetByUserAsync(userId, false, cancellationToken);
        return await views.BuildAsync(cart, cancellationToken);
    }
}

public sealed class AddCartItemHandler(ICartRepository carts, ICartCatalogReader catalog,
    ICurrentUser currentUser, IUnitOfWork unitOfWork, CartViewBuilder views)
    : IRequestHandler<AddCartItemCommand, CartDto>
{
    public async Task<CartDto> Handle(AddCartItemCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var cart = await carts.GetByUserAsync(userId, true, cancellationToken);
        if (cart is null)
        {
            cart = new Cart(userId, request.Currency);
            carts.Add(cart);
        }
        cart.ChangeCurrency(request.Currency);
        var item = cart.AddItem(request.ProductVariantId, request.Quantity);
        var variants = await catalog.GetVariantsAsync([request.ProductVariantId], cancellationToken);
        if (!variants.TryGetValue(request.ProductVariantId, out var variant))
            throw new NotFoundException("Variant not found.");
        CartAvailability.EnsureAvailable(variant, cart.Currency, item.Quantity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await views.BuildAsync(cart, cancellationToken);
    }

}

public sealed class SetCartItemQuantityHandler(ICartRepository carts, ICartCatalogReader catalog,
    ICurrentUser currentUser, IUnitOfWork unitOfWork, CartViewBuilder views)
    : IRequestHandler<SetCartItemQuantityCommand, CartDto>
{
    public async Task<CartDto> Handle(SetCartItemQuantityCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var cart = await carts.GetByUserAsync(userId, true, cancellationToken);
        if (cart is null || !cart.Items.Any(item => item.ProductVariantId == request.ProductVariantId))
            throw new NotFoundException("Variant is not in your cart.");

        var item = cart.SetQuantity(request.ProductVariantId, request.Quantity);
        var variants = await catalog.GetVariantsAsync([request.ProductVariantId], cancellationToken);
        if (!variants.TryGetValue(request.ProductVariantId, out var variant))
            throw new NotFoundException("Variant not found.");
        CartAvailability.EnsureAvailable(variant, cart.Currency, item.Quantity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await views.BuildAsync(cart, cancellationToken);
    }
}

public sealed class SetCartCurrencyHandler(ICartRepository carts, ICurrentUser currentUser,
    IUnitOfWork unitOfWork, CartViewBuilder views) : IRequestHandler<SetCartCurrencyCommand, CartDto>
{
    public async Task<CartDto> Handle(SetCartCurrencyCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var cart = await carts.GetByUserAsync(userId, true, cancellationToken);
        if (cart is null)
        {
            cart = new Cart(userId, request.Currency);
            carts.Add(cart);
        }
        else
            cart.ChangeCurrency(request.Currency);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await views.BuildAsync(cart, cancellationToken);
    }
}

public sealed class RemoveCartItemHandler(ICartRepository carts, ICurrentUser currentUser, IUnitOfWork unitOfWork)
    : IRequestHandler<RemoveCartItemCommand>
{
    public async Task Handle(RemoveCartItemCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var cart = await carts.GetByUserAsync(userId, true, cancellationToken);
        if (cart is null)
            return;
        cart.RemoveItem(request.ProductVariantId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed class ClearCartHandler(ICartRepository carts, ICurrentUser currentUser, IUnitOfWork unitOfWork)
    : IRequestHandler<ClearCartCommand>
{
    public async Task Handle(ClearCartCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var cart = await carts.GetByUserAsync(userId, true, cancellationToken);
        if (cart is null)
            return;
        cart.Clear();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
