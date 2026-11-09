#requires -Version 7.0
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
[xml]$properties = Get-Content (Join-Path $repo 'Directory.Build.props')
$version = [string]$properties.Project.PropertyGroup.Version
if ($version -notmatch '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(-(preview|beta)\.(0|[1-9]\d*))?$') { throw 'Expected the current stable or preview/beta version.' }
$notes = Get-Content (Join-Path $repo 'RELEASE_NOTES.md') -Raw
$notes = [regex]::Replace($notes, '(\]\()([^\s)]+)(\))', {
    param($match)
    $path = $match.Groups[2].Value
    if ($path -match '^(https?:|#)') { return $match.Value }
    $base = if ($path -match '\.png$') { 'https://raw.githubusercontent.com/Memo-B/dragonmark-skyrim-achievement-manager' } else { 'https://github.com/Memo-B/dragonmark-skyrim-achievement-manager/blob' }
    return $match.Groups[1].Value + $base + '/v' + $version + '/' + $path + ')'
})
New-Item -ItemType Directory -Path (Join-Path $repo 'artifacts') -Force | Out-Null
$target = Join-Path $repo "artifacts/RELEASE_NOTES-v$version.md"
Set-Content -LiteralPath $target -Value $notes -Encoding utf8
Write-Host "Prepared $target. Tag-scoped links become valid only after the owner pushes the matching tag. Nothing was published."
