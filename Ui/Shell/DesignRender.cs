#if DEBUG
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AudioMirror.Ui.Shell;

/// <summary>
/// Zeichnet ein Fenster in eine PNG-Datei, ohne es sichtbar zu machen.
///
/// Gedacht für die Arbeit an der Oberfläche: das Fenster entsteht weit außerhalb des
/// Bildschirms, wird ausgemessen, abgezeichnet und wieder geschlossen. So lässt sich das
/// Ergebnis ansehen, ohne jemandem ein Fenster vor die laufende Arbeit zu setzen.
///
/// Was hier fehlt, ist alles, was der Fenstermanager beisteuert: Mica, der Schlagschatten
/// und die runden Ecken entstehen erst beim Zusammensetzen auf dem Bildschirm und tauchen in
/// dieser Aufnahme nicht auf. Beurteilen lassen sich Anordnung, Farben, Schrift und Abstände.
/// </summary>
internal static class DesignRender
{
    public static void Capture(Window window, string path, int width, int height, double scale = 1.5)
    {
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = -20000;
        window.Top = -20000;
        window.Width = width;
        window.Height = height;
        window.ShowInTaskbar = false;

        // Für die Aufnahme den Rückfallton unterlegen: Mica entsteht erst beim Zusammensetzen
        // auf dem Bildschirm, ohne Grund stünde heller Text auf durchsichtiger Fläche. Der Ton
        // kommt auf das Wurzelelement, weil abgezeichnet wird, was im Fenster steht - der
        // Fensterhintergrund selbst gehört nicht dazu.
        if (window.Content is System.Windows.Controls.Panel root
            && window.TryFindResource("WindowFallbackBrush") is Brush fallback)
        {
            root.Background = fallback;
        }

        window.Show();

        // Ohne diesen Durchlauf stehen Vorlagen und Maße noch nicht fest.
        window.UpdateLayout();
        Dispatch();

        var visual = (Visual)window.Content;
        var bounds = new Rect(0, 0, width, height);

        var target = new RenderTargetBitmap(
            (int)(width * scale), (int)(height * scale), 96 * scale, 96 * scale, PixelFormats.Pbgra32);
        target.Render(visual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(target));

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using (FileStream file = File.Create(path))
        {
            encoder.Save(file);
        }

        window.Close();
    }

    /// <summary>Lässt die Warteschlange einmal leerlaufen, damit die Anordnung wirklich steht.</summary>
    private static void Dispatch()
    {
        var frame = new System.Windows.Threading.DispatcherFrame();
        System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.ContextIdle,
            new Action(() => frame.Continue = false));
        System.Windows.Threading.Dispatcher.PushFrame(frame);
    }
}
#endif
