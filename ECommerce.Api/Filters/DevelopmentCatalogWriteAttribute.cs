using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ECommerce.Api.Filters;

[AttributeUsage(AttributeTargets.Method)]
public sealed class DevelopmentCatalogWriteAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var services = context.HttpContext.RequestServices;
        var environment = services.GetRequiredService<IHostEnvironment>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var remoteAddress = context.HttpContext.Connection.RemoteIpAddress;
        if (environment.IsDevelopment() && configuration.GetValue<bool>("Catalog:EnableDevelopmentWrites")
            && remoteAddress is not null && IPAddress.IsLoopback(remoteAddress))
            return;

        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status503ServiceUnavailable,
            Title = "Catalog writes are disabled",
            Detail = "Administrator authentication is not implemented yet. Writes are available only in explicitly enabled local development."
        }) { StatusCode = StatusCodes.Status503ServiceUnavailable };
    }
}
