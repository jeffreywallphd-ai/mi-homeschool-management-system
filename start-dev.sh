#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

powershell -ExecutionPolicy Bypass -File "$script_dir/Start-Dev.ps1" "$@"
