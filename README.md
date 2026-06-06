# mi-homeschool-management-system

A local-first homeschool management system for parents of K-12 students in Michigan, with an initial focus on parent-owned high-school records, evidence, credits, transcripts, diplomas, portfolio exports, and family archive packets.

## Documentation

Start with [docs/README.md](docs/README.md). Automated agents should also read [AGENTS.md](AGENTS.md) before non-trivial work.

## Current Slice

The first vertical slice provides:

- ASP.NET Core Blazor Web App shell.
- Modular monolith projects for Web, Application, Domain, Infrastructure, and Tests.
- Parent/admin and student role boundary foundation.
- Household, school profile, student, and school-year setup contracts.
- Michigan requirement seed and checklist query.
- Local development data file creation.
- Package-free contract test runner.

The accepted architecture targets SQLite. This first slice uses a repository abstraction with a package-free local data-file adapter so the app can build and run in the current environment without external package restore beyond the platform SDK. Replacing the adapter with a SQLite provider should preserve the application/domain contracts.

## Development Install

Prerequisite:

- .NET SDK 9.

From the repository root:

```powershell
$env:DOTNET_CLI_HOME = Join-Path (Get-Location) ".dotnet-home"
$env:NUGET_PACKAGES = Join-Path (Get-Location) ".nuget-packages"
$env:APPDATA = Join-Path (Get-Location) ".appdata"
$env:LOCALAPPDATA = Join-Path (Get-Location) ".localappdata"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"

dotnet restore HomeschoolManager.sln --configfile NuGet.Config
dotnet build HomeschoolManager.sln --no-restore
dotnet run --project src\HomeschoolManager.Tests\HomeschoolManager.Tests.csproj --no-build
dotnet run --project src\HomeschoolManager.Web\HomeschoolManager.Web.csproj --launch-profile http
```

Open:

```text
http://localhost:5171
```

Development data is stored under:

```text
src/HomeschoolManager.Web/.dev-data/HomeschoolManager
```

This folder is ignored by Git.

## Production Install

The first production-friendly path is a local publish and run on the parent PC:

```powershell
dotnet publish src\HomeschoolManager.Web\HomeschoolManager.Web.csproj -c Release -o publish\HomeschoolManager
.\publish\HomeschoolManager\HomeschoolManager.Web.exe
```

Open the URL printed by the app. By default, production family data is stored separately from application binaries under:

```text
%LOCALAPPDATA%/HomeschoolManager
```

Keep that data folder backed up. Future production packaging can wrap the published app in a simpler installer or launcher without changing the family data location.

## Login

- Parent/admin: uses the current local Windows user as the simple parent/admin session for this slice.
- Student: uses a local PIN. The development default PIN is `1234`.

Student access cannot perform parent/admin setup actions.
