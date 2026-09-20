using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace AudioMirror.Ui.Shell;

/// <summary>
/// Dunkle Titelleiste und runde Ecken - beides stellt der Fenstermanager, nicht WPF. WPF kennt
/// dafür keine Eigenschaften, also führt der Weg über <c>DwmSetWindowAttribute</c>.
///
/// Beide Angaben sind ab einer bestimmten Windows-Fassung vorhanden, und ältere antworten
/// schlicht mit einem Fehlercode, statt zu stürzen. Ein Rückfall ist deshalb nicht nötig: die
/// Farben des Fensters stehen ohnehin fest, betroffen wären nur Rahmen und Ecken.
///
/// Mica wird bewusst nicht gesetzt. Der Hintergrund des Fensters ist deckend (#202020), ein
/// durchscheinender Untergrund wäre davon vollständig verdeckt.
/// </summary>
internal static class WindowEffects
{
    private const int UseImmersiveDarkMode = 20;
    private const int WindowCornerPreference = 33;

    private const int CornerRound = 2;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    /// <summary>Anzuwenden, sobald das Fenster ein Handle hat - vorher greift keine der Angaben.</summary>
    public static void Apply(Window window)
    {
        IntPtr handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        // Dunkle Titelleiste. Betrifft hier nur noch den Rahmen, weil die Leiste selbst
        // gezeichnet wird - der Unterschied fällt am Fensterrand und beim Umschalten auf.
        Set(handle, UseImmersiveDarkMode, 1);

        // Runde Ecken: Windows 11 rundet von sich aus, aber nicht bei jedem Fensterstil.
        Set(handle, WindowCornerPreference, CornerRound);
    }

    private static void Set(IntPtr handle, int attribute, int value)
    {
        DwmSetWindowAttribute(handle, attribute, ref value, sizeof(int));
    }
}
