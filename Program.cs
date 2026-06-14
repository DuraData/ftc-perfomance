using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var clientDist = Path.Combine(app.Environment.ContentRootPath, "wwwroot");

if (Directory.Exists(clientDist))
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}
else
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(app.Environment.ContentRootPath)
    });
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapFallback(async context =>
{
    var indexPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "index.html");

    if (File.Exists(indexPath))
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.SendFileAsync(indexPath);
        return;
    }

    context.Response.ContentType = "text/plain; charset=utf-8";
    await context.Response.WriteAsync("FTCERP frontend has not been built yet. Run npm install and npm run build in ClientApp.");
});

app.Run();
