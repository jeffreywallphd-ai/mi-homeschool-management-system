param(
    [string]$AppExe = "$env:LOCALAPPDATA\HomeschoolManager\current\HomeschoolManager.exe",

    [string]$ServiceName = "HomeschoolManager",

    [string]$ParentWindowsAccount = "",

    [ValidateSet("Localhost", "Wifi")]
    [string]$AdminMode = "Localhost",

    [ValidateSet("Localhost", "Wifi")]
    [string]$StudentMode = "Localhost",

    [string]$AdminWifiHost = "",

    [string]$StudentWifiHost = "",

    [int]$AdminPort = 5171,

    [int]$StudentPort = 5172,

    [switch]$Start
)

$ErrorActionPreference = "Stop"

if (-not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Please run this script from an Administrator PowerShell window. Windows requires that to install a background service."
}

$resolvedAppExe = Resolve-Path -LiteralPath $AppExe -ErrorAction SilentlyContinue
if (-not $resolvedAppExe) {
    throw "Could not find HomeschoolManager.exe at '$AppExe'. Install the app first, or pass -AppExe with the full path to HomeschoolManager.exe."
}

$dataRoot = Join-Path $env:ProgramData "HomeschoolManager"
New-Item -ItemType Directory -Force -Path `
    $dataRoot, `
    (Join-Path $dataRoot "data"), `
    (Join-Path $dataRoot "files"), `
    (Join-Path $dataRoot "templates"), `
    (Join-Path $dataRoot "backups\automatic"), `
    (Join-Path $dataRoot "backups\manual"), `
    (Join-Path $dataRoot "backups\exports"), `
    (Join-Path $dataRoot "logs"), `
    (Join-Path $dataRoot "config") | Out-Null

$configureArgs = @(
    "--dry-run",
    "--host-mode", "Service",
    "--service-name", $ServiceName,
    "--admin-mode", $AdminMode,
    "--admin-port", $AdminPort,
    "--admin-wifi-host", $AdminWifiHost,
    "--student-mode", $StudentMode,
    "--student-port", $StudentPort,
    "--student-wifi-host", $StudentWifiHost,
    "--no-browser",
    "--skip-update-check"
)

if (-not [string]::IsNullOrWhiteSpace($ParentWindowsAccount)) {
    $configureArgs += @("--parent-windows-account", $ParentWindowsAccount)
}

$env:HOMESCHOOL_MANAGER_PRODUCTION_ROOT = $dataRoot
try {
    & $resolvedAppExe.Path @configureArgs | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "Homeschool Manager service configuration failed with exit code $LASTEXITCODE."
    }
}
finally {
    Remove-Item Env:\HOMESCHOOL_MANAGER_PRODUCTION_ROOT -ErrorAction SilentlyContinue
}

$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
$binaryPath = "`"$($resolvedAppExe.Path)`" --service --service-name `"$ServiceName`""
if ($existing) {
    sc.exe config $ServiceName binPath= $binaryPath start= auto | Out-Host
}
else {
    sc.exe create $ServiceName binPath= $binaryPath start= auto DisplayName= "Homeschool Manager" | Out-Host
}

if ($LASTEXITCODE -ne 0) {
    throw "Windows could not create or update the Homeschool Manager service."
}

sc.exe sidtype $ServiceName unrestricted | Out-Host
sc.exe failure $ServiceName reset= 86400 actions= restart/60000/restart/60000/none/0 | Out-Host

icacls $dataRoot /inheritance:r | Out-Host
icacls $dataRoot /grant:r "SYSTEM:(OI)(CI)(F)" "Administrators:(OI)(CI)(F)" "NT SERVICE\$ServiceName`:(OI)(CI)(M)" | Out-Host
if (-not [string]::IsNullOrWhiteSpace($ParentWindowsAccount)) {
    icacls $dataRoot /grant "$ParentWindowsAccount`:(OI)(CI)(M)" | Out-Host
}

if ($Start) {
    Start-Service -Name $ServiceName
}

Write-Host ""
Write-Host "Homeschool Manager background service is installed."
Write-Host "Family records for service mode are stored at: $dataRoot"
Write-Host "Parent/Admin portal mode: $AdminMode on port $AdminPort"
Write-Host "Student portal mode: $StudentMode on port $StudentPort"
