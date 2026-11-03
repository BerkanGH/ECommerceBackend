using ECommerce.Api.Errors;
using ECommerce.Api.Health;
using ECommerce.Api.Identity;
using ECommerce.Application.Abstractions;
using ECommerce.Application.Categories;
using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using System.Threading.RateLimiting;

var bootstrapAdmin = args.Contains("--bootstrap-admin", StringComparer.Ordinal);
var builder = WebApplication.CreateBuilder(args.Where(argument => argument != "--bootstrap-admin").ToArray());
var connectionString = builder.Configuration.GetConnectionString("ECommerce")
    ?? "Host=localhost;Port=5432;Database=ecommerce;Username=postgres";

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDataProtection().SetApplicationName("ECommerceBackend");
builder.Services.AddAuthentication(IdentityConstants.BearerScheme)
    .AddBearerToken(IdentityConstants.BearerScheme, options =>
    {
        options.BearerTokenExpiration = TimeSpan.FromMinutes(15);
        options.RefreshTokenExpiration = TimeSpan.FromDays(7);
    });
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("authentication", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});
builder.Services.AddMediatR(configuration =>
{
    configuration.RegisterServicesFromAssemblyContaining<CreateCategoryCommand>();
    var licenseKey = builder.Configuration["MediatR:LicenseKey"];
    if (!string.IsNullOrWhiteSpace(licenseKey))
        configuration.LicenseKey = licenseKey;
});
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddHealthChecks().AddCheck<DatabaseReadinessCheck>("database");

await using var app = builder.Build();
if (bootstrapAdmin)
{
    await using var scope = app.Services.CreateAsyncScope();
    var email = builder.Configuration["AdminBootstrap:Email"]
        ?? throw new InvalidOperationException("AdminBootstrap:Email must be supplied through secrets or environment.");
    var password = builder.Configuration["AdminBootstrap:Password"]
        ?? throw new InvalidOperationException("AdminBootstrap:Password must be supplied through secrets or environment.");
    await scope.ServiceProvider.GetRequiredService<AdminBootstrapper>().CreateAsync(email, password, CancellationToken.None);
    app.Logger.LogInformation("Administrator bootstrap completed. No HTTP server was started.");
    return;
}
app.UseExceptionHandler();
app.UseStatusCodePages();
if (app.Environment.IsDevelopment())
    app.MapOpenApi();
else
    app.UseHttpsRedirection();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health/live", () => Results.Ok(new { status = "alive" }));
app.MapHealthChecks("/health/ready");
app.MapGet("/", () => Results.Ok(new
{
    name = "ECommerceBackend",
    version = "1.0",
    documentation = app.Environment.IsDevelopment() ? "/openapi/v1.json" : null,
    health = "/health/ready"
}));
app.Run();
