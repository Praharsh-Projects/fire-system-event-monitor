using System.Security.Cryptography;
using System.Text;

namespace FireSystemEventMonitor.Api.Security;

public sealed class TenantResolutionMiddleware(
    RequestDelegate next,
    IConfiguration configuration,
    ILogger<TenantResolutionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext httpContext, ITenantContext tenantContext)
    {
        if (httpContext.Request.Path.StartsWithSegments("/health"))
        {
            await next(httpContext);
            return;
        }

        var tenantId = httpContext.Request.Headers["X-Tenant-Id"].ToString();
        var suppliedKey = httpContext.Request.Headers["X-Api-Key"].ToString();
        var configuredKey = configuration[$"Tenants:{tenantId}"];

        if (string.IsNullOrWhiteSpace(tenantId) ||
            string.IsNullOrWhiteSpace(suppliedKey) ||
            string.IsNullOrWhiteSpace(configuredKey) ||
            !KeysMatch(suppliedKey, configuredKey))
        {
            logger.LogWarning("Rejected request with missing or invalid tenant credentials.");
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await httpContext.Response.WriteAsJsonAsync(new { error = "Valid tenant credentials are required." });
            return;
        }

        tenantContext.TenantId = tenantId;
        await next(httpContext);
    }

    private static bool KeysMatch(string supplied, string configured)
    {
        var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(supplied));
        var configuredHash = SHA256.HashData(Encoding.UTF8.GetBytes(configured));
        return CryptographicOperations.FixedTimeEquals(suppliedHash, configuredHash);
    }
}

