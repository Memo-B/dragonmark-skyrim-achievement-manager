# Manual live integration test plan

Automated tests use fake or demo providers only. A maintainer must personally approve
the exact real achievement before any live write; development authorization alone is
not authorization to change a Steam account. Never mass-unlock as a test.

## Read-only checks

1. Run the probe without a native library and verify an actionable failure.
2. Supply the official matching library, start Steam and connect using your own licensed
   account. Record OS, wrapper/native version, actual App ID and enumeration count.
3. Compare several locked/unlocked states, hidden flags and timestamps against Steam.
4. Check metadata language, percentage-unavailable fallback and icon-unavailable fallback.
5. Refresh and verify selection survives search/sort. Switch accounts only after closing
   Dragonmark, and verify snapshots do not mix accounts.
6. Exercise Steam-closed/offline, wrong App ID, missing license and callback timeout on
   an appropriate test setup. Do not modify the user's main installation to force errors.

## One explicitly confirmed unlock

1. The account owner chooses one locked achievement and accepts the account consequences.
2. Record its API ID and initial state. Select only that item; inspect the preview.
3. Confirm personally. Capture per-target local acceptance, StoreStats acknowledgement,
   refreshed state, and the final provider state. Never infer success from acceptance alone.
4. Independently inspect the Steam achievements page, then restart Dragonmark and refresh.
5. Verify no unrelated achievements or gameplay stats changed. Record only redacted evidence.
6. If any stage fails or times out, stop. Check Steam before retrying; shutdown can persist
   staged changes. Do not attempt a compensating clear.

## Individual clear — future gated validation

Live capability is currently false and there is no UI switch that bypasses it. Validate
only in a dedicated development branch and only with the owner's separate exact-target
authorization. Confirm persistence independently before enabling the capability in code.
Do not call ResetAllStats or modify associated gameplay statistics.

## Unlock All verification

Use demo/fake data first: apply a search that shows one item, leave manual selection
empty, and click Unlock All. The preview must include every locked item from the full
refreshed collection. Cancel it and verify zero changes; repeat and confirm in demo.
Verify a single StoreStats call in fake-provider tests, duplicate execution rejection,
and separate successful/failed/unverified counts after callback failures.

A real Unlock All test changes every currently locked achievement. It is **not** part
of routine validation and requires the account owner's separate, exact confirmation.
Do not consider live batch persistence validated by demo success or read-only discovery.

## Failure/UX matrix

Verify close-during-write waits, repeated clicks do not duplicate requests, unknown imports
are rejected, cancellation before staging causes zero writes, and cancellation after
staging still waits for bounded evidence. Most of these are already covered by fake tests.
Verify 100/125/150/200% DPI, keyboard-only navigation, screen-reader labels, minimum window
size, and long descriptions on Windows 10 and 11. Record untested cases explicitly.
