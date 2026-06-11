using HomeschoolManager.Web.Components;
using HomeschoolManager.Application.Persistence;
using HomeschoolManager.Infrastructure;
using HomeschoolManager.Web.Services;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddHomeschoolManagerInfrastructure(builder.Configuration);
builder.Services.AddSingleton<SessionState>();
builder.Services.AddSingleton<PortalState>();

var dataProtectionDirectory = builder.Environment.IsDevelopment()
    ? Path.Combine(builder.Environment.ContentRootPath, ".dev-data", "DataProtection-Keys")
    : Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "HomeschoolManager",
        "DataProtection-Keys");
Directory.CreateDirectory(dataProtectionDirectory);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionDirectory))
    .SetApplicationName("HomeschoolManager");

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var repository = scope.ServiceProvider.GetRequiredService<IHomeschoolRepository>();
    await repository.EnsureStoreCreatedAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

var portalState = app.Services.GetRequiredService<PortalState>();
app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    if (IsFrameworkOrAssetPath(path))
    {
        await next();
        return;
    }

    if (IsStudentPath(path))
    {
        var studentPath = path.HasValue ? path.Value : "/student";
        context.Response.Redirect($"{portalState.StudentPortalBaseUrl}{studentPath}");
        return;
    }

    await next();
});


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static bool IsStudentPath(PathString path)
{
    return path == "/student" || path.StartsWithSegments("/student/");
}

static bool IsFrameworkOrAssetPath(PathString path)
{
    var value = path.Value ?? "";
    return System.IO.Path.HasExtension(value)
        || value.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("/_content", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("/icons", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("/lib", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("/session.js", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("/app.css", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase);
}
