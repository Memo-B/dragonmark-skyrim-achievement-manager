# Install Dragonmark

Current release: **1.0.0** for Windows x64. Use only the [official release page](https://github.com/Memo-B/dragonmark-skyrim-achievement-manager/releases/tag/v1.0.0).

## Recommended: Windows installer

1. Download `Dragonmark-v1.0.0-win-x64-Setup.exe` and its `.sha256` file.
2. Compare the file hash with the sidecar:

   ```powershell
   Get-FileHash .\Dragonmark-v1.0.0-win-x64-Setup.exe -Algorithm SHA256
   Get-Content .\Dragonmark-v1.0.0-win-x64-Setup.exe.sha256
   ```

3. Run Setup. It installs for the current Windows user without administrator privileges. The default location is `%LOCALAPPDATA%\Programs\Dragonmark`.
4. Keep **Download the verified Steam connection component** selected for live use. Setup downloads the pinned Valve binary directly from the official Steamworks.NET 2024.8.0 tag over HTTPS and validates its SHA-256 before installing application files. This requires Internet. No manual DLL copying, .NET installation, SDK or PowerShell command is required.
5. Start Steam under the same Windows user, sign in to an account licensed for Skyrim SE/AE, then launch Dragonmark from the Start Menu and choose **Connect Steam**.

The .NET 10.0.14 runtime is included. Windows 11 was tested locally; Windows 10 and other environments remain part of the compatibility test plan. The owner reported successful individual unlock and Unlock All tests on one real account. Broader compatibility and independent restart-persistence testing are ongoing.

If the component download or checksum fails, Setup stops before copying application files. Retry after fixing the connection, or go Back and deselect that task for **Demo Mode only**. The desktop app itself never downloads code or performs an achievement write on startup. The native DLL is not embedded in the published installer or ZIP; installing it does not grant redistribution rights.

The installer and application are unsigned. SmartScreen may show an unknown-publisher warning. Verify the official download and checksum; a checksum from an untrusted source does not prove authenticity.

## Portable ZIP

Download the matching ZIP and checksum, verify the hash and extract the whole folder before running `Dragonmark.exe`. Keep runtime files and `steam_appid.txt` together. Explore demo works immediately without Steam.

For live Steam access in the portable package, use PowerShell 7 from the extracted folder:

```powershell
./scripts/Setup-SteamNative.ps1
Copy-Item .\native\steam_api64.dll .\steam_api64.dll
```

The script verifies both the pinned checksum and the Valve Authenticode signature. Alternatively, use the recommended installer; it handles the dependency itself. Do not use arbitrary DLL download sites.

## Updating and removing

Close Dragonmark before running a newer installer. Setup updates the same per-user location and preserves settings/history. Uninstall through Windows **Settings → Apps → Installed apps → Dragonmark**. Local preferences/history in `%LOCALAPPDATA%\Dragonmark` are retained; removing the app never reverses Steam achievements.

For a portable build, extract a new version into a new folder and remove the old application folder after closing it. Do not mix runtime files across versions.

## Source build

Install the .NET 10 SDK matching `global.json` and PowerShell 7, then follow [development](development.md). For local live use, run `scripts/Setup-SteamNative.ps1` before building. No C++ toolchain or SKSE is required.
