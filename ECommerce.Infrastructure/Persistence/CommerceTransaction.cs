using ECommerce.Application.Abstractions;
using Microsoft.EntityFrameworkCore.Storage;

namespace ECommerce.Infrastructure.Persistence;

internal sealed class CommerceTransaction(IDbContextTransaction transaction) : ICommerceTransaction
{
    public Task CommitAsync(CancellationToken cancellationToken) => transaction.CommitAsync(cancellationToken);
    public ValueTask DisposeAsync() => transaction.DisposeAsync();
}
