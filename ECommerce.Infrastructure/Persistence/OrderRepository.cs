using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Orders;
using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence;

public sealed class OrderRepository(ECommerceDbContext context) : IOrderRepository
{
    public async Task LockCustomerAsync(Guid userId, CancellationToken cancellationToken)
    {
        RequireTransaction();
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({userId.ToString()}, 0))", cancellationToken);
    }

    public Task<Order?> FindByKeyAsync(Guid userId, Guid idempotencyKey, CancellationToken cancellationToken) =>
        context.Orders.AsNoTracking().Include(order => order.Items)
            .SingleOrDefaultAsync(order => order.UserId == userId && order.IdempotencyKey == idempotencyKey, cancellationToken);

    public Task<Cart?> GetCartForCheckoutAsync(Guid userId, CancellationToken cancellationToken)
    {
        RequireTransaction();
        return context.Carts.FromSqlInterpolated(
                $"SELECT * FROM \"Carts\" WHERE \"UserId\" = {userId} FOR UPDATE")
            .Include(cart => cart.Items).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, ProductVariant>> LockVariantsAsync(
        IReadOnlyCollection<Guid> variantIds, CancellationToken cancellationToken)
    {
        RequireTransaction();
        var ids = variantIds.Distinct().ToArray();
        var productIds = await context.ProductVariants.AsNoTracking().Where(variant => ids.Contains(variant.Id))
            .Select(variant => variant.ProductId).Distinct().ToArrayAsync(cancellationToken);
        await context.Products.FromSqlInterpolated(
            $"SELECT * FROM \"Products\" WHERE \"Id\" = ANY({productIds}) ORDER BY \"Id\" FOR UPDATE").ToArrayAsync(cancellationToken);
        var variants = await context.ProductVariants.FromSqlInterpolated(
            $"SELECT * FROM \"ProductVariants\" WHERE \"Id\" = ANY({ids}) ORDER BY \"Id\" FOR UPDATE").ToArrayAsync(cancellationToken);
        return variants.ToDictionary(variant => variant.Id);
    }

    public Task<Order?> GetAsync(Guid id, Guid? userId, bool forUpdate, CancellationToken cancellationToken)
    {
        IQueryable<Order> query;
        if (forUpdate)
        {
            RequireTransaction();
            query = userId.HasValue
                ? context.Orders.FromSqlInterpolated($"SELECT * FROM \"Orders\" WHERE \"Id\" = {id} AND \"UserId\" = {userId.Value} FOR UPDATE")
                : context.Orders.FromSqlInterpolated($"SELECT * FROM \"Orders\" WHERE \"Id\" = {id} FOR UPDATE");
        }
        else
        {
            query = context.Orders.AsNoTracking().Where(order => order.Id == id);
            if (userId.HasValue)
                query = query.Where(order => order.UserId == userId.Value);
        }
        return query.Include(order => order.Items).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResult<OrderSummaryDto>> ListAsync(Guid? userId, OrderStatus? status,
        int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = context.Orders.AsNoTracking();
        if (userId.HasValue)
            query = query.Where(order => order.UserId == userId.Value);
        if (status.HasValue)
            query = query.Where(order => order.Status == status.Value);
        var count = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(order => order.CreatedAtUtc).ThenBy(order => order.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(order => new { order.Id, order.UserId, order.Currency, order.TotalAmount, order.Status, order.CreatedAtUtc })
            .ToArrayAsync(cancellationToken);
        var items = rows.Select(order => new OrderSummaryDto(order.Id, order.UserId, order.Currency,
            order.TotalAmount, order.Status.ToString(), order.CreatedAtUtc)).ToArray();
        return new PagedResult<OrderSummaryDto>(items, count, page, pageSize);
    }

    public void Add(Order order) => context.Orders.Add(order);

    private void RequireTransaction()
    {
        if (context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Order mutations require a transaction.");
    }
}
