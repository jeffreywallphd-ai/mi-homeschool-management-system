#!/usr/bin/env bash
set -euo pipefail

script_path="${BASH_SOURCE[0]}"
case "$script_path" in
  */*) script_dir="${script_path%/*}" ;;
  *) script_dir="." ;;
esac
script_dir="$(cd -- "$script_dir" && pwd)"
powershell_cmd=""

if command -v powershell.exe >/dev/null 2>&1; then
  powershell_cmd="powershell.exe"
elif command -v powershell >/dev/null 2>&1; then
  powershell_cmd="powershell"
elif command -v pwsh >/dev/null 2>&1; then
  powershell_cmd="pwsh"
else
  echo "Could not find PowerShell. Install PowerShell or run tools/release/build-windows-release.ps1 from a PowerShell prompt." >&2
  exit 1
fi

exec "$powershell_cmd" -NoProfile -ExecutionPolicy Bypass -File "$script_dir/build-windows-release.ps1" "$@"
