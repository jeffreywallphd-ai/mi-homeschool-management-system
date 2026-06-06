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


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
