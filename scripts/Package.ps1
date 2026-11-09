#requires -Version 7.0
param([string]$Version, [string]$InstallerCompiler)
$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_UI_LANGUAGE = 'en-US'
$repo = Split-Path $PSScriptRoot -Parent
[xml]$properties = Get-Content (Join-Path $repo 'Directory.Build.props')
$declared = [string]$properties.Project.PropertyGroup.Version
if (!$Version) { $Version = $declared }
if ($Version -cne $declared) { throw 'Archive and assembly versions must match Directory.Build.props.' }
if ($Version -notmatch '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)-(preview|beta)\.(0|[1-9]\d*)$') {
    throw 'Only X.Y.Z-preview.N or X.Y.Z-beta.N prereleases are permitted while broader validation remains pending.'
}
$archive = Join-Path $repo "artifacts\Dragonmark-v$Version-win-x64.zip"
if (Test-Path -LiteralPath $archive) { throw "Archive already exists: $archive. Use a new version or remove it explicitly." }
Push-Location $repo
try {
    & ./scripts/Check-Repository.ps1
    dotnet restore Dragonmark.sln --locked-mode
    if ($LASTEXITCODE -ne 0) { throw 'Locked restore failed.' }
    dotnet format Dragonmark.sln --verify-no-changes --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Formatting failed.' }
    dotnet build Dragonmark.sln -c Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    dotnet test Dragonmark.sln -c Release --no-build --logger trx --results-directory TestResults
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
    & ./scripts/Check-Dependencies.ps1
    # A new directory prevents stale/private files or a native DLL entering the archive.
    $stage = Join-Path $repo ('artifacts\stage-' + [Guid]::NewGuid().ToString('N'))
    dotnet publish src/Dragonmark.App/Dragonmark.App.csproj -c Release --self-contained true -p:RestoreLockedMode=true -p:IncludeSteamNative=false -p:DebugType=None -o $stage
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
    if (Test-Path (Join-Path $stage 'steam_api64.dll')) { throw 'The proprietary native binary requires redistribution review.' }
    Copy-Item README.md,README.tr.md,LICENSE,NOTICE.md,THIRD_PARTY_NOTICES.md,RELEASE_NOTES.md,CHANGELOG.md,CONTRIBUTING.md,CODE_OF_CONDUCT.md,SECURITY.md,ROADMAP.md -Destination $stage
    Copy-Item docs,licenses,assets -Destination $stage -Recurse
    Copy-Item .github -Destination $stage -Recurse
    New-Item -ItemType Directory -Path (Join-Path $stage 'scripts') | Out-Null
    Copy-Item scripts/Setup-SteamNative.ps1 -Destination (Join-Path $stage 'scripts')
    & ./scripts/Check-PackagePrivacy.ps1 -Directory $stage
    $runtime = Get-Content (Join-Path $stage 'Dragonmark.runtimeconfig.json') -Raw | ConvertFrom-Json
    $files = Get-ChildItem -LiteralPath $stage -File -Recurse | Sort-Object FullName | ForEach-Object {
        [ordered]@{ path = [IO.Path]::GetRelativePath($stage, $_.FullName).Replace('\','/'); sha256 = (Get-FileHash -LiteralPath $_.FullName).Hash.ToLowerInvariant() }
    }
    [ordered]@{ product = 'Dragonmark'; version = $Version; platform = 'win-x64'; selfContained = $true; nativeSteamIncluded = $false; liveWriteEvidence = 'Owner-reported successful individual and Unlock All test on 2026-11-09'; independentRestartPersistenceValidated = $false; frameworks = $runtime.runtimeOptions.includedFrameworks; files = @($files) } |
        ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $stage 'manifest.json') -Encoding utf8
    Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $archive
    $hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $([IO.Path]::GetFileName($archive))" | Set-Content -LiteralPath ($archive + '.sha256') -Encoding ascii
    if ($InstallerCompiler) { & ./scripts/Build-Installer.ps1 -Stage $stage -Compiler $InstallerCompiler }
    Write-Host "Created $archive"
    Write-Host "Stage: $stage"
    Write-Host 'Prerelease artifacts only. .NET included; the installer offers verified Steam component download. Nothing was published.'
} finally { Pop-Location }
