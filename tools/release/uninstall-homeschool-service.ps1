param(
    [string]$ServiceName = "HomeschoolManager",

    [switch]$RemoveFamilyData
)

$ErrorActionPreference = "Stop"

if (-not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Please run this script from an Administrator PowerShell window. Windows requires that to remove a background service."
}

$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($service) {
    if ($service.Status -ne "Stopped") {
        Stop-Service -Name $ServiceName -Force
    }

    sc.exe delete $ServiceName | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "Windows could not remove the Homeschool Manager service."
    }
}

$dataRoot = Join-Path $env:ProgramData "HomeschoolManager"
if ($RemoveFamilyData) {
    if (Test-Path -LiteralPath $dataRoot) {
        Remove-Item -LiteralPath $dataRoot -Recurse -Force
    }

    Write-Host "Homeschool Manager service and service-mode family data were removed."
}
else {
    Write-Host "Homeschool Manager service was removed. Family records were left in place at: $dataRoot"
}
