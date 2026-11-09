# Dragonmark development

## First build

On Windows x64, install .NET 10 SDK and PowerShell 7, then open the source directory. `global.json` selects SDK 10.0.100 with latestFeature roll-forward. Runtime packages are pinned separately to 10.0.14 for portable publication. No C++ toolchain is used.

```powershell
dotnet restore Dragonmark.sln --locked-mode
dotnet build Dragonmark.sln -c Release --no-restore
dotnet test Dragonmark.sln -c Release --no-build
dotnet run --project src/Dragonmark.App/Dragonmark.App.csproj -c Release -- --demo
```

The app must remain entirely English. Turkish and future translated documentation are welcome. Steam integration is optional for UI/tests: no native library, Steam client or account is required for the demo and automated tests. Live setup is described separately in [installation](installation.md).

## Checks before a PR

```powershell
./scripts/Check-Repository.ps1
dotnet format Dragonmark.sln --verify-no-changes --no-restore
./scripts/Check-Dependencies.ps1
dotnet test Dragonmark.sln -c Release --no-build --logger trx --results-directory TestResults
```

Run `dotnet format Dragonmark.sln --no-restore` to apply standard formatting, then rebuild when code changes. The repository check validates local Markdown target paths, balanced fences, topics and a narrow set of credential/binary patterns. It does not validate every Markdown anchor, external URL or possible secret. Manually review Git staging and rendered documentation.

NuGet audit includes direct/transitive dependencies and checks parsed JSON because a vulnerability listing can exit successfully despite findings. A clean audit means no reported advisories at that time, not proof of security. Package lock files are committed. For a deliberate dependency update, restore without `--locked-mode`, inspect the changed lock files and license metadata, and rerun all checks. Update [third-party notices](../THIRD_PARTY_NOTICES.md) and regenerate the [license inventory](dependency-licenses.csv) with `./scripts/Export-DependencyLicenses.ps1`.

## Style and tests

Follow `.editorconfig`, nullable annotations and constructor injection. Keep provider decisions out of WPF views. Write behavioral tests for outcome evidence, callback identity, account/context changes, timeout and concurrency cases; do not mirror implementation details. Keep fixtures fictional. Normal tests/CI must never initialize real Steam or change achievements.

UI work can use the demo provider and [reproducible screenshots](SCREENSHOTS.md). Verify keyboard focus, contrast, scrolling and high-DPI layouts when changing views. The list is virtualized; no performance benchmark is claimed.

## Packaging and publication

```powershell
./scripts/Package.ps1
```

This runs repository checks, locked restore, format, build, tests and dependency audit before self-contained publication into a fresh stage. Version comes from `Directory.Build.props`; stable `X.Y.Z` and prerelease `X.Y.Z-preview.N` / `X.Y.Z-beta.N` versions are accepted after the same validation gates. Do not relabel a failed build as stable.

The ZIP excludes the proprietary native DLL, includes licenses and a SHA-256 file manifest, and has a separate archive checksum. Existing archives are not overwritten. Runtime inputs are pinned, but ZIP timestamps and the host SDK can affect byte reproducibility. The manual GitHub workflow uploads artifacts only; it has no permission to create releases. [Owner publication commands](PUBLISHING.md).
