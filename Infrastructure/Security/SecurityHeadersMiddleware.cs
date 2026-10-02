namespace FTCERP.Host.Infrastructure.Security;

public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=(), payment=(), usb=()");
        // Swagger UI bootstraps itself with an inline development-only script.
        if (!context.Request.Path.StartsWithSegments("/swagger"))
            headers.Append("Content-Security-Policy", "default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'none'; form-action 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; font-src 'self' data:; connect-src 'self'");
        return next(context);
    }
}
