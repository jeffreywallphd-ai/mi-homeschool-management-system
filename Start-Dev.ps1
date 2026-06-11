param(
    [switch]$SkipRestore,
    [switch]$SkipTests,
    [switch]$NoWatch,
    [switch]$StudentPortal,
    [switch]$SkipBuild
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

if ($StudentPortal) {
    $projectPath = "./src/HomeschoolManager.StudentPortal.Web/HomeschoolManager.StudentPortal.Web.csproj"
    $restoreTarget = $projectPath
    $buildTarget = $projectPath
    $launchProfile = "student-wifi-http"
} else {
    $projectPath = "./src/HomeschoolManager.Web/HomeschoolManager.Web.csproj"
    $restoreTarget = "HomeschoolManager.sln"
    $buildTarget = "HomeschoolManager.sln"
    $launchProfile = "parent-admin-http"
}

if (-not $SkipRestore -and -not $SkipBuild) {
    Write-Host "`nRestoring packages..." -ForegroundColor Cyan
    dotnet restore $restoreTarget --configfile NuGet.Config
}

if (-not $SkipBuild) {
    Write-Host "`nBuilding app..." -ForegroundColor Cyan
    dotnet build $buildTarget --no-restore
}

if (-not $SkipTests -and -not $SkipBuild) {
    Write-Host "`nRunning tests..." -ForegroundColor Cyan
    if ($StudentPortal) {
        dotnet run --project "./src/HomeschoolManager.Tests/HomeschoolManager.Tests.csproj"
    } else {
        dotnet run --project "./src/HomeschoolManager.Tests/HomeschoolManager.Tests.csproj" --no-build
    }
}

if ($StudentPortal) {
    Write-Host "`nStarting student portal..." -ForegroundColor Cyan
    Write-Host "Open http://localhost:5172 on this computer."
    Write-Host "For another device on the same Wi-Fi network, open http://<this-computer-ip>:5172"
    Write-Host "Student portal hot reload is enabled by default. Add -NoWatch only when you want a plain run."
} else {
    Write-Host "`nStarting parent/admin area..." -ForegroundColor Cyan
    Write-Host "Open http://127.0.0.1:5171"
    Write-Host "Start the student portal in a second terminal with .\Start-Dev.ps1 -StudentPortal"
}
Write-Host "Press Ctrl+C in this window to stop the app.`n"

if ($NoWatch) {
    if ($SkipBuild) {
        dotnet run --project $projectPath --launch-profile $launchProfile --no-build
    } else {
        dotnet run --project $projectPath --launch-profile $launchProfile
    }
} else {
    dotnet watch --project $projectPath --launch-profile $launchProfile
}
