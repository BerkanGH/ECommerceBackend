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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ECommerceDbContext).Assembly);
    }

    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken)
    {
        ChangeTracker.DetectChanges();
        foreach (var entry in ChangeTracker.Entries()
                     .Where(entry => entry.Entity is Product or Category)
                     .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Unchanged))
        {
            entry.Property("Revision").CurrentValue = Guid.NewGuid();
        }

        await SaveChangesAsync(cancellationToken);
    }
}
