#requires -Version 7.0
param([Parameter(Mandatory)][string]$Stage, [Parameter(Mandatory)][string]$Compiler)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$stagePath = (Resolve-Path -LiteralPath $Stage).Path
$manifest = Get-Content -LiteralPath (Join-Path $stagePath 'manifest.json') -Raw | ConvertFrom-Json
$version = $manifest.version
if ($version -notmatch '^\d+\.\d+\.\d+-(preview|beta)\.\d+$') { throw 'Expected a prerelease manifest.' }
if (Test-Path -LiteralPath (Join-Path $stagePath 'steam_api64.dll')) { throw 'Do not embed the Valve native binary in the installer.' }
foreach ($entry in $manifest.files) {
    $candidate = [IO.Path]::GetFullPath((Join-Path $stagePath $entry.path))
    if (!$candidate.StartsWith($stagePath + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Manifest path escaped the stage.' }
    if ((Get-FileHash -LiteralPath $candidate).Hash -ine $entry.sha256) { throw "Manifest mismatch: $($entry.path)" }
}
$artifactDir = Join-Path $repo 'artifacts'
$installer = Join-Path $artifactDir "Dragonmark-v$version-win-x64-Setup.exe"
if (Test-Path -LiteralPath $installer) { throw 'Installer already exists; use a new version or explicitly move the previous artifact.' }
& $Compiler "/DSourceDir=$stagePath" "/DProductVersion=$version" "/DArtifactDir=$artifactDir" (Join-Path $PSScriptRoot 'Dragonmark.iss')
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
$hash = (Get-FileHash -LiteralPath $installer).Hash.ToLowerInvariant()
"$hash  $([IO.Path]::GetFileName($installer))" | Set-Content -LiteralPath ($installer + '.sha256') -Encoding ascii
Write-Host "Created $installer. Installation downloads the pinned Steam component only when selected."
