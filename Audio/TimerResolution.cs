using System.Runtime.InteropServices;

namespace AudioMirror.Audio;

/// <summary>
/// Hebt die Windows-Timeraufloesung waehrend der Spiegelung auf 1 ms an.
///
/// Der Aufnahmethread von NAudio wartet mit <c>Thread.Sleep</c>. Bei der Standardaufloesung
/// von 15,6 ms schlaeft ein angefordertes Sleep(7) real ~15 ms - die Samples kommen dadurch
/// in unregelmaessigen Schueben an und der Ringpuffer schwankt entsprechend stark. Mit 1 ms
/// Aufloesung laeuft die Aufnahme gleichmaessig. Die Einstellung wirkt seit Windows 10 2004
/// nur prozesslokal und wird beim Stoppen wieder freigegeben.
/// </summary>
internal static class TimerResolution
{
    private static int refCount;

    [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
    private static extern uint TimeBeginPeriod(uint milliseconds);

    [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
    private static extern uint TimeEndPeriod(uint milliseconds);

    public static void Acquire()
    {
        if (Interlocked.Increment(ref refCount) == 1)
        {
            try
            {
                TimeBeginPeriod(1);
            }
            catch (DllNotFoundException)
            {
                // Ohne winmm laeuft die Spiegelung weiter, nur mit groesserem Jitter.
            }
        }
    }

    public static void Release()
    {
        if (Interlocked.Decrement(ref refCount) == 0)
        {
            try
            {
                TimeEndPeriod(1);
            }
            catch (DllNotFoundException)
            {
                // s. o.
            }
        }
    }
}
