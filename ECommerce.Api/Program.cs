using ECommerce.Api.Errors;
using ECommerce.Application.Categories;
using ECommerce.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("ECommerce")
    ?? "Host=localhost;Port=5432;Database=ecommerce;Username=postgres";

builder.Services.AddInfrastructure(connectionString);
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

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
if (app.Environment.IsDevelopment())
    app.MapOpenApi();
else
    app.UseHttpsRedirection();
app.MapControllers();
app.MapGet("/health/live", () => Results.Ok(new { status = "alive" }));
app.Run();
