# Capture the current Dragonmark interface

On an interactive Windows desktop with PowerShell 7 and .NET 10 SDK, from the source root:

```powershell
./scripts/Capture-Screenshots.ps1
# Additional pages, empty states, compact layout and scaled raster checks:
./scripts/Capture-Screenshots.ps1 -Qa
```

The first command builds and writes the seven required real captures to `assets/screenshots/`. The second writes supplemental review images and the measured DPI to ignored `artifacts/ui-review/`. Neither connects to Steam, clicks confirmation nor changes any achievement.

The underlying executable supports:

```powershell
$folder = Join-Path (Get-Location) 'assets/screenshots'
dotnet src/Dragonmark.App/bin/Release/net10.0-windows/win-x64/Dragonmark.dll --demo --capture-suite $folder
$image = Join-Path (Get-Location) 'artifacts/single-dashboard.png'
dotnet src/Dragonmark.App/bin/Release/net10.0-windows/win-x64/Dragonmark.dll --demo --screenshot $image
```

Both require an absolute output path and `--demo`. Captures use a fresh temporary profile rather than ordinary settings, snapshots or personal theme artwork. The suite opens the actual production confirmation layout and closes it without confirming. It uses WPF rendering at 96 DPI for sharp, consistently cropped content; no UI is composited or redrawn afterward. The QA scale render is a raster check, not proof of per-monitor DPI behavior.

Inspect every image for clipping and privacy before committing. Demo Mode must stay visible. Do not package private native logs or real-account screenshots. Regenerate after changing layout, product version, branding or captions. Public images correspond to **0.3.0-beta.2**.
