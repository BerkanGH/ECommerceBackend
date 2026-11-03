using System.Net;
using System.Net.Http.Json;
using ECommerce.Api.Contracts;
using ECommerce.Application.Abstractions;
using ECommerce.Application.Carts;
using ECommerce.Application.Orders;
using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.IntegrationTests;

[Collection("PostgreSQL")]
public class CheckoutTests(PostgresFixture fixture)
{
    private static CheckoutRequest Request(decimal total) => new("TRY", total,
        new ShippingAddressRequest("Test Customer", "555", "TR", "Istanbul", "34000", "Test street"));

    private static async Task<HttpResponseMessage> CheckoutAsync(HttpClient client, Guid key, decimal total)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
        {
            Content = JsonContent.Create(Request(total))
        };
        request.Headers.Add("Idempotency-Key", key.ToString());
        return await client.SendAsync(request);
    }

    private static async Task AddAsync(HttpClient client, Guid variantId, int quantity = 1, string currency = "TRY")
    {
        using var response = await client.PostAsJsonAsync("/api/cart/items",
            new { productVariantId = variantId, quantity, currency, unitPrice = 0.01 });
        response.EnsureSuccessStatusCode();
    }

    private Task<int> StockAsync(Guid variantId) => fixture.WithContextAsync(context =>
        context.ProductVariants.Where(variant => variant.Id == variantId).Select(variant => variant.StockQuantity).SingleAsync());

    [PostgresFact]
    public async Task Checkout_is_atomic_and_retry_does_not_create_a_second_order()
    {
        var product = await fixture.SeedProductAsync();
        using var customer = await fixture.CreateCustomerAsync();
        await AddAsync(customer, product.VariantId, 2);
        var key = Guid.NewGuid();
        using var first = await CheckoutAsync(customer, key, 200);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var order = await first.Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal(200m, order!.TotalAmount);
        Assert.Equal(100m, Assert.Single(order.Items).UnitPrice);
        Assert.Equal(8, await StockAsync(product.VariantId));
        var cart = await customer.GetFromJsonAsync<CartDto>("/api/cart");
        Assert.Empty(cart!.Items);
        using var replay = await CheckoutAsync(customer, key, 200);
        Assert.Equal(order.Id, (await replay.Content.ReadFromJsonAsync<OrderDto>())!.Id);
        Assert.Equal(8, await StockAsync(product.VariantId));
        using var changed = await CheckoutAsync(customer, key, 201);
        Assert.Equal(HttpStatusCode.Conflict, changed.StatusCode);
    }

    [PostgresFact]
    public async Task Stock_shortage_rolls_back_all_lines_and_preserves_cart()
    {
        var first = await fixture.SeedProductAsync();
        var second = await fixture.SeedProductAsync(stock: 1);
        using var customer = await fixture.CreateCustomerAsync();
        await AddAsync(customer, first.VariantId);
        await AddAsync(customer, second.VariantId);
        await fixture.WithContextAsync(async context =>
        {
            var product = await context.Products.Include(product => product.Variants).SingleAsync(product => product.Id == second.ProductId);
            product.Variants.Single().UpdateStockQuantity(0);
            await ((IUnitOfWork)context).SaveChangesAsync(default);
            return true;
        });
        using var response = await CheckoutAsync(customer, Guid.NewGuid(), 200);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(10, await StockAsync(first.VariantId));
        Assert.Equal(0, await StockAsync(second.VariantId));
        Assert.Equal(2, (await customer.GetFromJsonAsync<CartDto>("/api/cart"))!.Items.Count);
        var orders = await customer.GetFromJsonAsync<ECommerce.Application.Common.PagedResult<OrderSummaryDto>>("/api/orders");
        Assert.Empty(orders!.Items);
    }

    [PostgresFact]
    public async Task Last_stock_unit_can_only_be_sold_once()
    {
        var product = await fixture.SeedProductAsync(stock: 1);
        using var first = await fixture.CreateCustomerAsync();
        using var second = await fixture.CreateCustomerAsync();
        await AddAsync(first, product.VariantId);
        await AddAsync(second, product.VariantId);
        var responses = await Task.WhenAll(CheckoutAsync(first, Guid.NewGuid(), 100), CheckoutAsync(second, Guid.NewGuid(), 100));
        try
        {
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
            Assert.Equal(0, await StockAsync(product.VariantId));
        }
        finally
        {
            foreach (var response in responses) response.Dispose();
        }
    }

    [PostgresFact]
    public async Task Concurrent_retries_return_the_same_order()
    {
        var product = await fixture.SeedProductAsync();
        using var customer = await fixture.CreateCustomerAsync();
        await AddAsync(customer, product.VariantId, 2);
        var key = Guid.NewGuid();
        var responses = await Task.WhenAll(CheckoutAsync(customer, key, 200), CheckoutAsync(customer, key, 200));
        try
        {
            Assert.All(responses, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));
            var first = await responses[0].Content.ReadFromJsonAsync<OrderDto>();
            var second = await responses[1].Content.ReadFromJsonAsync<OrderDto>();
            Assert.Equal(first!.Id, second!.Id);
            Assert.Equal(8, await StockAsync(product.VariantId));
        }
        finally
        {
            foreach (var response in responses) response.Dispose();
        }
    }

    [PostgresFact]
    public async Task Concurrent_cancellation_returns_stock_only_once()
    {
        var product = await fixture.SeedProductAsync();
        using var customer = await fixture.CreateCustomerAsync();
        await AddAsync(customer, product.VariantId, 2);
        using var created = await CheckoutAsync(customer, Guid.NewGuid(), 200);
        var order = await created.Content.ReadFromJsonAsync<OrderDto>();
        var responses = await Task.WhenAll(customer.PostAsync($"/api/orders/{order!.Id}/cancel", null),
            customer.PostAsync($"/api/orders/{order.Id}/cancel", null));
        try
        {
            Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
            Assert.Equal(10, await StockAsync(product.VariantId));
        }
        finally
        {
            foreach (var response in responses) response.Dispose();
        }
    }

    [PostgresFact]
    public async Task Customer_cannot_read_or_cancel_another_customers_order()
    {
        var product = await fixture.SeedProductAsync();
        using var owner = await fixture.CreateCustomerAsync();
        using var stranger = await fixture.CreateCustomerAsync();
        await AddAsync(owner, product.VariantId);
        using var created = await CheckoutAsync(owner, Guid.NewGuid(), 100);
        var order = await created.Content.ReadFromJsonAsync<OrderDto>();
        using var detail = await stranger.GetAsync($"/api/orders/{order!.Id}");
        using var cancel = await stranger.PostAsync($"/api/orders/{order.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.NotFound, detail.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, cancel.StatusCode);
        using var admin = await stranger.GetAsync("/api/admin/orders");
        Assert.Equal(HttpStatusCode.Forbidden, admin.StatusCode);
    }

    [PostgresFact]
    public async Task Changed_price_requires_confirmation_and_does_not_change_past_order()
    {
        var product = await fixture.SeedProductAsync();
        using var customer = await fixture.CreateCustomerAsync();
        await AddAsync(customer, product.VariantId);
        await fixture.WithContextAsync(async context =>
        {
            var entity = await context.Products.Include(product => product.Variants).ThenInclude(variant => variant.Prices)
                .SingleAsync(candidate => candidate.Id == product.ProductId);
            entity.Variants.Single().SetPrice("TRY", 120);
            await ((IUnitOfWork)context).SaveChangesAsync(default);
            return true;
        });
        var key = Guid.NewGuid();
        using var stale = await CheckoutAsync(customer, key, 100);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal(10, await StockAsync(product.VariantId));
        using var correct = await CheckoutAsync(customer, key, 120);
        var order = await correct.Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal(120m, order!.TotalAmount);
        await fixture.WithContextAsync(async context =>
        {
            var entity = await context.Products.Include(product => product.Variants).ThenInclude(variant => variant.Prices)
                .SingleAsync(candidate => candidate.Id == product.ProductId);
            entity.UpdateDetails("Changed name", null);
            entity.Variants.Single().SetPrice("TRY", 150);
            await ((IUnitOfWork)context).SaveChangesAsync(default);
            return true;
        });
        var history = await customer.GetFromJsonAsync<OrderDto>($"/api/orders/{order.Id}");
        Assert.Equal("Book", Assert.Single(history!.Items).ProductName);
        Assert.Equal(120m, history.TotalAmount);
    }

    [PostgresFact]
    public async Task Dollar_checkout_uses_USD_price_and_does_not_fall_back_to_TRY()
    {
        var priced = await fixture.SeedProductAsync();
        var missingPrice = await fixture.SeedProductAsync();
        await fixture.WithContextAsync(async context =>
        {
            await context.Set<ProductVariantPrice>().Where(price =>
                price.ProductVariantId == missingPrice.VariantId && price.Currency == "USD").ExecuteDeleteAsync();
            return true;
        });
        using var customer = await fixture.CreateCustomerAsync();
        await AddAsync(customer, priced.VariantId, currency: "USD");
        using var missing = await customer.PostAsJsonAsync("/api/cart/items",
            new { productVariantId = missingPrice.VariantId, quantity = 1, currency = "USD" });
        Assert.Equal(HttpStatusCode.Conflict, missing.StatusCode);
        var cart = await customer.GetFromJsonAsync<CartDto>("/api/cart");
        Assert.Equal(5m, cart!.TotalAmount);
        var payload = Request(5) with { ExpectedCurrency = "USD" };
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders") { Content = JsonContent.Create(payload) };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        using var response = await customer.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal("USD", order!.Currency);
        Assert.Equal(5m, order.TotalAmount);
        Assert.Equal(10, await StockAsync(missingPrice.VariantId));
    }

    [PostgresFact]
    public async Task Inactive_product_cannot_be_purchased_but_past_order_can_be_cancelled()
    {
        var product = await fixture.SeedProductAsync();
        using var customer = await fixture.CreateCustomerAsync();
        await AddAsync(customer, product.VariantId);
        async Task SetActive(bool active) => await fixture.WithContextAsync(async context =>
        {
            var entity = await context.Products.SingleAsync(candidate => candidate.Id == product.ProductId);
            entity.SetAvailability(active);
            await ((IUnitOfWork)context).SaveChangesAsync(default);
            return true;
        });
        await SetActive(false);
        using var hidden = await customer.GetAsync($"/api/products/{product.ProductId}");
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        var cart = await customer.GetFromJsonAsync<CartDto>("/api/cart");
        Assert.Equal("product_inactive", Assert.Single(cart!.Items).Issue);
        using var rejected = await CheckoutAsync(customer, Guid.NewGuid(), 100);
        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
        Assert.Equal(10, await StockAsync(product.VariantId));
        await SetActive(true);
        using var created = await CheckoutAsync(customer, Guid.NewGuid(), 100);
        var order = await created.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(order);
        await SetActive(false);
        using var cancelled = await customer.PostAsync($"/api/orders/{order.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode);
        Assert.Equal(10, await StockAsync(product.VariantId));
    }
}
