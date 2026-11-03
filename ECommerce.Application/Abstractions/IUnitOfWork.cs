namespace ECommerce.Application.Abstractions;

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<ICommerceTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
}
