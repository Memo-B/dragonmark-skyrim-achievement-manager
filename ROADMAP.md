# Dragonmark roadmap

## Beta evidence and remaining validation

- [x] Owner reported successful real individual unlock and Unlock All tests on 2026-11-09.
- [x] Add a per-user Windows installer with verified dependency download and an uninstaller.

- [ ] Validate an explicitly confirmed single-target live unlock, callback acknowledgement, refreshed state, independent Steam inspection and a later restart read.
- [ ] Validate the live missing-only batch and error/reconnect behavior without implying rollback after timeout.
- [ ] Resolve Valve native-library redistribution terms; keep public packages explicit about the separate setup until then.
- [x] First hosted main-branch Build and test workflow passed; validation reports were uploaded.

## Next improvements

- [ ] Test Windows 10, additional Windows 11 builds, high DPI, screen readers and keyboard-only flows.
- [ ] Validate live individual clearing before enabling it; no mass-reset promise.
- [ ] Expand network loss and account-switch manual tests.
- [ ] Evaluate code signing; the first beta has an installer but remains unsigned.
- [ ] Consider a bridge only if reproducible standalone integration failures justify it. No SKSE implementation is currently planned as a dependency.

## Good first issue recommendations

These are proposed tasks, not claims that labeled GitHub issues already exist:

- Review English and Turkish instructions against a fresh demo-only build.
- Test focus order and describe specific accessibility failures with a redacted screenshot.
- Add a meaningful malformed-preset edge case or CSV interoperability test.
- Improve keyboard shortcut discoverability without changing confirmation semantics.
- Review additional documentation translations while keeping the application UI English.

UI, documentation, tests and platform integration contributions are welcome through [CONTRIBUTING](CONTRIBUTING.md). Automated tests must remain account-safe. No milestone date or feature delivery is promised.
