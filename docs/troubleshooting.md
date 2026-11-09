# Dragonmark troubleshooting

| Symptom | What to check |
| --- | --- |
| Missing native library / Steam will not initialize | Complete the matching x64 [native setup](installation.md); keep the DLL and steam_appid.txt beside Dragonmark.exe |
| Wrong app, missing ownership or disconnected account | Use your own licensed account, running Steam under the same user/elevation; target is App ID 489830 |
| Empty or unavailable metadata | Refresh with F5; optional icons/global rarity can be unavailable without preventing enumeration |
| Buttons disabled | Wait for the current operation; check connection state, selected rows and the two-minute batch cooldown |
| Unlock All preview includes hidden/filtered rows | Expected: it refreshes every achievement and targets all locked entries, independent of filters |
| Store callback timed out or result unverified | Do not assume rollback or success. Inspect Steam separately; writes are blocked for that session after ambiguity |
| Game quest still incomplete | Steam achievements and game progress are independent; Dragonmark does not edit saves |
| Windows SmartScreen warning | Preview is unsigned. Verify provenance and checksums before choosing whether to run it |
| Cannot reset achievements | Live clearing is intentionally disabled; no mass reset or snapshot restore exists |
| Non-English achievement descriptions | Steam supplies metadata language; choose English for Skyrim in Steam. The app UI itself is English |

## Reports that help

Include app version, Windows build, connection mode, exact steps, expected/actual behavior and redacted per-target results. Use **Diagnostics**, **Activity History** and **View results**. Do not publish Steam IDs, raw native logs, credentials, personal snapshots or an entire local-data folder.

Data is stored at `%LOCALAPPDATA%\Dragonmark`. An export contains history; inspect it before sharing. Importing a preset changes only selection and never executes achievements.

## Read-only developer probe

After a build with the matching native DLL:

```powershell
.\publish\Dragonmark.exe --probe-steam
Get-Content "$env:LOCALAPPDATA\Dragonmark\steam-probe.json"
```

Create `publish/` with `build.bat` first. The probe initializes the actual client and reads achievements; it does not call SetAchievement, ClearAchievement or StoreStats. It exits 0 on successful read, 1 on failure. It is a **manual diagnostic**, never a normal CI step. Native console output can contain account identifiers.

If a live operation is uncertain, restarting does not undo it. Follow [the manual validation plan](LIVE_TEST_PLAN.md) before considering any retry.

## Visual preview troubleshooting

- **Artwork missing:** live entries show authentic Steam icons when available; a labelled empty holder is used while unavailable. Refresh after Steam reconnects. The app never substitutes a demo illustration in live mode.
- **Local background missing or rejected:** choose a local PNG/JPEG under 16 MB and at most 16384 pixels per side, or use Restore original mountains. The file stays outside the application package.
- **Motion preference:** Reduce motion disables fades and the busy pulse. Windows animation preferences also apply.
- **Small display:** the app bounds its initial size to the work area and uses scrolling. Minimum window size is 1050 × 700 logical pixels. Smaller logical work areas are not validated.
