#requires -Version 7.0
param([Parameter(Mandatory)][string]$Directory)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $Directory).Path
$issues = [Collections.Generic.List[string]]::new()
$files = @(Get-ChildItem -LiteralPath $root -File -Recurse)
# Inspect binary strings too: excluding .pdb files alone does not remove the
# original PDB pathname from a PE debug directory in an already-built DLL.
$profilePath = [regex]::new('[A-Za-z]:[\\/]+Users[\\/]+[^\\/\x00\r\n]{1,100}[\\/]', [Text.RegularExpressions.RegexOptions]::IgnoreCase)
$steamId = [regex]::new('(?<![0-9])7656119[0-9]{10}(?![0-9])')
foreach ($file in $files) {
    $relative = [IO.Path]::GetRelativePath($root, $file.FullName)
    if ($file.Name -match '^(settings\.json|activity\.log.*|snapshot-.*\.json|steam-probe\.json|\.env.*)$' -or $file.Extension -in '.pdb','.trx','.log','.dmp','.pfx','.pem','.key') {
        $issues.Add("Private/debug file: $relative")
    }
    $bytes = [IO.File]::ReadAllBytes($file.FullName)
    $variants = @([Text.Encoding]::Latin1.GetString($bytes), [Text.Encoding]::Unicode.GetString($bytes))
    if ($bytes.Length -gt 1) { $variants += [Text.Encoding]::Unicode.GetString($bytes, 1, $bytes.Length - 1) }
    foreach ($value in $variants) {
        if ($value.Contains('Users', [StringComparison]::OrdinalIgnoreCase) -and $profilePath.IsMatch($value)) { $issues.Add("Absolute Windows profile path: $relative"); break }
        if ($value.Contains('7656119', [StringComparison]::Ordinal) -and $steamId.IsMatch($value)) { $issues.Add("Steam account identifier pattern: $relative"); break }
    }
}
if ($issues.Count) { throw ('Package privacy gate failed. Matched values are intentionally omitted.' + "`n" + ($issues -join "`n")) }
Write-Host "Package privacy gate passed: $($files.Count) files; binary/text path and Steam ID checks; private/debug file exclusions."
