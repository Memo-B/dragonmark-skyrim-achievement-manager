#requires -Version 7.0
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$files = @(Get-ChildItem -LiteralPath $repo -File)
foreach ($folder in 'src','tests','scripts','docs','assets','licenses','.github') {
    $files += Get-ChildItem -LiteralPath (Join-Path $repo $folder) -File -Recurse |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }
}
$errorsFound = [Collections.Generic.List[string]]::new()
foreach ($file in $files) {
    $relative = [IO.Path]::GetRelativePath($repo, $file.FullName)
    if ($file.Extension -in '.dll','.exe','.pdb','.zip','.pfx','.pem','.key' -or $file.Name -match '^\.env($|\.)') {
        $errorsFound.Add("Unexpected binary or sensitive file: $relative")
    }
    if ($file.Extension -in '.png','.ico','.ttf') { continue }
    $content = Get-Content -LiteralPath $file.FullName -Raw
    if ($content -match '(?m)-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----|\bgh[pousr]_[A-Za-z0-9]{30,}\b|\bgithub_pat_[A-Za-z0-9_]{60,}\b') {
        $errorsFound.Add("Potential credential in $relative; inspect privately.")
    }
    if ($file.Extension -ne '.md') { continue }
    if (([regex]::Matches($content, '(?m)^```')).Count % 2 -ne 0) { $errorsFound.Add("Unclosed Markdown fence: $relative") }
    $withoutCode = [regex]::Replace($content, '(?ms)^```.*?^```[^\r\n]*', '')
    foreach ($match in [regex]::Matches($withoutCode, '\]\(([^\s)]+)(?:\s+"[^"]*")?\)')) {
        $link = $match.Groups[1].Value.Trim('<','>')
        if ($link -match '^(https?:|mailto:|#)') { continue }
        $path = [Uri]::UnescapeDataString(($link -split '#')[0])
        if ($path -and !(Test-Path -LiteralPath (Join-Path $file.DirectoryName $path))) {
            $errorsFound.Add("Broken local link in ${relative}: $link")
        }
    }
}
$metadata = Get-Content (Join-Path $repo '.github/repository-metadata.json') -Raw | ConvertFrom-Json
if ($metadata.topics.Count -gt 20 -or ($metadata.topics | Where-Object { $_ -notmatch '^[a-z0-9-]{1,50}$' }) -or ($metadata.topics -contains 'skse')) {
    $errorsFound.Add('Repository topics are invalid or claim an absent SKSE integration.')
}
if ($metadata.description.Length -gt 350) { $errorsFound.Add('GitHub About description exceeds 350 characters.') }
if ($errorsFound.Count) { throw ($errorsFound -join "`n") }
Write-Host "Repository checks passed: $($files.Count) source/document/asset files; local Markdown targets, fences, metadata and credential patterns. This is not a full security audit."
