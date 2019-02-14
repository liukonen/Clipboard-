; Script generated by the Inno Script Studio Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "Clipboard++"
#define MyAppVersion "1.0"
#define MyAppPublisher "Luke Liukonen"
#define MyAppURL "https://github.com/liukonen/Clipboard-"
#define MyAppExeName "Clipboard.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{ABEB0BC3-4E83-4650-A63A-26AEE2286E00}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={pf}\{#MyAppName}
DefaultGroupName={#MyAppName}
LicenseFile=..\Clipboard-\Clipboard\Clipboard++ License.lic
InfoBeforeFile=..\Clipboard-\Clipboard\msgpack License.lic
OutputDir=C:\Users\Luke\Desktop
OutputBaseFilename=Clipboard++ setup
Compression=lzma
SolidCompression=yes
DisableWelcomePage=False

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\Clipboard-\Clipboard\bin\Release\Clipboard.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\Clipboard-\Clipboard\bin\Release\Clipboard.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\Clipboard-\Clipboard\bin\Release\Clipboard.exe.manifest"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\Clipboard-\Clipboard\bin\Release\Clipboard.pdb"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\Clipboard-\Clipboard\bin\Release\MsgPack.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\Clipboard-\Clipboard\bin\Release\MsgPack.xml"; DestDir: "{app}"; Flags: ignoreversion
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Messages]
InformationTitle=MsgPack License
InfoBeforeClickLabel=If you agree to the license, click Next.
InfoBeforeLabel=License information for MessagePack, used in the project
WelcomeLabel2=This will install [name/ver] on your computer.%n%nIt is recommended that you close all other applications before continuing.%n%nApplication needs Microsoft .Net version 4.6 or above to function. This version should be included in any version of Windows 10 or above. Previous versions will need to install the update. Please check your version before installing.