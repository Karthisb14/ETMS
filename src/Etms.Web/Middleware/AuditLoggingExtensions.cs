namespace Etms.Web.Middleware;

// Records who accessed what, when — the legacy app had no audit trail at all.
// HIPAA/PII accountability requirement (KAN-12). Logs via the standard logging
// pipeline; route to a durable sink (e.g. a dedicated audit table or log store)
// as part of the compliance hardening pass.
public static class AuditLoggingExtensions
{
    public static IApplicationBuilder UseAuditLogging(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var logger = context.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("AuditLog");

            await next();

            var user = context.User?.Identity?.IsAuthenticated == true
                ? context.User.Identity!.Name
                : "anonymous";

            logger.LogInformation(
                "AUDIT user={User} method={Method} path={Path} status={StatusCode}",
                user, context.Request.Method, context.Request.Path, context.Response.StatusCode);
        });
    }
}
