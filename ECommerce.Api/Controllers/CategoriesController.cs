using ECommerce.Api.Contracts;
using ECommerce.Application.Identity;
using ECommerce.Application.Categories;
using ECommerce.Application.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<CategoryDto>> List(CancellationToken cancellationToken, int page = 1, int pageSize = 20) =>
        sender.Send(new ListCategoriesQuery(page, pageSize), cancellationToken);

    [HttpPost]
    [Authorize(Roles = AccountRoles.Admin)]
    public async Task<ActionResult<CategoryDto>> Create(CategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await sender.Send(new CreateCategoryCommand(request.Name, request.Description), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, category);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = AccountRoles.Admin)]
    public Task<CategoryDto> Update(Guid id, CategoryRequest request, CancellationToken cancellationToken) =>
        sender.Send(new UpdateCategoryCommand(id, request.Name, request.Description), cancellationToken);
}
