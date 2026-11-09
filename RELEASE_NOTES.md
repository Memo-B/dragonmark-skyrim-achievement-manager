# Dragonmark 1.0.0 · Stable release

The planned Skyrim SE/AE Steam achievement manager is complete. This release fixes the final review's account-switch boundary and small-screen layout issues, adds real WPF workflow coverage and moves the project into maintenance mode.

![Dragonmark 1.0.0 running in Demo Mode](assets/screenshots/dashboard.png)

## Download and install

Download **Dragonmark-v1.0.0-win-x64-Setup.exe** and its `.sha256` file. Run Setup and keep **Download the verified Steam connection component** selected. .NET is included; no SDK, administrator rights or manual DLL copying is needed. Setup downloads the pinned official Steamworks.NET component and checks its SHA-256 before installing it. Internet is required for live setup.

Start Steam, sign in to the account that owns Skyrim SE/AE, open Dragonmark and choose **Connect Steam**. Select entries or click **Unlock All**, review the count and confirm once. Inspect the successful, failed and unverified results after completion.

The portable ZIP is also attached; see [installation](docs/installation.md). For offline Demo Mode, deselect the Steam component during Setup. The app and installer are unsigned, so Windows may show an unknown-publisher warning. Download from this repository and compare the sidecar checksum with `Get-FileHash -Algorithm SHA256`.

## Fixed in 1.0.0

- Revalidate the Steam account, login, license and App ID after refresh, before each setter, before StoreStats and after verification reads. Context loss stops subsequent writes; staged but unconfirmed changes remain unverified and block retries in that session.
- Fit windows to the monitor work area and make content scrollable, keeping confirmation and cancellation controls reachable on small displays.
- Add nine account-boundary regression cases and eleven tests exercising the actual WPF windows and view model: filtered/no-selection Unlock All, one confirmation, duplicate prevention, Enter/Escape cancellation, explicit confirmation, focus traversal and logical work areas corresponding to 100/125/150/200% scaling.
- Preserve the earlier build-path privacy fix and mandatory package scan. Update the application identity, guides and screenshots for the stable release.

## Included

Dynamic achievements and authentic Steam artwork; selected unlock and Unlock All; one StoreStats batch with callback/refreshed-state verification; search, filters, rarity, details, presets, history, exports, diagnostics and isolated Demo Mode. No write runs automatically on startup. The English interface includes reduced motion and optional local artwork.

Release checks include **89 automated tests**, locked restore, formatting, build, dependency audit, package privacy checks and installer validation. The self-contained Windows x64 packages include .NET 10.0.14, component notices, file hashes and download checksums.

## Evidence and scope

The owner successfully tested individual unlocking and Unlock All on a real account on 2026-11-09. Read-only testing retrieved all 75 achievements and 75 current-state icons. Automated tests use fake APIs and demo data; they do not establish new live-write evidence.

Windows 11 x64 was tested locally. Windows 10 and a distinct AE installation remain unverified. The new logical-layout and WPF key-routing tests do not replace physical per-monitor or screen-reader testing. Independent Steam-profile/restart persistence was not separately recorded. Live clearing remains disabled. Original Skyrim, Skyrim VR and non-Steam editions are unsupported.

A timeout is not a rollback: Steam may persist staged changes later. The app reports uncertainty and prevents a blind retry. The installer and ZIP omit Valve's proprietary native binary; Setup supplies its explicit verified download.

See [validation](docs/VALIDATION.md), [compatibility](docs/compatibility.md), [changelog](CHANGELOG.md), [maintenance scope](ROADMAP.md) and [security](SECURITY.md).
