# Dragonmark Steam integration

**Real read integration is working on one tested installation. On 2026-11-09 the owner reported successful individual unlock and Unlock All tests.** This owner-reported evidence supports a public beta; independent restart persistence and broader compatibility checks remain pending. No simulated result is presented as Steam success.

The provider uses Steamworks.NET 2024.8.0 and the matching Valve x64 library, targeting App ID **489830**. It verifies the active app, account, login and license. It dynamically enumerates API identifiers and retrieves current achievement states; no hard-coded Skyrim achievement list is required.

## Batch contract

- Refresh before planning Unlock All; include every currently locked achievement, regardless of display filters or selection.
- Present the count and all targets in one explicit confirmation. Never execute at startup.
- Serialize native operations and refuse duplicate batches.
- Stage each accepted `SetAchievement`, then use one `StoreStats` request for the accepted batch.
- Wait for the game-matching `UserStatsStored_t` callback; `UserAchievementStored_t` is supplementary evidence only.
- Refresh user stats and verify each target using `GetUserAchievementAndUnlockTime`.
- Separate successful, failed and unverified results; refresh the visible list after the attempt.
- Never promote a local setter acceptance, missing callback, failed refresh or ambiguous result into success.

The wrapper's initial load uses `RequestCurrentStats`; explicit refresh uses `RequestUserStats`. Callback deadlines are 25 seconds and batch spacing is two minutes. Store callbacks have no request IDs; late-callback ambiguity blocks subsequent writes in the session. Staged changes can persist later or at shutdown, so no rollback promise is made.

A successful store callback plus refreshed cache is the available API evidence, not independent remote-profile proof. The [manual live plan](LIVE_TEST_PLAN.md) requires an explicitly approved real single-target write and independent Steam inspection before validating the larger batch path. No real write was performed during repository preparation.

See the [detailed investigation](STEAM_INTEGRATION.md), [architecture](architecture.md), [validation report](VALIDATION.md) and Valve's [ISteamUserStats reference](https://partner.steamgames.com/doc/api/ISteamUserStats). No game injection, save modification, ResetAllStats, runtime downloader or SKSE bridge is present. Live ClearAchievement is disabled.
