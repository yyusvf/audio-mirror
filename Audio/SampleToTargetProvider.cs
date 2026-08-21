using NAudio.Wave;

namespace AudioMirror.Audio;

/// <summary>
/// Letzte Stufe der Kette: wandelt die intern durchgaengig verwendeten Float-Samples in das
/// tatsaechlich ausgehandelte Geraeteformat um und meldet exakt dieses Format.
///
/// Im Shared Mode liefert Windows praktisch immer 32-Bit-Float. Es gibt aber Treiber, die
/// stattdessen ganzzahlige Formate melden - deshalb werden 16, 24 und 32 Bit PCM ebenfalls
/// bedient, statt Float einfach vorauszusetzen.
/// </summary>
internal sealed class SampleToTargetProvider : IWaveProvider
{
    private readonly ISampleProvider source;
    private readonly WaveFormatEncoding encoding;
    private readonly int bytesPerSample;
    private float[] samples = [];

    public SampleToTargetProvider(ISampleProvider source, WaveFormat targetFormat)
    {
        this.source = source;
        WaveFormat = targetFormat;
        bytesPerSample = targetFormat.BitsPerSample / 8;

        WaveFormat standard = Standardize(targetFormat);
        encoding = standard.Encoding;

        bool supported = encoding switch
        {
            WaveFormatEncoding.IeeeFloat => standard.BitsPerSample == 32,
            WaveFormatEncoding.Pcm => standard.BitsPerSample is 16 or 24 or 32,
            _ => false,
        };

        if (!supported)
        {
            throw new NotSupportedException(
                $"Audioformat des Geraets wird nicht unterstuetzt ({standard.Encoding}, {standard.BitsPerSample} Bit).");
        }
    }

    public WaveFormat WaveFormat { get; }

    /// <summary>Loest WaveFormatExtensible in die zugrunde liegende Kodierung auf.</summary>
    public static WaveFormat Standardize(WaveFormat format)
    {
        if (format is WaveFormatExtensible extensible)
        {
            try
            {
                return extensible.ToStandardWaveFormat();
            }
            catch (InvalidOperationException)
            {
                // Unbekannter SubFormat-GUID - dann bleibt es beim Ausgangsformat.
            }
        }
        return format;
    }

    public int Read(byte[] buffer, int offset, int count)
    {
        int wanted = count / bytesPerSample;
        if (samples.Length < wanted)
        {
            samples = new float[wanted];
        }

        int read = source.Read(samples, 0, wanted);

        if (encoding == WaveFormatEncoding.IeeeFloat)
        {
            // Auch im Float-Pfad begrenzen: ein Mehrkanal-Downmix kann rechnerisch ueber 1,0
            // hinausgehen, und WASAPI erwartet den Bereich -1..1.
            for (int i = 0; i < read; i++)
            {
                samples[i] = Math.Clamp(samples[i], -1f, 1f);
            }
            Buffer.BlockCopy(samples, 0, buffer, offset, read * 4);
            return read * 4;
        }

        int position = offset;
        for (int i = 0; i < read; i++)
        {
            float sample = Math.Clamp(samples[i], -1f, 1f);
            switch (bytesPerSample)
            {
                case 2:
                    short pcm16 = (short)(sample * short.MaxValue);
                    buffer[position++] = (byte)pcm16;
                    buffer[position++] = (byte)(pcm16 >> 8);
                    break;

                case 3:
                    int pcm24 = (int)(sample * 8388607f);
                    buffer[position++] = (byte)pcm24;
                    buffer[position++] = (byte)(pcm24 >> 8);
                    buffer[position++] = (byte)(pcm24 >> 16);
                    break;

                default:
                    int pcm32 = (int)(sample * int.MaxValue);
                    buffer[position++] = (byte)pcm32;
                    buffer[position++] = (byte)(pcm32 >> 8);
                    buffer[position++] = (byte)(pcm32 >> 16);
                    buffer[position++] = (byte)(pcm32 >> 24);
                    break;
            }
        }

        return read * bytesPerSample;
    }
}
