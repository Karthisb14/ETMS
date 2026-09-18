namespace Etms.Web.Middleware;

// Minimal defense-in-depth headers (KAN-12 compliance hardening). Not a replacement for a
// full CSP policy pass, which should be revisited once static asset usage is finalized.
public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            await next();
        });
    }
}
