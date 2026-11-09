#requires -Version 7.0
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$toolsDir = Join-Path $repo 'artifacts/installer-compiler'
New-Item -ItemType Directory -Path $toolsDir -Force | Out-Null
$setup = Join-Path $toolsDir 'innosetup-7.1.1-x64.exe'
Invoke-WebRequest -Uri 'https://github.com/jrsoftware/issrc/releases/download/is-7_1_1/innosetup-7.1.1-x64.exe' -OutFile $setup
if ((Get-FileHash -LiteralPath $setup).Hash -ne '8A5A6711C3766A6E5FB74F280BA0E3551E719CE94538D901FDBD1F8F9920B3F0') { throw 'Installer compiler checksum mismatch.' }
$signature = Get-AuthenticodeSignature -LiteralPath $setup
if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'O=Pyrsys B\.V\.') { throw 'Installer compiler signature mismatch.' }
$destination = Join-Path $toolsDir 'inno'
$process = Start-Process -FilePath $setup -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/CURRENTUSER','/NOICONS',('/DIR="' + $destination + '"')) -WindowStyle Hidden -PassThru -Wait
if ($process.ExitCode -ne 0) { throw 'Installer compiler setup failed.' }
Write-Output (Join-Path $destination 'ISCC.exe')
