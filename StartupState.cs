namespace AudioMirror;

/// <summary>
/// Unterscheidet einen Start von Hand von einem, den Windows selbst ausgelöst hat.
///
/// Der Autostart-Eintrag übergibt dafür ein Argument. Windows holt Programme nach der Anmeldung
/// aber teils auch von sich aus wieder hoch (Einstellung „Apps nach der Anmeldung neu starten“) -
/// und übergibt dabei nichts, sieht also aus wie ein Doppelklick.
///
/// Deshalb hinterlässt das Programm eine Markierung, wenn Windows es beim Abmelden oder
/// Herunterfahren beendet. Ist sie beim nächsten Start vorhanden, war das die Wiederherstellung,
/// und das Fenster bleibt zu. Die Markierung wird dabei sofort verbraucht, damit jeder weitere
/// Start wieder ganz normal ein Fenster öffnet.
/// </summary>
internal static class StartupState
{
    private static string FlagPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AudioMirror",
        "restored.flag");

    /// <summary>Merkt vor, dass Windows das Programm gerade beendet und später wiederherstellen könnte.</summary>
    public static void MarkStoppedByWindows()
    {
        try
        {
            string path = FlagPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, DateTime.UtcNow.ToString("o"));
        }
        catch
        {
            // Ohne Markierung öffnet sich beim nächsten Mal eben das Fenster - unkritisch.
        }
    }

    /// <summary>
    /// Liefert true, wenn dieser Start auf ein von Windows ausgelöstes Beenden folgt, und
    /// entfernt die Markierung.
    /// </summary>
    public static bool ConsumeStoppedByWindows()
    {
        try
        {
            string path = FlagPath;
            if (!File.Exists(path))
            {
                return false;
            }
            File.Delete(path);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
