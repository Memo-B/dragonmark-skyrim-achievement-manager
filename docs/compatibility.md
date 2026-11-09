# Dragonmark compatibility

Evidence recorded on **2026-11-09**. Target support is not equivalent to a completed test matrix.

| Component | Target | Observed evidence |
| --- | --- | --- |
| OS | Windows 10/11 x64 | Windows 11 Home Single Language, build 26300, tested locally; Windows 10 untested |
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
| High DPI / screen readers / account switching / offline recovery | Further testing required | Fake-API coverage does not replace manual environment tests |

Steam must run under the same Windows user and elevation, with a valid license and authenticated client. Skyrim itself is not launched or injected into by Dragonmark. No universal standalone Steamworks availability is promised.

All app-owned labels, errors and dialogs are English. Steam controls localized achievement metadata; Windows controls native file-dialog language. The Turkish README is documentation, not an alternate app locale.

Consult [validation evidence](VALIDATION.md) and [Steam investigation](STEAM_INTEGRATION.md). The TFM's default `windows7.0` metadata in generated assets does not establish Windows 7 support; the actual .NET runtime support matrix governs deployment. See [Microsoft's .NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core).

The 0.3 visual pass rendered 1100 × 740 and 1440 × 960 window layouts, with a 96-DPI measured WPF environment and an additional 150% raster check. Real per-monitor DPI, physical keyboard/focus and screen-reader checks remain pending. See [visual validation](visual-design.md).
