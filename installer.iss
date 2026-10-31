; Inno Setup Script for WheelConvert
; Universal Installer: Supports both 64-bit and 32-bit Windows 10 / 11

#define MyAppName "WheelConvert"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "PersonalProject"
#define MyAppExeName "WheelConvert.exe"

[Setup]
AppId={{E8C22DA5-B623-4E84-95E0-05D95C246E2B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=publish\installer
OutputBaseFilename=WheelConvert-Setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "startup"; Description: "Launch WheelConvert automatically when Windows starts"; GroupDescription: "Startup options:"

[Files]
; 64-bit executable installed on 64-bit Windows 10/11
Source: "publish\x64\WheelConvert.exe"; DestDir: "{app}"; DestName: "{#MyAppExeName}"; Check: Is64BitInstallMode; Flags: ignoreversion
; 32-bit executable installed on 32-bit Windows 10
Source: "publish\x86\WheelConvert.exe"; DestDir: "{app}"; DestName: "{#MyAppExeName}"; Check: not Is64BitInstallMode; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startup

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Messages]
WelcomeLabel2=WheelConvert allows you to convert and process files directly from Windows Explorer using a sleek radial wheel menu.%n%nThis installer automatically detects whether your PC is 64-bit or 32-bit and installs the optimal version for your system.
FinishedLabel=Setup has finished installing WheelConvert on your computer.%n%nHow to use:%n1. Select any file (or multiple files) in File Explorer.%n2. Press [ Ctrl + Shift + Space ] on your keyboard.%n3. Choose Convert, Crop, Compress, or view Metadata from the radial wheel!

