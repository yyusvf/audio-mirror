; Setup für Audio Mirror - erzeugt mit Inno Setup.
;
; Bewusst ein einziges Setup für x64 und ARM64: es legt jeweils nur die passende Datei ab,
; damit Nutzer nicht zwischen Varianten wählen müssen.
;
; Bauen:  ISCC.exe setup\AudioMirror.iss
; Erwartet die veröffentlichten Dateien unter dist\ und dist\arm64\.

#define AppName        "Audio Mirror"
#define AppVersion     "1.3.2"
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

; Bei einer Aktualisierung werden Zielordner, Startmenügruppe und die Auswahl der
; Zusatzaufgaben aus der vorigen Installation übernommen - die zugehörigen Seiten entfallen
; dann (siehe ShouldSkipPage).
UsePreviousAppDir=yes
UsePreviousGroup=yes
UsePreviousTasks=yes

DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=no
DisableDirPage=no
AllowNoIcons=yes

LicenseFile=..\LICENSE
OutputDir=..\release
; Die Fassung steht vorn: GitHub sortiert die Dateien einer Veroeffentlichung alphabetisch,
; und so steht das Setup vor dem portablen Archiv statt dahinter.
OutputBaseFilename=AudioMirror-{#AppVersion}-Setup
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
; faellt sonst auf den ersten Eintrag zurueck - ohne Sprachabfrage beim Start. Dieselben
; dreizehn Sprachen beherrscht auch die Anwendung selbst.
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "german";  MessagesFile: "compiler:Languages\German.isl"
Name: "french";  MessagesFile: "compiler:Languages\French.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "dutch";   MessagesFile: "compiler:Languages\Dutch.isl"
Name: "polish";  MessagesFile: "compiler:Languages\Polish.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "ukrainian"; MessagesFile: "compiler:Languages\Ukrainian.isl"
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "czech";   MessagesFile: "compiler:Languages\Czech.isl"
Name: "swedish"; MessagesFile: "compiler:Languages\Swedish.isl"

[CustomMessages]
english.AutostartTask=Start Audio Mirror with Windows (in the notification area)
english.AutostartGroup=At sign-in:
english.RuntimeDownloading=Downloading the Microsoft .NET 8 Desktop Runtime (about 56 MB):
english.RuntimeInstalling=Installing the Microsoft .NET 8 Desktop Runtime. Windows will ask for permission.
english.RuntimeFailed=The .NET 8 Desktop Runtime was not installed, and Audio Mirror cannot start without it.%n%nInstall it from https://dotnet.microsoft.com/download/dotnet/8.0 and run this setup again.
english.RemoveSettings=Also remove the saved settings (device selection, volumes)?

german.AutostartTask=Audio Mirror mit Windows starten (im Infobereich)
german.AutostartGroup=Beim Anmelden:
german.RuntimeDownloading=Microsoft .NET 8 Desktop Runtime wird geladen (etwa 56 MB):
german.RuntimeInstalling=Microsoft .NET 8 Desktop Runtime wird installiert. Windows fragt gleich nach der Erlaubnis.
german.RuntimeFailed=Die .NET 8 Desktop Runtime wurde nicht installiert, und ohne sie startet Audio Mirror nicht.%n%nInstallieren Sie sie von https://dotnet.microsoft.com/download/dotnet/8.0 und starten Sie dieses Setup erneut.
german.RemoveSettings=Sollen die gespeicherten Einstellungen (Geräteauswahl, Lautstärken) ebenfalls entfernt werden?

french.AutostartTask=Démarrer Audio Mirror avec Windows (dans la zone de notification)
french.AutostartGroup=À la connexion :
french.RuntimeDownloading=Téléchargement de Microsoft .NET 8 Desktop Runtime (environ 56 Mo) :
french.RuntimeInstalling=Installation de Microsoft .NET 8 Desktop Runtime. Windows va demander l’autorisation.
french.RuntimeFailed=Le .NET 8 Desktop Runtime n’a pas été installé, et Audio Mirror ne peut pas démarrer sans lui.%n%nInstallez-le depuis https://dotnet.microsoft.com/download/dotnet/8.0 puis relancez ce programme d’installation.
french.RemoveSettings=Supprimer aussi les réglages enregistrés (choix des périphériques, volumes) ?

spanish.AutostartTask=Iniciar Audio Mirror con Windows (en el área de notificación)
spanish.AutostartGroup=Al iniciar sesión:
spanish.RuntimeDownloading=Descargando Microsoft .NET 8 Desktop Runtime (unos 56 MB):
spanish.RuntimeInstalling=Instalando Microsoft .NET 8 Desktop Runtime. Windows pedirá permiso.
spanish.RuntimeFailed=No se ha instalado .NET 8 Desktop Runtime y Audio Mirror no puede arrancar sin él.%n%nInstálalo desde https://dotnet.microsoft.com/download/dotnet/8.0 y vuelve a ejecutar este instalador.
spanish.RemoveSettings=¿Eliminar también los ajustes guardados (selección de dispositivos, volúmenes)?

italian.AutostartTask=Avvia Audio Mirror con Windows (nell’area di notifica)
italian.AutostartGroup=All’accesso:
italian.RuntimeDownloading=Download di Microsoft .NET 8 Desktop Runtime (circa 56 MB):
italian.RuntimeInstalling=Installazione di Microsoft .NET 8 Desktop Runtime. Windows chiederà l’autorizzazione.
italian.RuntimeFailed=Il .NET 8 Desktop Runtime non è stato installato e Audio Mirror non può avviarsi senza di esso.%n%nInstallalo da https://dotnet.microsoft.com/download/dotnet/8.0 ed esegui di nuovo questo programma di installazione.
italian.RemoveSettings=Rimuovere anche le impostazioni salvate (scelta dei dispositivi, volumi)?

brazilianportuguese.AutostartTask=Iniciar o Audio Mirror com o Windows (na área de notificação)
brazilianportuguese.AutostartGroup=Ao entrar:
brazilianportuguese.RuntimeDownloading=Baixando o Microsoft .NET 8 Desktop Runtime (cerca de 56 MB):
brazilianportuguese.RuntimeInstalling=Instalando o Microsoft .NET 8 Desktop Runtime. O Windows vai pedir permissão.
brazilianportuguese.RuntimeFailed=O .NET 8 Desktop Runtime não foi instalado e o Audio Mirror não inicia sem ele.%n%nInstale-o em https://dotnet.microsoft.com/download/dotnet/8.0 e execute este instalador novamente.
brazilianportuguese.RemoveSettings=Remover também as configurações salvas (seleção de dispositivos, volumes)?

dutch.AutostartTask=Audio Mirror met Windows starten (in het systeemvak)
dutch.AutostartGroup=Bij aanmelden:
dutch.RuntimeDownloading=Microsoft .NET 8 Desktop Runtime wordt gedownload (ongeveer 56 MB):
dutch.RuntimeInstalling=Microsoft .NET 8 Desktop Runtime wordt geïnstalleerd. Windows vraagt zo om toestemming.
dutch.RuntimeFailed=De .NET 8 Desktop Runtime is niet geïnstalleerd en Audio Mirror kan zonder deze niet starten.%n%nInstalleer deze via https://dotnet.microsoft.com/download/dotnet/8.0 en voer dit installatieprogramma opnieuw uit.
dutch.RemoveSettings=Ook de opgeslagen instellingen verwijderen (apparaatkeuze, volumes)?

polish.AutostartTask=Uruchamiaj Audio Mirror z systemem Windows (w obszarze powiadomień)
polish.AutostartGroup=Przy logowaniu:
polish.RuntimeDownloading=Pobieranie Microsoft .NET 8 Desktop Runtime (około 56 MB):
polish.RuntimeInstalling=Instalowanie Microsoft .NET 8 Desktop Runtime. Windows poprosi o zgodę.
polish.RuntimeFailed=Środowisko .NET 8 Desktop Runtime nie zostało zainstalowane, a bez niego Audio Mirror się nie uruchomi.%n%nZainstaluj je ze strony https://dotnet.microsoft.com/download/dotnet/8.0 i uruchom ten instalator ponownie.
polish.RemoveSettings=Usunąć także zapisane ustawienia (wybór urządzeń, głośności)?

russian.AutostartTask=Запускать Audio Mirror вместе с Windows (в области уведомлений)
russian.AutostartGroup=При входе в систему:
russian.RuntimeDownloading=Загрузка Microsoft .NET 8 Desktop Runtime (около 56 МБ):
russian.RuntimeInstalling=Установка Microsoft .NET 8 Desktop Runtime. Windows запросит разрешение.
russian.RuntimeFailed=.NET 8 Desktop Runtime не установлен, без него Audio Mirror не запустится.%n%nУстановите его с https://dotnet.microsoft.com/download/dotnet/8.0 и запустите эту программу установки снова.
russian.RemoveSettings=Удалить также сохранённые настройки (выбор устройств, громкость)?

ukrainian.AutostartTask=Запускати Audio Mirror разом із Windows (в області сповіщень)
ukrainian.AutostartGroup=Під час входу:
ukrainian.RuntimeDownloading=Завантаження Microsoft .NET 8 Desktop Runtime (близько 56 МБ):
ukrainian.RuntimeInstalling=Встановлення Microsoft .NET 8 Desktop Runtime. Windows запитає дозвіл.
ukrainian.RuntimeFailed=.NET 8 Desktop Runtime не встановлено, без нього Audio Mirror не запуститься.%n%nВстановіть його з https://dotnet.microsoft.com/download/dotnet/8.0 і запустіть цю програму встановлення ще раз.
ukrainian.RemoveSettings=Видалити також збережені налаштування (вибір пристроїв, гучність)?

turkish.AutostartTask=Audio Mirror’ı Windows ile başlat (bildirim alanında)
turkish.AutostartGroup=Oturum açıldığında:
turkish.RuntimeDownloading=Microsoft .NET 8 Desktop Runtime indiriliyor (yaklaşık 56 MB):
turkish.RuntimeInstalling=Microsoft .NET 8 Desktop Runtime kuruluyor. Windows birazdan izin isteyecek.
turkish.RuntimeFailed=.NET 8 Desktop Runtime kurulmadı ve Audio Mirror onsuz başlayamaz.%n%nhttps://dotnet.microsoft.com/download/dotnet/8.0 adresinden kurun ve bu kurulumu yeniden çalıştırın.
turkish.RemoveSettings=Kaydedilmiş ayarlar da kaldırılsın mı (aygıt seçimi, ses düzeyleri)?

czech.AutostartTask=Spouštět Audio Mirror se systémem Windows (v oznamovací oblasti)
czech.AutostartGroup=Při přihlášení:
czech.RuntimeDownloading=Stahuje se Microsoft .NET 8 Desktop Runtime (asi 56 MB):
czech.RuntimeInstalling=Instaluje se Microsoft .NET 8 Desktop Runtime. Windows za chvíli požádá o svolení.
czech.RuntimeFailed=.NET 8 Desktop Runtime nebyl nainstalován a bez něj se Audio Mirror nespustí.%n%nNainstalujte jej z https://dotnet.microsoft.com/download/dotnet/8.0 a spusťte tuto instalaci znovu.
czech.RemoveSettings=Odstranit také uložená nastavení (výběr zařízení, hlasitosti)?

swedish.AutostartTask=Starta Audio Mirror med Windows (i meddelandefältet)
swedish.AutostartGroup=Vid inloggning:
swedish.RuntimeDownloading=Hämtar Microsoft .NET 8 Desktop Runtime (cirka 56 MB):
swedish.RuntimeInstalling=Installerar Microsoft .NET 8 Desktop Runtime. Windows kommer att be om tillåtelse.
swedish.RuntimeFailed=.NET 8 Desktop Runtime installerades inte och Audio Mirror kan inte starta utan den.%n%nInstallera den från https://dotnet.microsoft.com/download/dotnet/8.0 och kör det här installationsprogrammet igen.
swedish.RemoveSettings=Vill du även ta bort de sparade inställningarna (enhetsval, volymer)?

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "autostart"; Description: "{cm:AutostartTask}"; GroupDescription: "{cm:AutostartGroup}"; Flags: unchecked

[Files]
; Je nach Prozessor nur die passende Fassung ablegen.
Source: "..\dist\fdd\x64\{#AppExe}";   DestDir: "{app}"; DestName: "{#AppExe}"; Flags: ignoreversion; Check: not IsArm64
Source: "..\dist\fdd\arm64\{#AppExe}"; DestDir: "{app}"; DestName: "{#AppExe}"; Flags: ignoreversion; Check: IsArm64
Source: "..\README.md";            DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE";              DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}";                  Filename: "{app}\{#AppExe}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}";            Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Registry]
; Autostart nur bei der Erstinstallation anlegen, und nur wenn dort gewünscht. Danach gehört
; der Eintrag der Anwendung - sie pflegt ihn über ihr Häkchen und den Infobereich. Würde eine
; Aktualisierung ihn neu schreiben, käme ein abgeschalteter Autostart stillschweigend zurück.
; Der zweite Eintrag schreibt nichts, er sorgt nur dafür, dass der Wert beim Deinstallieren in
; jedem Fall verschwindet.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; \
    ValueName: "AudioMirror"; ValueData: """{app}\{#AppExe}"" --minimized"; \
    Flags: uninsdeletevalue; Tasks: autostart; Check: not IsUpgrade
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: none; \
    ValueName: "AudioMirror"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run"; \
    ValueType: none; ValueName: "AudioMirror"; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent
; Nach einer automatischen Aktualisierung startet Audio Mirror von selbst wieder - und
; zwar so, wie es vorher lief. Die Befehlszeile dafuer setzt das Programm, bevor es sich
; fuer die Installation beendet.
Filename: "{app}\{#AppExe}"; Parameters: "{code:RelaunchArgs}"; Flags: nowait; Check: RelaunchRequested

[UninstallDelete]
; Der Merker aus StartupState.cs; die Einstellungen bleiben absichtlich erhalten, falls später
; erneut installiert wird.
Type: files; Name: "{userappdata}\AudioMirror\restored.flag"

[Code]
const
  // Dieselbe Kennung wie AppId oben; daraus baut Inno seinen Uninstall-Schlüssel.
  AppGuid = '{8F3C6A1E-2B47-4D9A-9E51-7C0A5D8B4F62}';

  RuntimeUrlX64   = 'https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe';
  RuntimeUrlArm64 = 'https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-arm64.exe';

  // Rückgabewerte des Laufzeit-Installers, die keinen Fehler bedeuten.
  RuntimeOk            = 0;
  RuntimeRestartNeeded = 3010;
  RuntimeAlreadyThere  = 1638;

var
  UpgradeChecked: Boolean;
  UpgradeDetected: Boolean;

// "C:\Program Files", auch wenn das Setup selbst 32-bittig läuft. Genau dafür setzt Windows
// ProgramW6432; nur falls es fehlt, wird auf ProgramFiles zurückgegriffen.
function ProgramFilesNative: String;
begin
  Result := GetEnv('ProgramW6432');
  if Result = '' then
    Result := GetEnv('ProgramFiles');
end;

// Gesucht wird ein Ordner der Desktop-Laufzeit 8.x. Neuere Hauptfassungen zählen bewusst nicht:
// die Anwendung ist gegen .NET 8 gebaut und rollt von sich aus nur innerhalb davon weiter.
function DesktopRuntimeInstalled: Boolean;
var
  Rec: TFindRec;
begin
  Result := False;
  if FindFirst(ProgramFilesNative + '\dotnet\shared\Microsoft.WindowsDesktop.App\8.*', Rec) then
  begin
    try
      repeat
        if (Rec.Attributes and FILE_ATTRIBUTE_DIRECTORY) <> 0 then
        begin
          Result := True;
          Exit;
        end;
      until not FindNext(Rec);
    finally
      FindClose(Rec);
    end;
  end;
end;

function RuntimeUrl: String;
begin
  if IsArm64 then
    Result := RuntimeUrlArm64
  else
    Result := RuntimeUrlX64;
end;

function OnDownloadProgress(const Url, FileName: String; const Progress, ProgressMax: Int64): Boolean;
begin
  if ProgressMax > 0 then
    WizardForm.PreparingLabel.Caption := ExpandConstant('{cm:RuntimeDownloading}') + ' ' +
      IntToStr((Progress * 100) div ProgressMax) + ' %';
  Result := True;
end;

// Ist die Anwendung schon installiert? Dann ist dies eine Aktualisierung. Die Antwort wird
// gemerkt: Inno legt seinen eigenen Uninstall-Schlüssel während der Installation an, danach
// sähe auch eine Erstinstallation wie eine Aktualisierung aus.
function IsUpgrade: Boolean;
var
  Key, Dummy: String;
begin
  if UpgradeChecked then
  begin
    Result := UpgradeDetected;
    Exit;
  end;

  Key := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\' + AppGuid + '_is1';
  UpgradeDetected := RegQueryStringValue(HKCU, Key, 'UninstallString', Dummy) or
                     RegQueryStringValue(HKLM, Key, 'UninstallString', Dummy);
  UpgradeChecked := True;
  Result := UpgradeDetected;
end;

// Bei einer Aktualisierung stehen alle Antworten schon fest: Inno übernimmt Zielordner,
// Startmenügruppe und Zusatzaufgaben aus der vorigen Installation. Die Seiten noch einmal
// vorzulegen kostet nur Klicks. Übrig bleiben Fortschritt und Abschluss.
function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := IsUpgrade and
    ((PageID = wpLicense) or (PageID = wpSelectDir) or (PageID = wpSelectProgramGroup) or
     (PageID = wpSelectTasks) or (PageID = wpReady));
end;

// Die Laufzeit wird hier geholt, nicht auf einer eigenen Seite: PrepareToInstall läuft auch
// dann, wenn die Seiten übersprungen werden oder das Setup still installiert.
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
  Installer: String;
begin
  Result := '';
  if DesktopRuntimeInstalled then
    Exit;

  Installer := ExpandConstant('{tmp}\windowsdesktop-runtime.exe');
  try
    DownloadTemporaryFile(RuntimeUrl, 'windowsdesktop-runtime.exe', '', @OnDownloadProgress);
  except
    Result := ExpandConstant('{cm:RuntimeFailed}');
    Exit;
  end;

  // Die Laufzeit installiert maschinenweit und verlangt Administratorrechte - "runas" löst
  // die Nachfrage von Windows aus. "/passive" zeigt einen Fortschritt, fragt aber nichts.
  WizardForm.PreparingLabel.Caption := ExpandConstant('{cm:RuntimeInstalling}');
  if not ShellExec('runas', Installer, '/install /passive /norestart', '', SW_SHOW,
                   ewWaitUntilTerminated, ResultCode) then
  begin
    Result := ExpandConstant('{cm:RuntimeFailed}');
    Exit;
  end;

  if ResultCode = RuntimeRestartNeeded then
    NeedsRestart := True
  else if (ResultCode <> RuntimeOk) and (ResultCode <> RuntimeAlreadyThere) then
    Result := ExpandConstant('{cm:RuntimeFailed}');
end;

// Wurde das Setup von der automatischen Aktualisierung gestartet? Dann soll Audio Mirror
// hinterher wieder laufen, ohne dass jemand darauf klicken muss.
function RelaunchRequested: Boolean;
begin
  Result := CompareText(ExpandConstant('{param:relaunch|no}'), 'yes') = 0;
end;

// Lief es zuvor still im Infobereich, startet es auch wieder still.
function RelaunchArgs(Value: String): String;
begin
  if CompareText(ExpandConstant('{param:relaunchmin|no}'), 'yes') = 0 then
    Result := '--minimized'
  else
    Result := '';
end;

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
      Question := ExpandConstant('{cm:RemoveSettings}');
      if SuppressibleMsgBox(Question,
                            mbConfirmation, MB_YESNO or MB_DEFBUTTON2, IDNO) = IDYES then
      begin
        DelTree(SettingsDir, True, True, True);
      end;
    end;
  end;
end;
