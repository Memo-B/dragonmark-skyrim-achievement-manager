# Dragonmark roadmap

Dragonmark's first public beta delivers the core Skyrim achievement workflow: connect to Steam, inspect the collection, review a selection or every missing achievement, confirm once and inspect the outcome. **The owner has tested individual unlocking and Unlock All successfully on a real account.**

This roadmap distinguishes shipped capabilities from upcoming work. Priorities may change with reproducible reports; no release dates or untested compatibility are promised. [Download the current beta](https://github.com/Memo-B/dragonmark-skyrim-achievement-manager/releases) · [Validation evidence](docs/VALIDATION.md) · [Report an issue](https://github.com/Memo-B/dragonmark-skyrim-achievement-manager/issues).

## Delivered in 0.3.0-beta.1

- [x] Real Steamworks integration for Skyrim SE/AE App ID 489830, with dynamic achievement enumeration.
- [x] Authentic Steam achievement metadata and current-state icons; all 75 loaded in the local read test.
- [x] Individual/selected unlock and a dedicated Unlock All / Unlock Missing action with one target-count confirmation.
- [x] Owner-reported successful real individual unlock and Unlock All tests on 2026-11-09.
- [x] One StoreStats batch, matching callbacks, refreshed-state checks and separate successful/failed/unverified results.
- [x] No automatic startup writes, duplicate prevention, batch cooldown and uncertain-write protection.
- [x] Search, filters, rarity, detail cards, completion progress, presets, history and JSON/CSV exports.
- [x] Original Nordic design, reduced motion, optional local background and isolated offline Demo Mode.
- [x] English application and English/Turkish documentation with real current-version screenshots.
- [x] Per-user Windows installer, .NET included, verified Steam dependency download, shortcuts and uninstaller.
- [x] Local live-component and offline-demo installation, app launch and uninstall checks.
- [x] 69 automated tests, locked dependencies, audit checks and a successful first hosted CI run.
- [x] Release checksums, file manifest, third-party notices, issue templates and private security reporting.

## Delivered in 0.3.0-beta.2

- [x] Withdraw beta.1 downloads after discovering local build-directory paths in application DLL debug metadata.
- [x] Remove Release debug metadata, map build paths to a neutral root and add binary privacy gates to CI and packaging.
- [x] Check the replacement package for actual local profile/account identifiers, verify all 465 file hashes and test install, launch and uninstall.

## Next: broaden confidence across environments

| Priority | Work | Completion evidence |
| --- | --- | --- |
| High | Record independent Steam-profile and later restart-persistence checks | Redacted before/after evidence and a fresh-session read, keeping personal account data private |
| High | Extend Windows compatibility | Documented installer/live-read results on Windows 10 and additional Windows 11 systems |
| High | Exercise interrupted connections and account changes | Manual failure/reconnect results alongside the existing fake-API tests; no false-success or silent retry behavior |
| High | Validate accessibility and display scaling | Keyboard-only confirmation/cancel flow, screen-reader labels and real per-monitor 125/150/200% DPI checks |
| Medium | Improve release provenance | Evaluate code signing and publish the actual verification status; retain checksums and pinned dependencies |
| Medium | Test upgrades and support diagnostics | Verify updates preserve preferences and improve redacted troubleshooting instructions |

A stable release will be based on this broader evidence, not just a version-number change. The working beta remains available while these checks expand.

## Later: consider after evidence and demand

- **Live individual clearing:** keep it disabled until compatibility and persistence are verified. There is no planned mass-reset or snapshot-rollback promise.
- **Offline dependency delivery:** evaluate native-library redistribution terms before embedding it. Today's installer already handles the verified download, so users do not copy DLLs manually.
- **Additional usability refinements:** prioritize concrete reports about finding achievements, reviewing results and keyboard access.
- **Further documentation translations:** welcome contributions while keeping the application UI English.

## Project boundaries

Dragonmark targets Skyrim SE/AE achievements on Steam. It is not a save editor, quest-completion tool, game launcher or credential manager. No SKSE bridge is required. Original Skyrim, Skyrim VR and other stores need different integration work and are not advertised as supported. Presets only restore selections; they never execute changes automatically.

## Useful first contributions

1. Follow the installer guide on another supported Windows environment and report the exact app/OS versions and results.
2. Test keyboard focus order or high-DPI layouts and attach a redacted screenshot of a specific issue.
3. Add a meaningful malformed-preset, export compatibility or callback edge-case test.
4. Improve a troubleshooting step using a reproducible failure and its confirmed resolution.
5. Review the English/Turkish guides against the current release.

See [CONTRIBUTING](CONTRIBUTING.md) before changing behavior. Automated tests must remain account-safe. Report exploitable issues through the [security policy](SECURITY.md), not a public issue containing sensitive details.
