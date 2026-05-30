; ZgrzytDesktop Windows installer (Inno Setup 6)
; Build (CI or local, after publish):
;   iscc /DAppVersionDefine=0.1.0 /DPublishDir="...\publish" /DOutputDir="...\release" ZgrzytDesktop.iss

#ifndef PublishDir
  #define PublishDir "..\ZgrzytDesktop\bin\Release\net10.0-windows\win-x64\publish"
#endif

#ifndef OutputDir
  #define OutputDir "..\release"
#endif

#ifndef AppVersionDefine
  #define AppVersionDefine "0.1.0"
#endif

#ifndef BrandingDir
  #define BrandingDir "..\assets\branding"
#endif

[Setup]
AppId={{8F4E2B1A-3C5D-4E6F-9A0B-1C2D3E4F5A6B}
AppName=ZgrzytDesktop
AppVersion={#AppVersionDefine}
AppVerName=ZgrzytDesktop {#AppVersionDefine}
DefaultDirName={autopf}\ZgrzytDesktop
DefaultGroupName=ZgrzytDesktop
OutputDir={#OutputDir}
OutputBaseFilename=ZgrzytDesktopSetup
SetupIconFile={#BrandingDir}\zgrzyt-logo.ico
WizardSmallImageFile={#BrandingDir}\wizard-small.png
Compression=lzma2/max
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\ZgrzytDesktop.exe
UninstallDisplayName=ZgrzytDesktop
PrivilegesRequired=lowest
WizardStyle=modern
SetupLogging=yes
DisableProgramGroupPage=no
VersionInfoVersion={#AppVersionDefine}
CloseApplications=yes
CloseApplicationsFilter=ZgrzytDesktop.exe

[Languages]
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\ZgrzytDesktop"; Filename: "{app}\ZgrzytDesktop.exe"; IconFilename: "{app}\ZgrzytDesktop.exe"
Name: "{group}\{cm:UninstallProgram,ZgrzytDesktop}"; Filename: "{uninstallexe}"; IconFilename: "{app}\ZgrzytDesktop.exe"
Name: "{autodesktop}\ZgrzytDesktop"; Filename: "{app}\ZgrzytDesktop.exe"; IconFilename: "{app}\ZgrzytDesktop.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\ZgrzytDesktop.exe"; Description: "{cm:LaunchProgram,ZgrzytDesktop}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; %AppData%\ZgrzytDesktop — token, cache, settings, logs (AppDataPaths)
Type: filesandordirs; Name: "{userappdata}\ZgrzytDesktop"

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
  begin
    { Ensure local app data is removed even if subfolders were recreated at runtime. }
    if DirExists(ExpandConstant('{userappdata}\ZgrzytDesktop')) then
      DelTree(ExpandConstant('{userappdata}\ZgrzytDesktop'), True, True, True);
  end;
end;
