# Dragonmark · Skyrim Achievement Manager

![Dragonmark — Your achievements. A clearer view.](assets/branding/hero.png)

[![Build and test](https://github.com/Memo-B/dragonmark-skyrim-achievement-manager/actions/workflows/ci.yml/badge.svg)](https://github.com/Memo-B/dragonmark-skyrim-achievement-manager/actions/workflows/ci.yml)
[![Public beta](https://img.shields.io/badge/release-0.3.0--beta.1-D8BB7D)](https://github.com/Memo-B/dragonmark-skyrim-achievement-manager/releases/tag/v0.3.0-beta.1)
[![Windows x64](https://img.shields.io/badge/platform-Windows_x64-0078D4)](docs/compatibility.md)
[![MIT license](https://img.shields.io/badge/license-MIT-D8BB7D)](LICENSE)

**See your progress. Choose what to unlock. Make every change deliberately.**

Dragonmark is a free, open-source Windows application for **Skyrim Special Edition / Anniversary Edition Steam achievements**. Browse authentic achievement artwork, inspect progress and rarity, unlock selected entries, or unlock every missing achievement with **one explicit confirmation**.

**[Download Windows installer](https://github.com/Memo-B/dragonmark-skyrim-achievement-manager/releases/download/v0.3.0-beta.1/Dragonmark-v0.3.0-beta.1-win-x64-Setup.exe)** · **[Release notes & checksums](https://github.com/Memo-B/dragonmark-skyrim-achievement-manager/releases/tag/v0.3.0-beta.1)** · [Portable ZIP](https://github.com/Memo-B/dragonmark-skyrim-achievement-manager/releases/download/v0.3.0-beta.1/Dragonmark-v0.3.0-beta.1-win-x64.zip)

[English](README.md) · [Türkçe](README.tr.md) · [Installation](docs/installation.md) · [Troubleshooting](docs/troubleshooting.md) · [Roadmap](ROADMAP.md)

![Dragonmark dashboard captured from the compiled application](assets/screenshots/dashboard.png)

*Actual application, visibly labelled Demo Mode. Public screenshots use fictional entries to keep account data private; live mode displays authentic Steam achievement artwork.*

## Start in three steps

1. **Install.** Download `Dragonmark-v0.3.0-beta.1-win-x64-Setup.exe` and run it. Leave **Download the verified Steam connection component** selected. Setup includes .NET and handles the small Steam component download; no SDK, manual DLL copying or administrator rights are needed.
2. **Connect.** Start Steam, sign in to your account with Skyrim SE/AE, launch Dragonmark from the Start Menu and click **Connect Steam**.
3. **Review and unlock.** Select individual achievements or click **Unlock All**. Review the target count, confirm once, then inspect the results.

Internet is needed for the Steam component download and live Steam access. For an offline tour, deselect that component during Setup and choose **Explore demo**. The installer supports normal Windows uninstallation; portable users can follow the [ZIP setup guide](docs/installation.md).

## Built for the achievement collection

| Feature | What you can do |
| --- | --- |
| **Unlock All / Unlock Missing** | Find every currently locked achievement automatically, regardless of the active filter or selection |
| Selected unlock | Preview and confirm a single achievement or your chosen group |
| Authentic Steam artwork | See names, descriptions, icons, unlock dates and available global rarity from Steam |
| Search and filters | Find entries by name/API identifier and narrow the collection by status or rarity |
| Progress and details | Inspect completion, recent unlocks and each achievement's recorded state |
| Results you can inspect | Review successful, failed and unverified outcomes separately, with per-target evidence |
| Presets and reports | Save selections, load them for review, and export JSON/CSV reports |
| A considered desktop interface | Original Nordic artwork, licensed Cinzel typography, reduced motion and optional local backgrounds |
| Offline Demo Mode | Explore the interface using fictional achievements without changing a Steam account |

All application-owned UI is **English**. Turkish documentation is included; Steam metadata and Windows file dialogs may follow their own language settings.

## Tested, with evidence you can inspect

| Check | Recorded result |
| --- | --- |
| Real Steam reading | All **75 achievements** and **75 current-state icons** retrieved on the tested installation |
| Real unlocking | The owner tested individual unlocking and **Unlock All** and reported both working successfully on 2026-11-09 |
| Automated behavior tests | **69 passed**, covering batch planning, callback handling, result classification, account checks and artwork state |
| Windows installer | Install, verified Steam component download, application launch, offline demo install and uninstall passed locally |
| Build and dependencies | Zero build errors/warnings; locked dependencies and audit checks passed |
| Hosted checks | [First main-branch CI run passed](https://github.com/Memo-B/dragonmark-skyrim-achievement-manager/actions/runs/42670925791); the badge above shows current status |
| Download integrity | Installer and ZIP each have a SHA-256 sidecar; the ZIP includes a per-file manifest |

This is a **working public beta**. Live unlock evidence is the owner's manual test report; broader Windows/account coverage and independent restart-persistence checks remain on the [validation plan](docs/VALIDATION.md). These limits are kept separate from completed work in the [roadmap](ROADMAP.md).

## Your account, your confirmation

Dragonmark uses the running Steam client. It never asks for a Steam password, Steam Guard code or Web API key, and it has no application telemetry, service or driver. It does not edit Skyrim saves or complete in-game quests.

- **No automatic unlocking:** connecting, starting the app or loading a preset never starts an achievement write.
- **One clear confirmation:** Unlock All refreshes the full collection and shows the missing count and targets before applying changes.
- **Verified result reporting:** a setter returning true is insufficient. Reported success requires Steam's matching store acknowledgement and matching refreshed achievement state.
- **Duplicate protection:** overlapping operations are blocked; batch writes have a two-minute cooldown.
- **Honest uncertainty:** timeouts and ambiguous results are labelled unverified, and further writes are blocked in that session. Inspect Steam before retrying; a timeout is not a rollback.

Settings and history stay in `%LOCALAPPDATA%\Dragonmark`. Review exports before sharing them. Use your own account; changes are not promised to be reversible. [Security and private vulnerability reporting](SECURITY.md) · [How Steam integration works](docs/steam-integration.md).

## Interface gallery

| Achievement browser | Achievement details |
| --- | --- |
| ![Demo achievement gallery](assets/screenshots/achievements-gallery.png) | ![Demo achievement details](assets/screenshots/achievement-details.png) |
| One explicit confirmation | Search and rarity filters |
| ![Unlock All confirmation](assets/screenshots/unlock-all-confirmation.png) | ![Filtered collection](assets/screenshots/achievement-filters.png) |

[Settings](assets/screenshots/settings.png) · [Diagnostics](assets/screenshots/diagnostics.png) · [Design and asset provenance](docs/visual-design.md). Demo artwork is original; authentic Steam artwork is fetched in live mode and is not bundled in downloads.

## Compatibility and current limits

| Environment | Status |
| --- | --- |
| Windows 11 x64 | Built, installed and tested locally; owner's live test succeeded |
| Windows 10 x64 | Targeted; broader testing is pending |
| Steam Skyrim SE / AE, App ID **489830** | Target game; a separate AE installation has not been tested |
| Original Skyrim, Skyrim VR, GOG, Game Pass, consoles | Unsupported |
| SKSE or running Skyrim | Neither is required by Dragonmark; Steam must be running and authenticated |
| Live achievement clearing | Disabled; demo clearing is available in advanced mode |

The installer and app are **unsigned**, so Windows SmartScreen may show an unknown-publisher warning. Download only from this repository's release page and verify the accompanying checksum. This is an independent community tool, not an official Valve or Bethesda application. [Full compatibility notes](docs/compatibility.md).

## Verify a download

Download the installer and its `.sha256` file from the same release, then compare:

```powershell
Get-FileHash .\Dragonmark-v0.3.0-beta.1-win-x64-Setup.exe -Algorithm SHA256
Get-Content .\Dragonmark-v0.3.0-beta.1-win-x64-Setup.exe.sha256
```

Setup retrieves its Steam component from a pinned official Steamworks.NET tag over HTTPS and rejects a mismatched SHA-256. The application itself never downloads executable code. The Valve native binary is not embedded in the published installer or ZIP. A checksum checks integrity; it does not establish trust in an unrelated download site.

## Build and contribute

Use Windows x64, the .NET 10 SDK specified by `global.json`, and PowerShell 7:

```powershell
dotnet restore Dragonmark.sln --locked-mode
dotnet build Dragonmark.sln -c Release --no-restore
dotnet test Dragonmark.sln -c Release --no-build
dotnet run --project src/Dragonmark.App/Dragonmark.App.csproj -c Release -- --demo

# Optional local Steam dependency, followed by a rebuild:
./scripts/Setup-SteamNative.ps1

# Validate and build ZIP; add the installer with a verified Inno Setup compiler:
./scripts/Package.ps1 -InstallerCompiler 'C:\Path\To\Inno Setup 7\ISCC.exe'
```

The app separates WPF presentation, core batch planning, Steamworks integration and local storage. Public packages include .NET 10.0.14 and component licenses. CI uses fictional fixtures and never changes a real Steam account. [Architecture](docs/architecture.md) · [Development](docs/development.md) · [Contributing](CONTRIBUTING.md) · [Release procedure](docs/PUBLISHING.md).

Found a problem? [Open an issue](https://github.com/Memo-B/dragonmark-skyrim-achievement-manager/issues) with the app version, Windows version, steps and a redacted result. Never attach raw account logs or credentials. Security issues belong in the [private disclosure channel](https://github.com/Memo-B/dragonmark-skyrim-achievement-manager/security/advisories/new).

## License and credits

Original code and Dragonmark artwork are [MIT licensed](LICENSE), with original attribution retained. Cinzel and dependencies have their own [licenses and notices](THIRD_PARTY_NOTICES.md). Dragonmark is unaffiliated with Valve, Steam, Bethesda or Microsoft. Maintained by [Memo-B](https://github.com/Memo-B).
