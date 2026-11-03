using System.Net;
using System.Net.Http.Json;
using ECommerce.Application.Abstractions;
using ECommerce.Application.Carts;
using ECommerce.Application.Common;
using ECommerce.Application.Identity;
using ECommerce.Application.Products;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace ECommerce.IntegrationTests;

[Collection("PostgreSQL")]
public class PersistenceTests(PostgresFixture fixture)
{
    [PostgresFact]
    public async Task Health_and_catalog_use_migrated_PostgreSQL()
    {
        var product = await fixture.SeedProductAsync();
        using var client = fixture.CreateClient();
        using var live = await client.GetAsync("/health/live");
        using var ready = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
        var page = await client.GetFromJsonAsync<PagedResult<ProductSummaryDto>>("/api/products?pageSize=100");
        Assert.Contains(page!.Items, item => item.Id == product.ProductId);
        using var anonymousWrite = await client.PostAsJsonAsync("/api/categories", new { name = "Denied" });
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousWrite.StatusCode);
    }

    [PostgresFact]
    public async Task Revision_changes_for_modified_child_but_not_unrelated_aggregate()
    {
        var first = await fixture.SeedProductAsync();
        var second = await fixture.SeedProductAsync();
        await fixture.WithContextAsync(async context =>
        {
            var repository = new ProductRepository(context);
            var changed = (await repository.GetAsync(first.ProductId, true, default))!;
            var untouched = (await repository.GetAsync(second.ProductId, true, default))!;
            var firstRevision = context.Entry(changed).Property<Guid>("Revision").CurrentValue;
            var secondRevision = context.Entry(untouched).Property<Guid>("Revision").CurrentValue;
            await ((IUnitOfWork)context).SaveChangesAsync(default);
            Assert.Equal(firstRevision, context.Entry(changed).Property<Guid>("Revision").CurrentValue);
            changed.Variants.Single().SetPrice("TRY", 101);
            await ((IUnitOfWork)context).SaveChangesAsync(default);
            Assert.NotEqual(firstRevision, context.Entry(changed).Property<Guid>("Revision").CurrentValue);
            Assert.Equal(secondRevision, context.Entry(untouched).Property<Guid>("Revision").CurrentValue);
            return true;
        });
    }

    [PostgresFact]
    public async Task Stale_stock_write_is_rejected_and_rolled_back()
    {
        var product = await fixture.SeedProductAsync();
        await using var firstScope = fixture.Factory.Services.CreateAsyncScope();
        await using var secondScope = fixture.Factory.Services.CreateAsyncScope();
        var first = firstScope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
        var second = secondScope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
        var firstProduct = (await new ProductRepository(first).GetAsync(product.ProductId, true, default))!;
        var secondProduct = (await new ProductRepository(second).GetAsync(product.ProductId, true, default))!;
        firstProduct.Variants.Single().DecreaseStock(1);
        secondProduct.Variants.Single().UpdateStockQuantity(50);
        await ((IUnitOfWork)first).SaveChangesAsync(default);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => ((IUnitOfWork)second).SaveChangesAsync(default));
        var stock = await fixture.WithContextAsync(context => context.ProductVariants
            .Where(variant => variant.Id == product.VariantId).Select(variant => variant.StockQuantity).SingleAsync());
        Assert.Equal(9, stock);
    }

    [PostgresFact]
    public async Task Clearing_cart_advances_revision_and_deletes_items()
    {
        var product = await fixture.SeedProductAsync();
        using var customer = await fixture.CreateCustomerAsync();
        using var add = await customer.PostAsJsonAsync("/api/cart/items",
            new { productVariantId = product.VariantId, quantity = 1, currency = "TRY" });
        add.EnsureSuccessStatusCode();
        var me = await customer.GetFromJsonAsync<AccountDto>("/api/auth/me");
        var revision = await fixture.WithContextAsync(async context =>
        {
            var cart = (await new CartRepository(context).GetByUserAsync(me!.Id, true, default))!;
            var before = context.Entry(cart).Property<Guid>("Revision").CurrentValue;
            cart.Clear();
            await ((IUnitOfWork)context).SaveChangesAsync(default);
            Assert.NotEqual(before, context.Entry(cart).Property<Guid>("Revision").CurrentValue);
            return before;
        });
        Assert.Empty((await customer.GetFromJsonAsync<CartDto>("/api/cart"))!.Items);
        Assert.NotEqual(Guid.Empty, revision);
    }

    [PostgresFact]
    public async Task Database_rejects_global_duplicate_sku()
    {
        var product = await fixture.SeedProductAsync();
        var sku = await fixture.WithContextAsync(context => context.ProductVariants
            .Where(variant => variant.Id == product.VariantId).Select(variant => variant.Sku).SingleAsync());
        await fixture.WithContextAsync(async context =>
        {
            var category = new Category("Other category");
            var duplicate = new Product(category.Id, "Duplicate");
            duplicate.AddVariant(sku.ToLowerInvariant(), 1, []);
            context.AddRange(category, duplicate);
            var error = await Assert.ThrowsAsync<DbUpdateException>(() => ((IUnitOfWork)context).SaveChangesAsync(default));
            Assert.Equal(PostgresErrorCodes.UniqueViolation, Assert.IsType<PostgresException>(error.InnerException).SqlState);
            return true;
        });
    }

    [PostgresFact]
    public async Task Currency_and_ownership_cannot_be_overridden_by_request_fields()
    {
        var product = await fixture.SeedProductAsync();
        using var owner = await fixture.CreateCustomerAsync();
        using var stranger = await fixture.CreateCustomerAsync();
        var strangerProfile = await stranger.GetFromJsonAsync<AccountDto>("/api/auth/me");
        using var add = await owner.PostAsJsonAsync("/api/cart/items", new
        {
            productVariantId = product.VariantId, quantity = 1, currency = "TRY",
            userId = strangerProfile!.Id, unitPrice = 0.01
        });
        add.EnsureSuccessStatusCode();
        var cart = await add.Content.ReadFromJsonAsync<CartDto>();
        Assert.Equal(100m, cart!.TotalAmount);
        Assert.Empty((await stranger.GetFromJsonAsync<CartDto>("/api/cart"))!.Items);
        using var mix = await owner.PostAsJsonAsync("/api/cart/items",
            new { productVariantId = product.VariantId, quantity = 1, currency = "USD" });
        Assert.Equal(HttpStatusCode.Conflict, mix.StatusCode);
        Assert.DoesNotContain("Admin", strangerProfile.Roles);
    }
}
