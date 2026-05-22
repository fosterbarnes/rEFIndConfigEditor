#define AppName "rEFIndConfigEditor"
#define AppDisplayName "rEFInd Config Editor (x86)"
#ifndef AppVersion
#define AppVersion "0.0.0"
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
#define ExeName "rEFIndConfigEditor.exe"

[Setup]
AppId={{C4E8F1A2-3B5D-4E6F-9A0C-1D2E3F4A5B6C}
AppName={#AppName}
UninstallDisplayName={#AppDisplayName}
AppVersion={#AppVersion}
DisableDirPage=auto
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
DefaultDirName={localappdata}\{#AppName}\app
UninstallDisplayIcon={app}\{#ExeName}
ArchitecturesAllowed=x86compatible
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=commandline
OutputDir=Output
OutputBaseFilename=rEFIndConfigEditor-x86-installer
SetupIconFile={#SetupIconFile}
SolidCompression=yes
WizardStyle=classic dark

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Messages]
SetupWindowTitle=rEFInd Config Editor v{#AppVersion} installer (x86)

[CustomMessages]
CreateStartMenuIcon=Create Start Menu shortcut

[Tasks]
Name: "desktopicon"; Description: "Create Desktop shortcut"; GroupDescription: "{cm:AdditionalIcons}"
Name: "startmenuicon"; Description: "{cm:CreateStartMenuIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\publish\x86\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#ExeName}"; Tasks: startmenuicon
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#ExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#ExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
