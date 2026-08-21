using System.Runtime.InteropServices;

namespace AudioMirror;

/// <summary>
/// Sorgt dafür, dass immer nur ein Audio Mirror läuft.
///
/// Das ist wichtig, seit das Fenster beim Start ausgeblendet bleibt: ein Doppelklick auf die
/// Datei sieht sonst folgenlos aus, man klickt erneut - und hätte zwei Instanzen, die beide
/// spiegeln. Das Ergebnis wäre doppelter Ton und zwei Symbole im Infobereich.
///
/// Stattdessen meldet sich der zweite Start beim ersten und beendet sich selbst; der erste
/// holt daraufhin sein Fenster nach vorn.
/// </summary>
internal static class SingleInstance
{
    private const string MutexName = @"Local\AudioMirror.SingleInstance";
    private static readonly IntPtr BroadcastWindow = new(0xFFFF);

    private static Mutex? mutex;

    /// <summary>Fensternachricht, mit der eine zweite Instanz das Fenster anfordert.</summary>
    public static uint ShowWindowMessage { get; } = RegisterWindowMessage("AudioMirrorShowWindow");

    /// <summary>Belegt die Instanzsperre. Liefert false, wenn bereits eine Instanz läuft.</summary>
    public static bool TryAcquire()
    {
        try
        {
            mutex = new Mutex(initiallyOwned: true, MutexName, out bool created);
            return created;
        }
        catch
        {
            // Ist die Sperre nicht anlegbar, lieber starten als gar nicht laufen.
            return true;
        }
    }

    /// <summary>Bittet die laufende Instanz, ihr Fenster zu zeigen.</summary>
    public static void SignalExistingInstance()
    {
        if (ShowWindowMessage != 0)
        {
            PostMessage(BroadcastWindow, ShowWindowMessage, IntPtr.Zero, IntPtr.Zero);
        }
    }

    public static void Release()
    {
        try
        {
            mutex?.ReleaseMutex();
        }
        catch
        {
            // Beim Beenden unkritisch.
        }
        mutex?.Dispose();
        mutex = null;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern uint RegisterWindowMessage(string message);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam);
}
