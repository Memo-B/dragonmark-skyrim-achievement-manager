# Explicit developer setup only. Dragonmark never downloads code at runtime.
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$destination = Join-Path $repo 'native\steam_api64.dll'
$expected = '1ADD7F151FA644870A735AE86E68D1F019F296130D8E7C0A7ED3ECC7482DCCBC'
$url = 'https://raw.githubusercontent.com/rlabrecque/Steamworks.NET/2024.8.0/com.rlabrecque.steamworks.net/Plugins/steam_api64.dll'
New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
if (Test-Path $destination) {
    if ((Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash -ne $expected) { throw 'An unexpected native library already exists. Review it before replacing it.' }
    Write-Host 'The pinned native library is already present.'
    exit 0
}
$temporary = $destination + '.download'
try {
    Invoke-WebRequest -Uri $url -OutFile $temporary
    if ((Get-FileHash -LiteralPath $temporary -Algorithm SHA256).Hash -ne $expected) { throw 'Native library checksum mismatch.' }
    $signature = Get-AuthenticodeSignature -LiteralPath $temporary
    if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'O=Valve Corp\.') { throw 'Expected a valid Valve signature.' }
    Move-Item -LiteralPath $temporary -Destination $destination
    Write-Host 'Installed the official pinned library for local development. Do not add it to Git or public releases.'
} finally { if (Test-Path $temporary) { Remove-Item -LiteralPath $temporary } }
