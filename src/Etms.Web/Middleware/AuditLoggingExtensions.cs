using Etms.Data;
using Etms.Domain;

namespace Etms.Web.Middleware;

// Records who accessed what, when — the legacy app had no audit trail at all.
// HIPAA/PII accountability requirement (KAN-12). Persisted to AuditLogEntry so
// it's queryable, not just scattered across log output. Static assets are
// skipped to keep the table meaningful (page views and form submissions only).
public static class AuditLoggingExtensions
{
    private static readonly string[] SkipPrefixes = ["/css", "/js", "/lib", "/favicon.ico", "/_framework"];

    public static IApplicationBuilder UseAuditLogging(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            await next();

            var path = context.Request.Path.Value ?? string.Empty;
            if (SkipPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            var logger = context.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("AuditLog");

            var user = context.User?.Identity?.IsAuthenticated == true
                ? context.User.Identity!.Name ?? "unknown"
                : "anonymous";

            try
            {
                var db = context.RequestServices.GetRequiredService<EtmsDbContext>();
                db.AuditLogEntries.Add(new AuditLogEntry
                {
                    TimestampUtc = DateTime.UtcNow,
                    User = user,
                    Method = context.Request.Method,
                    Path = path,
                    StatusCode = context.Response.StatusCode,
                });
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Audit logging must never break the request it's observing.
                logger.LogError(ex,
                    "Failed to persist audit log entry for user={User} method={Method} path={Path}",
                    user, context.Request.Method, path);
            }
        });
    }
}

