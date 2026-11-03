using ECommerce.Application.Abstractions;
using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence;

public sealed class CartRepository(ECommerceDbContext context) : ICartRepository
{
    public Task<Cart?> GetByUserAsync(Guid userId, bool tracking, CancellationToken cancellationToken)
    {
        IQueryable<Cart> query = context.Carts;
        if (!tracking)
            query = query.AsNoTracking();
        return query.Include(cart => cart.Items)
            .SingleOrDefaultAsync(cart => cart.UserId == userId, cancellationToken);
    }

    public void Add(Cart cart) => context.Carts.Add(cart);
}
