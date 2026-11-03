using ECommerce.Application.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Identity;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.HasIndex(user => user.NormalizedEmail).HasDatabaseName("EmailIndex").IsUnique();
    }
}

public sealed class ApplicationRoleConfiguration : IEntityTypeConfiguration<IdentityRole<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityRole<Guid>> builder)
    {
        builder.HasData(
            new IdentityRole<Guid>(AccountRoles.Customer)
            {
                Id = Guid.Parse("b940722b-23a1-44c2-a1e4-64279516a1bb"),
                NormalizedName = "CUSTOMER",
                ConcurrencyStamp = "35d8673a-a4d8-458a-abbb-fb2173c44f78"
            },
            new IdentityRole<Guid>(AccountRoles.Admin)
            {
                Id = Guid.Parse("6d49b3e5-e899-4872-9b3e-41e9913cfe2a"),
                NormalizedName = "ADMIN",
                ConcurrencyStamp = "dab3dd1e-67e9-47b7-8f67-00873497d990"
            });
    }
}
