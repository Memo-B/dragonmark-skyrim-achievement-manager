# Changelog

## 0.3.0-beta.2 — 2026-11-09

- Withdrew the beta.1 binary downloads after finding local build paths in PE debug metadata in four application DLLs.
- Disabled Release debug metadata and mapped build source paths to a neutral root.
- Added a mandatory binary/text privacy gate before ZIP packaging and installer compilation; it rejects Windows profile paths, Steam ID patterns and private/debug files.
- Removed detailed local Windows edition/build information from current compatibility documentation.
- No Steam account identifier, private account snapshot, settings or activity log was found in the audited beta.1 payload. Existing copies cannot be recalled.
- Achievement operation behavior is unchanged.


## 0.3.0-beta.1 — 2026-11-09

- First public beta, following owner-reported successful individual unlock and Unlock All testing on a real Steam account.
- Added a per-user Windows installer, Start Menu/optional desktop shortcuts and an uninstaller.
- Installer offers a visible, checksum-verified Steam component download; .NET is included and manual DLL copying is unnecessary. Proprietary native code is not embedded in release assets.
- Updated release documentation and current-version demo captures. No achievement write semantics changed from the owner-tested build.
- Independent restart-persistence, broader compatibility, accessibility and code-signing work remain pending.


## 0.3.0-preview.1 — 2026-11-09 (unpublished)

- Rebuilt the WPF visual system with original mountains, Nordic metalwork, OFL Cinzel headings, frost-white text and styled native controls.
- Added variant-aware Steam icon callbacks/caching and progressive artwork in every achievement entry and detail view; read-only probing loaded 75/75 icons.
- Added per-entry unlock previews, optional local theme artwork, reduced motion, About, refined confirmation, per-target outcome cards and explicit operation phases.
- Added seven authentic Demo Mode screenshots, extra resolution/state review captures and version-scoped release-note preparation.
- Added six artwork/progress behavior tests, preserving existing Steam persistence and Unlock All requirements.
- Live writes remain unverified. Final mouse/keyboard automation and real per-monitor DPI testing remain pending; no stable release or remote publication.

## 0.2.0-preview.1 — 2026-11-09 (unpublished)

- Standardized Dragonmark · Skyrim Achievement Manager identity and assembly metadata.
- Added original SVG/PNG/ICO branding and a refreshed isolated demo screenshot.
- Added the Locked-page Unlock Missing alias to the existing confirmed Unlock All flow.
- Rewrote English/Turkish documentation and added architecture, installation, compatibility, troubleshooting, development and naming research.
- Added locked restore, dependency/license audits, pinned CI actions and preview-only ZIP packaging with manifest and SHA-256 checksums.
- Prepared owner-controlled GitHub metadata and publication commands; no remote changes or releases performed.
- Live write persistence remains unverified. No claim of completed stable-release validation.

## 0.1.0-preview — 2026-11-09 (local development, unpublished)

- Re-engineered the original WinForms project into an English WPF application.
- Migrated the final application to .NET 10 LTS with runtime 10.0.14 in portable packages.
- Separated domain, Steam integration, local storage and desktop presentation.
- Added explicit demo mode, dynamic discovery, filters, sorting, previews, diagnostics,
  snapshots, rarity summaries, presets and JSON/CSV exports.
- Added serialized writes, persistence callbacks, refreshed-state verification,
  two-minute batch spacing and uncertain-write protection.
- Added account-safe tests, Windows packaging and GitHub workflows.
- Added a dedicated Unlock All command with a fresh full enumeration, one explicit
  confirmation, one StoreStats batch and separate successful/failed/unverified reporting.
- Verified live initialization and read-only enumeration of 75 achievements on one machine.
  Live unlocking remains unverified; live clearing remains disabled.
