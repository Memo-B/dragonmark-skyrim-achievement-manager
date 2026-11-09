#ifndef SourceDir
  #error SourceDir must identify the verified self-contained package stage
#endif
#ifndef ProductVersion
  #error ProductVersion is required
#endif
#ifndef ArtifactDir
  #error ArtifactDir is required
#endif

[Setup]
AppId={{7B106946-C65F-4C58-B7D7-79484ABEA65A}
AppName=Dragonmark
AppVersion={#ProductVersion}
AppVerName=Dragonmark {#ProductVersion}
AppPublisher=Dragonmark contributors
AppPublisherURL=https://github.com/Memo-B/dragonmark-skyrim-achievement-manager
AppSupportURL=https://github.com/Memo-B/dragonmark-skyrim-achievement-manager/issues
DefaultDirName={localappdata}\Programs\Dragonmark
DefaultGroupName=Dragonmark
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputDir={#ArtifactDir}
OutputBaseFilename=Dragonmark-v{#ProductVersion}-win-x64-Setup
SetupIconFile={#SourceDir}\assets\branding\dragonmark.ico
UninstallDisplayIcon={app}\Dragonmark.exe
LicenseFile={#SourceDir}\LICENSE
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes
CloseApplications=yes
RestartApplications=no
SetupLogging=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "steamcomponent"; Description: "Download the verified Steam connection component (Internet required)"; GroupDescription: "Steam integration:"; Flags: checkedonce
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{tmp}\steam_api64.dll"; DestDir: "{app}"; Flags: external ignoreversion; Tasks: steamcomponent

[Icons]
Name: "{group}\Dragonmark"; Filename: "{app}\Dragonmark.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\Dragonmark"; Filename: "{app}\Dragonmark.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\Dragonmark.exe"; Description: "Launch Dragonmark"; WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent

[Code]
function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  if WizardIsTaskSelected('steamcomponent') then
  begin
    WizardForm.StatusLabel.Caption := 'Downloading and verifying the Steam connection component...';
    try
      DownloadTemporaryFile(
        'https://raw.githubusercontent.com/rlabrecque/Steamworks.NET/2024.8.0/com.rlabrecque.steamworks.net/Plugins/steam_api64.dll',
        'steam_api64.dll',
        '1add7f151fa644870a735ae86e68d1f019f296130d8e7c0a7ed3ecc7482dccbc', nil);
    except
      Result := 'The Steam component could not be downloaded or verified. No application files have been installed. Check your Internet connection and retry, or go Back and deselect the Steam component to install Demo Mode only. ' + GetExceptionMessage;
    end;
  end;
end;
