using System.Runtime.InteropServices;
using AudioMirror.Ui.Shell;

namespace AudioMirror;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
#if DEBUG
        // Schreibt AudioMirror.ico neu aus der Zeichnung in AppIconArt - aufzurufen, wenn
        // sich die Form aendert.
        int iconAt = Array.FindIndex(args, a => string.Equals(a, "--makeicon", StringComparison.OrdinalIgnoreCase));
        if (iconAt >= 0 && iconAt + 1 < args.Length)
        {
            Ui.AppIconFile.Write(args[iconAt + 1]);
            return;
        }

        // Nur zum Ansehen während der Arbeit an der Oberfläche: zeichnet das Fenster in eine
        // PNG-Datei, ohne es auf den Bildschirm zu legen.
        int renderAt = Array.FindIndex(args, a => string.Equals(a, "--render", StringComparison.OrdinalIgnoreCase));
        if (renderAt >= 0 && renderAt + 1 < args.Length)
        {
            Strings.Configure(AppSettings.Load().Language);
            WpfHost.Ensure();

            var shell = new ShellWindow();
            bool wantsSettings = args.Any(a => string.Equals(a, "settings", StringComparison.OrdinalIgnoreCase));
            shell.SetContent(wantsSettings
                ? new Ui.Views.SettingsPage { DataContext = new Ui.ViewModels.SettingsPageViewModel(AppSettings.Load()) }
                : new Ui.Views.DevicesPage { DataContext = DesignData.Devices() });

            DesignRender.Capture(shell, args[renderAt + 1], 720, 560);
            return;
        }
#endif

        bool byAutostartEntry = args.Any(a =>
            string.Equals(a, Autostart.MinimizedArgument, StringComparison.OrdinalIgnoreCase));

        // Läuft bereits eine Instanz? Dann sich selbst beenden. Bei einem Start von Hand wird
        // zusätzlich deren Fenster geholt - der Doppelklick soll ja etwas bewirken. Ein stiller
        // Start (Autostart) bleibt dagegen stumm.
        if (!SingleInstance.TryAcquire())
        {
            if (!byAutostartEntry)
            {
                SingleInstance.SignalExistingInstance();
            }
            return;
        }

        // Die Sprache steht vor dem ersten Text fest: Windows-Sprache, sofern nichts anderes
        // eingestellt ist.
        Strings.Configure(AppSettings.Load().Language);

        // Spec 4.5: kein Absturz bei unerwarteten Fehlern - stattdessen verständliche Meldung.
        AppDomain.CurrentDomain.UnhandledException += (_, e) => ShowFatal(e.ExceptionObject as Exception);

        // Windows startet Programme nach der Anmeldung teils von sich aus wieder ("Apps nach der
        // Anmeldung neu starten") - dabei ohne jedes Argument. Hiermit hinterlegen wir die
        // Befehlszeile, die Windows in so einem Fall verwenden soll.
        try
        {
            RegisterApplicationRestart(Autostart.MinimizedArgument, 0);
        }
        catch (EntryPointNotFoundException)
        {
            // Auf älteren Systemen nicht vorhanden - unkritisch.
        }

        // Ohne Fenster starten, wenn der Autostart-Eintrag das so vorgibt - oder wenn Windows
        // das Programm zuletzt selbst beendet hat und es jetzt wiederherstellt. Ein Start von
        // Hand öffnet dagegen ganz normal das Fenster.
        bool restoredByWindows = StartupState.ConsumeStoppedByWindows();
        bool startedQuietly = byAutostartEntry || restoredByWindows;

        System.Windows.Application application = WpfHost.Ensure();
        application.DispatcherUnhandledException += (_, e) =>
        {
            ShowFatal(e.Exception);
            e.Handled = true;
        };

        using var controller = new MirrorController(startedQuietly);
        application.Run();

        SingleInstance.Release();
    }

    private static void ShowFatal(Exception? ex)
    {
        System.Windows.MessageBox.Show(
            Strings.UnexpectedError(ex?.Message ?? Strings.Unknown),
            Strings.AppTitle,
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Error);
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int RegisterApplicationRestart(string? commandLine, int flags);
}
