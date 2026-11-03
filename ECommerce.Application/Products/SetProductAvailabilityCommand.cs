using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using MediatR;

namespace ECommerce.Application.Products;

public record SetProductAvailabilityCommand(Guid Id, bool IsActive) : IRequest<ProductDto>;

public sealed class SetProductAvailabilityHandler(IProductRepository products, IUnitOfWork unitOfWork)
    : IRequestHandler<SetProductAvailabilityCommand, ProductDto>
{
    public async Task<ProductDto> Handle(SetProductAvailabilityCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetAsync(request.Id, true, cancellationToken)
            ?? throw new NotFoundException("Product not found.");
        product.SetAvailability(request.IsActive);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ProductDto.From(product);
    }
}
