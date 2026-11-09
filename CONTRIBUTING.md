# Contributing to Dragonmark

Start with [development setup](docs/development.md), [architecture](docs/architecture.md) and the [roadmap](ROADMAP.md). Use Windows x64, .NET 10 SDK and PowerShell 7. The demo and tests require no Steam account or native library.

```powershell
dotnet restore Dragonmark.sln --locked-mode
dotnet build Dragonmark.sln -c Release --no-restore
dotnet test Dragonmark.sln -c Release --no-build
dotnet format Dragonmark.sln --verify-no-changes --no-restore
./scripts/Check-Repository.ps1
./scripts/Check-Dependencies.ps1
```

Keep application UI, comments and diagnostics in English. Translated user documentation is welcome; update both English and Turkish guides when changing shared usage instructions. Follow `.editorconfig`, constructor injection and the provider boundary. Avoid cosmetic API/namespace renames.

Never turn API acceptance or uncertainty into success. Keep the single confirmation, target count, missing-only semantics, duplicate prevention and per-target evidence in Unlock All and Unlock Missing. Add meaningful behavior tests for callback identity, timeouts, account changes and uncertain persistence. Preset imports remain selection-only. Normal tests and CI never contact Steam or change accounts.

Use `--demo` for UI work. Live testing is manual and separately opt-in; follow the [live test plan](docs/LIVE_TEST_PLAN.md). Do not attach personal snapshots or raw Steam logs. Report vulnerabilities using [SECURITY](SECURITY.md).

Submit focused PRs explaining the concrete problem, final behavior, validation and limitations. Follow the [PR template](.github/PULL_REQUEST_TEMPLATE.md) and [code of conduct](CODE_OF_CONDUCT.md). Do not copy competing tools or official game artwork. Record new dependency origins and licenses. Contributors retain copyright and submit original contributions under the project's MIT license.
