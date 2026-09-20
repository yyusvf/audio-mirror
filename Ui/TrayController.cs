using System.Drawing;
using System.Windows.Forms;
using AudioMirror;
using AudioMirror.Ui.Theme;
using System.Runtime.InteropServices;

namespace AudioMirror.Ui;

/// <summary>Ein Eintrag der Geräteliste im Tray-Menü.</summary>
internal sealed record TrayDeviceEntry(string Id, string Name, bool Enabled, bool IsSource);

/// <summary>
/// Symbol im Infobereich samt Kontextmenü zur Schnellsteuerung.
///
/// Die Geräteliste wird bei jedem Öffnen des Menüs frisch über <see cref="DeviceProvider"/>
/// abgefragt, damit sie immer mit dem Hauptfenster übereinstimmt - auch wenn zwischenzeitlich
/// ein Gerät ein- oder ausgesteckt wurde.
/// </summary>
internal sealed class TrayController : IDisposable
{
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr handle);

    private readonly NotifyIcon notifyIcon;
    private readonly ContextMenuStrip menu = new();
    private readonly IntPtr iconHandle;
    private string tooltip = Strings.AppTitle;
    private bool disposed;

    public TrayController()
    {
        Icon = CreateIcon(out iconHandle);

        // Das Menü zeichnet sich über einen Renderer; der eingebaute holt seine Farben aus
        // der Systemtabelle und wäre hell. Text und Grund kommen zusätzlich direkt ans Menü,
        // weil beides an den Einträgen hängt und nicht an der Farbtabelle.
        menu.RenderMode = ToolStripRenderMode.Professional;
        menu.Renderer = new DarkMenuRenderer();
        menu.BackColor = Palette.Window;
        menu.ForeColor = Palette.Text;

        notifyIcon = new NotifyIcon
        {
            Icon = Icon,
            Text = tooltip,
            Visible = true,
            ContextMenuStrip = menu,
        };

        // Das Menü wird beim Drücken der rechten Taste aufgebaut - also bevor NotifyIcon es
        // anzeigt. Zuvor geschah das im Opening-Ereignis: wer dort sämtliche Einträge austauscht,
        // bekommt beim ersten Klick ein Menü, das Windows sofort wieder verwirft. Genau daher
        // musste man zweimal klicken.
        notifyIcon.MouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Right)
            {
                RebuildMenu();
            }
        };

        // Der Doppelklick tut, was in den Einstellungen dafür hinterlegt ist. Der einfache
        // Klick bleibt bewusst folgenlos - sonst käme ihm der erste Klick eines Doppelklicks
        // stets zuvor und die Einstellung wäre wirkungslos. Für die rechte Maustaste zeigt
        // NotifyIcon von sich aus das Kontextmenü.
        notifyIcon.DoubleClick += (_, _) =>
        {
            switch (DoubleClickActionProvider?.Invoke() ?? TrayAction.OpenWindow)
            {
                case TrayAction.OpenWindow:
                    ShowWindowRequested?.Invoke();
                    break;
                case TrayAction.ToggleMirroring:
                    ToggleRequested?.Invoke();
                    break;
            }
        };
    }

    /// <summary>Liefert die aktuell anzuzeigenden Geräte. Wird beim Öffnen des Menüs aufgerufen.</summary>
    public Func<IReadOnlyList<TrayDeviceEntry>>? DeviceProvider { get; set; }

    /// <summary>Was ein Doppelklick auf das Symbol auslösen soll.</summary>
    public Func<TrayAction>? DoubleClickActionProvider { get; set; }

    public event Action? ShowWindowRequested;

    /// <summary>Gesamte Spiegelung an- bzw. ausschalten.</summary>
    public event Action? ToggleRequested;

    public event Action? ExitRequested;

    /// <summary>"Mit Windows starten" wurde im Tray-Menü angeklickt.</summary>
    public event Action? AutostartToggled;

    /// <summary>Gerät wurde im Tray-Menü an- oder abgehakt.</summary>
    public event Action<string, bool>? DeviceToggled;

    /// <summary>Symbol, das auch das Hauptfenster verwendet.</summary>
    public Icon Icon { get; }

    /// <summary>Kurztext beim Überfahren des Symbols (Windows erlaubt maximal 63 Zeichen).</summary>
    public void SetTooltip(string text)
    {
        string trimmed = text.Length > 63 ? text[..60] + "..." : text;
        if (trimmed == tooltip)
        {
            return;
        }
        tooltip = trimmed;
        notifyIcon.Text = trimmed;
    }

    public void ShowHint(string title, string message)
    {
        notifyIcon.BalloonTipTitle = title;
        notifyIcon.BalloonTipText = message;
        notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
        notifyIcon.ShowBalloonTip(4000);
    }

    private void RebuildMenu()
    {
        menu.Items.Clear();

        IReadOnlyList<TrayDeviceEntry> devices = DeviceProvider?.Invoke() ?? [];
        if (devices.Count == 0)
        {
            menu.Items.Add(Style(new ToolStripMenuItem(Strings.NoOutputDevices) { Enabled = false }));
        }

        foreach (TrayDeviceEntry device in devices)
        {
            var item = new ToolStripMenuItem(device.IsSource ? device.Name + Strings.SourceSuffix : device.Name)
            {
                Checked = device.Enabled,
                CheckOnClick = false,
                // Das Quellgerät gibt bereits nativ aus und würde als Ziel rückkoppeln.
                Enabled = !device.IsSource,
            };

            string id = device.Id;
            bool nowEnabled = !device.Enabled;
            item.Click += (_, _) => DeviceToggled?.Invoke(id, nowEnabled);
            menu.Items.Add(Style(item));
        }

        menu.Items.Add(new ToolStripSeparator());

        // Der Haken wird bei jedem Öffnen frisch aus der Registrierung gelesen: so stimmt er
        // auch dann, wenn der Autostart anderswo geändert wurde - etwa im Task-Manager.
        var autostart = new ToolStripMenuItem(Strings.StartWithWindowsShort)
        {
            Checked = Autostart.IsEnabled(),
            CheckOnClick = false,
            Enabled = Autostart.IsSupported,
        };
        autostart.Click += (_, _) => AutostartToggled?.Invoke();
        menu.Items.Add(Style(autostart));

        var open = new ToolStripMenuItem(Strings.OpenWindow);
        open.Click += (_, _) => ShowWindowRequested?.Invoke();
        menu.Items.Add(Style(open));

        var exit = new ToolStripMenuItem(Strings.Exit);
        exit.Click += (_, _) => ExitRequested?.Invoke();
        menu.Items.Add(Style(exit));
    }

    /// <summary>
    /// Farben an einem Menüeintrag. Die Farbtabelle des Renderers deckt nur Flächen ab; den
    /// Text holt sich jeder Eintrag selbst, und ein abgeschalteter nimmt dafür den Grauton des
    /// Systems - auf dunklem Grund kaum zu sehen.
    /// </summary>
    private static ToolStripMenuItem Style(ToolStripMenuItem item)
    {
        item.BackColor = Palette.Window;
        item.ForeColor = item.Enabled ? Palette.Text : Palette.TextDisabled;
        return item;
    }

    /// <summary>
    /// Das Symbol im Infobereich - dieselbe Zeichnung wie Taskleiste, Fenster und Titelleiste,
    /// siehe <see cref="AppIconArt"/>.
    /// </summary>
    private static Icon CreateIcon(out IntPtr handle)
    {
        using Bitmap bitmap = AppIconArt.Render(32);
        handle = bitmap.GetHicon();
        return Icon.FromHandle(handle);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }
        disposed = true;

        // Ohne explizites Ausblenden bleibt das Symbol als Leiche im Infobereich stehen,
        // bis der Nutzer mit der Maus darüberfährt.
        notifyIcon.Visible = false;
        notifyIcon.Dispose();
        menu.Dispose();
        Icon.Dispose();
        DestroyIcon(handle: iconHandle);
    }
}
