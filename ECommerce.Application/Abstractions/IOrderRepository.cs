using ECommerce.Application.Common;
using ECommerce.Application.Orders;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Abstractions;

public interface IOrderRepository
{
    Task LockCustomerAsync(Guid userId, CancellationToken cancellationToken);
    Task<Order?> FindByKeyAsync(Guid userId, Guid idempotencyKey, CancellationToken cancellationToken);
    Task<Cart?> GetCartForCheckoutAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, ProductVariant>> LockVariantsAsync(IReadOnlyCollection<Guid> variantIds, CancellationToken cancellationToken);
    Task<Order?> GetAsync(Guid id, Guid? userId, bool forUpdate, CancellationToken cancellationToken);
    Task<PagedResult<OrderSummaryDto>> ListAsync(Guid? userId, OrderStatus? status, int page, int pageSize, CancellationToken cancellationToken);
    void Add(Order order);
}
