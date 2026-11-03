using ECommerce.Domain.Entities;

namespace ECommerce.Application.Abstractions;

public interface ICartRepository
{
    Task<Cart?> GetByUserAsync(Guid userId, bool tracking, CancellationToken cancellationToken);
    void Add(Cart cart);
}
