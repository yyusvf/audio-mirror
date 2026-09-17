using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using AudioMirror.Ui;

namespace AudioMirror;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        // Vorschau der neuen Oberfläche, solange sie entsteht. Bewusst vor der Instanzsperre:
        // so lässt sie sich ansehen, während die laufende Fassung weiterspiegelt.
        if (args.Any(a => string.Equals(a, "--newui", StringComparison.OrdinalIgnoreCase)))
        {
            Strings.Configure(AppSettings.Load().Language);
            Ui.Shell.WpfHost.Ensure();

            var preview = new Ui.Shell.ShellWindow();
#if DEBUG
            preview.SetContent(new Ui.Views.DevicesPage
            {
                DataContext = Ui.Shell.DesignData.Devices(),
            });
#endif
            preview.Show();
            System.Windows.Threading.Dispatcher.Run();
            return;
        }

#if DEBUG
        // Nur zum Ansehen während der Arbeit an der Oberfläche: zeichnet das Fenster in eine
        // PNG-Datei, ohne es auf den Bildschirm zu legen.
        int renderAt = Array.FindIndex(args, a => string.Equals(a, "--render", StringComparison.OrdinalIgnoreCase));
        if (renderAt >= 0 && renderAt + 1 < args.Length)
        {
            Strings.Configure(AppSettings.Load().Language);
            Ui.Shell.WpfHost.Ensure();

            var shell = new Ui.Shell.ShellWindow();
            shell.SetContent(new Ui.Views.DevicesPage
            {
                DataContext = Ui.Shell.DesignData.Devices(),
            });

            Ui.Shell.DesignRender.Capture(shell, args[renderAt + 1], 720, 560);
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
        Application.ThreadException += (_, e) => ShowFatal(e.Exception);
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

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm(startedQuietly));
        SingleInstance.Release();
    }

    private static void ShowFatal(Exception? ex)
    {
        MessageBox.Show(
            Strings.UnexpectedError(ex?.Message ?? Strings.Unknown),
            Strings.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int RegisterApplicationRestart(string? commandLine, int flags);
}
