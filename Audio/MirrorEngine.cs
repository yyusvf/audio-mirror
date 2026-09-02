using AudioMirror;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace AudioMirror.Audio;

/// <summary>Bauform eines Ausgabegeräts - bestimmt das Symbol in der Liste.</summary>
internal enum AudioDeviceKind
{
    Other,
    Speakers,
    Headphones,
    Display,
    Digital,
}

/// <param name="Connected">
/// Ob das Gerät gerade angeschlossen und benutzbar ist. Getrennte Geräte bleiben in der Liste
/// sichtbar, damit ihre gespeicherte Einstellung erkennbar bleibt.
/// </param>
/// <param name="IconPath">
/// Der von Windows hinterlegte Symbolverweis (z. B. <c>%windir%\system32\mmres.dll,-3010</c>),
/// damit die Liste dieselben Symbole zeigt wie die Windows-Soundeinstellungen.
/// </param>
internal sealed record AudioDeviceInfo(
    string Id, string Name, bool IsDefault, bool Connected, AudioDeviceKind Kind, string? IconPath);

/// <summary>Eine Anwendung, die auf ein Zielgerät gemischt wird.</summary>
internal sealed record MirrorAppTarget(string AppKey, float Volume);

/// <summary>
/// Ein Zielgerät mit seiner Haupt-Lautstärke und den darauf gemischten Anwendungen.
/// </summary>
/// <param name="WholeDevice">
/// True, solange der Nutzer nichts abgewählt oder leiser gestellt hat: dann wird der komplette
/// Geräteton abgegriffen (einschließlich Systemklängen), nicht Anwendung für Anwendung.
/// <paramref name="Apps"/> wird in diesem Fall nicht ausgewertet.
/// </param>
internal sealed record MirrorTarget(
    string DeviceId, float MasterVolume, bool WholeDevice, IReadOnlyList<MirrorAppTarget> Apps);

/// <summary>
/// Kern-Engine: greift den Ton einzelner Anwendungen ab und verteilt ihn auf beliebig viele
/// Wiedergabegeräte.
///
/// Je Anwendung läuft genau eine Aufnahme (Prozess-Loopback), unabhängig davon, wie viele
/// Zielgeräte sie hören wollen. Jedes Zielgerät mischt die für es freigeschalteten Anwendungen
/// zusammen und regelt sie einzeln sowie über eine Haupt-Lautstärke.
///
/// Die Zielmenge wird deklarativ gesetzt (<see cref="SetTargets"/>) und laufend abgeglichen,
/// sodass einzelne Geräte und Anwendungen sofort starten und stoppen, ohne die übrigen zu
/// unterbrechen.
/// </summary>
internal sealed class MirrorEngine : IDisposable
{
    /// <summary>Schlüssel der Gerätequelle (kompletter Ton des Quellgeräts).</summary>
    public const string WholeDeviceKey = "";

    private const int TimerIntervalMs = 100;
    private const int MaintenanceEveryTicks = 20;

    private readonly MMDeviceEnumerator enumerator = new();
    private readonly DeviceNotificationClient notifications = new();
    private readonly System.Timers.Timer timer = new(TimerIntervalMs) { AutoReset = true };
    private readonly object gate = new();
    private readonly Dictionary<string, CaptureEntry> captures = new(StringComparer.OrdinalIgnoreCase);

    private DeviceOutput[] outputs = [];
    private MirrorTarget[] activeTargets = [];
    private int activeBufferMs = 30;
    private int runningBufferMs = 30;
    private int tickCounter;
    private int maintenanceGuard;
    private bool disposed;

    public MirrorEngine()
    {
        notifications.DeviceListChanged += () => DeviceListChanged?.Invoke();
        notifications.DefaultRenderDeviceChanged += () => DefaultDeviceChanged?.Invoke();
        enumerator.RegisterEndpointNotificationCallback(notifications);
        timer.Elapsed += (_, _) => OnTimer();
    }

    public event Action? DeviceListChanged;

    public event Action? DefaultDeviceChanged;

    public bool IsRunning { get; private set; }

    /// <summary>
    /// Fest gewähltes Quellgerät, oder <c>null</c> um dem Windows-Standardgerät zu folgen.
    /// </summary>
    public string? FixedSourceDeviceId { get; set; }

    /// <summary>Das Gerät, das gerade als Quelle gilt (fest gewählt oder aktueller Standard).</summary>
    public string? SourceDeviceId { get; private set; }

    public string? SourceDeviceName { get; private set; }

    /// <summary>
    /// Gesetzt, wenn ein fest gewähltes Quellgerät nicht verfügbar ist. Die Spiegelung pausiert
    /// dann bewusst, statt still auf ein anderes Gerät auszuweichen.
    /// </summary>
    public bool SourceUnavailable { get; private set; }

    public IReadOnlyList<DeviceOutput> Outputs => outputs;

    /// <summary>
    /// Anwendungen, die gerade auf dem Quellgerät Ton ausgeben - dieselbe Liste wie im
    /// Windows-Lautstärkemixer für dieses Gerät.
    /// </summary>
    public IReadOnlyList<AudioAppInfo> ListAudioApps() => AudioAppEnumerator.List(enumerator, ResolveSourceId());

    /// <summary>Aktuelle Quelle: das fest gewählte Gerät, sonst das Windows-Standardgerät.</summary>
    private string? ResolveSourceId() => FixedSourceDeviceId ?? TryGetDefaultDeviceId();

    /// <summary>Ob ein Gerät gerade als aktiver Endpunkt vorhanden ist.</summary>
    private bool IsDevicePresent(string deviceId)
    {
        try
        {
            foreach (MMDevice device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                string id = device.ID;
                device.Dispose();
                if (string.Equals(id, deviceId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }
        catch
        {
            return false;
        }
        return false;
    }

    /// <summary>
    /// Welche Aufnahmen gerade laufen: <see cref="WholeDeviceKey"/> für den kompletten
    /// Geräteton, sonst je ein Anwendungsschlüssel. Für Diagnose und Statusanzeige.
    /// </summary>
    public IReadOnlyList<string> ActiveCaptureKeys
    {
        get
        {
            lock (gate)
            {
                return captures.Where(c => c.Value.Capture != null).Select(c => c.Key).ToList();
            }
        }
    }

    /// <summary>Fehler beim Abgreifen des kompletten Gerätetons, oder <c>null</c>.</summary>
    public string? WholeDeviceError => GetAppError(WholeDeviceKey);

    /// <summary>Fehler beim Abgreifen einer Anwendung, oder <c>null</c>.</summary>
    public string? GetAppError(string appKey)
    {
        lock (gate)
        {
            return captures.TryGetValue(appKey, out CaptureEntry? entry) ? entry.Error : null;
        }
    }

    /// <summary>
    /// Alle Ausgabegeräte: die angeschlossenen und zusätzlich die, deren Buchse gerade leer ist.
    ///
    /// Deaktivierte und gar nicht vorhandene Endpunkte bleiben bewusst außen vor - davon führt
    /// Windows dutzende Karteileichen (alte HDMI-Ausgänge, virtuelle Kabel), die die Liste
    /// unbrauchbar machen würden.
    /// </summary>
    public IReadOnlyList<AudioDeviceInfo> ListOutputDevices()
    {
        string? defaultId = TryGetDefaultDeviceId();
        var result = new List<AudioDeviceInfo>();

        Collect(DeviceState.Active, connected: true);
        Collect(DeviceState.Unplugged, connected: false);

        return Disambiguate(result)
            .OrderByDescending(d => d.Connected)
            .ThenBy(d => d.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        void Collect(DeviceState state, bool connected)
        {
            foreach (MMDevice device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, state))
            {
                try
                {
                    result.Add(new AudioDeviceInfo(
                        device.ID, device.FriendlyName, connected && device.ID == defaultId,
                        connected, DetermineKind(device), ReadIconPath(device)));
                }
                catch
                {
                    // Gerät ist während der Aufzählung verschwunden - überspringen.
                }
                finally
                {
                    device.Dispose();
                }
            }
        }
    }

    /// <summary>Liest den Symbolverweis, den Windows selbst für dieses Gerät verwendet.</summary>
    private static string? ReadIconPath(MMDevice device)
    {
        try
        {
            return device.Properties.Contains(PropertyKeys.PKEY_Device_IconPath)
                ? device.Properties[PropertyKeys.PKEY_Device_IconPath].Value as string
                : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Leitet die Bauform aus der Windows-Eigenschaft "FormFactor" ab. Ist sie nicht lesbar
    /// oder unbekannt, bleibt es beim neutralen Symbol.
    /// </summary>
    private static AudioDeviceKind DetermineKind(MMDevice device)
    {
        try
        {
            if (!device.Properties.Contains(PropertyKeys.PKEY_AudioEndpoint_FormFactor))
            {
                return AudioDeviceKind.Other;
            }

            return Convert.ToInt32(device.Properties[PropertyKeys.PKEY_AudioEndpoint_FormFactor].Value) switch
            {
                1 or 2 => AudioDeviceKind.Speakers,          // Speakers, LineLevel
                3 or 5 => AudioDeviceKind.Headphones,        // Headphones, Headset
                7 or 8 => AudioDeviceKind.Digital,           // Passthrough, S/PDIF
                9 => AudioDeviceKind.Display,                // HDMI / DisplayPort
                _ => AudioDeviceKind.Other,
            };
        }
        catch
        {
            return AudioDeviceKind.Other;
        }
    }

    private static List<AudioDeviceInfo> Disambiguate(List<AudioDeviceInfo> devices)
    {
        var duplicates = devices.GroupBy(d => d.Name).Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet();
        if (duplicates.Count == 0)
        {
            return devices;
        }

        var seen = new Dictionary<string, int>();
        var result = new List<AudioDeviceInfo>(devices.Count);
        foreach (AudioDeviceInfo device in devices)
        {
            if (!duplicates.Contains(device.Name))
            {
                result.Add(device);
                continue;
            }
            int index = seen.TryGetValue(device.Name, out int n) ? n + 1 : 1;
            seen[device.Name] = index;
            result.Add(device with { Name = $"{device.Name} ({index})" });
        }
        return result;
    }

    public string? TryGetDefaultDeviceId()
    {
        try
        {
            if (!enumerator.HasDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia))
            {
                return null;
            }
            using MMDevice device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            return device.ID;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Legt fest, welche Geräte welche Anwendungen bekommen. Neue Kombinationen starten sofort,
    /// weggefallene stoppen sofort, unveränderte laufen ununterbrochen weiter.
    /// </summary>
    public void SetTargets(IReadOnlyList<MirrorTarget> targets, int bufferMs)
    {
        lock (gate)
        {
            activeTargets = targets
                .GroupBy(t => t.DeviceId, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToArray();
            activeBufferMs = bufferMs;
            Reconcile();
        }
    }

    public void Stop()
    {
        lock (gate)
        {
            activeTargets = [];
            StopAll();
        }
    }

    public void SetMasterVolume(string deviceId, float volume)
    {
        foreach (DeviceOutput output in outputs)
        {
            if (output.DeviceId == deviceId)
            {
                output.Volume = volume;
            }
        }

        lock (gate)
        {
            for (int i = 0; i < activeTargets.Length; i++)
            {
                if (activeTargets[i].DeviceId == deviceId)
                {
                    activeTargets[i] = activeTargets[i] with { MasterVolume = volume };
                }
            }
        }
    }

    /// <summary>
    /// Gleicht Aufnahmen und Ausgaben mit der gewünschten Zielmenge ab. Wird sowohl bei jeder
    /// Änderung als auch regelmäßig vom Wartungstakt aufgerufen; dadurch werden ausgefallene
    /// Aufnahmen und Geräte von selbst wieder angebunden.
    /// </summary>
    private void Reconcile()
    {
        string? sourceId = ResolveSourceId();
        SourceDeviceId = sourceId;
        SourceDeviceName = sourceId == null ? null : ResolveName(sourceId);

        // Ein fest gewähltes Quellgerät, das nicht da ist, pausiert die Spiegelung bewusst -
        // ein stiller Wechsel auf ein anderes Gerät wäre für den Nutzer nicht nachvollziehbar.
        SourceUnavailable = FixedSourceDeviceId != null && !IsDevicePresent(FixedSourceDeviceId);
        if (SourceUnavailable)
        {
            StopAll();
            return;
        }

        // Das Quellgerät darf nie Ziel sein - das erzeugt eine Rückkopplung.
        MirrorTarget[] wanted = activeTargets
            .Where(t => !string.Equals(t.DeviceId, sourceId, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (wanted.Length == 0)
        {
            StopAll();
            return;
        }

        // Die Puffergröße steckt in der gesamten Kette.
        if (activeBufferMs != runningBufferMs)
        {
            StopAll();
        }
        runningBufferMs = activeBufferMs;

        // Geräte ohne Abwahl brauchen den kompletten Geräteton, die übrigen je Anwendung eine
        // eigene Aufnahme.
        var neededApps = wanted
            .SelectMany(t => t.WholeDevice ? [WholeDeviceKey] : t.Apps.Select(a => a.AppKey))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Nicht mehr benötigte Aufnahmen abbauen.
        foreach (string key in captures.Keys.Where(k => !neededApps.Contains(k)).ToList())
        {
            DisposeCapture(captures[key]);
            captures.Remove(key);
        }

        // Fehlende oder ausgefallene Aufnahmen (neu) starten.
        foreach (string key in neededApps)
        {
            if (!captures.TryGetValue(key, out CaptureEntry? entry))
            {
                entry = new CaptureEntry(key);
                captures[key] = entry;
            }
            if (entry.Capture == null)
            {
                TryStartCapture(entry);
            }
        }

        // Ausgabegeräte abgleichen.
        var existing = outputs.ToDictionary(o => o.DeviceId, StringComparer.OrdinalIgnoreCase);
        var result = new List<DeviceOutput>(wanted.Length);

        foreach (MirrorTarget target in wanted)
        {
            WaveFormat? required = RequiredFormat(target);
            if (required == null)
            {
                // Die benötigte Aufnahme läuft noch nicht - das Gerät wartet.
                if (existing.Remove(target.DeviceId, out DeviceOutput? pending))
                {
                    pending.Dispose();
                }
                continue;
            }

            if (existing.Remove(target.DeviceId, out DeviceOutput? output)
                && !output.CaptureFormat.Equals(required))
            {
                // Beim Wechsel zwischen Geräteton und Einzelanwendungen ändert sich das
                // Aufnahmeformat - dann muss die Kette neu aufgebaut werden.
                output.Dispose();
                output = null;
            }

            if (output == null)
            {
                output = new DeviceOutput(
                    enumerator, target.DeviceId, ResolveName(target.DeviceId),
                    required, runningBufferMs, target.MasterVolume);
                output.SetApps(RunningApps(target));
                output.Start(); // Misserfolg landet in output.Error und wird später erneut versucht.
            }
            else
            {
                output.Volume = target.MasterVolume;
                output.SetApps(RunningApps(target));
            }
            result.Add(output);
        }

        foreach (DeviceOutput stale in existing.Values)
        {
            stale.Dispose();
        }

        outputs = result.ToArray();

        // Jede Aufnahme bedient nur die Geräte, die diese Anwendung hören wollen.
        foreach (CaptureEntry entry in captures.Values)
        {
            entry.Subscribers = wanted
                .Where(t => t.WholeDevice
                    ? entry.Key == WholeDeviceKey
                    : t.Apps.Any(a => a.AppKey.Equals(entry.Key, StringComparison.OrdinalIgnoreCase)))
                .Select(t => result.FirstOrDefault(o => o.DeviceId == t.DeviceId))
                .Where(o => o != null)
                .Select(o => o!)
                .ToArray();
        }

        bool anyRunning = outputs.Length > 0;
        if (anyRunning && !IsRunning)
        {
            TimerResolution.Acquire();
            IsRunning = true;
            tickCounter = 0;
            timer.Start();
        }
        else if (!anyRunning && IsRunning)
        {
            timer.Stop();
            TimerResolution.Release();
            IsRunning = false;
        }
    }

    /// <summary>Das Aufnahmeformat, das dieses Gerät braucht - oder null, wenn die Quelle fehlt.</summary>
    private WaveFormat? RequiredFormat(MirrorTarget target)
    {
        if (!target.WholeDevice)
        {
            return ProcessLoopbackCapture.CaptureFormat;
        }
        return captures.TryGetValue(WholeDeviceKey, out CaptureEntry? entry) ? entry.Format : null;
    }

    /// <summary>Was dem Gerät zugemischt wird: der komplette Geräteton oder die laufenden Anwendungen.</summary>
    private List<MirrorAppTarget> RunningApps(MirrorTarget target)
    {
        if (target.WholeDevice)
        {
            return [new MirrorAppTarget(WholeDeviceKey, 1f)];
        }

        return target.Apps
            .Where(a => captures.TryGetValue(a.AppKey, out CaptureEntry? e) && e.Capture != null)
            .ToList();
    }

    private void TryStartCapture(CaptureEntry entry)
    {
        if (entry.Key == WholeDeviceKey)
        {
            TryStartWholeDeviceCapture(entry);
            return;
        }

        try
        {
            int? processId = AudioAppEnumerator.ResolveProcessId(entry.Key);
            if (processId == null)
            {
                throw new InvalidOperationException(Strings.AppNotRunning);
            }

            var capture = new ProcessLoopbackCapture(processId.Value, Math.Clamp(runningBufferMs / 2, 10, 40));
            try
            {
                capture.DataAvailable += entry.OnData;
                capture.RecordingStopped += entry.OnStopped;
                capture.StartRecording();
                entry.Capture = capture;
                entry.Format = ProcessLoopbackCapture.CaptureFormat;
                entry.ProcessId = processId.Value;
                entry.Error = null;
            }
            catch
            {
                capture.DataAvailable -= entry.OnData;
                capture.RecordingStopped -= entry.OnStopped;
                capture.Dispose();
                throw;
            }
        }
        catch (Exception ex)
        {
            entry.Capture = null;
            entry.Format = null;
            entry.Subscribers = [];
            entry.Error = DescribeAppError(ex);
        }
    }

    /// <summary>
    /// Greift den kompletten Ton des Quellgeräts ab - so wie ursprünglich, einschließlich
    /// Systemklängen und allem, was keiner Audiositzung zugeordnet ist.
    /// </summary>
    private void TryStartWholeDeviceCapture(CaptureEntry entry)
    {
        try
        {
            string? sourceId = ResolveSourceId()
                ?? throw new InvalidOperationException(Strings.NoSourceDevice);

            MMDevice source = enumerator.GetDevice(sourceId);
            entry.Device = source;

            // Kleine Aufnahmepuffer sind für die Latenz entscheidend, werden aber nicht von jedem
            // Treiber akzeptiert - notfalls schrittweise größer werden.
            int preferred = Math.Clamp(runningBufferMs / 2, 10, 40);
            Exception? failure = null;

            foreach (int captureBufferMs in new[] { preferred, 25, 50, 100 }.Distinct().OrderBy(x => x))
            {
                LowLatencyLoopbackCapture? attempt = null;
                try
                {
                    attempt = new LowLatencyLoopbackCapture(source, captureBufferMs);
                    attempt.DataAvailable += entry.OnData;
                    attempt.RecordingStopped += entry.OnStopped;
                    attempt.StartRecording();
                    entry.Capture = attempt;
                    entry.Format = attempt.WaveFormat;
                    entry.Error = null;
                    return;
                }
                catch (Exception ex)
                {
                    failure = ex;
                    if (attempt != null)
                    {
                        attempt.DataAvailable -= entry.OnData;
                        attempt.RecordingStopped -= entry.OnStopped;
                        try
                        {
                            attempt.Dispose();
                        }
                        catch
                        {
                            // Aufräumen darf den nächsten Versuch nicht verhindern.
                        }
                    }
                }
            }

            throw new InvalidOperationException(
                Strings.SourceCaptureFailed(failure?.Message ?? "?"));
        }
        catch (Exception ex)
        {
            entry.Capture = null;
            entry.Format = null;
            entry.Subscribers = [];
            entry.Error = ex.Message;
            if (entry.Device != null)
            {
                try
                {
                    entry.Device.Dispose();
                }
                catch
                {
                    // Beim Aufräumen unkritisch.
                }
                entry.Device = null;
            }
        }
    }

    private static string DescribeAppError(Exception ex)
    {
        if (ex is InvalidOperationException)
        {
            return ex.Message;
        }

        // 0x80070490 = Element nicht gefunden: der Prozess gibt gerade keinen Ton aus.
        return ex.HResult == unchecked((int)0x80070490)
            ? Strings.AppSilent
            : Strings.CaptureFailed(ex.Message);
    }

    private string ResolveName(string deviceId)
    {
        try
        {
            using MMDevice device = enumerator.GetDevice(deviceId);
            return device.FriendlyName;
        }
        catch
        {
            return Strings.UnknownDevice;
        }
    }

    private void DisposeCapture(CaptureEntry entry)
    {
        entry.Stopping = true;
        if (entry.Capture != null)
        {
            entry.Capture.DataAvailable -= entry.OnData;
            entry.Capture.RecordingStopped -= entry.OnStopped;
            try
            {
                entry.Capture.StopRecording();
            }
            catch
            {
                // Bereits beendeter Prozess - unkritisch.
            }
            try
            {
                entry.Capture.Dispose();
            }
            catch
            {
                // s. o.
            }
            entry.Capture = null;
        }

        if (entry.Device != null)
        {
            try
            {
                entry.Device.Dispose();
            }
            catch
            {
                // Beim Aufräumen unkritisch.
            }
            entry.Device = null;
        }

        entry.Format = null;
        entry.Subscribers = [];
        entry.Stopping = false;
    }

    private void StopAll()
    {
        timer.Stop();
        if (IsRunning)
        {
            TimerResolution.Release();
        }
        IsRunning = false;

        foreach (CaptureEntry entry in captures.Values)
        {
            DisposeCapture(entry);
        }
        captures.Clear();

        DeviceOutput[] current = outputs;
        outputs = [];
        foreach (DeviceOutput output in current)
        {
            output.Dispose();
        }
    }

    private void OnTimer()
    {
        foreach (DeviceOutput output in outputs)
        {
            try
            {
                output.UpdateDriftCompensation();
            }
            catch
            {
                // Eine fehlgeschlagene Regelung darf den Timer nicht töten.
            }
        }

        if (++tickCounter % MaintenanceEveryTicks != 0)
        {
            return;
        }

        if (Interlocked.Exchange(ref maintenanceGuard, 1) == 1)
        {
            return;
        }

        try
        {
            Maintain();
        }
        catch
        {
            // Nie den Timer wegen eines Geräts verlieren.
        }
        finally
        {
            Volatile.Write(ref maintenanceGuard, 0);
        }
    }

    /// <summary>
    /// Bindet ausgefallene Aufnahmen und Zielgeräte wieder an, sobald sie verfügbar sind - etwa
    /// nach einem Bluetooth-Abbruch oder wenn eine ausgewählte Anwendung gestartet wird.
    /// </summary>
    private void Maintain()
    {
        lock (gate)
        {
            if (SourceUnavailable || captures.Values.Any(e => e.Capture == null) || outputs.Length != activeTargets.Length)
            {
                Reconcile();
            }

            foreach (DeviceOutput output in outputs.Where(o => o.Error != null))
            {
                output.Start();
            }
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }
        disposed = true;

        Stop();
        timer.Dispose();
        try
        {
            enumerator.UnregisterEndpointNotificationCallback(notifications);
        }
        catch
        {
            // Beim Herunterfahren unkritisch.
        }
        enumerator.Dispose();
    }

    /// <summary>Eine laufende Anwendungsaufnahme mit den Geräten, die sie hören.</summary>
    private sealed class CaptureEntry(string key)
    {
        public string Key { get; } = key;

        public IWaveIn? Capture { get; set; }

        /// <summary>Format dieser Aufnahme; beim Geräteton vom Gerät bestimmt.</summary>
        public WaveFormat? Format { get; set; }

        /// <summary>Nur beim Geräteton belegt: das aufgenommene Quellgerät.</summary>
        public MMDevice? Device { get; set; }

        public int ProcessId { get; set; }

        public string? Error { get; set; }

        public bool Stopping { get; set; }

        public DeviceOutput[] Subscribers { get; set; } = [];

        public void OnData(object? sender, WaveInEventArgs e)
        {
            if (e.BytesRecorded <= 0)
            {
                return;
            }

            DeviceOutput[] current = Subscribers;
            foreach (DeviceOutput output in current)
            {
                output.AddSamples(Key, e.Buffer, 0, e.BytesRecorded);
            }
        }

        public void OnStopped(object? sender, StoppedEventArgs e)
        {
            if (Stopping)
            {
                return;
            }

            // Anwendung ist beendet worden - der Wartungstakt bindet sie wieder an, sobald sie zurück ist.
            Capture = null;
            Format = null;
            Subscribers = [];
            Error = e.Exception?.Message ?? Strings.SourceEnded;
        }
    }
}
