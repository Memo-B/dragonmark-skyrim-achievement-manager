# Dragonmark validation · 1.0.0

Recorded on **2026-11-09**. Automated test evidence, the owner's real-account test report and unverified environments are distinguished below.

## Automated release gates

- **89 tests passed**: 78 core/provider tests and 11 real WPF integration cases, with no failures or skips. Tests use fictional achievements and fake APIs; they never initialize Steam or modify a real account.
- Nine new regression cases cover account changes after the stats callback, after enumeration, during callback draining, between setters, immediately before StoreStats and during the verification read; login/license/App ID loss is also covered. No success is reported from a different account's refreshed state.
- WPF tests open the actual application windows with an isolated demo profile. A filtered list with no selection still previews all six missing fixtures. Enter/Escape cancel, explicit confirmation yields 6 successful / 0 failed / 0 unverified, only one dialog appears, and duplicate execution is blocked. The test routes access keys through WPF's presentation source and invokes buttons via their automation peers.
- Window bounds and reachability of bottom actions are tested against logical work areas corresponding to a 1366 × 768 display at 100/125/150/200% scaling. Focus can move from cancellation to confirmation. These are logical-layout and framework-routing checks, not physical monitor/input tests.
- Locked restore, formatting, Release build with zero warnings/errors, repository checks and the restored direct/transitive dependency audit pass. [Hosted CI](https://github.com/Memo-B/dragonmark-skyrim-achievement-manager/actions/workflows/ci.yml) runs the same solution, including WPF tests.
- Release builds map source paths to a neutral root and omit debug metadata. Both packaging and installer compilation scan actual binary contents for local profile paths, Steam identifier patterns and private/debug files. Public captures use fictional Demo Mode data only.

## Real Steam evidence

On one local Windows 11 installation, initialization, App ID 489830, login/license/context checks, **75 achievement reads** and **75/75 current-state icons** succeeded. The native library's official origin, pinned SHA-256 and Valve signature were checked. The read-only probe does not call SetAchievement, ClearAchievement or StoreStats.

The owner then manually tested individual unlocking and Unlock All and reported both working on 2026-11-09. The agent did not perform those live writes. Independent Steam-profile/restart-persistence evidence was not separately captured. Tests added for 1.0.0 validate failure boundaries without making new account changes.

## Installer and artifact checks

The stable self-contained ZIP and Setup are checked against their SHA-256 sidecars and per-file manifest. Local unattended tests cover the selected Steam download task, offline installation, application launch and uninstallation. The upgrade path from beta.2 to 1.0.0 uses the unchanged application identifier. Installation preserves the user-data folder.

The pinned Steam component is downloaded only when its visible Setup task is selected and is verified before installation proceeds. It is not embedded in either public package. The self-contained runtime is .NET 10.0.14. The app and installer are unsigned.

## Explicit limits

Windows 10, a separate AE installation, physical per-monitor DPI changes, screen readers and additional real account/network interruption scenarios are not claimed as tested. Framework key-routing tests do not establish all physical keyboard or mouse behavior. Live clearing remains disabled. No universal compatibility, rollback, platform-policy guarantee or security certification is claimed.

Version 1.0.0 closes the identified account-boundary, small-display and missing-WPF-test findings within the documented scope. [Maintenance policy](../ROADMAP.md) · [Compatibility](compatibility.md) · [Live test plan](LIVE_TEST_PLAN.md).
