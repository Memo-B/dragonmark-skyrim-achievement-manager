#requires -Version 7.0
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
Push-Location $repo
try {
    # --vulnerable can return zero even when findings exist: inspect the JSON.
    $output = dotnet list Dragonmark.sln package --vulnerable --include-transitive --format json --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Dependency audit command failed.' }
    $report = ($output -join "`n") | ConvertFrom-Json
    New-Item -ItemType Directory -Path artifacts -Force | Out-Null
    $output | Set-Content artifacts/dependency-audit.json -Encoding utf8
    $expected = @([regex]::Matches((Get-Content Dragonmark.sln -Raw), 'Project\("[^"]+"\) = "[^"]+", "([^"]+\.csproj)",') | ForEach-Object {
        [IO.Path]::GetFullPath((Join-Path $repo $_.Groups[1].Value))
    })
    $audited = @($report.projects | ForEach-Object { [IO.Path]::GetFullPath($_.path) })
    if (!$expected.Count -or !$report.sources -or $audited.Count -ne $expected.Count -or (Compare-Object $expected $audited)) {
        throw 'Incomplete dependency audit report: it must cover every solution project.'
    }
    if (@($report.logs | Where-Object level -eq 'error').Count) { throw 'Dependency audit reported errors.' }
    $findings = @($report.projects.frameworks | ForEach-Object {
        @($_.topLevelPackages) + @($_.transitivePackages) | Where-Object { $_ -and $_.vulnerabilities }
    })
    if ($findings.Count) { throw "Vulnerable dependencies found: $($findings.id -join ', '). Review artifacts/dependency-audit.json." }
    Write-Host 'NuGet audit: no reported vulnerabilities in restored direct or transitive packages.'
} finally { Pop-Location }
