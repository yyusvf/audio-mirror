using System.Text.Json;
using System.Text.Json.Serialization;

namespace AudioMirror;

/// <summary>Ein/Aus und Lautstärke einer einzelnen Anwendung auf einem bestimmten Zielgerät.</summary>
internal sealed class AppMixSetting
{
    /// <summary>Neu auftauchende Anwendungen werden mitgespiegelt, bis sie abgeschaltet werden.</summary>
    public bool Enabled { get; set; } = true;

    public float Volume { get; set; } = 1f;
}

/// <summary>Was ein Doppelklick auf das Tray-Symbol tut.</summary>
internal enum TrayAction
{
    OpenWindow = 0,
    ToggleMirroring = 1,
    Nothing = 2,
}

internal sealed class DeviceSetting
{
    public bool Enabled { get; set; }

    /// <summary>Haupt-Lautstärke des Geräts; wirkt multiplikativ auf alle Anwendungen.</summary>
    public float Volume { get; set; } = 1f;

    /// <summary>Ob der Anwendungsbereich unter dem Gerät aufgeklappt ist.</summary>
    public bool Expanded { get; set; }

    /// <summary>
    /// Zuletzt gesehener Anzeigename und Bauform. Nur dafür da, ein Gerät auch dann noch im
    /// Abschnitt „Getrennt“ benennen zu können, wenn Windows es gar nicht mehr aufzählt.
    /// </summary>
    public string? Name { get; set; }

    public int Kind { get; set; }

    /// <summary>Zuletzt gesehener Symbolverweis von Windows.</summary>
    public string? IconPath { get; set; }

    /// <summary>
    /// Einstellungen je Anwendung, geschlüsselt über den Namen der ausführbaren Datei.
    /// Bewusst nicht über die Prozess-ID: die ändert sich bei jedem Start der Anwendung,
    /// der Dateiname bleibt gleich und findet die Einstellung darum wieder.
    /// </summary>
    public Dictionary<string, AppMixSetting> Apps { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Merkt sich Geräteauswahl, Lautstärken und Anwendungsmischung zwischen Programmstarts.
/// Liegt unter %APPDATA%\AudioMirror\settings.json.
/// </summary>
internal sealed class AppSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Einstellungen je Gerät, geschlüsselt über die stabile WASAPI-Endpoint-ID
    /// (nicht über den Anzeigenamen).
    /// </summary>
    public Dictionary<string, DeviceSetting> Devices { get; set; } = new();

    public int BufferMs { get; set; } = 30;

    /// <summary>
    /// Fest gewähltes Quellgerät. <c>null</c> bedeutet: dem Windows-Standardgerät folgen.
    /// </summary>
    public string? SourceDeviceId { get; set; }

    /// <summary>Tastenkombination des Gesamt-Umschalters (Wert von <c>Keys</c>).</summary>
    public int HotkeyToggleAll { get; set; }

    public bool HotkeyToggleAllEnabled { get; set; } = true;

    /// <summary>
    /// Sprache der Oberfläche: leer bzw. "auto" folgt der Windows-Anzeigesprache,
    /// sonst "en" oder "de".
    /// </summary>
    public string? Language { get; set; }

    /// <summary>Was ein Doppelklick auf das Symbol im Infobereich auslöst.</summary>
    public TrayAction DoubleClickAction { get; set; } = TrayAction.OpenWindow;

    /// <summary>Wie mit neuen Fassungen verfahren wird.</summary>
    public UpdateMode Updates { get; set; } = UpdateMode.Notify;

    /// <summary>Ob auch Vorabfassungen berücksichtigt werden.</summary>
    public bool IncludeBeta { get; set; }

    /// <summary>Wann zuletzt nach einer neuen Fassung gesehen wurde.</summary>
    public DateTime LastUpdateCheckUtc { get; set; } = DateTime.MinValue;

    /// <summary>
    /// Der Zustand, der beim Ausschalten per Hotkey galt - je Gerät die Auswahl und die
    /// Anwendungsmischung. Wird beim Wiedereinschalten zurückgespielt und überdauert bewusst
    /// auch einen Programmneustart.
    /// </summary>
    public Dictionary<string, DeviceSetting>? HotkeySnapshot { get; set; }

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AudioMirror",
        "settings.json");

    public static AppSettings Load()
    {
        try
        {
            string path = FilePath;
            if (!File.Exists(path))
            {
                return new AppSettings();
            }
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), JsonOptions) ?? new AppSettings();
        }
        catch
        {
            // Beschädigte Einstellungen dürfen den Start nicht verhindern.
            return new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            string path = FilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch
        {
            // Nicht schreibbares Profil darf das Beenden nicht blockieren.
        }
    }

    public DeviceSetting For(string deviceId)
    {
        if (!Devices.TryGetValue(deviceId, out DeviceSetting? setting))
        {
            setting = new DeviceSetting();
            Devices[deviceId] = setting;
        }
        // Aus älteren Dateien kann das Wörterbuch ohne Groß-/Kleinschreibungsregel kommen.
        setting.Apps ??= new Dictionary<string, AppMixSetting>(StringComparer.OrdinalIgnoreCase);
        return setting;
    }

    /// <summary>
    /// Liest den gespeicherten Zustand einer Anwendung, ohne einen Eintrag anzulegen.
    /// Unbekannte Anwendungen werden mitgespiegelt, bis sie abgeschaltet werden.
    /// </summary>
    public (bool Enabled, float Volume) Lookup(string deviceId, string appKey)
    {
        if (Devices.TryGetValue(deviceId, out DeviceSetting? device)
            && device.Apps != null
            && device.Apps.TryGetValue(appKey, out AppMixSetting? app))
        {
            return (app.Enabled, app.Volume);
        }
        return (true, 1f);
    }

    public AppMixSetting For(string deviceId, string appKey)
    {
        DeviceSetting device = For(deviceId);
        if (!device.Apps.TryGetValue(appKey, out AppMixSetting? app))
        {
            app = new AppMixSetting();
            device.Apps[appKey] = app;
        }
        return app;
    }
}
