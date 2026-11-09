# Dragonmark original brand assets

The geometric dragon profile and achievement check are original vector geometry created for this project. They do not use Bethesda's dragon emblem, Skyrim logo, Steam artwork or another achievement manager's identity. Assets are covered by the repository's [MIT license](../../LICENSE), subject to the [independence notice](../../NOTICE.md).

| Asset | Purpose |
| --- | --- |
| mark.svg / mark.png | 256 × 256 source mark and application image |
| dragonmark.ico | Windows icon, PNG frames at 16, 24, 32, 48, 64, 128 and 256 pixels |
| hero.svg / hero.png | 1280 × 400 README banner |
| social-preview.svg / social-preview.png | 1280 × 640 GitHub social preview |

Palette: charcoal `#0C1822`, antique gold `#D5BD86`, muted gray `#A9B6C2`, warm white `#F1F1EE`. Typography: bundled OFL Cinzel for display text, installed Segoe UI for body text. See the [font license and provenance](../fonts/README.md).

From an interactive Windows desktop with PowerShell 7:

```powershell
./scripts/Generate-Branding.ps1
```

The script contains the shared geometry and layout values, writes SVG sources and renders matching PNG/ICO assets using WPF. Change the generator when changing the mark; regenerate both formats. Raster text appearance can vary with installed system fonts and rendering versions. This is reproducible vector artwork, not a screenshot substitute.

Use `social-preview.png` in GitHub repository Settings → General → Social preview. Upload is an owner action. Product screenshots are kept separately in [assets/screenshots](../screenshots/README.md).
