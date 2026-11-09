# Dragonmark roadmap

**Version 1.0.0 completes the planned Skyrim achievement manager.** The project is now in maintenance mode: no additional feature development is scheduled. Reproducible defects, security issues and required Steam/Windows compatibility updates can still receive fixes.

[Download the stable release](https://github.com/Memo-B/dragonmark-skyrim-achievement-manager/releases/latest) · [Validation evidence](docs/VALIDATION.md) · [Report an issue](https://github.com/Memo-B/dragonmark-skyrim-achievement-manager/issues)

## Completed scope

- [x] Real Steamworks integration for Skyrim SE/AE App ID 489830, dynamic achievement enumeration and authentic current-state artwork.
- [x] Individual/selected unlock and dedicated Unlock All / Unlock Missing with one target-count confirmation.
- [x] Owner-reported successful real individual unlock and Unlock All tests on 2026-11-09.
- [x] One StoreStats batch, matching callbacks, refreshed-state verification and separate successful/failed/unverified results.
- [x] No startup writes, duplicate prevention, cooldown and persistent session blocking after an uncertain write.
- [x] Account, login, license and App ID checks at refresh, staging, store and verification boundaries.
- [x] Search, filters, rarity, details, presets, history and JSON/CSV exports.
- [x] Original Nordic interface, reduced motion, optional local background and isolated offline Demo Mode.
- [x] Windows constrained to the monitor work area, with scrollable content on smaller displays.
- [x] Real WPF tests for single confirmation, cancellation, default Enter/Escape actions, focus navigation and constrained layouts.
- [x] English application, English/Turkish guides and actual application screenshots.
- [x] Per-user installer, included .NET runtime, verified Steam component download, shortcuts and uninstaller.
- [x] Locked dependencies, audit checks, release checksums, file manifest and package privacy gates.

## Maintenance policy

The current release preserves this scope. Bug reports should include the app/Windows version, reproducible steps and redacted evidence. Automated tests use fictional data and never mutate a Steam account. Security reports belong in the [private disclosure channel](SECURITY.md).

Code signing, additional Windows installations, physical per-monitor DPI and screen-reader coverage are not claimed as completed. See the [compatibility matrix](docs/compatibility.md) for tested versus intended support. Stable means the documented feature set has passed its release checks; it does not promise every future Steam or Windows version will behave identically.

## Intentional boundaries

Live achievement clearing stays disabled. There is no mass reset, snapshot rollback, save editing, quest completion, SKSE bridge, other-game support or non-Steam integration. Presets restore selections only. The proprietary Valve native library is not embedded; Setup offers the pinned, checksum-verified download.

No expansion of these boundaries is planned. Contributions should focus on a concrete defect or maintenance need; see [CONTRIBUTING](CONTRIBUTING.md).
