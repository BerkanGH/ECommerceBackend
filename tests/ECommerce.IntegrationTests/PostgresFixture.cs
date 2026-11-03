using System.Net.Http.Headers;
using System.Net.Http.Json;
using ECommerce.Application.Abstractions;
using ECommerce.Application.Identity;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace ECommerce.IntegrationTests;

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ECOMMERCE_TEST_CONNECTION")))
            Skip = "Set ECOMMERCE_TEST_CONNECTION to a PostgreSQL database with CREATE SCHEMA permission.";
    }
}

[CollectionDefinition("PostgreSQL", DisableParallelization = true)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;

public sealed class PostgresFixture : IAsyncLifetime
{
    private string? _previousConnection;
    private string? _adminConnection;
    private bool _schemaCreated;
    private bool _environmentChanged;
    private readonly string _schema = "ecommerce_test_" + Guid.NewGuid().ToString("N");
    public TestApplication Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _adminConnection = Environment.GetEnvironmentVariable("ECOMMERCE_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(_adminConnection))
            return;
        await using var connection = new NpgsqlConnection(_adminConnection);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"CREATE SCHEMA \"{_schema}\"", connection);
        await command.ExecuteNonQueryAsync();
        _schemaCreated = true;

        var builder = new NpgsqlConnectionStringBuilder(_adminConnection) { SearchPath = _schema };
        var testConnection = builder.ConnectionString;
        _previousConnection = Environment.GetEnvironmentVariable("ConnectionStrings__ECommerce");
        _environmentChanged = true;
        Environment.SetEnvironmentVariable("ConnectionStrings__ECommerce", testConnection);
        Factory = new TestApplication(testConnection, _schema);
        await using var scope = Factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ECommerceDbContext>().Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        try
        {
            if (Factory is not null)
                await Factory.DisposeAsync();
        }
        finally
        {
            if (_environmentChanged)
                Environment.SetEnvironmentVariable("ConnectionStrings__ECommerce", _previousConnection);
            if (_schemaCreated)
            {
                if (!_schema.StartsWith("ecommerce_test_", StringComparison.Ordinal) ||
                    !Guid.TryParseExact(_schema["ecommerce_test_".Length..], "N", out _))
                    throw new InvalidOperationException("Refusing to remove an unexpected schema.");
                await using var connection = new NpgsqlConnection(_adminConnection);
                await connection.OpenAsync();
                await using var command = new NpgsqlCommand($"DROP SCHEMA IF EXISTS \"{_schema}\" CASCADE", connection);
                await command.ExecuteNonQueryAsync();
            }
        }
    }

    public HttpClient CreateClient() => Factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost")
    });

    public async Task<HttpClient> CreateCustomerAsync()
    {
        var client = CreateClient();
        var email = $"{Guid.NewGuid():N}@example.test";
        var password = $"Test1!{Guid.NewGuid():N}";
        using var registration = await client.PostAsJsonAsync("/api/auth/register", new { email, password, roles = new[] { "Admin" } });
        registration.EnsureSuccessStatusCode();
        using var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        login.EnsureSuccessStatusCode();
        var token = await login.Content.ReadFromJsonAsync<TokenResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        return client;
    }

    public async Task<(Guid ProductId, Guid VariantId)> SeedProductAsync(int stock = 10, decimal price = 100m)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
        var category = new Category("Books");
        var product = new Product(category.Id, "Book");
        var variant = product.AddVariant($"BOOK-{Guid.NewGuid():N}", stock, []);
        variant.SetPrice("TRY", price);
        variant.SetPrice("USD", 5m);
        context.AddRange(category, product);
        await ((IUnitOfWork)context).SaveChangesAsync(default);
        return (product.Id, variant.Id);
    }

    public async Task<T> WithContextAsync<T>(Func<ECommerceDbContext, Task<T>> action)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<ECommerceDbContext>());
    }

    private sealed record TokenResponse(string AccessToken);
}

public sealed class TestApplication(string connection, string schema) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureTestServices(services =>
        {
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.RemoveAll<DbContextOptions<ECommerceDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ECommerceDbContext>>();
            services.AddDbContext<ECommerceDbContext>(options =>
                options.UseNpgsql(connection, postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", schema)));
        });
    }
}
