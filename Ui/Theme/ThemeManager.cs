using Microsoft.Win32;
using Application = System.Windows.Application;
using ResourceDictionary = System.Windows.ResourceDictionary;

namespace AudioMirror.Ui.Theme;

/// <summary>
/// Hell oder dunkel - und die Möglichkeit, das Windows zu überlassen.
///
/// Umgeschaltet wird, indem der Farbsatz im Vorrat der Anwendung gegen den anderen getauscht
/// wird. Damit das an den Bedienelementen ankommt, holen die Vorlagen ihre Farben über
/// DynamicResource; eine StaticResource merkt sich den Pinsel und nicht den Schlüssel und
/// bliebe auf dem alten stehen.
/// </summary>
internal static class ThemeManager
{
    /// <summary>
    /// Stelle des Farbsatzes unter den eingebundenen Wörterbüchern: hinter den Maßen, vor den
    /// Vorlagen. Beim ersten Mal wird er dort eingeschoben, danach nur noch ausgetauscht -
    /// ein Zuweisen beim ersten Mal würde das Wörterbuch überschreiben, das dort steht.
    /// </summary>
    private const int PaletteSlot = 1;

    private static Application? host;
    private static bool installed;

    /// <summary>Was eingestellt ist - nicht, was daraus folgt.</summary>
    public static ThemeMode Mode { get; private set; } = ThemeMode.System;

    /// <summary>Was daraus folgt: bei <see cref="ThemeMode.System"/> die Windows-Einstellung.</summary>
    public static bool IsDark { get; private set; } = true;

    /// <summary>Nach jedem Wechsel. Fenster und Infobereich hängen sich hier ein.</summary>
    public static event Action? Changed;

    public static void Attach(Application application, ThemeMode mode)
    {
        host = application;
        Mode = mode;
        IsDark = Resolve(mode);
        Swap();

        // Windows meldet den Wechsel zwischen hellem und dunklem Modus über dieselbe
        // Nachricht wie andere Personalisierungs-Einstellungen. Nachgezogen wird nur, wenn
        // wir Windows überhaupt folgen - sonst wäre die eigene Wahl nichts wert.
        SystemEvents.UserPreferenceChanged += (_, e) =>
        {
            if (e.Category is not (UserPreferenceCategory.General or UserPreferenceCategory.VisualStyle))
            {
                return;
            }

            application.Dispatcher.BeginInvoke(() =>
            {
                if (Mode == ThemeMode.System)
                {
                    SetMode(ThemeMode.System);
                }
            });
        };
    }

    public static void SetMode(ThemeMode mode)
    {
        bool dark = Resolve(mode);
        if (host == null || (mode == Mode && dark == IsDark))
        {
            Mode = mode;
            return;
        }

        Mode = mode;
        IsDark = dark;
        Swap();
        Changed?.Invoke();
    }

    private static void Swap()
    {
        if (host == null)
        {
            return;
        }

        var palette = new ResourceDictionary
        {
            Source = new Uri(
                IsDark ? "pack://application:,,,/Ui/Theme/Dark.xaml"
                       : "pack://application:,,,/Ui/Theme/Light.xaml",
                UriKind.Absolute),
        };

        // Austauschen statt anhängen: zwei Farbsätze gleichzeitig im Vorrat würden sich
        // gegenseitig überdecken, und welcher gewinnt, hinge an der Reihenfolge.
        if (!installed)
        {
            host.Resources.MergedDictionaries.Insert(PaletteSlot, palette);
            installed = true;
        }
        else
        {
            host.Resources.MergedDictionaries[PaletteSlot] = palette;
        }

        // Der Akzent liegt über dem Farbsatz und wird gegen ihn gerechnet - im Hellen
        // abgedunkelt, im Dunklen aufgehellt. Also neu bestimmen, nicht bloß stehen lassen.
        SystemAccent.Refresh();
    }

    /// <summary>
    /// Was Windows selbst verwendet. Der Wert heißt <c>AppsUseLightTheme</c> und meint die
    /// Anwendungen; daneben steht einer für die Systemoberfläche, der anders stehen darf.
    /// </summary>
    private static bool Resolve(ThemeMode mode) => mode switch
    {
        ThemeMode.Light => false,
        ThemeMode.Dark => true,
        _ => SystemPrefersDark(),
    };

    private static bool SystemPrefersDark()
    {
        try
        {
            object? value = Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "AppsUseLightTheme",
                null);

            return value is int light ? light == 0 : true;
        }
        catch
        {
            // Nicht lesbar - dann dunkel, wie bisher.
            return true;
        }
    }
}
