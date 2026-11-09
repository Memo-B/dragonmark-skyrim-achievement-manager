# Steam integration investigation

## Actual local evidence — 2026-11-09

1. Baseline compilation succeeded. NuGet did not copy steam_api64.dll.
2. The new read-only probe failed with the expected missing-native-library diagnostic.
3. Downloaded the matching DLL from the official Steamworks.NET 2024.8.0 tag for local
   development. SHA-256: `1ADD7F151FA644870A735AE86E68D1F019F296130D8E7C0A7ED3ECC7482DCCBC`.
   Authenticode status was Valid, signer Valve Corp. The binary is ignored by Git.
4. With Steam already running, SteamAPI.Init succeeded. App ID, login and ownership
   checks passed. Current-user stats callback succeeded; **75 achievements** were read.
5. No SetAchievement, ClearAchievement or StoreStats call was made in this live probe.
   Real writes, persistence and cross-machine compatibility remain unverified.

This evidence supports a standalone read path on one installation. It does not establish
universal support or publisher authorization. No injection, authentication bypass or
Steam-client modification is used. No SKSE64 bridge was justified or implemented.

## Documented API behavior

Valve documents initialization preconditions, App ID discovery and same-user/license
requirements. The App ID file is a development context hint, not a guarantee of access.
See [Steam API initialization/shutdown](https://partner.steamgames.com/doc/api/steam_api).

The managed dependency targets Steamworks SDK 1.60. Its initial local-user load uses
RequestCurrentStats and the matching game/user callback. Current Valve documentation
marks that call deprecated for newer launch flows. RequestUserStats plus
GetUserAchievementAndUnlockTime is used for explicit refresh. Do not silently upgrade
the wrapper without testing that lifecycle. Discovery reads names, descriptions,
hidden status and dates; global percentages and icons are optional.

SetAchievement/ClearAchievement affect local state. StoreStats initiates persistence;
only the game-matching successful UserStatsStored callback followed by a successful
refresh and matching target state produces a verified result. UserAchievementStored is
diagnostic evidence only. Steam can upload staged changes later or at shutdown; failures
are not rollback. Batches are spaced by two minutes, a conservative application policy.
See [ISteamUserStats](https://partner.steamgames.com/doc/api/ISteamUserStats).

GetAppID is checked before writes; the authenticated local account is captured and
rechecked to prevent account switching. See [ISteamUtils](https://partner.steamgames.com/doc/api/ISteamUtils)
and [ISteamUser](https://partner.steamgames.com/doc/api/ISteamUser).

Callbacks are registered and disposed with the provider. Native calls are serialized;
there is one StoreStats request at a time, a 25-second deadline per required callback,
and no new writes after ambiguity. Store callbacks lack request identifiers, so this
policy avoids attributing a late response to a subsequent batch in the same provider.
There is no claim that a cache read alone is an independent server observation.

## Local setup

From the source root, run `pwsh -File scripts/Setup-SteamNative.ps1`, then build.
The explicit setup script verifies the pinned checksum and Valve signature. It is not
called at application startup. The official release is
[Steamworks.NET 2024.8.0](https://github.com/rlabrecque/Steamworks.NET/releases/tag/2024.8.0).

For a downloaded portable preview, obtain the matching x64 library from that official
release and place steam_api64.dll next to Dragonmark.exe. Never substitute a library
from a third-party achievement manager or a random DLL download site.

Public packaging omits the proprietary library until its redistribution terms for this
utility are reviewed. Steamworks.NET's MIT license covers the wrapper, not Valve's SDK.

Run a read-only probe after building:

```powershell
dotnet src/Dragonmark.App/bin/Release/net10.0-windows/win-x64/Dragonmark.dll --probe-steam
Get-Content "$env:LOCALAPPDATA\Dragonmark\steam-probe.json"
```

This writes a small account-redacted report locally and exits 0 on successful reads,
1 on failure. Steam's own native console output may include an account identifier;
do not publish raw stdout. The report explicitly says ChangesTested=false.


## Runtime artwork verification — 0.3.0-preview.1

A new read-only probe on 2026-11-09 loaded **75 of 75** current-state achievement icons, including locked and unlocked entries. UserAchievementIconFetched_t is now registered/disposed with the provider and matched by App ID, API identifier and achieved state. The artwork is cached in memory, never recolored to fabricate state and never packaged. See [visual design](visual-design.md). No real achievement writes were performed.

## Subsequent owner test — public beta

On 2026-11-09 the owner manually tested the real application and reported successful individual unlock and Unlock All operations, then authorized publication. This updates the historical investigation above; no independent restart-persistence evidence or new cross-machine claim is implied. The beta installer handles the verified dependency download with an explicit install task.
