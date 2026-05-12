; Inno Setup Script for WowSticky
; Md. Abdullah Al Mamun

#define MyAppName "WowSticky"
#define MyAppVersion "2.0"
#define MyAppIcon "app.ico"
#define MyAppPublisher "Md. Abdullah Al Mamun"
#define MyAppURL "https://github.com/Mamuntheprogrammer"
#define MyAppExeName "WowSticky.exe"

[Setup]
AppId={{B8F4C3A7-2D1E-4F6B-9A5C-8E7D3F1A2B4C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=installer
OutputBaseFilename=WowSticky-Setup-{#MyAppVersion}
Compression=lzma2/ultra
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
DisableProgramGroupPage=yes
SetupIconFile={#MyAppIcon}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: autostart; Description: "Launch on Windows startup"; GroupDescription: "Startup options:"; Flags: checkedonce

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "app.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch WowSticky"; Flags: postinstall nowait skipifsilent

[Registry]
; Add to startup if task is selected
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "WowSticky"; ValueData: """{app}\{#MyAppExeName}"""; Tasks: autostart

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    if not WizardIsTaskSelected('autostart') then
    begin
      // Remove startup entry if user unselected the task
      RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'WowSticky');
    end;
  end;
end;
