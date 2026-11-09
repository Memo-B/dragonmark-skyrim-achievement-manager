# Third-party notices

| Component | Version | License / origin | Use |
| --- | --- | --- | --- |
| Steamworks.NET | 2024.8.0 | MIT, Riley Labrecque; https://github.com/rlabrecque/Steamworks.NET | Managed wrapper |
| .NET / WPF runtime | 10.0.14 in portable packages | MIT with component notices; https://github.com/dotnet/runtime and https://github.com/dotnet/wpf | Included in self-contained packages |
| Microsoft.NET.Test.Sdk | 17.11.1 | MIT; https://github.com/microsoft/vstest | Development only |
| xUnit | 2.9.2 | Apache-2.0; https://github.com/xunit/xunit | Development only |
| xunit.runner.visualstudio | 2.8.2 | Apache-2.0; https://github.com/xunit/visualstudio.xunit | Development only |

The full Steamworks.NET license is retained in licenses/Steamworks.NET.txt. Runtime
licenses and third-party notices are retained under licenses/ from the corresponding
official source tags. Test libraries are not shipped in the application distribution.

Valve's steam_api64.dll is proprietary and separate from the managed wrapper's MIT
license. It is used only as a locally supplied dependency and excluded from the public
archive pending redistribution review. A signature proves provenance, not permission.

Runtime achievement metadata/icons come from the user's Steam client. No Bethesda
artwork is bundled. Demo fixtures and the geometric visual identity are original.

## Dependency license review — 2026-11-09

The restored dependency graph contains 14 NuGet packages; exact versions and declared license metadata are recorded in [dependency-licenses.csv](docs/dependency-licenses.csv). The application graph adds only Steamworks.NET (MIT). Microsoft test platform components, Microsoft.CodeCoverage and Newtonsoft.Json declare MIT; xUnit packages declare Apache-2.0. The older xunit.abstractions package links the xUnit license instead of embedding an expression; that upstream license was inspected as Apache-2.0. The xUnit license text from tag v2-2.9.2 is retained in [licenses/xunit-Apache-2.0.txt](licenses/xunit-Apache-2.0.txt).

MIT and Apache-2.0 dependencies do not relicense their authors' work as Dragonmark code. Preserve their attribution and terms when redistributing them. Test packages are development-only and excluded from the portable application. Self-contained runtime notices include their separately identified third-party components. No GPL dependency was found in the restored NuGet graph; this is an inventory review, not blanket legal clearance.

Regenerate metadata with `./scripts/Export-DependencyLicenses.ps1` after a locked restore. Runtime license files come from the official .NET runtime/WPF v10.0.14 sources. The proprietary Valve native DLL remains the unresolved redistribution item and is excluded from the public ZIP.

## Cinzel display font

The app embeds the unmodified Cinzel font from the [Google Fonts source](https://github.com/google/fonts/tree/main/ofl/cinzel). Copyright 2020 The Cinzel Project Authors; SIL Open Font License 1.1. The full license is retained in [licenses/Cinzel-OFL.txt](licenses/Cinzel-OFL.txt); source and checksum are in [assets/fonts/README.md](assets/fonts/README.md). The font is distributed with its license, separately from Dragonmark's MIT artwork/code. Windows Segoe UI files are not bundled.

## Inno Setup installer

The Windows installer is compiled with Inno Setup 7.1.1 by Jordan Russell, Martijn Laan and contributors. The upstream license is retained in [licenses/Inno-Setup.txt](licenses/Inno-Setup.txt). Its terms permit distribution of generated installers; the compiler is not an application runtime dependency. The installer downloads the pinned Valve component only when the visible installation task is selected, and does not embed that component in the distributed Setup executable.
