// Solange die alte Oberfläche noch im Projekt liegt, gibt es Application zweimal - einmal aus
// WinForms, einmal aus WPF. Der Alias hält hier fest, welches gemeint ist.
using Application = System.Windows.Application;
using ResourceDictionary = System.Windows.ResourceDictionary;
using ShutdownMode = System.Windows.ShutdownMode;

namespace AudioMirror.Ui.Shell;

/// <summary>
/// Erzeugt die WPF-Anwendung von Hand.
///
/// Üblicherweise übernimmt das eine App.xaml, die sich ihr eigenes Main erzeugen lässt. Hier
/// bleibt <see cref="Program"/> der Einstiegspunkt, weil daran die Instanzsperre, der
/// Neustart-Vermerk und der stille Start hängen - also werden Anwendung und Stilvorrat hier
/// zusammengesetzt.
/// </summary>
internal static class WpfHost
{
    private static Application? application;

    /// <summary>Die laufende WPF-Anwendung, bei Bedarf samt Stilvorrat erzeugt.</summary>
    public static Application Ensure(ThemeMode theme = ThemeMode.System)
    {
        if (application != null)
        {
            return application;
        }

        application = new Application
        {
            // Das Fenster schließt in den Infobereich, nicht aus dem Programm heraus.
            // Über das Ende entscheidet allein der Eintrag "Beenden" im Infobereich.
            ShutdownMode = ShutdownMode.OnExplicitShutdown,
        };

        // Reihenfolge zählt. Die Maße stehen vorn, weil die Vorlagen sie über StaticResource
        // holen - das wird beim Laden aufgelöst und braucht den Wert vorher. Die Farben holen
        // sie über DynamicResource, das sucht zur Laufzeit im ganzen Vorrat; auf welchem Platz
        // der Farbsatz liegt, ist dafür gleich. Dass er trotzdem einen festen Platz hat, liegt
        // am Umschalten: ThemeManager tauscht genau diesen aus.
        foreach (string path in new[]
        {
            "Ui/Theme/Metrics.xaml",
            "Ui/Theme/Controls.xaml",
            "Ui/Theme/Inputs.xaml",
        })
        {
            application.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri($"pack://application:,,,/{path}", UriKind.Absolute),
            });
        }

        // Erst nach den Wörterbüchern: die Akzent-Pinsel müssen stehen, bevor sie gefärbt
        // werden können. Der Farbsatz kommt dabei an seinen Platz.
        Theme.SystemAccent.Attach(application);
        Theme.ThemeManager.Attach(application, theme);

        return application;
    }
}
