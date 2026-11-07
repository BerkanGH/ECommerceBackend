using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Domain.Exceptions;
using MediatR;

namespace ECommerce.Application.Products;

public record CreateProductCommand(Guid CategoryId, string Name, string? Description, string? ImageUrl) : IRequest<ProductDto>;
public record UpdateProductCommand(Guid Id, Guid CategoryId, string Name, string? Description, string? ImageUrl) : IRequest<ProductDto>;
public record AddProductOptionCommand(Guid ProductId, string Name, IReadOnlyList<string> Values) : IRequest<ProductDto>;
public record AddProductOptionValueCommand(Guid ProductId, Guid OptionId, string Value) : IRequest<ProductDto>;
public record AddProductVariantCommand(Guid ProductId, string Sku, int StockQuantity,
    IReadOnlyList<Guid> OptionValueIds, IReadOnlyList<PriceDto> Prices) : IRequest<ProductDto>;
public record SetVariantPriceCommand(Guid ProductId, Guid VariantId, string Currency, decimal Amount) : IRequest<ProductDto>;
public record SetVariantStockCommand(Guid ProductId, Guid VariantId, int StockQuantity) : IRequest<ProductDto>;

public sealed class CreateProductHandler(IProductRepository products, ICategoryRepository categories, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        if (!await categories.ExistsAsync(request.CategoryId, cancellationToken))
            throw new NotFoundException("Category not found.");
        var product = new Domain.Entities.Product(request.CategoryId, request.Name, request.Description);
        product.SetImageUrl(request.ImageUrl);
        products.Add(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ProductDto.From(product);
    }
}

public sealed class UpdateProductHandler(IProductRepository products, ICategoryRepository categories, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetAsync(request.Id, true, cancellationToken)
            ?? throw new NotFoundException("Product not found.");
        if (!await categories.ExistsAsync(request.CategoryId, cancellationToken))
            throw new NotFoundException("Category not found.");
        product.ChangeCategory(request.CategoryId);
        product.UpdateDetails(request.Name, request.Description);
        product.SetImageUrl(request.ImageUrl);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ProductDto.From(product);
    }
}

public sealed class AddProductOptionHandler(IProductRepository products, IUnitOfWork unitOfWork)
    : IRequestHandler<AddProductOptionCommand, ProductDto>
{
    public async Task<ProductDto> Handle(AddProductOptionCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Values);
        if (request.Values.Count is < 1 or > 100)
            throw new ArgumentException("An option needs between 1 and 100 values.");
        var product = await products.GetAsync(request.ProductId, true, cancellationToken)
            ?? throw new NotFoundException("Product not found.");
        var option = product.AddOption(request.Name);
        foreach (var value in request.Values)
            product.AddOptionValue(option.Id, value);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ProductDto.From(product);
    }
}

public sealed class AddProductOptionValueHandler(IProductRepository products, IUnitOfWork unitOfWork)
    : IRequestHandler<AddProductOptionValueCommand, ProductDto>
{
    public async Task<ProductDto> Handle(AddProductOptionValueCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetAsync(request.ProductId, true, cancellationToken)
            ?? throw new NotFoundException("Product not found.");
        product.AddOptionValue(request.OptionId, request.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ProductDto.From(product);
    }
}

public sealed class AddProductVariantHandler(IProductRepository products, IUnitOfWork unitOfWork)
    : IRequestHandler<AddProductVariantCommand, ProductDto>
{
    public async Task<ProductDto> Handle(AddProductVariantCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.OptionValueIds);
        ArgumentNullException.ThrowIfNull(request.Prices);
        if (request.Prices.Count is < 1 or > 2)
            throw new ArgumentException("Provide one or two prices.");
        if (request.Prices.Any(price => price is null))
            throw new ArgumentException("Price entries cannot be null.");
        if (request.Prices.Select(price => price.Currency?.Trim().ToUpperInvariant()).Distinct().Count() != request.Prices.Count)
            throw new ArgumentException("Currencies cannot repeat.");
        var product = await products.GetAsync(request.ProductId, true, cancellationToken)
            ?? throw new NotFoundException("Product not found.");
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Sku);
        if (await products.SkuExistsAsync(request.Sku, cancellationToken))
            throw new DomainRuleException("SKU already exists.");
        var variant = product.AddVariant(request.Sku, request.StockQuantity, request.OptionValueIds);
        foreach (var price in request.Prices)
            variant.SetPrice(price.Currency, price.Amount);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ProductDto.From(product);
    }
}

public sealed class SetVariantPriceHandler(IProductRepository products, IUnitOfWork unitOfWork)
    : IRequestHandler<SetVariantPriceCommand, ProductDto>
{
    public async Task<ProductDto> Handle(SetVariantPriceCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetAsync(request.ProductId, true, cancellationToken)
            ?? throw new NotFoundException("Product not found.");
        var variant = product.Variants.SingleOrDefault(variant => variant.Id == request.VariantId)
            ?? throw new NotFoundException("Variant not found.");
        variant.SetPrice(request.Currency, request.Amount);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ProductDto.From(product);
    }
}

public sealed class SetVariantStockHandler(IProductRepository products, IUnitOfWork unitOfWork)
    : IRequestHandler<SetVariantStockCommand, ProductDto>
{
    public async Task<ProductDto> Handle(SetVariantStockCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetAsync(request.ProductId, true, cancellationToken)
            ?? throw new NotFoundException("Product not found.");
        var variant = product.Variants.SingleOrDefault(variant => variant.Id == request.VariantId)
            ?? throw new NotFoundException("Variant not found.");
        variant.UpdateStockQuantity(request.StockQuantity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ProductDto.From(product);
    }
}
