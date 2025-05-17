[Setup]
AppName=DevKit
AppVersion=1.0.0
DefaultDirName={commonpf}\DevKit
DefaultGroupName=DevKit
OutputDir=C:\Users\Administrator\Desktop
OutputBaseFilename=DevKit_Setup
SetupIconFile=D:\Code\RiderProjects\DevKit\DevKit\Images\launcher.ico
UninstallDisplayIcon={app}\DevKit.exe
Compression=lzma
SolidCompression=yes
PrivilegesRequired=none
ArchitecturesInstallIn64BitMode=x64

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; 从 Release 目录复制所有文件
Source: "D:\Code\RiderProjects\DevKit\DevKit\bin\Release\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\DevKit"; Filename: "{app}\DevKit.exe"
Name: "{commondesktop}\DevKit"; Filename: "{app}\DevKit.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\DevKit.exe"; Description: "{cm:LaunchProgram,DevKit}"; Flags: nowait postinstall skipifsilent