using NAudio.Wave;

namespace AudioMirror.Audio;

/// <summary>
/// Passt die Kanalzahl der Aufnahme an die des Zielgeraets an - in beide Richtungen.
///
/// Noetig, weil Quelle und Ziel voellig unterschiedliche Kanallayouts haben koennen: ein
/// AV-Receiver oder HDMI-Ausgang meldet 5.1/7.1, ein Headset-Freisprechprofil Mono, der
/// Normalfall ist Stereo. Die Zuordnung laeuft ueber eine einmalig aufgebaute Mischmatrix.
///
/// Kanalreihenfolge nach WASAPI-Konvention: FL, FR, FC, LFE, BL, BR, SL, SR.
/// </summary>
internal sealed class ChannelMapSampleProvider : ISampleProvider
{
    private const float Fold = 0.7071f; // -3 dB, ueblicher Faltungsfaktor
    private const int FrontLeft = 0;
    private const int FrontRight = 1;
    private const int FrontCentre = 2;
    private const int LowFrequency = 3;

    private readonly ISampleProvider source;
    private readonly int inChannels;
    private readonly int outChannels;
    private readonly float[,] matrix;
    private float[] sourceBuffer = [];

    private ChannelMapSampleProvider(ISampleProvider source, int outChannels)
    {
        this.source = source;
        inChannels = source.WaveFormat.Channels;
        this.outChannels = outChannels;
        matrix = BuildMatrix(inChannels, outChannels);
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(source.WaveFormat.SampleRate, outChannels);
    }

    public static ISampleProvider Create(ISampleProvider source, int outChannels) =>
        source.WaveFormat.Channels == outChannels ? source : new ChannelMapSampleProvider(source, outChannels);

    public WaveFormat WaveFormat { get; }

    /// <summary>Mischmatrix [Ausgangskanal, Eingangskanal].</summary>
    private static float[,] BuildMatrix(int inChannels, int outChannels)
    {
        var m = new float[outChannels, inChannels];

        if (outChannels == 1)
        {
            // Front je zur Haelfte (echter Mittelwert), Center/Surrounds zusaetzlich mit -3 dB.
            for (int i = 0; i < inChannels; i++)
            {
                m[0, i] = i == LowFrequency ? 0f : (i is FrontLeft or FrontRight ? 0.5f : Fold * 0.5f);
            }
            return m;
        }

        if (inChannels == 1)
        {
            // Mono auf die vorderen beiden Kanaele, der Rest bleibt still.
            m[FrontLeft, 0] = 1f;
            m[FrontRight, 0] = 1f;
            return m;
        }

        if (outChannels == 2 && inChannels > 2)
        {
            // Stereo-Downmix nach ITU-R BS.775: Center und Surrounds mit -3 dB einfalten,
            // LFE weglassen.
            //
            // Bewusst ohne Normierung auf die Worst-Case-Summe: die vorderen Kanaele behalten
            // ihren Pegel. Wuerde man durch die Gesamtsumme teilen, kaeme gewoehnlicher
            // Stereo-Inhalt, der ueber ein Mehrkanalgeraet laeuft - der haeufigste Fall -
            // rund 8 dB zu leise heraus. Gegen den seltenen Extremfall (alle Kanaele gleich
            // ausgesteuert) schuetzt die Begrenzung in SampleToTargetProvider.
            for (int i = 0; i < inChannels; i++)
            {
                if (i == LowFrequency) { continue; }
                if (i == FrontLeft) { m[0, i] = 1f; }
                else if (i == FrontRight) { m[1, i] = 1f; }
                else if (i == FrontCentre) { m[0, i] = Fold; m[1, i] = Fold; }
                else if (i % 2 == 0) { m[0, i] = Fold; }   // BL, SL
                else { m[1, i] = Fold; }                    // BR, SR
            }
            return m;
        }

        // Vorhandene Kanaele direkt uebernehmen, fehlende bleiben still. Bei Stereo auf
        // Mehrkanal landet der Ton damit korrekt auf den vorderen beiden Kanaelen.
        int shared = Math.Min(inChannels, outChannels);
        for (int i = 0; i < shared; i++)
        {
            m[i, i] = 1f;
        }
        return m;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int frames = count / outChannels;
        int needed = frames * inChannels;
        if (sourceBuffer.Length < needed)
        {
            sourceBuffer = new float[needed];
        }

        int read = source.Read(sourceBuffer, 0, needed);
        int readFrames = read / inChannels;

        for (int f = 0; f < readFrames; f++)
        {
            int src = f * inChannels;
            int dst = offset + (f * outChannels);

            for (int o = 0; o < outChannels; o++)
            {
                float sum = 0f;
                for (int i = 0; i < inChannels; i++)
                {
                    float gain = matrix[o, i];
                    if (gain != 0f)
                    {
                        sum += sourceBuffer[src + i] * gain;
                    }
                }
                buffer[dst + o] = sum;
            }
        }

        return readFrames * outChannels;
    }
}
