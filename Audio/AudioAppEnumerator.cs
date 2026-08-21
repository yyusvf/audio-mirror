using System.Diagnostics;
using NAudio.CoreAudioApi;

namespace AudioMirror.Audio;

/// <summary>Eine Anwendung, deren Ton einzeln gespiegelt werden kann.</summary>
/// <param name="Key">Stabiler Schlüssel (Name der ausführbaren Datei, klein geschrieben).</param>
/// <param name="Name">Anzeigename, z. B. aus der Dateibeschreibung.</param>
/// <param name="Running">Ob die Anwendung gerade läuft.</param>
internal sealed record AudioAppInfo(string Key, string Name, bool Running);

/// <summary>
/// Ermittelt die Anwendungen, die gerade Ton ausgeben - dieselbe Grundlage, die auch der
/// Windows-Lautstärkemixer verwendet (Audiositzungen der Wiedergabegeräte).
///
/// Als Schlüssel dient bewusst der Name der ausführbaren Datei und nicht die Prozess-ID: die
/// ändert sich bei jedem Neustart der Anwendung, der Dateiname bleibt gleich. Nur so lässt
/// sich die Auswahl dauerhaft speichern und später wiederfinden.
/// </summary>
internal static class AudioAppEnumerator
{
    /// <summary>
    /// Anwendungen mit Ton auf einem bestimmten Wiedergabegerät - also genau die Liste, die der
    /// Windows-Lautstärkemixer für dieses Gerät zeigt. Ohne Gerät wird über alle aktiven
    /// Geräte gesammelt.
    /// </summary>
    public static IReadOnlyList<AudioAppInfo> List(MMDeviceEnumerator enumerator, string? deviceId = null)
    {
        var found = new Dictionary<string, AudioAppInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (MMDevice device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
        {
            try
            {
                if (deviceId != null && !string.Equals(device.ID, deviceId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                SessionCollection sessions = device.AudioSessionManager.Sessions;
                for (int i = 0; i < sessions.Count; i++)
                {
                    TryAddSession(sessions[i], found);
                }
            }
            catch
            {
                // Ein Gerät, das gerade verschwindet, darf die Liste nicht verhindern.
            }
            finally
            {
                device.Dispose();
            }
        }

        return found.Values.OrderBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private static void TryAddSession(AudioSessionControl session, Dictionary<string, AudioAppInfo> found)
    {
        try
        {
            if (session.IsSystemSoundsSession)
            {
                return;
            }

            uint processId = session.GetProcessID;
            if (processId == 0)
            {
                return;
            }

            // Das eigene Programm gehört nicht in die Liste: seine Sitzungen sind die
            // gespiegelte Ausgabe selbst. Würde man sie abgreifen, liefe der Ton im Kreis
            // und schaukelte sich auf. Über die Prozess-ID statt über den Namen geprüft,
            // damit es auch nach einem Umbenennen der Datei greift.
            if (processId == (uint)Environment.ProcessId)
            {
                return;
            }

            using Process process = Process.GetProcessById((int)processId);
            string key = process.ProcessName.ToLowerInvariant();
            if (key.Length == 0 || found.ContainsKey(key))
            {
                return;
            }

            found[key] = new AudioAppInfo(key, DescribeProcess(process), true);
        }
        catch
        {
            // Beendete oder geschützte Prozesse werden übersprungen.
        }
    }

    /// <summary>Sucht die laufende Prozess-ID zu einem gespeicherten Schlüssel.</summary>
    public static int? ResolveProcessId(string key)
    {
        Process[] candidates;
        try
        {
            candidates = Process.GetProcessesByName(key);
        }
        catch
        {
            return null;
        }

        try
        {
            if (candidates.Length == 0)
            {
                return null;
            }

            // Bevorzugt der Prozess mit Fenster: bei mehrprozessigen Anwendungen (Browser,
            // Chat-Programme) ist das der Hauptprozess. Da die Aufnahme den ganzen
            // Prozessbaum erfasst, sind dessen Hilfsprozesse damit eingeschlossen.
            Process? best = candidates.FirstOrDefault(p => SafeHasWindow(p));
            best ??= candidates.OrderBy(SafeStartTime).First();
            return best.Id;
        }
        catch
        {
            return null;
        }
        finally
        {
            foreach (Process candidate in candidates)
            {
                candidate.Dispose();
            }
        }
    }

    /// <summary>Anzeigename für einen Schlüssel, auch wenn die Anwendung gerade nicht läuft.</summary>
    public static string DescribeKey(string key)
    {
        try
        {
            using Process? process = Process.GetProcessesByName(key).FirstOrDefault();
            if (process != null)
            {
                return DescribeProcess(process);
            }
        }
        catch
        {
            // Fällt unten auf den Schlüssel zurück.
        }

        return key.Length > 0 ? char.ToUpperInvariant(key[0]) + key[1..] : key;
    }

    private static string DescribeProcess(Process process)
    {
        try
        {
            string? description = process.MainModule?.FileVersionInfo.FileDescription;
            if (!string.IsNullOrWhiteSpace(description))
            {
                return description.Trim();
            }
        }
        catch
        {
            // Geschützte Prozesse lassen ihr Hauptmodul nicht auslesen.
        }

        string name = process.ProcessName;
        return name.Length > 0 ? char.ToUpperInvariant(name[0]) + name[1..] : name;
    }

    private static bool SafeHasWindow(Process process)
    {
        try
        {
            return process.MainWindowHandle != IntPtr.Zero;
        }
        catch
        {
            return false;
        }
    }

    private static DateTime SafeStartTime(Process process)
    {
        try
        {
            return process.StartTime;
        }
        catch
        {
            return DateTime.MaxValue;
        }
    }
}
