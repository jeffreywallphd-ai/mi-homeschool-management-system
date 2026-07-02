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

    [int]$StudentPort = 5172
)

$ErrorActionPreference = "Stop"

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal] $identity
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-IsAdministrator)) {
    Write-Host "Windows needs permission to turn on Always Available access."
    Write-Host "A Windows permission prompt will open. Choose Yes to continue."
    $pwshCommand = Get-Command pwsh -ErrorAction SilentlyContinue
    if ($pwshCommand) {
        $powerShell = $pwshCommand.Source
    }
    else {
        $powerShell = (Get-Command powershell -ErrorAction Stop).Source
    }

    $forwarded = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "`"$PSCommandPath`"")
    foreach ($parameter in $PSBoundParameters.GetEnumerator()) {
        $forwarded += "-$($parameter.Key)"
        $forwarded += "`"$($parameter.Value)`""
    }

    Start-Process -FilePath $powerShell -ArgumentList $forwarded -Verb RunAs -Wait
    return
}

if ([string]::IsNullOrWhiteSpace($ParentWindowsAccount)) {
    $ParentWindowsAccount = [Security.Principal.WindowsIdentity]::GetCurrent().Name
}

$migrateScript = Join-Path $PSScriptRoot "move-to-service-data-root.ps1"
$installScript = Join-Path $PSScriptRoot "install-homeschool-service.ps1"

if (-not (Test-Path -LiteralPath $migrateScript)) {
    throw "Could not find the Always Available data move helper at '$migrateScript'."
}

if (-not (Test-Path -LiteralPath $installScript)) {
    throw "Could not find the Always Available setup helper at '$installScript'."
}

Write-Host ""
Write-Host "Turning on Always Available access for Homeschool Manager..."
Write-Host "This keeps the student portal available while this PC is on and awake."
Write-Host "Your family records will be copied to the protected Windows folder used by Always Available access."
Write-Host ""

& $migrateScript

& $installScript `
    -AppExe $AppExe `
    -ServiceName $ServiceName `
    -ParentWindowsAccount $ParentWindowsAccount `
    -AdminMode $AdminMode `
    -StudentMode $StudentMode `
    -AdminWifiHost $AdminWifiHost `
    -StudentWifiHost $StudentWifiHost `
    -AdminPort $AdminPort `
    -StudentPort $StudentPort `
    -Start

Write-Host ""
Write-Host "Always Available access is turned on."
Write-Host "Students can use the student portal while this PC is on and awake."
