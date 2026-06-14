param(
    [string]$SourceRoot = "$env:LOCALAPPDATA\HomeschoolManager",

    [string]$TargetRoot = "$env:ProgramData\HomeschoolManager",

    [string]$BackupRoot = "",

    [switch]$Preview
)

$ErrorActionPreference = "Stop"

if (-not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Please run this script from an Administrator PowerShell window. Windows requires that to write to ProgramData."
}

if (-not (Test-Path -LiteralPath $SourceRoot)) {
    throw "Could not find the current Homeschool Manager data folder at '$SourceRoot'."
}

if ([string]::IsNullOrWhiteSpace($BackupRoot)) {
    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $BackupRoot = Join-Path $env:ProgramData "HomeschoolManager-migration-backup-$stamp"
}

Write-Host "Current data folder: $SourceRoot"
Write-Host "Service data folder: $TargetRoot"
Write-Host "Backup folder: $BackupRoot"

if ($Preview) {
    Write-Host "Preview only. No files were copied."
    return
}

New-Item -ItemType Directory -Force -Path $BackupRoot, $TargetRoot | Out-Null

robocopy $SourceRoot $BackupRoot /E /R:2 /W:2 | Out-Host
if ($LASTEXITCODE -gt 7) {
    throw "Backup copy failed with robocopy exit code $LASTEXITCODE."
}

robocopy $SourceRoot $TargetRoot /E /R:2 /W:2 | Out-Host
if ($LASTEXITCODE -gt 7) {
    throw "Service data copy failed with robocopy exit code $LASTEXITCODE."
}

Write-Host ""
Write-Host "Family records were copied to the service data folder."
Write-Host "The original folder was left in place. The backup is at: $BackupRoot"
