namespace ECommerce.Application.Abstractions;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<ICommerceTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
}
