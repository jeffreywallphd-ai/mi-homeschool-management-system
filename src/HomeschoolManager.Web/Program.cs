using HomeschoolManager.Web.Components;
using HomeschoolManager.Application.Persistence;
using HomeschoolManager.Application.Backups;
using HomeschoolManager.Application.Submissions;
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
builder.Services.AddSingleton<ProductionStatusService>();

var dataProtectionDirectory = builder.Environment.IsDevelopment()
    ? Path.Combine(builder.Environment.ContentRootPath, ".dev-data", "DataProtection-Keys")
    : Path.Combine(
        builder.Configuration["HomeschoolManager:DataRoot"]
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HomeschoolManager"),
        "config",
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

app.MapGet("/backups/google/callback", async (
    HttpContext context,
    SessionState session,
    RemoteBackupService remoteBackupService,
    CancellationToken cancellationToken) =>
{
    if (session.CurrentUser is null || !session.IsParentAdmin)
    {
        return Results.Redirect("/login");
    }

    var code = context.Request.Query["code"].ToString();
    var state = context.Request.Query["state"].ToString();
    var error = context.Request.Query["error"].ToString();
    if (!string.IsNullOrWhiteSpace(error))
    {
        return Results.Redirect($"/backups?googleError={Uri.EscapeDataString(error)}");
    }

    var redirectUri = $"{context.Request.Scheme}://{context.Request.Host}/backups/google/callback";
    var result = await remoteBackupService.CompleteGoogleConnectionAsync(
        session.CurrentUser,
        new CompleteGoogleConnectionCommand(state, code, redirectUri),
        cancellationToken);

    return result.Succeeded
        ? Results.Redirect("/backups?google=connected")
        : Results.Redirect($"/backups?googleError={Uri.EscapeDataString(string.Join(" ", result.Errors))}");
});

app.MapGet("/gradebook/submission-files/{submissionId:guid}/{fileId:guid}/preview", async (
    Guid submissionId,
    Guid fileId,
    SessionState session,
    SubmissionFilePreviewService previewService,
    CancellationToken cancellationToken) =>
{
    if (session.CurrentUser is null || !session.IsParentAdmin)
    {
        return Results.Forbid();
    }

    var result = await previewService.GetPreviewAsync(session.CurrentUser, submissionId, fileId, cancellationToken);
    if (!result.Succeeded)
    {
        return Results.NotFound(string.Join(Environment.NewLine, result.Errors));
    }

    var file = result.Value!;
    return Results.File(file.Content, file.ContentType, file.OriginalFileName, enableRangeProcessing: true);
});

app.MapGet("/gradebook/submission-files/{submissionId:guid}/{fileId:guid}/download", async (
    Guid submissionId,
    Guid fileId,
    SessionState session,
    SubmissionFilePreviewService previewService,
    CancellationToken cancellationToken) =>
{
    if (session.CurrentUser is null || !session.IsParentAdmin)
    {
        return Results.Forbid();
    }

    var result = await previewService.GetDownloadAsync(session.CurrentUser, submissionId, fileId, cancellationToken);
    if (!result.Succeeded)
    {
        return Results.NotFound(string.Join(Environment.NewLine, result.Errors));
    }

    var file = result.Value!;
    return Results.File(file.Content, file.ContentType, file.OriginalFileName);
});

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
