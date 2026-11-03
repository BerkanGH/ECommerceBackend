using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using MediatR;

namespace ECommerce.Application.Categories;

public record CategoryDto(Guid Id, string Name, string? Description);
public record CreateCategoryCommand(string Name, string? Description) : IRequest<CategoryDto>;
public record UpdateCategoryCommand(Guid Id, string Name, string? Description) : IRequest<CategoryDto>;
public record ListCategoriesQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<CategoryDto>>;

public sealed class CreateCategoryHandler(ICategoryRepository categories, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = new Domain.Entities.Category(request.Name, request.Description);
        categories.Add(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new CategoryDto(category.Id, category.Name, category.Description);
    }
}

public sealed class UpdateCategoryHandler(ICategoryRepository categories, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await categories.GetAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Category not found.");
        category.UpdateDetails(request.Name, request.Description);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new CategoryDto(category.Id, category.Name, category.Description);
    }
}

public sealed class ListCategoriesHandler(ICategoryRepository categories)
    : IRequestHandler<ListCategoriesQuery, PagedResult<CategoryDto>>
{
    public Task<PagedResult<CategoryDto>> Handle(ListCategoriesQuery request, CancellationToken cancellationToken)
    {
        Pagination.Validate(request.Page, request.PageSize);
        return categories.ListAsync(request.Page, request.PageSize, cancellationToken);
    }
}
