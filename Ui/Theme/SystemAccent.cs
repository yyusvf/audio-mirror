using System.Runtime.InteropServices;
using System.Windows.Media;
using Microsoft.Win32;
using Application = System.Windows.Application;

namespace AudioMirror.Ui.Theme;

/// <summary>
/// Färbt die Akzent-Pinsel aus Tokens.xaml mit der Akzentfarbe, die in den Windows-
/// Einstellungen steht. Betroffen ist alles, was in der Oberfläche farbig ist: Haken,
/// Schieberegler, Auswahlpunkte, der aktive Reiter, angehakte Zeilen.
///
/// Das Blau in Tokens.xaml bleibt als Rückfall stehen - es ist die Farbe des Programms und
/// greift, wenn sich die Systemfarbe nicht lesen lässt.
///
/// Die Pinsel werden ausgetauscht, nicht umgefärbt: WPF friert Pinsel ein, die aus einem
/// Wörterbuch kommen, und ein eingefrorener Pinsel lässt sich nicht mehr ändern. Der neue
/// kommt in den eigenen Vorrat der Anwendung, der vor den eingebundenen Wörterbüchern liegt.
/// Damit er dort auch ankommt, holen die Vorlagen ihn über DynamicResource - eine
/// StaticResource merkt sich den Pinsel selbst und nicht den Schlüssel.
/// </summary>
internal static class SystemAccent
{
    /// <summary>
    /// Helligkeit, die der Akzent auf dem dunklen Grund mindestens haben muss. Windows geht
    /// im dunklen Modus genauso vor und nimmt eine aufgehellte Stufe statt des Volltons -
    /// ein dunkelblauer Akzent wäre auf #202020 sonst kaum vom Hintergrund zu unterscheiden.
    /// </summary>
    private const double MinimumLuminance = 0.30;

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetColorizationColor(out uint colour, [MarshalAs(UnmanagedType.Bool)] out bool opaque);

    /// <summary>Setzt die Farben und hält sie nach, wenn der Nutzer sie in Windows ändert.</summary>
    public static void Attach(Application application)
    {
        Apply(application);

        // Die Akzentfarbe steckt in den Personalisierungs-Einstellungen; Windows meldet deren
        // Änderung über dieselbe Nachricht wie den Wechsel zwischen hellem und dunklem Modus.
        SystemEvents.UserPreferenceChanged += (_, e) =>
        {
            if (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.Color)
            {
                application.Dispatcher.BeginInvoke(() => Apply(application));
            }
        };
    }

    private static void Apply(Application application)
    {
        if (Read() is not Color system)
        {
            return;
        }

        Color accent = Lighten(system, MinimumLuminance);

        Set(application, "AccentBrush", accent);
        Set(application, "AccentHoverBrush", Blend(accent, Colors.White, 0.18));
        Set(application, "AccentSoftBrush", Color.FromArgb(0x26, accent.R, accent.G, accent.B));

        // Was auf der Akzentfläche liegt, richtet sich nach ihr: von Weiß und dem Fensterton
        // gewinnt, was sich stärker davon abhebt. Eine feste Schwelle auf der Helligkeit wäre
        // bei mittleren Tönen ein Münzwurf - gerade dort, wo es am ehesten schiefgeht.
        Color dark = Color.FromRgb(0x20, 0x20, 0x20);
        Set(application, "OnAccentBrush",
            Contrast(accent, dark) >= Contrast(accent, Colors.White) ? dark : Colors.White);
    }

    private static void Set(Application application, string key, Color colour)
    {
        var brush = new SolidColorBrush(colour);
        brush.Freeze();
        application.Resources[key] = brush;
    }

    /// <summary>
    /// Die eingestellte Akzentfarbe. Zuerst aus der Registrierung, wo sie als ABGR steht -
    /// also mit vertauschtem Rot und Blau gegenüber der üblichen Schreibweise. Hilfsweise
    /// über den Fenstermanager, der dieselbe Farbe als ARGB herausgibt.
    /// </summary>
    private static Color? Read()
    {
        try
        {
            if (Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM", "AccentColor", null)
                is int stored)
            {
                uint abgr = unchecked((uint)stored);
                return Color.FromRgb((byte)(abgr & 0xFF), (byte)((abgr >> 8) & 0xFF), (byte)((abgr >> 16) & 0xFF));
            }
        }
        catch
        {
            // Wert nicht lesbar - dann eben über den Fenstermanager.
        }

        if (DwmGetColorizationColor(out uint argb, out _) == 0)
        {
            return Color.FromRgb((byte)((argb >> 16) & 0xFF), (byte)((argb >> 8) & 0xFF), (byte)(argb & 0xFF));
        }

        return null;
    }

    /// <summary>Mischt so viel Weiß unter, bis die geforderte Helligkeit erreicht ist.</summary>
    private static Color Lighten(Color colour, double target)
    {
        Color result = colour;
        for (int i = 0; i < 8 && Luminance(result) < target; i++)
        {
            result = Blend(result, Colors.White, 0.15);
        }
        return result;
    }

    private static Color Blend(Color from, Color to, double amount) => Color.FromRgb(
        (byte)(from.R + (to.R - from.R) * amount),
        (byte)(from.G + (to.G - from.G) * amount),
        (byte)(from.B + (to.B - from.B) * amount));

    /// <summary>
    /// Wahrgenommene Helligkeit nach sRGB. Der Mittelwert der drei Kanäle taugt dafür nicht:
    /// Grün wiegt für das Auge weit schwerer als Blau.
    /// </summary>
    private static double Luminance(Color colour) =>
        0.2126 * Linear(colour.R) + 0.7152 * Linear(colour.G) + 0.0722 * Linear(colour.B);

    /// <summary>
    /// Verhältnis der Helligkeiten zweier Farben, wie es die Barrierefreiheits-Richtlinien
    /// festlegen: von 1 (nicht zu unterscheiden) bis 21 (Schwarz auf Weiß).
    /// </summary>
    private static double Contrast(Color a, Color b)
    {
        double first = Luminance(a);
        double second = Luminance(b);
        return first > second
            ? (first + 0.05) / (second + 0.05)
            : (second + 0.05) / (first + 0.05);
    }

    private static double Linear(byte channel)
    {
        double v = channel / 255.0;
        return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
    }
}
