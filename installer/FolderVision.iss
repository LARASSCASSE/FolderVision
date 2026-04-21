; ============================================================
;  FolderVision — Script Inno Setup 6
;  https://jrsoftware.org/isinfo.php
; ============================================================

#define MyAppName      "FolderVision"
#define MyAppVersion   "1.0"
#define MyAppPublisher "FolderVision"
#define MyAppExeName   "FolderVision.Wpf.exe"
#define MyAppExeSrc    "..\publish_out\FolderVision.Wpf.exe"
#define MyAppIcon      "..\CODE\FolderVision.Wpf\Ressources\app.ico"

; ── Métadonnées installeur ────────────────────────────────
[Setup]
AppId                        = {{B3C8E1F2-9D4A-4B7E-8F6C-1A2D3E4F5A6B}
AppName                      = {#MyAppName}
AppVersion                   = {#MyAppVersion}
AppVerName                   = {#MyAppName} {#MyAppVersion}
AppPublisher                 = {#MyAppPublisher}

; Dossier d'installation par défaut : C:\Program Files\FolderVision
DefaultDirName               = {autopf}\{#MyAppName}
DefaultGroupName             = {#MyAppName}
AllowNoIcons                 = yes

; Fichier de sortie
OutputDir                    = ..
OutputBaseFilename           = FolderVision_Setup

; Icône de l'installeur
SetupIconFile                = {#MyAppIcon}

; Compression maximale
Compression                  = lzma2/ultra64
SolidCompression             = yes

; Style wizard moderne Windows 11
WizardStyle                  = modern
WizardSizePercent            = 120

; 64-bit uniquement (l'app est compilée win-x64)
ArchitecturesInstallIn64BitMode = x64compatible

; Droits admin (pour écrire dans Program Files)
PrivilegesRequired           = admin

; ── Langues ──────────────────────────────────────────────
[Languages]
Name: "french";  MessagesFile: "compiler:Languages\French.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

; ── Tâches optionnelles (cases à cocher pendant l'install) ──
[Tasks]
Name: "desktopicon"; \
    Description: "Créer un raccourci sur le Bureau"; \
    GroupDescription: "Raccourcis supplémentaires :"; \
    Flags: checked

; ── Fichiers à installer ─────────────────────────────────
[Files]
; L'exe auto-contenu (tout le runtime .NET est dedans)
Source: "{#MyAppExeSrc}"; DestDir: "{app}"; Flags: ignoreversion

; ── Raccourcis ───────────────────────────────────────────
[Icons]
; Menu Démarrer
Name: "{group}\{#MyAppName}";             Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Désinstaller {#MyAppName}"; Filename: "{uninstallexe}"

; Bureau (si tâche cochée)
Name: "{autodesktop}\{#MyAppName}"; \
    Filename: "{app}\{#MyAppExeName}"; \
    Tasks: desktopicon

; ── Lancement après installation ─────────────────────────
[Run]
Filename: "{app}\{#MyAppExeName}"; \
    Description: "Lancer {#MyAppName}"; \
    Flags: nowait postinstall skipifsilent
