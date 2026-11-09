# Dragonmark compatibility

Evidence recorded on **2026-11-09**. Target support is not equivalent to a completed test matrix.

| Component | Target | Observed evidence |
| --- | --- | --- |
| OS | Windows 10/11 x64 | Windows 11 x64 tested locally; Windows 10 untested |
| CPU | x64 | x64 build and portable launch verified; ARM64/native x86 unsupported |
| Source SDK | .NET 10; global.json pins 10.0.100 with latestFeature roll-forward | 10.0.100 used locally |
| Portable runtime | .NET / Windows Desktop 10.0.14 | Included in self-contained Windows package; no external runtime needed |
| Steam wrapper | Steamworks.NET 2024.8.0, SDK 1.60 | Matching x64 native library checksum and Valve signature checked |
| Skyrim Special Edition | Steam App ID 489830 | Init, login/license/context and 75 achievement reads succeeded on one account |
| Anniversary Edition | Content on App ID 489830 | Intended same-app support; distinct AE installation untested |
| Live achievement writes | Confirmed selection and missing-only batch | Owner reported individual unlock and Unlock All success; independent restart test pending |
| Live clearing | Not enabled | Awaiting validation |
| Original Skyrim / VR / other stores / consoles | Unsupported | Different app/platform contexts |
| SKSE | No dependency | No plugin or bridge implemented |
| Logical display layouts and WPF key routing | 100/125/150/200% equivalent work areas | Automated window bounds, reachable actions, cancellation and focus tests passed |
| Physical per-monitor DPI / screen readers / account switching / offline recovery | Broader environment testing required | Framework/fake-API coverage does not replace real environment tests |

Steam must run under the same Windows user and elevation, with a valid license and authenticated client. Skyrim itself is not launched or injected into by Dragonmark. No universal standalone Steamworks availability is promised.

All app-owned labels, errors and dialogs are English. Steam controls localized achievement metadata; Windows controls native file-dialog language. The Turkish README is documentation, not an alternate app locale.

Consult [validation evidence](VALIDATION.md) and [Steam investigation](STEAM_INTEGRATION.md). The TFM's default `windows7.0` metadata in generated assets does not establish Windows 7 support; the actual .NET runtime support matrix governs deployment. See [Microsoft's .NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core).

The 0.3 visual pass rendered 1100 × 740 and 1440 × 960 window layouts, with a 96-DPI measured WPF environment and an additional 150% raster check. Version 1.0.0 additionally tests constrained logical work areas, WPF Enter/Escape routing and focus traversal. Physical per-monitor DPI/input and screen-reader checks remain unverified. See [visual validation](visual-design.md).
