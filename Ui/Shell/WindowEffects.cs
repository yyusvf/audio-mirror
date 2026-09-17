using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace AudioMirror.Ui.Shell;

/// <summary>
/// Dunkle Titelleiste, Mica-Hintergrund und runde Ecken - alles drei stellt der
/// Fenstermanager, nicht WPF. WPF kennt dafür keine Eigenschaften, also führt der Weg über
/// <c>DwmSetWindowAttribute</c>.
///
/// Jede der drei Angaben ist ab einer anderen Windows-Fassung vorhanden, und ältere
/// Fassungen antworten schlicht mit einem Fehlercode, statt zu stürzen. Genau darauf baut
/// dieser Code: versuchen, den Rückgabewert ansehen, und wenn Mica nicht getragen wird,
/// bekommt das Fenster einen einfarbigen dunklen Grund.
/// </summary>
internal static class WindowEffects
{
    private const int UseImmersiveDarkMode = 20;
    private const int WindowCornerPreference = 33;
    private const int SystemBackdropType = 38;

    private const int CornerRound = 2;
    private const int BackdropMica = 2;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    /// <summary>
    /// Wendet die drei Angaben an. Liefert false, wenn Mica nicht zur Verfügung steht - dann
    /// muss das Fenster selbst für einen Hintergrund sorgen.
    /// </summary>
    public static bool Apply(Window window)
    {
        IntPtr handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return false;
        }

        // Dunkle Titelleiste. Betrifft hier nur noch den Rahmen, weil die Leiste selbst
        // gezeichnet wird - der Unterschied fällt am Fensterrand und beim Umschalten auf.
        Set(handle, UseImmersiveDarkMode, 1);

        // Runde Ecken: Windows 11 rundet von sich aus, aber nicht bei jedem Fensterstil.
        Set(handle, WindowCornerPreference, CornerRound);

        return Set(handle, SystemBackdropType, BackdropMica);
    }

    /// <summary>Färbt das Fenster ein, wenn Mica nicht getragen wird.</summary>
    public static void ApplyFallbackBackground(Window window)
    {
        if (window.TryFindResource("WindowFallbackBrush") is Brush brush)
        {
            window.Background = brush;
        }
    }

    private static bool Set(IntPtr handle, int attribute, int value)
    {
        return DwmSetWindowAttribute(handle, attribute, ref value, sizeof(int)) == 0;
    }
}
