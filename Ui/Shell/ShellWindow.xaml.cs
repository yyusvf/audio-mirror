using System.Windows;

namespace AudioMirror.Ui.Shell;

/// <summary>
/// Das Fensterskelett: eigene Titelleiste mit den beiden Reitern, Mica-Grund, runde Ecken.
/// Was in den Reitern steht, setzt <see cref="SetPages"/> ein - dieses Fenster weiß nichts
/// davon, was gespiegelt wird.
/// </summary>
public partial class ShellWindow : Window
{
    private UIElement? devicesPage;
    private UIElement? settingsPage;

    public ShellWindow()
    {
        InitializeComponent();

        DevicesTab.Content = Strings.TabDevices;
        SettingsTab.Content = Strings.TabSettings;

        // Titelleiste und runde Ecken greifen erst, wenn das Fenster ein Handle hat.
        SourceInitialized += (_, _) => ApplyWindowEffects();

        // Der Farbsatz kann sich im laufenden Betrieb ändern - über die Einstellungen oder,
        // wenn Windows gefolgt wird, von außen. Der Rahmen zieht dann mit.
        Theme.ThemeManager.Changed += ApplyWindowEffects;
        Closed += (_, _) => Theme.ThemeManager.Changed -= ApplyWindowEffects;

        StateChanged += (_, _) => UpdateMaximizeGlyph();
    }

    /// <summary>Das Schließkreuz beendet nicht - darüber entscheidet, wer zuhört.</summary>
    public event Action? CloseRequested;

    /// <summary>Hinterlegt die beiden Seiten und zeigt die Geräteseite an.</summary>
    public void SetPages(UIElement devices, UIElement settings)
    {
        devicesPage = devices;
        settingsPage = settings;
        ShowDevices();
    }

    public void ShowDevices()
    {
        DevicesTab.IsChecked = true;
        Swap(devicesPage);
    }

    /// <summary>Einzelne Seite ohne Reiter - für die Vorschau während der Arbeit.</summary>
    public void SetContent(UIElement content)
    {
        ContentHost.Children.Clear();
        ContentHost.Children.Add(content);
    }

    private void Swap(UIElement? page)
    {
        if (page == null)
        {
            return;
        }

        ContentHost.Children.Clear();
        ContentHost.Children.Add(page);
    }

    private void OnDevicesTab(object sender, RoutedEventArgs e) => Swap(devicesPage);

    private void OnSettingsTab(object sender, RoutedEventArgs e) => Swap(settingsPage);

    private void OnMinimize(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnMaximize(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnClose(object sender, RoutedEventArgs e) => CloseRequested?.Invoke();

    private void ApplyWindowEffects() => WindowEffects.Apply(this, Theme.ThemeManager.IsDark);

    /// <summary>Wiederherstellen und Maximieren teilen sich die Taste, also auch die Glyphe.</summary>
    private void UpdateMaximizeGlyph() =>
        MaximizeButton.Content = WindowState == WindowState.Maximized ? "" : "";
}
