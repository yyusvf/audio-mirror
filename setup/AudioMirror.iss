; Setup für Audio Mirror - erzeugt mit Inno Setup.
;
; Bewusst ein einziges Setup für x64 und ARM64: es legt jeweils nur die passende Datei ab,
; damit Nutzer nicht zwischen Varianten wählen müssen.
;
; Bauen:  ISCC.exe setup\AudioMirror.iss
; Erwartet die veröffentlichten Dateien unter dist\ und dist\arm64\.

#define AppName        "Audio Mirror"
#define AppVersion     "1.2.0"
#define AppPublisher   "Yusuf Esad Mumcu"
#define AppUrl         "https://github.com/yyusvf/audio-mirror"
#define AppExe         "AudioMirror.exe"

[Setup]
AppId={{8F3C6A1E-2B47-4D9A-9E51-7C0A5D8B4F62}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}/issues
AppUpdatesURL={#AppUrl}/releases
VersionInfoVersion={#AppVersion}

; Voreingestellt eine Installation nur für den angemeldeten Benutzer - dann fragt Windows
; nicht nach Administratorrechten. Über die erste Seite lässt sich auf "für alle Benutzer"
; umstellen, wer das möchte.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=no
DisableDirPage=no
AllowNoIcons=yes

LicenseFile=..\LICENSE
OutputDir=..\release
OutputBaseFilename=AudioMirror-Setup-{#AppVersion}
SetupIconFile=..\AudioMirror.ico
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}

ShowLanguageDialog=no
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible or arm64
ArchitecturesInstallIn64BitMode=x64compatible or arm64

; Läuft das Programm noch, bietet das Setup an, es zu schließen, statt an einer gesperrten
; Datei zu scheitern. Der Name entspricht der Instanzsperre aus SingleInstance.cs.
AppMutex=AudioMirror.SingleInstance
CloseApplications=yes
RestartApplications=no

[Languages]
; Englisch zuerst: Inno waehlt die Sprache automatisch passend zur Windows-Anzeigesprache und
; faellt sonst auf den ersten Eintrag zurueck. Deutsches Windows bekommt Deutsch, alles andere
; Englisch - ohne Sprachabfrage beim Start.
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "german";  MessagesFile: "compiler:Languages\German.isl"

[CustomMessages]
english.AutostartTask=Start Audio Mirror with Windows (in the notification area)
english.AutostartGroup=At sign-in:
german.AutostartTask=Audio Mirror mit Windows starten (im Infobereich)
german.AutostartGroup=Beim Anmelden:

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "autostart"; Description: "{cm:AutostartTask}"; GroupDescription: "{cm:AutostartGroup}"; Flags: unchecked

[Files]
; Je nach Prozessor nur die passende Fassung ablegen.
Source: "..\dist\{#AppExe}";       DestDir: "{app}"; DestName: "{#AppExe}"; Flags: ignoreversion; Check: not IsArm64
Source: "..\dist\arm64\{#AppExe}"; DestDir: "{app}"; DestName: "{#AppExe}"; Flags: ignoreversion; Check: IsArm64
Source: "..\README.md";            DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE";              DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}";                  Filename: "{app}\{#AppExe}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}";            Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Registry]
; Autostart nur anlegen, wenn im Setup gewünscht. Das Programm pflegt denselben Eintrag später
; selbst über sein Häkchen, deshalb wird er bei der Deinstallation wieder entfernt.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; \
    ValueName: "AudioMirror"; ValueData: """{app}\{#AppExe}"" --minimized"; \
    Flags: uninsdeletevalue; Tasks: autostart
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: none; \
    ValueName: "AudioMirror"; Flags: uninsdeletevalue; Tasks: not autostart
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run"; \
    ValueType: none; ValueName: "AudioMirror"; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Der Merker aus StartupState.cs; die Einstellungen bleiben absichtlich erhalten, falls später
; erneut installiert wird.
Type: files; Name: "{userappdata}\AudioMirror\restored.flag"

[Code]
// Beim Deinstallieren anbieten, auch die gespeicherten Einstellungen zu entfernen.
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  SettingsDir: String;
  Question: String;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    SettingsDir := ExpandConstant('{userappdata}\AudioMirror');
    if DirExists(SettingsDir) then
    begin
      if ActiveLanguage = 'german' then
        Question := 'Sollen die gespeicherten Einstellungen (Geräteauswahl, Lautstärken) ebenfalls entfernt werden?'
      else
        Question := 'Also remove the saved settings (device selection, volumes)?';
      if SuppressibleMsgBox(Question,
                            mbConfirmation, MB_YESNO or MB_DEFBUTTON2, IDNO) = IDYES then
      begin
        DelTree(SettingsDir, True, True, True);
      end;
    end;
  end;
end;
