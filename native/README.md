# Local Steam library

`steam_api64.dll` is deliberately ignored by Git. For local development, run
`pwsh -File scripts/Setup-SteamNative.ps1` from the repository root.

The script retrieves the matching Valve-signed binary from the official
Steamworks.NET 2024.8.0 tag and verifies its pinned SHA-256 and Authenticode signature.
The desktop application does not download executables or libraries at runtime.

The public packaging script excludes this binary pending explicit review of Valve's
redistribution terms for this third-party utility. Steamworks.NET's MIT license
does not establish a license for Valve's native SDK binary.
