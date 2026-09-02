using System.Globalization;

namespace AudioMirror;

/// <summary>
/// Oberflächentexte in Englisch und Deutsch.
///
/// Englisch ist die Voreinstellung; Deutsch nur, wenn Windows selbst auf Deutsch eingestellt ist.
/// Maßgeblich ist die Anzeigesprache (<see cref="CultureInfo.CurrentUICulture"/>), nicht das
/// Regionsformat - jemand mit deutschem Datumsformat, aber englischem Windows, erwartet Englisch.
/// </summary>
internal static class Strings
{
    private static bool? forced;

    /// <summary>Wahr, wenn die Oberfläche auf Deutsch erscheint.</summary>
    public static bool German => forced ?? DetectGerman();

    /// <summary>
    /// Legt die Sprache fest: "de" oder "en" erzwingen, alles andere folgt Windows.
    /// Muss vor dem ersten Textzugriff aufgerufen werden.
    /// </summary>
    public static void Configure(string? language)
    {
        forced = language?.ToLowerInvariant() switch
        {
            "de" => true,
            "en" => false,
            _ => null,
        };
    }

    private static bool DetectGerman()
    {
        try
        {
            return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
                .Equals("de", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static string T(string english, string german) => German ? german : english;

    // Fenster und Kopfbereich
    public static string AppTitle => "Audio Mirror";
    public static string Source => T("Source", "Quelle");
    public static string TargetDevices => T("Output devices", "Zielgeräte");
    public static string Connected => T("Connected", "Verbunden");
    public static string Disconnected => T("Disconnected", "Getrennt");
    public static string Close => T("Close", "Schließen");
    public static string Ready => T("Ready.", "Bereit.");

    public static string WindowsDefaultDevice(string current) =>
        T($"Windows default device ({current})", $"Windows-Standardgerät ({current})");

    public static string NoDeviceAvailable => T("none available", "keines vorhanden");

    public static string LastChosenUnavailable =>
        T("Last selected device (unavailable)", "Zuletzt gewähltes Gerät (nicht verfügbar)");

    // Puffer
    public static string BufferLabel => T("Buffer / latency:", "Puffer / Latenz:");
    public static string Milliseconds => "ms";

    public static string BufferTip => T(
        "Smaller = less latency, larger = more headroom against dropouts."
        + Environment.NewLine + "Default is 30 ms. Increase it if you hear crackling.",
        "Kleiner = weniger Latenz, größer = mehr Reserve gegen Aussetzer."
        + Environment.NewLine + "Voreinstellung 30 ms. Bei Knacksern erhöhen.");

    // Hotkey
    public static string ToggleAllLabel => T("Toggle everything:", "Alles umschalten:");
    public static string HotkeyEnabled => T("Hotkey enabled", "Hotkey aktiviert");
    public static string PressCombination => T("Press a combination …", "Kombination drücken …");
    public static string NoHotkey => T("none", "keine");

    public static string HotkeyTaken =>
        T("That combination is already used by another program.",
          "Die Tastenkombination ist bereits von einem anderen Programm belegt.");

    public static string HotkeyAssignedTo(string combination, string owner) =>
        T($"\"{combination}\" is already assigned to {owner}.",
          $"„{combination}“ ist bereits für {owner} vergeben.");

    public static string MirroringOff => T("Mirroring off", "Spiegelung aus");
    public static string MirroringOn => T("Mirroring on", "Spiegelung an");

    public static string MutedDevices(int count) =>
        T($"{count} device(s) muted. Press again to restore them.",
          $"{count} Gerät(e) stummgeschaltet. Erneut drücken stellt sie wieder her.");

    public static string RestoredDevices(int restored) =>
        T($"{restored} device(s) restored.", $"{restored} Gerät(e) wiederhergestellt.");

    public static string RestoredDevicesPartly(int restored, int skipped) =>
        T($"{restored} device(s) restored, {skipped} no longer available.",
          $"{restored} Gerät(e) wiederhergestellt, {skipped} nicht mehr verfügbar.");

    public static string NoRememberedState =>
        T("Nothing remembered yet - tick a device first.",
          "Kein gemerkter Zustand vorhanden – erst etwas anhaken.");

    // Autostart
    public static string StartWithWindows =>
        T("Start with Windows (in the notification area)",
          "Mit Windows starten (startet im Infobereich)");

    // Infobereich
    public static string OpenWindow => T("Open window", "Fenster öffnen");
    public static string Exit => T("Exit", "Beenden");
    public static string NoOutputDevices => T("No output devices found", "Keine Ausgabegeräte gefunden");
    public static string SourceSuffix => T("  (source)", "  (Quelle)");
    public static string SourceShort => T("source", "Quelle");
    public static string NoAppPlaying => T("No application is playing sound.", "Zurzeit gibt keine Anwendung Ton aus.");

    public static string TrayNoMirroring => T("Audio Mirror – not mirroring", "Audio Mirror – keine Spiegelung");

    public static string TrayMirroring(int count) =>
        T($"Audio Mirror – mirroring to {count} device(s)",
          $"Audio Mirror – spiegelt auf {count} Gerät(e)");

    public static string StillRunningTitle => T("Audio Mirror keeps running", "Audio Mirror läuft weiter");

    public static string StillRunningBody =>
        T("The window is only hidden, mirroring continues in the background. "
          + "Double-click the icon to bring it back; \"Exit\" closes the program.",
          "Das Fenster ist nur ausgeblendet, die Spiegelung läuft im Hintergrund weiter. "
          + "Doppelklick auf das Symbol holt es zurück, „Beenden“ schließt das Programm.");

    // Statuszeile
    public static string NothingTicked =>
        T("No output device ticked - tick one to start mirroring right away.",
          "Kein Zielgerät angehakt – Haken setzen, um sofort dorthin zu spiegeln.");

    public static string MirroringOnDevices(int running) =>
        T($"Mirroring to {running} device(s).", $"Spiegelung läuft auf {running} Gerät(en).");

    public static string StillWaiting(int failed) =>
        T($" {failed} still waiting.", $" {failed} wartet noch.");

    public static string WaitingFor(string reason, int selected) =>
        T($"Waiting for {reason} ({selected} device(s) ticked).",
          $"Wartet auf {reason} ({selected} Gerät(e) angehakt).");

    public static string DeviceOrApp => T("device or application", "Gerät bzw. Anwendung");

    public static string SourceUnavailable =>
        T("The selected source device is unavailable - mirroring is paused until it returns "
          + "or another source is chosen.",
          "Das gewählte Quellgerät ist nicht verfügbar – die Spiegelung pausiert, "
          + "bis es zurück ist oder eine andere Quelle gewählt wird.");

    public static string RetryRunning => T(" – retrying", " – neuer Versuch läuft");

    public static string NotConnected =>
        T("not connected – setting is kept", "nicht verbunden – Einstellung bleibt gespeichert");

    public static string NoAppSelected => T("no application selected", "keine Anwendung ausgewählt");

    public static string RunningWholeSound(double ms) =>
        T($"running – full device sound, about {ms:0} ms",
          $"läuft – kompletter Ton, ca. {ms:0} ms");

    public static string RunningApps(int count, double ms) =>
        T($"running – {count} application(s), about {ms:0} ms",
          $"läuft – {count} Anwendung(en), ca. {ms:0} ms");

    // Fehlermeldungen
    public static string UnexpectedError(string message) =>
        T($"Unexpected error:\r\n\r\n{message}\r\n\r\nMirroring has stopped. Please restart the program.",
          $"Unerwarteter Fehler:\r\n\r\n{message}\r\n\r\nDie Spiegelung wurde gestoppt. Bitte das Programm neu starten.");

    public static string Unknown => T("Unknown", "Unbekannt");
    public static string UnknownDevice => T("Unknown device", "Unbekanntes Gerät");
    public static string UnknownError => T("Unknown error.", "Unbekannter Fehler.");

    public static string DeviceExclusive =>
        T("Device is used exclusively by another application.",
          "Gerät ist exklusiv von einer anderen Anwendung belegt.");

    public static string DeviceGone =>
        T("Device is no longer available (disconnected or disabled).",
          "Gerät ist nicht mehr verfügbar (getrennt oder deaktiviert).");

    public static string FormatUnsupported =>
        T("This device does not support the audio format.",
          "Audioformat wird von diesem Gerät nicht unterstützt.");

    public static string DeviceInUse =>
        T("Device is already in use by this application.",
          "Gerät wird bereits von dieser Anwendung verwendet.");

    public static string AudioServiceDown =>
        T("The Windows audio service is not running.", "Windows-Audiodienst läuft nicht.");

    public static string AccessDenied =>
        T("Access to the device was denied.", "Zugriff auf das Gerät verweigert.");

    public static string PlaybackStopped =>
        T("Playback was stopped by the system.", "Wiedergabe wurde vom System beendet.");

    public static string AppNotRunning => T("Application is not running.", "Anwendung läuft nicht.");

    public static string AppSilent =>
        T("Application is not playing any sound right now.", "Anwendung gibt gerade keinen Ton aus.");

    public static string CaptureFailed(string message) =>
        T("Sound cannot be captured: " + message, "Ton nicht abgreifbar: " + message);

    public static string NoSourceDevice => T("No source device available.", "Kein Quellgerät verfügbar.");

    public static string SourceCaptureFailed(string message) =>
        T("Could not start capturing the source device: " + message,
          "Die Aufnahme am Quellgerät konnte nicht gestartet werden: " + message);

    public static string SourceEnded =>
        T("The audio source was stopped.", "Die Tonquelle wurde beendet.");

    // Autostart-Fehler
    public static string ExecutablePathUnknown =>
        T("The program path could not be determined.", "Der Programmpfad konnte nicht ermittelt werden.");

    public static string RunKeyUnavailable =>
        T("The startup key in the registry is not accessible.",
          "Der Autostart-Schlüssel in der Registry ist nicht zugänglich.");

    public static string AutostartDenied =>
        T("Not allowed to change the startup entry (possibly blocked by a group policy).",
          "Keine Berechtigung, den Autostart zu ändern (evtl. durch eine Gruppenrichtlinie gesperrt).");

    public static string AutostartFailed(string message) =>
        T("Could not change the startup entry: " + message,
          "Autostart konnte nicht geändert werden: " + message);

    // Einstellungen
    public static string TabDevices => T("Devices", "Geräte");
    public static string TabSettings => T("Settings", "Einstellungen");
    public static string BasicSettings => T("Basic settings", "Allgemein");
    public static string AudioSettings => T("Audio", "Ton");
    public static string UpdateSettings => T("Updates", "Aktualisierungen");
    public static string LanguageLabel => T("Language", "Sprache");
    public static string LanguageAutomatic => T("Automatic (Windows)", "Automatisch (Windows)");
    public static string DoubleClickLabel => T("Double-click action", "Doppelklick");
    public static string ActionOpenWindow => T("Open window", "Fenster öffnen");
    public static string ActionToggle => T("Toggle mirroring", "Spiegelung umschalten");
    public static string ActionNothing => T("Do nothing", "Nichts tun");
    public static string RestartForLanguage =>
        T("The language changes after restarting the program.",
          "Die Sprache wird nach einem Neustart des Programms übernommen.");

    public static string UpdateAutomatic => T("Install updates automatically", "Aktualisierungen automatisch installieren");
    public static string UpdateNotify => T("Notify me when updates are available", "Nur benachrichtigen");
    public static string UpdateNever => T("Never check for updates", "Nie nach Aktualisierungen suchen");
    public static string IncludeBeta => T("Include beta versions", "Vorabfassungen einbeziehen");
    public static string CheckNow => T("Check now", "Jetzt suchen");
    public static string CheckingUpdates => T("Checking …", "Suche läuft …");
    public static string UpToDate => T("Audio Mirror is up to date.", "Audio Mirror ist aktuell.");

    public static string UpdateAvailable(string version) =>
        T($"Version {version} is available.", $"Fassung {version} ist verfügbar.");

    public static string UpdateDownloading(string version) =>
        T($"Downloading version {version} …", $"Fassung {version} wird geladen …");

    public static string UpdateDownloadFailed =>
        T("The download failed. Opening the release page instead.",
          "Der Download ist fehlgeschlagen. Stattdessen wird die Release-Seite geöffnet.");

    public static string CurrentVersion(string version) =>
        T($"Installed version: {version}", $"Installierte Fassung: {version}");

    // Tastennamen für die Hotkey-Anzeige
    public static string KeyControl => T("Ctrl", "Strg");
    public static string KeyShift => T("Shift", "Umschalt");
    public static string KeyAlt => "Alt";
    public static string KeySpace => T("Space", "Leertaste");
    public static string KeyPageUp => T("Page up", "Bild auf");
    public static string KeyPageDown => T("Page down", "Bild ab");
    public static string KeyNumPad => T("Num ", "Num ");
}
