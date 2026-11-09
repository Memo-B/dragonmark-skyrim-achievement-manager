#requires -Version 7.0
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$packages = @{}
foreach ($project in Get-ChildItem (Join-Path $repo 'src'),(Join-Path $repo 'tests') -Filter project.assets.json -Recurse) {
    $assets = Get-Content $project.FullName -Raw | ConvertFrom-Json -AsHashtable
    foreach ($entry in $assets.libraries.GetEnumerator()) {
        if ($entry.Value.type -ne 'package') { continue }
        $key = $entry.Key
        if (!$packages.ContainsKey($key)) {
            $packagePath = $null
            foreach ($folder in $assets.packageFolders.Keys) {
                $candidate = Join-Path $folder $entry.Value.path
                if (Test-Path -LiteralPath $candidate) { $packagePath = $candidate; break }
            }
            if (!$packagePath) { throw "Restore package $key before exporting licenses." }
            $spec = Get-ChildItem -LiteralPath $packagePath -Filter *.nuspec | Select-Object -First 1
            [xml]$xml = Get-Content $spec.FullName
            $meta = $xml.package.metadata
            $license = if ($meta.license) { [string]$meta.license.InnerText } else { [string]$meta.licenseUrl }
            if (!$license) { throw "No license metadata for $key; inspect manually." }
            $packages[$key] = [ordered]@{ Package = [string]$meta.id; Version = [string]$meta.version; LicenseMetadata = $license; LicenseType = [string]$meta.license.type; Project = [string]$meta.projectUrl; Usage = 'development/test' }
        }
        if ($project.FullName -match '[\\/]src[\\/]') { $packages[$key].Usage = 'application' }
    }
}
$packages.Values | ForEach-Object { [pscustomobject]$_ } | Sort-Object Package | Export-Csv (Join-Path $repo 'docs/dependency-licenses.csv') -NoTypeInformation -Encoding utf8
Write-Host "Exported $($packages.Count) dependency license records. File/URL license entries require manual inspection. Runtime notices are maintained separately."
