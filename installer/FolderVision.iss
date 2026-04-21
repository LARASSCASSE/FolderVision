; ============================================================
;  FolderVision - Inno Setup 6
;  https://jrsoftware.org/isinfo.php
; ============================================================

#define MyAppName      "FolderVision"
#define MyAppVersion   "1.0"
#define MyAppPublisher "FolderVision"
#define MyAppExeName   "FolderVision.exe"
#define MyAppExeSrc    "..\publish_out\FolderVision.exe"
#define MyAppIcon      "..\CODE\FolderVision.Wpf\Ressources\app.ico"

[Setup]
AppId                           = {{B3C8E1F2-9D4A-4B7E-8F6C-1A2D3E4F5A6B}
AppName                         = {#MyAppName}
AppVersion                      = {#MyAppVersion}
AppVerName                      = {#MyAppName} {#MyAppVersion}
AppPublisher                    = {#MyAppPublisher}

DefaultDirName                  = {autopf}\{#MyAppName}
DefaultGroupName                = {#MyAppName}

; No "Select additional tasks" page - shortcuts always created
AllowNoIcons                    = no

OutputDir                       = ..
OutputBaseFilename              = FolderVision_Setup
SetupIconFile                   = {#MyAppIcon}

Compression                     = lzma2/ultra64
SolidCompression                = yes
WizardStyle                     = modern
WizardSizePercent               = 120

ArchitecturesInstallIn64BitMode = x64compatible
PrivilegesRequired              = admin

[Languages]
Name: "french";  MessagesFile: "compiler:Languages\French.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
; App executable + all required native WPF DLLs
Source: "..\publish_out\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb"

[Icons]
; Start Menu
Name: "{group}\{#MyAppName}";              Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{group}\Desinstaller {#MyAppName}"; Filename: "{uninstallexe}"

; Desktop - always created, no task checkbox
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Lancer {#MyAppName}"; Flags: nowait postinstall skipifsilent
