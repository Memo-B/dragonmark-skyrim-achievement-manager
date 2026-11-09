# Source audit and implementation decisions

Audited all eight original files on 2026-11-09: MainForm.cs, Program.cs,
SkyrimAchievementSelector.csproj, build.bat, steam_appid.txt, README.md, LICENSE and
.gitignore. The baseline built under SDK 10.0.100 targeting net8.0-windows with zero
warnings/errors. It was not already a Git repository.

## Findings

- One WinForms class mixed native lifetime, selection, presentation and persistence.
- Callbacks lacked deadlines; a lost response left the UI indefinitely busy.
- RequestUserStats was followed by current-user getters rather than clearly using
  the corresponding user cache. User identity was not checked in callbacks.
- SetAchievement acceptance was counted without per-target verified results.
- Store failures left staged native state without a safe retry boundary.
- Wrong-App-ID and initialization failure paths did not consistently release resources.
- No account-switch guard, offline check, batch cooldown or close-during-write guard.
- Metadata failures were silently skipped; no hidden status, dates, rarity or icons.
- NuGet restored the managed wrapper but did not supply steam_api64.dll.
- Mixed-language UI/build/docs, no tests, presets, export, CI or release packaging.
- README suggested consulting a competing implementation. That suggestion was removed;
  no competing code or binaries were inspected or reused.

## Decisions

Keep Steamworks.NET 2024.8.0 for compatibility with the existing integration. Move to
.NET 10 LTS: Microsoft's release metadata reports .NET 8 support ending 2026-11-10,
one day after this implementation. Portable output pins the current 10.0.14 runtime.
Move to WPF with a view model and constructor-injected providers. Use a narrow native
facade to make failure tests independent of Steam. Preserve the original MIT notice.

Use one exclusive native operation at a time on a worker, pumping callbacks while
waiting; no blocking native waits on the UI thread. Stop accepting new operations
before disposing and wait for in-flight work to finish. Cancellation is permitted
before staging; after staging, finish bounded persistence/verification even if a token
is cancelled. No compensating mutations or automatic retries are performed.

Confirmed live reads remove the immediate justification for an SKSE bridge. An original
bridge remains conditional on reproducible evidence, supported runtime dependencies,
same-user IPC access controls, explicit request confirmation and a compatibility matrix.

The application remains English-only. Release preparation adds a Turkish README alongside the primary English documentation. Steam/Windows-owned text follows its
own language configuration.
