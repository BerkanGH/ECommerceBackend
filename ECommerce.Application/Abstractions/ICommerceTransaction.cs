namespace ECommerce.Application.Abstractions;

public interface ICommerceTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
}
