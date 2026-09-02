using System.Runtime.InteropServices;
using AudioMirror.Ui;

namespace AudioMirror;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
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
