param(
    [string]$ServiceName = "HomeschoolManager",

    [switch]$RemoveFamilyData
)

$ErrorActionPreference = "Stop"

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal] $identity
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-IsAdministrator)) {
    Write-Host "Windows needs permission to turn off Always Available access."
    Write-Host "A Windows permission prompt will open. Choose Yes to continue."
    $pwshCommand = Get-Command pwsh -ErrorAction SilentlyContinue
    if ($pwshCommand) {
        $powerShell = $pwshCommand.Source
    }
    else {
        $powerShell = (Get-Command powershell -ErrorAction Stop).Source
    }

    $forwarded = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "`"$PSCommandPath`"", "-ServiceName", "`"$ServiceName`"")
    if ($RemoveFamilyData) {
        $forwarded += "-RemoveFamilyData"
    }

    Start-Process -FilePath $powerShell -ArgumentList $forwarded -Verb RunAs -Wait
    return
}

$uninstallScript = Join-Path $PSScriptRoot "uninstall-homeschool-service.ps1"
if (-not (Test-Path -LiteralPath $uninstallScript)) {
    throw "Could not find the Always Available removal helper at '$uninstallScript'."
}

Write-Host ""
Write-Host "Turning off Always Available access for Homeschool Manager..."
if ($RemoveFamilyData) {
    Write-Host "You chose to remove the protected Always Available records folder."
}
else {
    Write-Host "Your protected Always Available records folder will be left in place."
}

if ($RemoveFamilyData) {
    & $uninstallScript -ServiceName $ServiceName -RemoveFamilyData
}
else {
    & $uninstallScript -ServiceName $ServiceName
}

Write-Host ""
Write-Host "Always Available access is turned off."
