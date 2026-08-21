using NAudio.Wave;

namespace AudioMirror.Audio;

/// <summary>
/// Linear interpolierender Resampler mit im Betrieb verstellbarem Verhaeltnis.
///
/// Zweck ist die Drift-Kompensation: Aufnahmegeraet und jedes Wiedergabegeraet laufen auf
/// eigenen Quarzen (Bluetooth-Kopfhoerer weichen typischerweise am staerksten ab). Ohne
/// Korrektur laeuft der Ringpuffer eines Zielgeraets ueber Minuten entweder leer (Aussetzer)
/// oder voll (stetig wachsende Latenz). <see cref="Ratio"/> wird daher regelmaessig anhand
/// des Pufferfuellstands minimal nachgeregelt (max. +/-1%, also unhoerbar).
///
/// Wichtig fuer die Latenz: pro <see cref="Read"/> wird nur so viel Eingangsmaterial geholt,
/// wie fuer die angeforderten Ausgangs-Frames noetig ist. Ein Vorauslesen in festen Bloecken
/// wuerde den Ringpuffer schubweise leeren und die Latenz entsprechend schwanken lassen.
/// Nicht verbrauchte Frames werden in den naechsten Aufruf uebernommen, damit an der
/// Blockgrenze kein Frame verloren geht.
/// </summary>
internal sealed class AdaptiveResampler : ISampleProvider
{
    private readonly ISampleProvider source;
    private readonly int channels;
    private readonly float[] previousFrame;
    private readonly float[] currentFrame;

    private float[] input = [];
    private int availableFrames;
    private int consumedFrames;
    private double position;
    private bool primed;
    private double ratio = 1.0;

    public AdaptiveResampler(ISampleProvider source)
    {
        this.source = source;
        channels = source.WaveFormat.Channels;
        previousFrame = new float[channels];
        currentFrame = new float[channels];
        WaveFormat = source.WaveFormat;
    }

    public WaveFormat WaveFormat { get; }

    /// <summary>Eingangs-Frames pro Ausgangs-Frame. 1.0 = keine Korrektur.</summary>
    public double Ratio
    {
        get => ratio;
        set => ratio = Math.Clamp(value, 0.95, 1.05);
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int outFrames = count / channels;
        if (outFrames == 0)
        {
            return 0;
        }

        double step = ratio;

        if (!primed)
        {
            if (!Fill(2))
            {
                return 0;
            }
            Array.Copy(input, 0, previousFrame, 0, channels);
            Array.Copy(input, channels, currentFrame, 0, channels);
            consumedFrames = 2;
            position = 0.0;
            primed = true;
        }

        // Voraussichtlicher Bedarf plus ein Frame Reserve: die schrittweise Summation von
        // "position" kann minimal von der Vorausberechnung abweichen. Ueberzaehlige Frames
        // gehen nicht verloren, sie bleiben fuer den naechsten Aufruf liegen.
        Fill((int)Math.Floor(position + (outFrames * step)) + 1);

        int produced = 0;
        while (produced < outFrames)
        {
            int destination = offset + (produced * channels);
            for (int c = 0; c < channels; c++)
            {
                buffer[destination + c] = (float)(previousFrame[c] + ((currentFrame[c] - previousFrame[c]) * position));
            }
            produced++;

            position += step;
            while (position >= 1.0)
            {
                if (consumedFrames >= availableFrames && !Fill(1))
                {
                    // Quelle erschoepft - beim naechsten Read neu einschwingen.
                    primed = false;
                    Compact();
                    return produced * channels;
                }

                Array.Copy(currentFrame, previousFrame, channels);
                Array.Copy(input, consumedFrames * channels, currentFrame, 0, channels);
                consumedFrames++;
                position -= 1.0;
            }
        }

        Compact();
        return produced * channels;
    }

    /// <summary>Sorgt dafuer, dass ab <c>consumedFrames</c> mindestens <paramref name="frames"/> Frames bereitliegen.</summary>
    private bool Fill(int frames)
    {
        int missing = consumedFrames + frames - availableFrames;
        if (missing <= 0)
        {
            return true;
        }

        int required = (availableFrames + missing) * channels;
        if (input.Length < required)
        {
            Array.Resize(ref input, required);
        }

        int read = source.Read(input, availableFrames * channels, missing * channels) / channels;
        availableFrames += read;
        return read == missing;
    }

    /// <summary>Schiebt die noch nicht verbrauchten Frames an den Pufferanfang.</summary>
    private void Compact()
    {
        int remaining = availableFrames - consumedFrames;
        if (remaining > 0 && consumedFrames > 0)
        {
            Array.Copy(input, consumedFrames * channels, input, 0, remaining * channels);
        }
        availableFrames = remaining;
        consumedFrames = 0;
    }
}
