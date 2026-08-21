using System.Drawing.Drawing2D;
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
    private string tooltip = "Audio Mirror";
    private bool disposed;

    public TrayController()
    {
        Icon = CreateIcon(out iconHandle);

        menu.Opening += (_, _) => RebuildMenu();

        notifyIcon = new NotifyIcon
        {
            Icon = Icon,
            Text = tooltip,
            Visible = true,
            ContextMenuStrip = menu,
        };

        // Linksklick und Doppelklick holen das Fenster zurück; das Kontextmenü übernimmt
        // NotifyIcon selbst für die rechte Maustaste.
        notifyIcon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                ShowWindowRequested?.Invoke();
            }
        };
        notifyIcon.DoubleClick += (_, _) => ShowWindowRequested?.Invoke();
    }

    /// <summary>Liefert die aktuell anzuzeigenden Geräte. Wird beim Öffnen des Menüs aufgerufen.</summary>
    public Func<IReadOnlyList<TrayDeviceEntry>>? DeviceProvider { get; set; }

    public event Action? ShowWindowRequested;

    public event Action? ExitRequested;

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

        var header = new ToolStripMenuItem("Zielgeräte") { Enabled = false };
        menu.Items.Add(header);

        IReadOnlyList<TrayDeviceEntry> devices = DeviceProvider?.Invoke() ?? [];
        if (devices.Count == 0)
        {
            menu.Items.Add(new ToolStripMenuItem("Keine Ausgabegeräte gefunden") { Enabled = false });
        }

        foreach (TrayDeviceEntry device in devices)
        {
            var item = new ToolStripMenuItem(device.IsSource ? device.Name + "  (Quelle)" : device.Name)
            {
                Checked = device.Enabled,
                CheckOnClick = false,
                // Das Quellgerät gibt bereits nativ aus und würde als Ziel rückkoppeln.
                Enabled = !device.IsSource,
            };

            string id = device.Id;
            bool nowEnabled = !device.Enabled;
            item.Click += (_, _) => DeviceToggled?.Invoke(id, nowEnabled);
            menu.Items.Add(item);
        }

        menu.Items.Add(new ToolStripSeparator());
        var open = new ToolStripMenuItem("Fenster öffnen");
        open.Click += (_, _) => ShowWindowRequested?.Invoke();
        menu.Items.Add(open);

        var exit = new ToolStripMenuItem("Beenden");
        exit.Click += (_, _) => ExitRequested?.Invoke();
        menu.Items.Add(exit);
    }

    /// <summary>
    /// Zeichnet das Symbol zur Laufzeit - ein Punkt mit zwei abgehenden Wellen. Dadurch braucht
    /// das Projekt keine mitgelieferte .ico-Datei, und das Ergebnis bleibt bei jeder
    /// Anzeigeskalierung sauber.
    /// </summary>
    private static Icon CreateIcon(out IntPtr handle)
    {
        using var bitmap = new Bitmap(32, 32);
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            var colour = Color.FromArgb(0x33, 0x9A, 0xF0);
            using var brush = new SolidBrush(colour);
            using var pen = new Pen(colour, 3.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };

            g.FillEllipse(brush, 3, 12, 9, 9);
            g.DrawArc(pen, 8, 8, 17, 17, -62, 124);
            g.DrawArc(pen, 4, 2, 27, 27, -62, 124);
        }

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
