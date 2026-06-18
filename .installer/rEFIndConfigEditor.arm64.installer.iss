#define AppName "rEFIndConfigEditor"
#define AppDisplayName "rEFInd Config Editor (ARM64)"
#ifndef AppVersion
#define AppVersion "1.1.0"
#endif
#ifndef AppCopyright
#define AppCopyright "Copyright © 2026 Foster Barnes"
#endif
#ifndef AppPublisher
#define AppPublisher "fosterbarnes"
#endif
#ifndef AppURL
#define AppURL "https://github.com/fosterbarnes/rEFIndConfigEditor"
#endif
#ifndef SetupIconFile
#define SetupIconFile "..\.resources\icon\refind.ico"
#endif
#ifndef WizardSmallImageFile
#define WizardSmallImageFile "wizard\installer-wizard-small.png"
#endif
#ifndef WizardImageFile
#define WizardImageFile "wizard\installer-wizard-large.png"
#endif
#define ExeName "rEFIndConfigEditor.exe"

[Setup]
AppId={{C4E8F1A2-3B5D-4E6F-9A0C-1D2E3F4A5B6C}
AppName={#AppName}
UninstallDisplayName={#AppDisplayName}
AppVersion={#AppVersion}
AppCopyright={#AppCopyright}
DisableDirPage=auto
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
DefaultDirName={localappdata}\{#AppName}\app
UninstallDisplayIcon={app}\{#ExeName}
ArchitecturesAllowed=arm64
ArchitecturesInstallIn64BitMode=arm64
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=commandline
OutputDir=Output
OutputBaseFilename=rEFIndConfigEditor-arm64-installer
SetupIconFile={#SetupIconFile}
WizardSmallImageFile={#WizardSmallImageFile}
WizardImageFile={#WizardImageFile}
SolidCompression=yes
WizardStyle=classic dark

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Messages]
SetupWindowTitle=rEFInd Config Editor v{#AppVersion} installer (ARM64)

[CustomMessages]
CreateStartMenuIcon=Create Start Menu shortcut

[Tasks]
Name: "desktopicon"; Description: "Create Desktop shortcut"; GroupDescription: "{cm:AdditionalIcons}"
Name: "startmenuicon"; Description: "{cm:CreateStartMenuIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\publish\arm64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#ExeName}"; Tasks: startmenuicon
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#ExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#ExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

#include "rEFIndConfigEditor.uninstall.iss"
