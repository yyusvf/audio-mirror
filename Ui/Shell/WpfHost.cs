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
    public static Application Ensure()
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

        // Reihenfolge zählt: Tokens zuerst, danach alles, was sich darauf beruft.
        foreach (string path in new[]
        {
            "Ui/Theme/Tokens.xaml",
            "Ui/Theme/Controls.xaml",
            "Ui/Theme/Inputs.xaml",
        })
        {
            application.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri($"pack://application:,,,/{path}", UriKind.Absolute),
            });
        }

        return application;
    }
}
