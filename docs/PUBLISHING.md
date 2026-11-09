# Publishing Dragonmark

Repository: [Memo-B/dragonmark-skyrim-achievement-manager](https://github.com/Memo-B/dragonmark-skyrim-achievement-manager). The owner authorized the first source push and public release after reporting successful individual unlock and Unlock All tests. The current stable version is **1.0.0**, authorized after the final quality fixes. Compatibility limits remain explicit.

## Build and review

Use PowerShell 7, .NET SDK from `global.json`, and Inno Setup 7.1.1 from its [official download](https://jrsoftware.org/isdl.php). The compiler is a developer build tool, not an end-user dependency.

```powershell
./scripts/Capture-Screenshots.ps1
./scripts/Package.ps1 -InstallerCompiler 'C:\Path\To\Inno Setup 7\ISCC.exe'
./scripts/Prepare-ReleaseNotes.ps1
```

Package.ps1 performs source checks, locked restore, formatting, build, all tests and a dependency audit. It creates a unique stage, a self-contained ZIP with file hashes and, when the compiler is provided, Setup.exe. Both get SHA-256 sidecars. The installer excludes the Valve binary and offers a visible, pinned download task. Build-Installer.ps1 validates stage hashes before compilation. Existing same-version archives/installers are never silently overwritten.

Before publishing, inspect the staged Git diff, all package hashes, a fresh extracted demo launch, installer live-component and offline-demo paths, and uninstall behavior. Do not add native DLLs, generated archives, credentials, account logs, settings or private reports to Git.

## Source, metadata and tag

For a new local checkout targeting the existing empty remote:

```powershell
git init -b main
git remote add origin https://github.com/Memo-B/dragonmark-skyrim-achievement-manager.git
git add .
git diff --cached --check
git diff --cached --stat
git diff --cached
git commit -m "Release Dragonmark 1.0.0 with Windows installer"
git push -u origin main
```

If the remote already contains history, fetch and integrate it first. Never overwrite it with a force push. Confirm hosted CI before tagging the final commit:

```powershell
gh run list --repo Memo-B/dragonmark-skyrim-achievement-manager
gh run watch RUN_ID --repo Memo-B/dragonmark-skyrim-achievement-manager --exit-status
$metadata = Get-Content .github/repository-metadata.json -Raw | ConvertFrom-Json
gh repo edit $metadata.repository --description $metadata.description --add-topic ($metadata.topics -join ',')
git tag -a v1.0.0 -m "Dragonmark 1.0.0 stable release"
git push origin v1.0.0
```

The metadata file contains the exact About description and 15 relevant topics. No homepage or SKSE integration is invented. Upload `assets/branding/social-preview.png` through **Repository Settings → General → Social preview**. Current screenshots appear in both READMEs and tag-scoped release notes.

## Publish the reviewed artifacts

```powershell
gh release create v1.0.0 .\artifacts\Dragonmark-v1.0.0-win-x64-Setup.exe .\artifacts\Dragonmark-v1.0.0-win-x64-Setup.exe.sha256 .\artifacts\Dragonmark-v1.0.0-win-x64.zip .\artifacts\Dragonmark-v1.0.0-win-x64.zip.sha256 --repo Memo-B/dragonmark-skyrim-achievement-manager --verify-tag --latest --title "Dragonmark 1.0.0 — Windows installer" --notes-file .\artifacts\RELEASE_NOTES-v1.0.0.md
```

Verify all four public assets and their checksums after publication. Use explicit version URLs for reproducible downloads; `/releases/latest` points to the latest stable release. Do not claim passing hosted checks from local tests alone. Future versions should bump `Directory.Build.props`, regenerate screenshots, update documentation, rebuild and repeat validation.
