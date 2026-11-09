@echo off
setlocal
cd /d "%~dp0"
set DOTNET_CLI_UI_LANGUAGE=en-US
where dotnet >nul 2>nul
if errorlevel 1 (
  echo ERROR: Install the .NET 10 SDK or a compatible newer SDK from https://dotnet.microsoft.com/download
  exit /b 1
)
dotnet build Dragonmark.sln -c Release
if errorlevel 1 exit /b 1
dotnet test Dragonmark.sln -c Release --no-build
if errorlevel 1 exit /b 1
dotnet publish src\Dragonmark.App\Dragonmark.App.csproj -c Release --self-contained true -o publish
if errorlevel 1 exit /b 1
echo Ready: publish\Dragonmark.exe
echo Run with --demo to preview without connecting to Steam.
echo Public packaging: pwsh -File scripts\Package.ps1
