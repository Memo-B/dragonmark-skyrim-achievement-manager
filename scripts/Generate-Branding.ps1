#requires -Version 7.0
# Original vector artwork. Windows/WPF; bundled OFL Cinzel, no network or game assets.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationCore,WindowsBase
$root = Split-Path $PSScriptRoot -Parent
$out = Join-Path $root 'assets/branding'
New-Item -ItemType Directory -Force -Path $out | Out-Null
$inv = [Globalization.CultureInfo]::InvariantCulture
$gold = '#D5BD86'; $ink = '#0C1822'; $muted = '#A9B6C2'
# An asymmetric folded wing / dragon profile; the lower cut forms an achievement tick.
$wing = 'M 35,136 L 64,56 L 135,24 L 116,63 L 209,83 L 164,108 L 132,99 L 150,131 L 118,174 L 83,141 L 97,113 L 64,139 L 77,191 L 135,228 L 93,217 L 48,186 Z'
$tick = 'M 111,176 L 131,192 L 193,129 L 211,147 L 132,225 L 94,193 Z'
$eye = 'M 162,85 L 177,89 L 161,95 Z'
$mark = "<path fill='$gold' d='$wing'/><path fill='$gold' d='$tick'/><path fill='$ink' d='$eye'/>"
function SvgFile([string]$name, [int]$w, [int]$h, [string]$body, [string]$description) {
    $svg = "<svg xmlns='http://www.w3.org/2000/svg' width='$w' height='$h' viewBox='0 0 $w $h' role='img' aria-labelledby='title desc'><title id='title'>Dragonmark - Skyrim Achievement Manager</title><desc id='desc'>$description</desc>$body</svg>"
    [IO.File]::WriteAllText((Join-Path $out $name), $svg + "`n", [Text.UTF8Encoding]::new($false))
}
SvgFile 'mark.svg' 256 256 $mark 'Original angular dragon profile and achievement check. Not a Bethesda emblem.'
function Brush([string]$color) { return [Windows.Media.SolidColorBrush]::new([Windows.Media.ColorConverter]::ConvertFromString($color)) }
function DrawMark($dc, [double]$x, [double]$y, [double]$size) {
    $dc.PushTransform([Windows.Media.TranslateTransform]::new($x,$y))
    $dc.PushTransform([Windows.Media.ScaleTransform]::new($size/256,$size/256))
    $dc.DrawGeometry((Brush $gold),$null,[Windows.Media.Geometry]::Parse($wing))
    $dc.DrawGeometry((Brush $gold),$null,[Windows.Media.Geometry]::Parse($tick))
    $dc.DrawGeometry((Brush $ink),$null,[Windows.Media.Geometry]::Parse($eye))
    $dc.Pop(); $dc.Pop()
}
function TextLine($dc,[string]$text,[double]$x,[double]$y,[double]$size,[string]$color) {
    $font=[Windows.Media.Typeface]::new('Segoe UI')
    if ($text -eq 'Dragonmark') { $family=[Windows.Media.FontFamily]::new([Uri]::new((Join-Path $root 'assets/fonts/')), './#Cinzel'); $font=[Windows.Media.Typeface]::new($family,[Windows.FontStyles]::Normal,[Windows.FontWeights]::Normal,[Windows.FontStretches]::Normal) }
    $formatted=[Windows.Media.FormattedText]::new($text,$inv,[Windows.FlowDirection]::LeftToRight,$font,$size,(Brush $color),1.0)
    $dc.DrawText($formatted,[Windows.Point]::new($x,$y))
}
function Png([string]$name,[int]$w,[int]$h,[scriptblock]$draw) {
    $visual=[Windows.Media.DrawingVisual]::new(); $dc=$visual.RenderOpen()
    try { & $draw $dc } finally { $dc.Close() }
    $bitmap=[Windows.Media.Imaging.RenderTargetBitmap]::new($w,$h,96,96,[Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($visual)
    $encoder=[Windows.Media.Imaging.PngBitmapEncoder]::new(); $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $stream=[IO.File]::Create((Join-Path $out $name))
    try { $encoder.Save($stream) } finally { $stream.Dispose() }
}
Png 'mark.png' 256 256 { param($dc) DrawMark $dc 0 0 256 }
$mountains = 'M 0,640 L 220,366 L 360,480 L 540,240 L 700,458 L 850,320 L 1010,470 L 1160,290 L 1280,440 L 1280,640 Z'
$mountainsSvg = "<path d='$mountains' fill='#203541' opacity='.55'/><path d='M 360,480 L 540,240 L 700,458 L 584,360 L 545,303 L 501,369 Z' fill='#354D5C' opacity='.5'/>"
function DrawMountains($dc, [double]$scaleY) {
    $dc.PushTransform([Windows.Media.ScaleTransform]::new(1,$scaleY))
    $dc.PushOpacity(.55)
    $dc.DrawGeometry((Brush '#203541'),$null,[Windows.Media.Geometry]::Parse($mountains))
    $dc.DrawGeometry((Brush '#354D5C'),$null,[Windows.Media.Geometry]::Parse('M 360,480 L 540,240 L 700,458 L 584,360 L 545,303 L 501,369 Z'))
    $dc.Pop(); $dc.Pop()
}
SvgFile 'hero.svg' 1280 400 "<rect width='1280' height='400' fill='$ink'/><g transform='scale(1 .625)'>$mountainsSvg</g><path d='M 42,358 L 1238,358 M 42,42 L 1238,42' stroke='#343C47'/><g transform='translate(67 70) scale(1.0)'>$mark</g><g font-family='Segoe UI, sans-serif'><text x='362' y='153' font-family='Cinzel, Georgia, serif' font-size='60' fill='$gold'>Dragonmark</text><text x='365' y='211' font-size='25' fill='#F1F1EE'>Skyrim Achievement Manager</text><text x='365' y='262' font-size='19' fill='$muted'>Your achievements. A clearer view.</text><text x='365' y='320' font-size='14' fill='$muted'>WINDOWS x64  /  OPEN SOURCE  /  EXPERIMENTAL PREVIEW</text></g>" 'Charcoal and antique-gold original identity. Your achievements. A clearer view.'
Png 'hero.png' 1280 400 {
    param($dc)
    $dc.DrawRectangle((Brush $ink),$null,[Windows.Rect]::new(0,0,1280,400))
    DrawMountains $dc .625
    $pen=[Windows.Media.Pen]::new((Brush '#343C47'),1)
    $dc.DrawLine($pen,[Windows.Point]::new(42,42),[Windows.Point]::new(1238,42)); $dc.DrawLine($pen,[Windows.Point]::new(42,358),[Windows.Point]::new(1238,358))
    DrawMark $dc 67 70 256
    TextLine $dc 'Dragonmark' 362 89 60 $gold
    TextLine $dc 'Skyrim Achievement Manager' 365 180 25 '#F1F1EE'
    TextLine $dc 'Your achievements. A clearer view.' 365 239 19 $muted
    TextLine $dc 'WINDOWS x64  /  OPEN SOURCE  /  EXPERIMENTAL PREVIEW' 365 301 14 $muted
}
SvgFile 'social-preview.svg' 1280 640 "<rect width='1280' height='640' fill='$ink'/>$mountainsSvg<rect x='24' y='24' width='1232' height='592' rx='16' fill='none' stroke='#343C47'/><g transform='translate(64 148) scale(1.25)'>$mark</g><g font-family='Segoe UI, sans-serif'><text x='450' y='236' font-family='Cinzel, Georgia, serif' font-size='66' fill='$gold'>Dragonmark</text><text x='454' y='300' font-size='29' fill='#F1F1EE'>Skyrim Achievement Manager</text><text x='454' y='368' font-size='23' fill='$muted'>Your achievements. A clearer view.</text><text x='454' y='438' font-size='17' fill='$muted'>Inspect · Select · Confirm · Verify</text><text x='454' y='530' font-size='15' fill='$gold'>OPEN SOURCE · WINDOWS x64 · EXPERIMENTAL</text></g>" 'Dragonmark social preview. Independent Skyrim SE/AE achievement utility. Experimental.'
Png 'social-preview.png' 1280 640 {
    param($dc)
    $dc.DrawRectangle((Brush $ink),$null,[Windows.Rect]::new(0,0,1280,640))
    DrawMountains $dc 1
    $dc.DrawRoundedRectangle($null,[Windows.Media.Pen]::new((Brush '#343C47'),1),[Windows.Rect]::new(24,24,1232,592),16,16)
    DrawMark $dc 64 148 320
    TextLine $dc 'Dragonmark' 450 164 66 $gold
    TextLine $dc 'Skyrim Achievement Manager' 454 265 29 '#F1F1EE'
    TextLine $dc 'Your achievements. A clearer view.' 454 340 23 $muted
    TextLine $dc 'Inspect · Select · Confirm · Verify' 454 416 17 $muted
    TextLine $dc 'OPEN SOURCE · WINDOWS x64 · EXPERIMENTAL' 454 511 15 $gold
}
# ICO directory with lossless PNG frames at Windows shell sizes.
$sizes=@(16,24,32,48,64,128,256); $frames=@()
foreach($size in $sizes) {
    $file="icon-$size.png"
    Png $file $size $size { param($dc) $dc.DrawRoundedRectangle((Brush $ink),$null,[Windows.Rect]::new(0,0,$size,$size),$size/6,$size/6); DrawMark $dc 0 0 $size }
    $frames+=,[IO.File]::ReadAllBytes((Join-Path $out $file))
    Remove-Item -LiteralPath (Join-Path $out $file)
}
$stream=[IO.File]::Create((Join-Path $out 'dragonmark.ico')); $writer=[IO.BinaryWriter]::new($stream)
try {
    $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$sizes.Count)
    $offset=6+16*$sizes.Count
    for($i=0;$i -lt $sizes.Count;$i++) {
        $dim=if($sizes[$i] -eq 256){0}else{$sizes[$i]}
        $writer.Write([byte]$dim); $writer.Write([byte]$dim); $writer.Write([byte]0); $writer.Write([byte]0)
        $writer.Write([uint16]1); $writer.Write([uint16]32); $writer.Write([uint32]$frames[$i].Length); $writer.Write([uint32]$offset)
        $offset+=$frames[$i].Length
    }
    foreach($frame in $frames){$writer.Write([byte[]]$frame)}
} finally {$writer.Dispose()}
Write-Host 'Generated original SVG, PNG and Windows icon assets in assets/branding.'
