using ECommerce.Application.Carts;

namespace ECommerce.Application.Abstractions;

public interface ICartCatalogReader
{
    Task<IReadOnlyDictionary<Guid, CartVariantSnapshot>> GetVariantsAsync(
        IReadOnlyCollection<Guid> variantIds, CancellationToken cancellationToken);
}
