using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Identity;
using MediatR;

namespace ECommerce.Application.Products;

public record GetProductQuery(Guid Id, bool IncludeInactive = false) : IRequest<ProductDto>;
public record ListProductsQuery(Guid? CategoryId = null, string? Search = null, int Page = 1, int PageSize = 20, bool IncludeInactive = false)
    : IRequest<PagedResult<ProductSummaryDto>>;

public sealed class GetProductHandler(IProductRepository products, ICurrentUser currentUser) : IRequestHandler<GetProductQuery, ProductDto>
{
    public async Task<ProductDto> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        if (request.IncludeInactive && !currentUser.IsInRole(AccountRoles.Admin))
            throw new ForbiddenException();
        var product = await products.GetAsync(request.Id, false, cancellationToken)
            ?? throw new NotFoundException("Product not found.");
        if (!product.IsActive && !request.IncludeInactive)
            throw new NotFoundException("Product not found.");
        return ProductDto.From(product);
    }
}

public sealed class ListProductsHandler(IProductRepository products, ICurrentUser currentUser)
    : IRequestHandler<ListProductsQuery, PagedResult<ProductSummaryDto>>
{
    public Task<PagedResult<ProductSummaryDto>> Handle(ListProductsQuery request, CancellationToken cancellationToken)
    {
        if (request.IncludeInactive && !currentUser.IsInRole(AccountRoles.Admin))
            throw new ForbiddenException();
        Pagination.Validate(request.Page, request.PageSize);
        if (request.Search?.Length > 100)
            throw new ArgumentException("Search cannot exceed 100 characters.");
        return products.ListAsync(request.CategoryId, request.Search, request.Page, request.PageSize, request.IncludeInactive, cancellationToken);
    }
}
