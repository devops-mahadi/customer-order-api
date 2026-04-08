using System.Security.Claims;
using CustomerOrder.Application.Interfaces;
using CustomerOrder.Domain.Constants;

namespace CustomerOrder.Infrastructure.Middleware;

/// <summary>
/// Middleware for auditing all HTTP requests (GDPR Article 30)
/// Logs who accessed what data, when, and from where
/// </summary>
public class AuditMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAuditService auditService)
    {
        // Extract request information
        var userEmail = context.User?.FindFirst(ClaimTypes.Email)?.Value ?? "Anonymous";
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var userAgent = context.Request.Headers["User-Agent"].ToString();
        var httpMethod = context.Request.Method;
        var endpoint = context.Request.Path.ToString();

        // Determine entity type and ID from route
        var (entityType, entityId, action) = ExtractEntityInfo(context, httpMethod, endpoint);

        // Call the next middleware in the pipeline
        await next(context);

        // Log after response (to capture status code)
        if (context.Response.StatusCode < 400 && !string.IsNullOrEmpty(entityType))
        {
            // Only log successful requests to actual resources
            await auditService.LogAsync(
                entityType,
                entityId,
                action,
                userEmail,
                ipAddress,
                userAgent,
                httpMethod,
                endpoint);
        }
    }

    private static (string EntityType, string EntityId, string Action) ExtractEntityInfo(
        HttpContext context,
        string httpMethod,
        string endpoint)
    {
        var path = endpoint.ToLower();

        // Determine entity type
        string entityType = path.Contains("/customers")
            ? ApplicationConstants.Audit.EntityTypes.Customer
            : path.Contains("/orders")
                ? ApplicationConstants.Audit.EntityTypes.Order
                : string.Empty;

        // Determine action based on HTTP method
        string action = httpMethod switch
        {
            "GET" when path.Contains("/export") => ApplicationConstants.Audit.Actions.Exported,
            "GET" => ApplicationConstants.Audit.Actions.Viewed,
            "POST" when path.Contains("/anonymize") => ApplicationConstants.Audit.Actions.Anonymized,
            "POST" when path.Contains("/consents") => ApplicationConstants.Audit.Actions.ConsentGranted,
            "POST" => ApplicationConstants.Audit.Actions.Created,
            "PUT" => ApplicationConstants.Audit.Actions.Updated,
            "DELETE" when path.Contains("/consents") => ApplicationConstants.Audit.Actions.ConsentRevoked,
            "DELETE" => ApplicationConstants.Audit.Actions.Deleted,
            _ => "Unknown"
        };

        // Extract entity ID from route (email or order number)
        var entityId = ExtractEntityId(context);

        return (entityType, entityId, action);
    }

    private static string ExtractEntityId(HttpContext context)
    {
        // Try to get email or orderNumber from route values
        if (context.Request.RouteValues.TryGetValue("email", out var email))
        {
            return email?.ToString() ?? "Unknown";
        }

        if (context.Request.RouteValues.TryGetValue("orderNumber", out var orderNumber))
        {
            return orderNumber?.ToString() ?? "Unknown";
        }

        return "Unknown";
    }
}
