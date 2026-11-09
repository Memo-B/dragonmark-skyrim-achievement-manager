# Dragonmark security and privacy

Dragonmark asks for no passwords, Steam Guard codes, keys, cookies or tokens. It uses the active local Steam client, has no telemetry, and installs no services or drivers. It does not edit Skyrim saves, call ResetAllStats or alter gameplay statistics.

Settings, metadata, account-scoped snapshots and rotating logs live under `%LOCALAPPDATA%\Dragonmark`. Account keys are truncated SHA-256 hashes, not anonymous identities. Reports contain achievement history; review them before sharing. Native Steam debug output can contain identifiers outside Dragonmark's redaction.

## Reporting a vulnerability

The current development line is **0.3.x beta**; no fixed support period is promised. Check whether the designated [repository](https://github.com/Memo-B/dragonmark-skyrim-achievement-manager) offers a private **Report a vulnerability** action. If enabled, use it for exploitable issues. Private vulnerability reporting is enabled for the repository.

If that channel is unavailable, contact the [repository owner](https://github.com/Memo-B) through a contact method they publish. If none exists, open a minimal issue requesting a private disclosure channel **without technical exploit details or personal data**. Do not invent an email address, post secrets or attach raw account logs. Maintainers should enable a private channel before public release.

Include affected version, impact, safe reproduction steps and suggested mitigation privately. No response-time or bounty commitment is made.

## Distribution

Preview binaries are unsigned. Verify archive checksums and source provenance. The explicit developer setup accepts the pinned Valve-signed library. Public packaging omits that proprietary binary pending redistribution review. The desktop app contains no runtime code downloader. The installer offers a visible Steam-component task that downloads from the official Steamworks.NET tag and checks the pinned SHA-256 before copying any application files. A valid signature proves provenance, not redistribution permission.

Dependency audit results describe known advisories at a point in time, not a security certification. An uncertain Steam operation may persist later; do not treat failure or timeout as rollback. See [Steam integration](docs/steam-integration.md).

## Release build privacy

Release builds disable debug output and normalize source paths. Packaging and installer compilation run `scripts/Check-PackagePrivacy.ps1` against actual binary contents, not only file names. This blocks embedded Windows profile paths, Steam account identifier patterns and private/debug files. Beta.1 downloads were withdrawn after embedded build paths were discovered; use beta.2 or later. The audit did not find Steam account IDs or local settings/history in that payload.
