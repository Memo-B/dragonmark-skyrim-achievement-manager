#requires -Version 7.0
param([switch]$Qa)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
Push-Location $repo
try {
    dotnet build Dragonmark.sln -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    $folder = Join-Path $repo $(if ($Qa) { 'artifacts/ui-review' } else { 'assets/screenshots' })
    $captureArgs = @('src/Dragonmark.App/bin/Release/net10.0-windows/win-x64/Dragonmark.dll', '--demo', '--capture-suite', $folder)
    if ($Qa) { $captureArgs += '--qa' }
    & dotnet @captureArgs
    if ($LASTEXITCODE -ne 0) { throw 'Screenshot capture failed.' }
    Write-Host "Captured the actual compiled WPF demo in $folder. No Steam calls or achievement changes."
} finally { Pop-Location }
