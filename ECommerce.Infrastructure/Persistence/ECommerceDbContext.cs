using ECommerce.Application.Abstractions;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence;

public sealed class ECommerceDbContext(DbContextOptions<ECommerceDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IUnitOfWork
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<Order> Orders => Set<Order>();

    async Task<ICommerceTransaction> IUnitOfWork.BeginTransactionAsync(CancellationToken cancellationToken) =>
        new CommerceTransaction(await Database.BeginTransactionAsync(cancellationToken));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ECommerceDbContext).Assembly);
    }

    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken)
    {
        ChangeTracker.DetectChanges();
        var entries = ChangeTracker.Entries().ToArray();
        var changed = entries.Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToArray();
        var productIds = new HashSet<Guid>();
        var cartIds = new HashSet<Guid>();
        var orderIds = new HashSet<Guid>();
        var categoryIds = new HashSet<Guid>();
        var variants = entries.Select(entry => entry.Entity).OfType<ProductVariant>().ToDictionary(variant => variant.Id);
        var options = entries.Select(entry => entry.Entity).OfType<ProductOption>().ToDictionary(option => option.Id);

        foreach (var entry in changed)
        {
            switch (entry.Entity)
            {
                case Product product: productIds.Add(product.Id); break;
                case ProductVariant variant: productIds.Add(variant.ProductId); break;
                case ProductOption option: productIds.Add(option.ProductId); break;
                case ProductOptionValue value when options.TryGetValue(value.ProductOptionId, out var option):
                    productIds.Add(option.ProductId); break;
                case ProductVariantPrice price when variants.TryGetValue(price.ProductVariantId, out var variant):
                    productIds.Add(variant.ProductId); break;
                case ProductVariantOptionValue value when variants.TryGetValue(value.ProductVariantId, out var variant):
                    productIds.Add(variant.ProductId); break;
                case Cart cart: cartIds.Add(cart.Id); break;
                case CartItem item: cartIds.Add(item.CartId); break;
                case Order order: orderIds.Add(order.Id); break;
                case OrderItem item: orderIds.Add(item.OrderId); break;
                case ShippingAddress address:
                    foreach (var order in entries.Select(candidate => candidate.Entity).OfType<Order>()
                                 .Where(order => ReferenceEquals(order.ShippingAddress, address)))
                        orderIds.Add(order.Id);
                    break;
                case Category category: categoryIds.Add(category.Id); break;
            }
        }

        foreach (var entry in entries
                     .Where(entry => entry.Entity is Product or Category or Cart or Order)
                     .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Unchanged))
        {
            var modified = entry.Entity switch
            {
                Product product => productIds.Contains(product.Id),
                Cart cart => cartIds.Contains(cart.Id),
                Order order => orderIds.Contains(order.Id),
                Category category => categoryIds.Contains(category.Id),
                _ => false
            };
            if (modified)
                entry.Property("Revision").CurrentValue = Guid.NewGuid();
        }

        await SaveChangesAsync(cancellationToken);
    }
}
