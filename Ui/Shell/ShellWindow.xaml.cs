using System.Windows;

namespace AudioMirror.Ui.Shell;

/// <summary>
/// Das Fensterskelett: eigene Titelleiste, Mica-Grund, runde Ecken. Den Inhalt setzt
/// <see cref="SetContent"/> ein - dieses Fenster weiß nichts davon, was gespiegelt wird.
/// </summary>
public partial class ShellWindow : Window
{
    public ShellWindow()
    {
        InitializeComponent();

        // Mica lässt sich erst setzen, wenn das Fenster ein Handle hat.
        SourceInitialized += (_, _) =>
        {
            if (!WindowEffects.Apply(this))
            {
                WindowEffects.ApplyFallbackBackground(this);
            }
        };

        StateChanged += (_, _) => UpdateMaximizeGlyph();
    }

    public void SetContent(UIElement content)
    {
        ContentHost.Children.Clear();
        ContentHost.Children.Add(content);
    }

    private void OnMinimize(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnMaximize(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnClose(object sender, RoutedEventArgs e) => Close();

    /// <summary>Wiederherstellen und Maximieren teilen sich die Taste, also auch die Glyphe.</summary>
    private void UpdateMaximizeGlyph() =>
        MaximizeButton.Content = WindowState == WindowState.Maximized ? "" : "";
}
