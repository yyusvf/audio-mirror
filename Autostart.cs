using Microsoft.Win32;

namespace AudioMirror;

/// <summary>
/// Eintrag unter HKEY_CURRENT_USER\...\Run, damit das Programm mit Windows startet.
///
/// Bewusst nur im Benutzerzweig: keine Administratorrechte nötig, keine Auswirkung auf andere
/// Benutzerkonten. Standardmäßig ausgeschaltet - der Eintrag entsteht erst, wenn er in der
/// Oberfläche aktiviert wird.
///
/// Zusätzlich wird der Zustand unter <c>StartupApproved\Run</c> gepflegt. Das ist der Ort, den
/// der Task-Manager und die Windows-Einstellungen unter „Autostart-Apps“ auswerten: dort kann
/// ein Eintrag abgeschaltet werden, ohne dass der Run-Eintrag verschwindet. Ohne diese Pflege
/// würden Programm und Task-Manager unterschiedliche Aussagen treffen.
/// </summary>
internal static class Autostart
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ApprovedKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    private const string ValueName = "AudioMirror";

    /// <summary>Erstes Byte des StartupApproved-Werts: 2 = aktiviert, 3 = vom Nutzer abgeschaltet.</summary>
    private const byte ApprovedEnabled = 0x02;

    private const byte ApprovedDisabled = 0x03;

    /// <summary>Beim Autostart mitgegebenes Argument - das Fenster bleibt dann im Infobereich.</summary>
    public const string MinimizedArgument = "--minimized";

    /// <summary>Pfad der laufenden .exe, oder <c>null</c> wenn nicht ermittelbar.</summary>
    public static string? ExecutablePath
    {
        get
        {
            string? path = Environment.ProcessPath;
            return string.IsNullOrEmpty(path) ? null : path;
        }
    }

    public static bool IsSupported => ExecutablePath != null;

    /// <summary>
    /// Autostart aktiv? Nur wenn der Run-Eintrag existiert <em>und</em> er nicht über den
    /// Task-Manager abgeschaltet wurde.
    /// </summary>
    public static bool IsEnabled()
    {
        try
        {
            using RegistryKey? run = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            if (run?.GetValue(ValueName) is not string value || value.Length == 0)
            {
                return false;
            }
            return !IsDisabledByWindows();
        }
        catch
        {
            // Gesperrte Registry (Gruppenrichtlinie) - dann gilt der Autostart als aus.
            return false;
        }
    }

    /// <summary>Ob der Eintrag im Task-Manager bzw. in den Windows-Einstellungen abgeschaltet ist.</summary>
    private static bool IsDisabledByWindows()
    {
        try
        {
            using RegistryKey? approved = Registry.CurrentUser.OpenSubKey(ApprovedKeyPath, writable: false);
            return approved?.GetValue(ValueName) is byte[] { Length: > 0 } state
                   && state[0] == ApprovedDisabled;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Setzt oder entfernt den Eintrag. Gibt bei Misserfolg eine lesbare Meldung zurück.</summary>
    public static string? TrySetEnabled(bool enabled)
    {
        string? executable = ExecutablePath;
        if (enabled && executable == null)
        {
            return Strings.ExecutablePathUnknown;
        }

        try
        {
            using RegistryKey? run = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            if (run == null)
            {
                return Strings.RunKeyUnavailable;
            }

            if (enabled)
            {
                run.SetValue(ValueName, $"\"{executable}\" {MinimizedArgument}", RegistryValueKind.String);

                // Ausdrücklich als aktiviert eintragen. Wurde der Eintrag vorher im Task-Manager
                // abgeschaltet, bliebe er sonst wirkungslos, obwohl das Häkchen gesetzt ist.
                SetApproved(ApprovedEnabled);
            }
            else
            {
                if (run.GetValue(ValueName) != null)
                {
                    run.DeleteValue(ValueName, throwOnMissingValue: false);
                }

                // Auch die Zustandsmarke entfernen, damit kein Rest zurückbleibt.
                RemoveApproved();
            }

            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return Strings.AutostartDenied;
        }
        catch (Exception ex)
        {
            return Strings.AutostartFailed(ex.Message);
        }
    }

    private static void SetApproved(byte state)
    {
        try
        {
            using RegistryKey approved = Registry.CurrentUser.CreateSubKey(ApprovedKeyPath, writable: true);
            // Zwölf Byte: Zustand, dann der Zeitpunkt der Abschaltung (bei "aktiviert" null).
            var value = new byte[12];
            value[0] = state;
            approved.SetValue(ValueName, value, RegistryValueKind.Binary);
        }
        catch
        {
            // Ohne diese Marke funktioniert der Autostart trotzdem - nur die Anzeige im
            // Task-Manager kann dann von der eigenen abweichen.
        }
    }

    private static void RemoveApproved()
    {
        try
        {
            using RegistryKey? approved = Registry.CurrentUser.OpenSubKey(ApprovedKeyPath, writable: true);
            if (approved?.GetValue(ValueName) != null)
            {
                approved.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }
        catch
        {
            // s. o.
        }
    }
}
