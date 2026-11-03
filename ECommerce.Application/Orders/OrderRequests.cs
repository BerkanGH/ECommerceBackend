using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ECommerce.Application.Abstractions;
using ECommerce.Application.Carts;
using ECommerce.Application.Common;
using ECommerce.Application.Identity;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;
using MediatR;

namespace ECommerce.Application.Orders;

public record CheckoutCommand(Guid IdempotencyKey, string ExpectedCurrency, decimal ExpectedTotal, AddressDto ShippingAddress)
    : IRequest<OrderDto>;
public record GetOrderQuery(Guid Id, bool Administrative = false) : IRequest<OrderDto>;
public record ListOrdersQuery(bool Administrative = false, OrderStatus? Status = null, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<OrderSummaryDto>>;
public record CancelOrderCommand(Guid Id, bool Administrative = false) : IRequest<OrderDto>;
public record AdvanceOrderCommand(Guid Id, OrderStatus Status) : IRequest<OrderDto>;

internal static class OrderAccess
{
    public static Guid User(ICurrentUser currentUser, bool administrative)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        if (!currentUser.IsInRole(administrative ? AccountRoles.Admin : AccountRoles.Customer))
            throw new ForbiddenException();
        return userId;
    }
}

public sealed class CheckoutHandler(IOrderRepository orders, ICartCatalogReader catalog, ICurrentUser currentUser,
    IUnitOfWork unitOfWork, TimeProvider timeProvider) : IRequestHandler<CheckoutCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CheckoutCommand request, CancellationToken cancellationToken)
    {
        var userId = OrderAccess.User(currentUser, false);
        if (request.IdempotencyKey == Guid.Empty)
            throw new ArgumentException("A nonempty Idempotency-Key header is required.");
        if (request.ExpectedTotal < 0 || decimal.Round(request.ExpectedTotal, 2) != request.ExpectedTotal)
            throw new ArgumentException("Expected total must be nonnegative with at most two decimal places.");
        ArgumentNullException.ThrowIfNull(request.ShippingAddress);
        var currency = CurrencyCode.Normalize(request.ExpectedCurrency);
        var input = request.ShippingAddress;
        var address = new ShippingAddress(input.RecipientName, input.Phone, input.CountryCode, input.City, input.PostalCode, input.AddressLine);
        var canonical = JsonSerializer.Serialize(new { Currency = currency,
            ExpectedTotal = request.ExpectedTotal.ToString("0.00", CultureInfo.InvariantCulture), Address = address });
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        await orders.LockCustomerAsync(userId, cancellationToken);
        var previous = await orders.FindByKeyAsync(userId, request.IdempotencyKey, cancellationToken);
        if (previous is not null)
        {
            if (previous.RequestFingerprint != fingerprint)
                throw new DomainRuleException("Idempotency key was already used with a different request.");
            await transaction.CommitAsync(cancellationToken);
            return OrderDto.From(previous);
        }

        var cart = await orders.GetCartForCheckoutAsync(userId, cancellationToken);
        if (cart is null || cart.Items.Count == 0)
            throw new DomainRuleException("Your cart is empty.");
        if (cart.Currency != currency)
            throw new DomainRuleException("Cart currency changed. Reload the cart.");
        var ids = cart.Items.Select(item => item.ProductVariantId).ToArray();
        var variants = await orders.LockVariantsAsync(ids, cancellationToken);
        var snapshots = await catalog.GetVariantsAsync(ids, cancellationToken);
        var lines = new List<OrderLineDraft>();
        foreach (var item in cart.Items)
        {
            if (!snapshots.TryGetValue(item.ProductVariantId, out var snapshot) || !variants.ContainsKey(item.ProductVariantId))
                throw new DomainRuleException("A cart variant is no longer available.");
            CartAvailability.EnsureAvailable(snapshot, currency, item.Quantity);
            lines.Add(new OrderLineDraft(snapshot.Id, snapshot.ProductName, snapshot.Sku,
                string.Join("; ", snapshot.Options.Select(option => $"{option.Name}: {option.Value}")),
                item.Quantity, snapshot.Prices.Single(price => price.Currency == currency).Amount));
        }

        var order = new Order(userId, request.IdempotencyKey, fingerprint, currency, address, lines, timeProvider.GetUtcNow());
        if (order.TotalAmount != request.ExpectedTotal)
            throw new DomainRuleException("Prices changed. Reload the cart and confirm the current total.");
        foreach (var item in cart.Items)
            variants[item.ProductVariantId].DecreaseStock(item.Quantity);
        cart.Clear();
        orders.Add(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return OrderDto.From(order);
    }
}

public sealed class GetOrderHandler(IOrderRepository orders, ICurrentUser currentUser) : IRequestHandler<GetOrderQuery, OrderDto>
{
    public async Task<OrderDto> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        var userId = OrderAccess.User(currentUser, request.Administrative);
        var order = await orders.GetAsync(request.Id, request.Administrative ? null : userId, false, cancellationToken)
            ?? throw new NotFoundException("Order not found.");
        return OrderDto.From(order);
    }
}

public sealed class ListOrdersHandler(IOrderRepository orders, ICurrentUser currentUser)
    : IRequestHandler<ListOrdersQuery, PagedResult<OrderSummaryDto>>
{
    public Task<PagedResult<OrderSummaryDto>> Handle(ListOrdersQuery request, CancellationToken cancellationToken)
    {
        var userId = OrderAccess.User(currentUser, request.Administrative);
        Pagination.Validate(request.Page, request.PageSize);
        if (request.Status.HasValue && !Enum.IsDefined(request.Status.Value))
            throw new ArgumentException("Invalid order status.");
        return orders.ListAsync(request.Administrative ? null : userId, request.Status, request.Page, request.PageSize, cancellationToken);
    }
}

public sealed class CancelOrderHandler(IOrderRepository orders, ICurrentUser currentUser,
    IUnitOfWork unitOfWork, TimeProvider timeProvider) : IRequestHandler<CancelOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var userId = OrderAccess.User(currentUser, request.Administrative);
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var order = await orders.GetAsync(request.Id, request.Administrative ? null : userId, true, cancellationToken)
            ?? throw new NotFoundException("Order not found.");
        if (order.Cancel(timeProvider.GetUtcNow()))
        {
            var variants = await orders.LockVariantsAsync(order.Items.Select(item => item.ProductVariantId).ToArray(), cancellationToken);
            foreach (var item in order.Items)
            {
                if (!variants.TryGetValue(item.ProductVariantId, out var variant))
                    throw new DomainRuleException("Cannot restore stock for a missing variant.");
                variant.IncreaseStock(item.Quantity);
            }
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        return OrderDto.From(order);
    }
}

public sealed class AdvanceOrderHandler(IOrderRepository orders, ICurrentUser currentUser,
    IUnitOfWork unitOfWork, TimeProvider timeProvider) : IRequestHandler<AdvanceOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(AdvanceOrderCommand request, CancellationToken cancellationToken)
    {
        OrderAccess.User(currentUser, true);
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var order = await orders.GetAsync(request.Id, null, true, cancellationToken)
            ?? throw new NotFoundException("Order not found.");
        order.Advance(request.Status, timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return OrderDto.From(order);
    }
}
