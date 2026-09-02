using AudioMirror;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace AudioMirror.Audio;

/// <summary>
/// Ein Ziel-Wiedergabegerät. Es mischt die Tonströme mehrerer Anwendungen zusammen und gibt
/// sie über eine eigene WasapiOut-Instanz aus. Jedes Gerät läuft unabhängig: fällt eines aus,
/// laufen die anderen weiter.
///
/// Lautstärken wirken multiplikativ: jede Anwendung hat ihren eigenen Regler, und die
/// Haupt-Lautstärke des Geräts skaliert die fertige Mischung. 50 % × 50 % ergeben also 25 %.
///
/// Das Gerät wird bei jedem Startversuch frisch aufgelöst, damit auch nach einem
/// Abziehen/Wiederanstecken sauber neu verbunden werden kann.
/// </summary>
internal sealed class DeviceOutput : IDisposable
{
    private readonly MMDeviceEnumerator enumerator;
    private readonly WaveFormat captureFormat;
    private readonly AppMixerSampleProvider mixer;
    private readonly Dictionary<string, AppStream> streams = new(StringComparer.OrdinalIgnoreCase);
    private readonly object streamGate = new();
    private readonly double bytesPerMs;
    private readonly double targetFloorMs;
    private readonly double resyncFillMs;
    private readonly int bufferMs;
    private readonly int[] latencyLadder;

    private MMDevice? device;
    private WasapiOut? output;
    private AdaptiveResampler? drift;
    private VolumeSampleProvider? masterStage;
    private BufferProbeSampleProvider? probe;
    private float volume;
    private int outputLatencyMs;
    private bool disposed;

    public DeviceOutput(
        MMDeviceEnumerator enumerator,
        string deviceId,
        string friendlyName,
        WaveFormat captureFormat,
        int bufferMs,
        float volume)
    {
        this.enumerator = enumerator;
        this.captureFormat = captureFormat;
        this.bufferMs = bufferMs;
        this.volume = volume;
        DeviceId = deviceId;
        FriendlyName = friendlyName;

        // Geregelt wird der kleinste Füllstand, nicht der momentane. Der Sollwert ist daher
        // ein Sicherheitsabstand zur Pufferleere, kein Zielmittelwert.
        targetFloorMs = Math.Max(4.0, bufferMs / 3.0);
        resyncFillMs = Math.Max(bufferMs * 5, 250);
        bytesPerMs = captureFormat.AverageBytesPerSecond / 1000.0;
        outputLatencyMs = Math.Clamp(bufferMs / 2, 10, 100);

        // Nicht jedes Gerät akzeptiert kleine Puffer - notfalls schrittweise größer werden.
        latencyLadder = new[] { outputLatencyMs, 30, 60, 100 }.Distinct().OrderBy(x => x).ToArray();

        mixer = new AppMixerSampleProvider(captureFormat);
    }

    public string DeviceId { get; }

    public string FriendlyName { get; }

    /// <summary>Format der Aufnahme, an dem die Kette hängt. Ändert es sich, wird neu aufgebaut.</summary>
    public WaveFormat CaptureFormat => captureFormat;

    /// <summary>Gesetzt, wenn dieses Gerät ausgefallen ist. Andere Geräte bleiben davon unberührt.</summary>
    public string? Error { get; private set; }

    /// <summary>Ausgehandeltes Geräteformat, für die Diagnose.</summary>
    public string FormatDescription { get; private set; } = "-";

    public bool IsPlaying => output != null && Error == null;

    /// <summary>Anzahl der Anwendungen, die derzeit auf dieses Gerät gemischt werden.</summary>
    public int ActiveAppCount
    {
        get
        {
            lock (streamGate)
            {
                return streams.Count;
            }
        }
    }

    /// <summary>Haupt-Lautstärke des Geräts (0..1), wirkt auf die fertige Mischung.</summary>
    public float Volume
    {
        get => volume;
        set
        {
            volume = Math.Clamp(value, 0f, 1f);
            if (masterStage != null)
            {
                masterStage.Volume = volume;
            }
        }
    }

    /// <summary>Kleinster Füllstand über alle Anwendungspuffer, in Millisekunden.</summary>
    public double BufferFillMs => (MinimumFillBytes() ?? 0) / bytesPerMs;

    /// <summary>Grober Schätzwert der aktuell durch dieses Programm entstehenden Zusatzlatenz.</summary>
    public double EstimatedLatencyMs => BufferFillMs + outputLatencyMs;

    /// <summary>
    /// Legt fest, welche Anwendungen mit welcher Lautstärke auf dieses Gerät gemischt werden.
    /// Wirkt sofort und ohne Unterbrechung der Wiedergabe: bereits laufende Anwendungen
    /// behalten ihren Puffer, nur die Eingangsliste des Mischers wird ausgetauscht.
    /// </summary>
    public void SetApps(IReadOnlyList<MirrorAppTarget> apps)
    {
        lock (streamGate)
        {
            var wanted = apps.ToDictionary(a => a.AppKey, a => a.Volume, StringComparer.OrdinalIgnoreCase);

            foreach (string gone in streams.Keys.Where(k => !wanted.ContainsKey(k)).ToList())
            {
                streams.Remove(gone);
            }

            foreach ((string key, float appVolume) in wanted)
            {
                if (streams.TryGetValue(key, out AppStream? stream))
                {
                    stream.Gain.Volume = Math.Clamp(appVolume, 0f, 1f);
                    continue;
                }

                var buffer = new BufferedWaveProvider(captureFormat)
                {
                    BufferDuration = TimeSpan.FromMilliseconds(Math.Max(600, bufferMs * 10)),
                    DiscardOnBufferOverflow = true,
                    ReadFully = true,
                };

                // Neu hinzukommende Anwendungen auf den Füllstand der bereits laufenden bringen,
                // damit sie zeitlich zu ihnen passen statt hinterherzuhinken.
                long align = MinimumFillBytesLocked() ?? 0;
                if (align > 0)
                {
                    buffer.AddSamples(new byte[align], 0, (int)align);
                }

                streams[key] = new AppStream(
                    buffer,
                    new VolumeSampleProvider(buffer.ToSampleProvider()) { Volume = Math.Clamp(appVolume, 0f, 1f) });
            }

            mixer.SetInputs(streams.Values.Select(s => (ISampleProvider)s.Gain).ToArray());
        }
    }

    public bool Start()
    {
        DisposeOutput();

        lock (streamGate)
        {
            foreach (AppStream stream in streams.Values)
            {
                stream.Buffer.ClearBuffer();
            }
        }

        Exception? failure = null;

        foreach (int latency in latencyLadder)
        {
            try
            {
                device = enumerator.GetDevice(DeviceId);
                WaveFormat target = OutputFormatNegotiator.Negotiate(device.AudioClient);

                var wasapi = new WasapiOut(device, AudioClientShareMode.Shared, true, latency);
                wasapi.PlaybackStopped += OnPlaybackStopped;
                wasapi.Init(BuildChain(target));
                wasapi.Play();

                output = wasapi;
                outputLatencyMs = latency;
                FormatDescription = Describe(target);
                Error = null;
                return true;
            }
            catch (Exception ex)
            {
                failure = ex;
                DisposeOutput();
            }
        }

        Error = DescribeError(failure);
        return false;
    }

    private IWaveProvider BuildChain(WaveFormat target)
    {
        ISampleProvider chain = ChannelMapSampleProvider.Create(mixer, target.Channels);

        drift = new AdaptiveResampler(chain);
        chain = drift;

        if (chain.WaveFormat.SampleRate != target.SampleRate)
        {
            chain = new WdlResamplingSampleProvider(chain, target.SampleRate);
        }

        // Erst die Anwendungslautstärken (in den einzelnen Strömen), dann hier die
        // Haupt-Lautstärke - dadurch wirken beide multiplikativ.
        masterStage = new VolumeSampleProvider(chain) { Volume = volume };
        probe = new BufferProbeSampleProvider(masterStage, MinimumFillBytes);
        return new SampleToTargetProvider(probe, target);
    }

    private static string Describe(WaveFormat format)
    {
        WaveFormat standard = SampleToTargetProvider.Standardize(format);
        return $"{format.SampleRate} Hz, {format.Channels} Kanäle, {standard.BitsPerSample} Bit {standard.Encoding}";
    }

    /// <summary>Nimmt Aufnahmedaten einer bestimmten Anwendung entgegen.</summary>
    public void AddSamples(string appKey, byte[] data, int offset, int count)
    {
        if (Error != null || count <= 0)
        {
            return;
        }

        BufferedWaveProvider? buffer;
        lock (streamGate)
        {
            buffer = streams.TryGetValue(appKey, out AppStream? stream) ? stream.Buffer : null;
        }

        if (buffer == null)
        {
            return;
        }

        try
        {
            buffer.AddSamples(data, offset, count);
        }
        catch (Exception ex)
        {
            Error = DescribeError(ex);
        }
    }

    private long? MinimumFillBytes()
    {
        lock (streamGate)
        {
            return MinimumFillBytesLocked();
        }
    }

    private long? MinimumFillBytesLocked()
    {
        long? minimum = null;
        foreach (AppStream stream in streams.Values)
        {
            long fill = stream.Buffer.BufferedBytes;
            if (minimum == null || fill < minimum)
            {
                minimum = fill;
            }
        }
        return minimum;
    }

    /// <summary>
    /// Regelt das Resampling-Verhältnis so nach, dass der kleinste Pufferfüllstand knapp
    /// oberhalb der Leere pendelt. Wird periodisch von der Engine aufgerufen.
    /// </summary>
    public void UpdateDriftCompensation()
    {
        if (Error != null || drift == null || probe == null)
        {
            return;
        }

        if (BufferFillMs > resyncFillMs)
        {
            // Gerät hat länger gestockt (z. B. BT-Reconnect): harter Resync statt wachsender Latenz.
            lock (streamGate)
            {
                foreach (AppStream stream in streams.Values)
                {
                    stream.Buffer.ClearBuffer();
                }
            }
            drift.Ratio = 1.0;
            return;
        }

        if (probe.TakeMinimumBytes() is not { } bytes)
        {
            // Das Gerät hat seit dem letzten Takt nichts abgeholt - keine Aussage möglich.
            return;
        }

        double error = (bytes / bytesPerMs) - targetFloorMs;
        drift.Ratio = 1.0 + Math.Clamp(error / 2000.0, -0.01, 0.01);
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        if (disposed)
        {
            return;
        }
        Error = e.Exception != null
            ? DescribeError(e.Exception)
            : Strings.PlaybackStopped;
    }

    private static string DescribeError(Exception? ex)
    {
        if (ex == null)
        {
            return Strings.UnknownError;
        }

        if (ex is NotSupportedException)
        {
            return ex.Message;
        }

        // NAudio meldet exklusiv belegte bzw. verschwundene Geräte als COM-HRESULT.
        return ex.HResult switch
        {
            unchecked((int)0x8889000A) => Strings.DeviceExclusive,
            unchecked((int)0x88890004) => Strings.DeviceGone,
            unchecked((int)0x88890008) => Strings.FormatUnsupported,
            unchecked((int)0x88890001) => Strings.DeviceInUse,
            unchecked((int)0x8889000E) => Strings.AudioServiceDown,
            unchecked((int)0x80070005) => Strings.AccessDenied,
            _ => ex.Message,
        };
    }

    private void DisposeOutput()
    {
        if (output != null)
        {
            output.PlaybackStopped -= OnPlaybackStopped;
            try
            {
                output.Stop();
            }
            catch
            {
                // Ein bereits entferntes Gerät wirft hier - unkritisch.
            }
            try
            {
                output.Dispose();
            }
            catch
            {
                // s. o.
            }
            output = null;
        }

        if (device != null)
        {
            try
            {
                device.Dispose();
            }
            catch
            {
                // s. o.
            }
            device = null;
        }

        drift = null;
        masterStage = null;
        probe = null;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }
        disposed = true;
        DisposeOutput();
    }

    private sealed record AppStream(BufferedWaveProvider Buffer, VolumeSampleProvider Gain);
}
