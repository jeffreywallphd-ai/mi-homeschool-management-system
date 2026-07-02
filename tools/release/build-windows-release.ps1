param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [string]$Runtime = "win-x64",

    [string]$Configuration = "Release",

    [string]$Channel = "stable",

    [string]$OutputRoot = "artifacts/release",

    [switch]$DisableNuGetAudit,

    [switch]$SkipVelopack
)

$ErrorActionPreference = "Stop"

if ($Version -notmatch '^\d+\.\d+\.\d+([\-+][0-9A-Za-z\-.]+)?$') {
    throw "Version must be SemVer2, for example 1.0.0 or 1.0.0-preview.1."
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$outputRootPath = Join-Path $repoRoot $OutputRoot
$layoutRoot = Join-Path $outputRootPath "layout"
$appRoot = Join-Path $layoutRoot "app"
$packagesRoot = Join-Path $outputRootPath "packages"
$setupRoot = Join-Path $outputRootPath "setup"

Remove-Item -LiteralPath $layoutRoot -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $setupRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $appRoot, $packagesRoot, $setupRoot | Out-Null

if ($Runtime -notlike "win-*") {
    throw "The family setup and maintenance app is Windows-only. Use a Windows runtime such as win-x64."
}

$nugetAuditArgs = @()
if ($DisableNuGetAudit) {
    $nugetAuditArgs += "-p:NuGetAudit=false"
}

dotnet publish (Join-Path $repoRoot "src/HomeschoolManager.DesktopHost/HomeschoolManager.DesktopHost.csproj") `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    @nugetAuditArgs `
    -p:PublishSingleFile=false `
    -o $appRoot
if ($LASTEXITCODE -ne 0) {
    throw "Desktop host publish failed with exit code $LASTEXITCODE."
}

dotnet publish (Join-Path $repoRoot "src/HomeschoolManager.Web/HomeschoolManager.Web.csproj") `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    @nugetAuditArgs `
    -p:PublishSingleFile=false `
    -o (Join-Path $appRoot "admin")
if ($LASTEXITCODE -ne 0) {
    throw "Parent/Admin portal publish failed with exit code $LASTEXITCODE."
}

dotnet publish (Join-Path $repoRoot "src/HomeschoolManager.StudentPortal.Web/HomeschoolManager.StudentPortal.Web.csproj") `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    @nugetAuditArgs `
    -p:PublishSingleFile=false `
    -o (Join-Path $appRoot "student")
if ($LASTEXITCODE -ne 0) {
    throw "Student portal publish failed with exit code $LASTEXITCODE."
}

dotnet publish (Join-Path $repoRoot "src/HomeschoolManager.Setup/HomeschoolManager.Setup.csproj") `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    @nugetAuditArgs `
    -p:PublishSingleFile=false `
    -o $setupRoot
if ($LASTEXITCODE -ne 0) {
    throw "Setup and maintenance app publish failed with exit code $LASTEXITCODE."
}

$serviceToolsRoot = Join-Path $appRoot "tools\service"
New-Item -ItemType Directory -Force -Path $serviceToolsRoot | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "install-homeschool-service.ps1") -Destination $serviceToolsRoot -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "uninstall-homeschool-service.ps1") -Destination $serviceToolsRoot -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "move-to-service-data-root.ps1") -Destination $serviceToolsRoot -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "enable-always-available.ps1") -Destination $serviceToolsRoot -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "disable-always-available.ps1") -Destination $serviceToolsRoot -Force

$manifest = [ordered]@{
    appId = "HomeschoolManager"
    version = $Version
    channel = $Channel
    runtime = $Runtime
    generatedAtUtc = (Get-Date).ToUniversalTime().ToString("O")
    mainExe = "HomeschoolManager.exe"
    layout = $appRoot
    packages = $packagesRoot
    familySetup = "packages/HomeschoolManager-Family-Setup/HomeschoolManager-Family-Setup.exe"
    desktopDataRoot = "%LOCALAPPDATA%/HomeschoolManagerData"
    serviceDataRoot = "%PROGRAMDATA%/HomeschoolManager"
    serviceTools = "tools/service"
    defaultAvailability = "AlwaysAvailable"
    adminPortal = "configurable: localhost or Wi-Fi"
    studentPortal = "configurable: localhost or Wi-Fi"
}
$manifest | ConvertTo-Json -Depth 5 | Set-Content -Path (Join-Path $layoutRoot "release-manifest.json") -Encoding UTF8

if (-not $SkipVelopack) {
    $vpk = Get-Command vpk -ErrorAction SilentlyContinue
    if ($vpk) {
        & $vpk.Source pack `
            --packId HomeschoolManager `
            --packTitle "Homeschool Manager" `
            --packVersion $Version `
            --packDir $appRoot `
            --mainExe "HomeschoolManager.exe" `
            --outputDir $packagesRoot `
            --channel $Channel `
            --runtime $Runtime
    }
    else {
        dotnet tool restore
        if ($LASTEXITCODE -ne 0) {
            throw "Velopack local tool restore failed with exit code $LASTEXITCODE."
        }

        Push-Location $repoRoot
        try {
            dotnet tool run vpk -- pack `
                --packId HomeschoolManager `
                --packTitle "Homeschool Manager" `
                --packVersion $Version `
                --packDir $appRoot `
                --mainExe "HomeschoolManager.exe" `
                --outputDir $packagesRoot `
                --channel $Channel `
                --runtime $Runtime
        }
        finally {
            Pop-Location
        }
    }

    if ($LASTEXITCODE -ne 0) {
        throw "Velopack packaging failed with exit code $LASTEXITCODE."
    }
}

$setupExe = Join-Path $setupRoot "HomeschoolManager-Family-Setup.exe"
if (Test-Path -LiteralPath $setupExe) {
    $familySetupRoot = Join-Path $packagesRoot "HomeschoolManager-Family-Setup"
    Remove-Item -LiteralPath $familySetupRoot -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force -Path $familySetupRoot | Out-Null
    Copy-Item -Path (Join-Path $setupRoot "*") -Destination $familySetupRoot -Recurse -Force
}
else {
    throw "Could not find the published setup and maintenance app at $setupExe."
}

Write-Host "Release layout: $appRoot"
Write-Host "Release packages: $packagesRoot"
Write-Host "Family setup: $(Join-Path $packagesRoot "HomeschoolManager-Family-Setup\HomeschoolManager-Family-Setup.exe")"
