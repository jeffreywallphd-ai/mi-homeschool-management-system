using HomeschoolManager.Application.Persistence;
using HomeschoolManager.Infrastructure;
using HomeschoolManager.StudentPortal.Web.Components;
using HomeschoolManager.StudentPortal.Web.Services;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddHomeschoolManagerInfrastructure(builder.Configuration);
builder.Services.AddSingleton<SessionState>();

var dataProtectionDirectory = builder.Environment.IsDevelopment()
    ? Path.Combine(builder.Environment.ContentRootPath, ".dev-data", "DataProtection-Keys")
    : Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "HomeschoolManager",
        "DataProtection-Keys");
Directory.CreateDirectory(dataProtectionDirectory);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionDirectory))
    .SetApplicationName("HomeschoolManager.StudentPortal");

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var repository = scope.ServiceProvider.GetRequiredService<IHomeschoolRepository>();
    await repository.EnsureStoreCreatedAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    if (!path.HasValue || path == "/")
    {
        context.Response.Redirect("/login");
        return;
    }

    if (IsFrameworkOrAssetPath(path) || IsStudentPortalPath(path))
    {
        await next();
        return;
    }

    context.Response.StatusCode = StatusCodes.Status404NotFound;
});

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static bool IsStudentPortalPath(PathString path)
{
    return path.StartsWithSegments("/login")
        || path.StartsWithSegments("/student");
}

static bool IsFrameworkOrAssetPath(PathString path)
{
    var value = path.Value ?? "";
    return Path.HasExtension(value)
        || value.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("/_content", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("/icons", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("/lib", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("/session.js", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("/app.css", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase);
}
