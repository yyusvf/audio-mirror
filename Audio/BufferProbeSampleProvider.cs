using NAudio.Wave;

namespace AudioMirror.Audio;

/// <summary>
/// Misst den Ringpuffer-Füllstand jeweils direkt nach einem Lesevorgang des Ausgabegeräts.
///
/// Genau dort treten die Minima auf. Die Drift-Regelung muss diese Minima kennen und nicht den
/// Mittelwert: ein Puffer, der im Mittel gut gefüllt ist, aber in den Tälern auf null fällt,
/// erzeugt hörbare Aussetzer.
///
/// Da ein Zielgerät mehrere Anwendungen mischt, wird der Füllstand über eine Funktion geholt,
/// die den kleinsten Wert über alle beteiligten Puffer liefert.
/// </summary>
internal sealed class BufferProbeSampleProvider : ISampleProvider
{
    private readonly ISampleProvider source;
    private readonly Func<long?> measureFill;
    private long minimumBytes = long.MaxValue;

    public BufferProbeSampleProvider(ISampleProvider source, Func<long?> measureFill)
    {
        this.source = source;
        this.measureFill = measureFill;
    }

    public WaveFormat WaveFormat => source.WaveFormat;

    public int Read(float[] samples, int offset, int count)
    {
        int read = source.Read(samples, offset, count);

        if (measureFill() is { } fill && fill < Interlocked.Read(ref minimumBytes))
        {
            Interlocked.Exchange(ref minimumBytes, fill);
        }

        return read;
    }

    /// <summary>Kleinster seit dem letzten Aufruf beobachteter Füllstand; <c>null</c> wenn nicht gelesen wurde.</summary>
    public long? TakeMinimumBytes()
    {
        long value = Interlocked.Exchange(ref minimumBytes, long.MaxValue);
        return value == long.MaxValue ? null : value;
    }
}
