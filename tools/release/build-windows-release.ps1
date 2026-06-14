param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [string]$Runtime = "win-x64",

    [string]$Configuration = "Release",

    [string]$Channel = "stable",

    [string]$OutputRoot = "artifacts/release",

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

Remove-Item -LiteralPath $layoutRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $appRoot, $packagesRoot | Out-Null

dotnet publish (Join-Path $repoRoot "src/HomeschoolManager.DesktopHost/HomeschoolManager.DesktopHost.csproj") `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=false `
    -o $appRoot
if ($LASTEXITCODE -ne 0) {
    throw "Desktop host publish failed with exit code $LASTEXITCODE."
}

dotnet publish (Join-Path $repoRoot "src/HomeschoolManager.Web/HomeschoolManager.Web.csproj") `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=false `
    -o (Join-Path $appRoot "admin")
if ($LASTEXITCODE -ne 0) {
    throw "Parent/Admin portal publish failed with exit code $LASTEXITCODE."
}

dotnet publish (Join-Path $repoRoot "src/HomeschoolManager.StudentPortal.Web/HomeschoolManager.StudentPortal.Web.csproj") `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=false `
    -o (Join-Path $appRoot "student")
if ($LASTEXITCODE -ne 0) {
    throw "Student portal publish failed with exit code $LASTEXITCODE."
}

$serviceToolsRoot = Join-Path $appRoot "tools\service"
New-Item -ItemType Directory -Force -Path $serviceToolsRoot | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "install-homeschool-service.ps1") -Destination $serviceToolsRoot -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "uninstall-homeschool-service.ps1") -Destination $serviceToolsRoot -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "move-to-service-data-root.ps1") -Destination $serviceToolsRoot -Force

$manifest = [ordered]@{
    appId = "HomeschoolManager"
    version = $Version
    channel = $Channel
    runtime = $Runtime
    generatedAtUtc = (Get-Date).ToUniversalTime().ToString("O")
    mainExe = "HomeschoolManager.exe"
    layout = $appRoot
    packages = $packagesRoot
    desktopDataRoot = "%LOCALAPPDATA%/HomeschoolManager"
    serviceDataRoot = "%PROGRAMDATA%/HomeschoolManager"
    serviceTools = "tools/service"
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

Write-Host "Release layout: $appRoot"
Write-Host "Release packages: $packagesRoot"
