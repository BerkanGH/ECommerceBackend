using ECommerce.Application.Common;
using ECommerce.Application.Identity;
using ECommerce.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ECommerce.Api.Errors;

public sealed class ApiExceptionHandler(IProblemDetailsService problems, ILogger<ApiExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            NotFoundException => (404, "Resource not found", exception.Message),
            UnauthorizedAccessException => (401, "Authentication required", "A valid account is required."),
            AccountValidationException => (400, "Account validation failed", "Check the supplied account details."),
            ArgumentException => (400, "Invalid request", exception.Message),
            DbUpdateConcurrencyException => (409, "Concurrent modification", "The resource changed. Reload it and retry."),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_Carts_UserId" or "IX_CartItems_CartId_ProductVariantId" } } =>
                (409, "Cart conflict", "Your cart changed. Reload it and retry."),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "EmailIndex" or "UserNameIndex" } } =>
                (409, "Duplicate account", "An account with this email already exists."),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } =>
                (409, "Duplicate resource", "SKU, option value or variant combination already exists."),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation } } =>
                (409, "Relationship conflict", "A related resource does not exist or is still in use."),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.CheckViolation } } =>
                (400, "Invalid value", "A database constraint rejected the value."),
            DomainRuleException => (409, "Business rule conflict", exception.Message),
            NpgsqlException => (503, "Database unavailable", "The database is temporarily unavailable."),
            _ => (500, "Unexpected error", "An unexpected error occurred.")
        };

        if (status >= 500)
            logger.LogError(exception, "Request failed with status {Status}", status);
        context.Response.StatusCode = status;
        ProblemDetails details = exception is AccountValidationException validation
            ? new ValidationProblemDetails(validation.Errors.ToDictionary(pair => pair.Key, pair => pair.Value))
            : new ProblemDetails();
        details.Status = status;
        details.Title = title;
        details.Detail = detail;
        details.Extensions["traceId"] = context.TraceIdentifier;
        await problems.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = details
        });
        return true;
    }
}
