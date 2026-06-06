param(
    [switch]$SkipRestore,
    [switch]$SkipTests,
    [switch]$NoWatch
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

$env:DOTNET_CLI_HOME = Join-Path $root ".dotnet-home"
$env:NUGET_PACKAGES = Join-Path $root ".nuget-packages"
$env:APPDATA = Join-Path $root ".appdata"
$env:LOCALAPPDATA = Join-Path $root ".localappdata"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"

Write-Host "Homeschool Manager dev startup" -ForegroundColor Cyan
Write-Host "Repository: $root"

if (-not $SkipRestore) {
    Write-Host "`nRestoring packages..." -ForegroundColor Cyan
    dotnet restore HomeschoolManager.sln --configfile NuGet.Config
}

Write-Host "`nBuilding solution..." -ForegroundColor Cyan
dotnet build HomeschoolManager.sln --no-restore

if (-not $SkipTests) {
    Write-Host "`nRunning tests..." -ForegroundColor Cyan
    dotnet run --project "./src/HomeschoolManager.Tests/HomeschoolManager.Tests.csproj" --no-build
}

Write-Host "`nStarting web app..." -ForegroundColor Cyan
Write-Host "Open http://localhost:5171"
Write-Host "Press Ctrl+C in this window to stop the app.`n"

if ($NoWatch) {
    dotnet run --project "./src/HomeschoolManager.Web/HomeschoolManager.Web.csproj" --launch-profile http
} else {
    dotnet watch --project "./src/HomeschoolManager.Web/HomeschoolManager.Web.csproj" --launch-profile http
}
