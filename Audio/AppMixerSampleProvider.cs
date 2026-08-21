using NAudio.Wave;

namespace AudioMirror.Audio;

/// <summary>
/// Mischt die Tonströme mehrerer Anwendungen zu einem Ausgangssignal zusammen.
///
/// Die Eingangsliste wird als Ganzes ausgetauscht statt einzeln verändert: der Audiothread
/// liest immer eine unveränderliche Momentaufnahme. Dadurch lässt sich eine Anwendung im
/// laufenden Betrieb zu- oder abschalten, ohne die Wiedergabe zu unterbrechen und ohne dass
/// im Audiothread gesperrt werden müsste.
/// </summary>
internal sealed class AppMixerSampleProvider : ISampleProvider
{
    private ISampleProvider[] inputs = [];
    private float[] scratch = [];

    public AppMixerSampleProvider(WaveFormat waveFormat)
    {
        WaveFormat = waveFormat;
    }

    public WaveFormat WaveFormat { get; }

    public void SetInputs(ISampleProvider[] newInputs) => Volatile.Write(ref inputs, newInputs);

    public int Read(float[] buffer, int offset, int count)
    {
        ISampleProvider[] current = Volatile.Read(ref inputs);

        Array.Clear(buffer, offset, count);
        if (current.Length == 0)
        {
            // Kein Zuspieler: Stille liefern, damit die Wiedergabe weiterläuft.
            return count;
        }

        if (current.Length == 1)
        {
            int read = current[0].Read(buffer, offset, count);
            return count > read ? count : read;
        }

        if (scratch.Length < count)
        {
            scratch = new float[count];
        }

        foreach (ISampleProvider input in current)
        {
            int read = input.Read(scratch, 0, count);
            for (int i = 0; i < read; i++)
            {
                buffer[offset + i] += scratch[i];
            }
        }

        return count;
    }
}
