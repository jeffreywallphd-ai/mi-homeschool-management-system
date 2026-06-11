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
.\Start-Dev.ps1
```

From Git Bash:

```bash
./start-dev.sh
```

That script restores packages, builds the solution, runs the contract tests, and starts the parent/admin web app with hot reload.

To start without hot reload:

```powershell
.\Start-Dev.ps1 -NoWatch
```

From Git Bash:

```bash
./start-dev.sh -NoWatch
```

The parent/admin area is the `HomeschoolManager.Web` build. It runs on the local computer only:

```text
http://127.0.0.1:5171
```

The student portal is the separate `HomeschoolManager.StudentPortal.Web` build. It shares the same core application services and local development data folder, but it serves only student portal routes on port `5172` and is bound for access from another device on the same Wi-Fi network:

```powershell
.\Start-Dev.ps1 -StudentPortal
```

The student portal start command uses `dotnet watch` by default, so Razor, CSS, and shared-code changes can hot reload or rebuild without manually restarting the portal. Use `-NoWatch` only when you intentionally want a plain non-watch run:

```powershell
.\Start-Dev.ps1 -StudentPortal -NoWatch
```

Open the student portal on this computer at:

```text
http://localhost:5172
```

From a student device on the same Wi-Fi network, use the parent computer's local network address:

```text
http://<parent-computer-ip>:5172
```

For manual startup:

```powershell
$env:DOTNET_CLI_HOME = Join-Path (Get-Location) ".dotnet-home"
$env:NUGET_PACKAGES = Join-Path (Get-Location) ".nuget-packages"
$env:APPDATA = Join-Path (Get-Location) ".appdata"
$env:LOCALAPPDATA = Join-Path (Get-Location) ".localappdata"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"

dotnet restore HomeschoolManager.sln --configfile NuGet.Config
dotnet build HomeschoolManager.sln --no-restore
dotnet run --project "./src/HomeschoolManager.Tests/HomeschoolManager.Tests.csproj" --no-build
dotnet watch --project "./src/HomeschoolManager.Web/HomeschoolManager.Web.csproj" --launch-profile parent-admin-http
dotnet watch --project "./src/HomeschoolManager.StudentPortal.Web/HomeschoolManager.StudentPortal.Web.csproj" --launch-profile student-wifi-http
```

Open:

```text
http://127.0.0.1:5171
```

Development data is stored under the shared repo-level development folder:

```text
.dev-data/HomeschoolManager
```

When using `Start-Dev.ps1`, both builds also use the repository-local `.localappdata` and `.appdata` folders. Development data folders are ignored by Git.

## Production Install

The first production-friendly path is a local publish and run on the parent PC:

```powershell
dotnet publish "./src/HomeschoolManager.Web/HomeschoolManager.Web.csproj" -c Release -o "./publish/HomeschoolManager"
.\publish\HomeschoolManager\HomeschoolManager.Web.exe
```

Open the URL printed by the app. By default, production family data is stored separately from application binaries under:

```text
%LOCALAPPDATA%/HomeschoolManager
```

Keep that data folder backed up. Future production packaging can wrap the published app in a simpler installer or launcher without changing the family data location.

## Login

- Parent/admin: uses the current local Windows user as the simple parent/admin session for this slice.
- Student: uses a local PIN on the separate student portal build and port. The development default PIN is `1234`.

Student access cannot perform parent/admin setup actions. Parent/admin student preview routes stay inside the parent/admin build under `/student-preview/...`; the true student portal routes are served by the student portal build under `/student/...`.
