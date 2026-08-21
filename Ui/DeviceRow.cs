using AudioMirror.Audio;

namespace AudioMirror.Ui;

/// <summary>
/// Ein Zielgerät in der Liste: obere Zeile mit Aufklapp-Pfeil, Auswahl-Checkbox,
/// Haupt-Lautstärkeregler und Status; darunter ein aufklappbarer Bereich mit je einer Zeile
/// pro Anwendung, die gerade Ton ausgibt.
///
/// Der Aufbau leitet alle Größen aus der Schrifthöhe ab und nutzt keine festen
/// Pixelpositionen, damit auch Anzeigeskalierungen über 100 % sauber aussehen.
/// </summary>
internal sealed class DeviceRow : TableLayoutPanel
{
    private readonly TableLayoutPanel header;
    private readonly Button expander;
    private readonly PictureBox icon;
    private readonly CheckBox check;
    private readonly TrackBar volume;
    private readonly Label percent;
    private readonly Label status;
    private readonly TableLayoutPanel appPanel;
    private readonly List<AppMixRow> appRows = [];
    private bool suppressEvents;

    public DeviceRow(
        string deviceId, string name, bool isSource, AudioDeviceKind kind, bool connected, string? iconPath)
    {
        IconPath = iconPath;
        DeviceId = deviceId;
        DisplayName = name;
        IsSource = isSource;
        Connected = connected;
        Kind = kind;

        Dock = DockStyle.Top;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        ColumnCount = 1;
        RowCount = 2;
        Margin = Padding.Empty;
        Padding = new Padding(0, 2, 0, 2);
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 6,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = new Padding(4, 0, 6, 0),
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));

        expander = new Button
        {
            Text = "▶",
            AutoSize = false,
            FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleCenter,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 2, 4, 2),
            TabStop = false,
        };
        expander.FlatAppearance.BorderSize = 0;
        expander.Click += (_, _) => Expanded = !Expanded;
        // Ohne Verbindung gibt es keine Anwendungen zu zeigen.
        expander.Visible = connected;

        icon = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.Zoom,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(2, 2, 6, 2),
            BackColor = Color.Transparent,
        };

        check = new CheckBox
        {
            Text = isSource ? name + "  (Quelle)" : name,
            AutoSize = false,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Enabled = !isSource,
            UseVisualStyleBackColor = true,
        };
        check.CheckedChanged += (_, _) =>
        {
            UpdateEnabledState();
            if (!suppressEvents)
            {
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        };

        percent = new Label
        {
            Text = "100 %",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleRight,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
        };

        volume = new TrackBar
        {
            // AutoSize muss vor Height gesetzt werden, sonst erzwingt die TrackBar ihre
            // Standardhöhe und ragt in die Nachbarzeile hinein.
            AutoSize = false,
            Minimum = 0,
            Maximum = 100,
            Value = 100,
            TickStyle = TickStyle.None,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Enabled = false,
        };
        volume.ValueChanged += (_, _) =>
        {
            percent.Text = volume.Value + " %";
            if (!suppressEvents)
            {
                VolumeChanged?.Invoke(this, EventArgs.Empty);
            }
        };

        status = new Label
        {
            AutoSize = false,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = SystemColors.GrayText,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Text = isSource ? "Quelle" : string.Empty,
        };

        header.Controls.Add(expander, 0, 0);
        header.Controls.Add(icon, 1, 0);
        header.Controls.Add(check, 2, 0);
        header.Controls.Add(volume, 3, 0);
        header.Controls.Add(percent, 4, 0);
        header.Controls.Add(status, 5, 0);

        appPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Margin = Padding.Empty,
            Padding = new Padding(0, 2, 0, 4),
            Visible = false,
        };
        appPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        Controls.Add(header, 0, 0);
        Controls.Add(appPanel, 0, 1);

        ApplyMetrics();
    }

    public event EventHandler? SelectionChanged;

    public event EventHandler? VolumeChanged;

    /// <summary>Eine Anwendung dieses Geräts wurde ein-/ausgeschaltet oder in der Lautstärke verändert.</summary>
    public event EventHandler<AppMixEventArgs>? AppMixChanged;

    /// <summary>Der Bereich wurde auf- oder zugeklappt.</summary>
    public event EventHandler? ExpandedChanged;

    public string DeviceId { get; }

    /// <summary>Gerätename ohne den Zusatz "(Quelle)" - für das Tray-Menü.</summary>
    public string DisplayName { get; }

    public bool IsSource { get; }

    /// <summary>Ob das Gerät gerade angeschlossen ist.</summary>
    public bool Connected { get; }

    public AudioDeviceKind Kind { get; }

    /// <summary>Symbolverweis von Windows; leer, wenn keiner hinterlegt ist.</summary>
    public string? IconPath { get; }

    public bool Expanded
    {
        get => appPanel.Visible;
        set
        {
            if (appPanel.Visible == value)
            {
                return;
            }
            appPanel.Visible = value;
            expander.Text = value ? "▼" : "▶";
            if (!suppressEvents)
            {
                ExpandedChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public bool Selected
    {
        get => check.Checked;
        set
        {
            suppressEvents = true;
            check.Checked = value && !IsSource;
            suppressEvents = false;
            UpdateEnabledState();
        }
    }

    /// <summary>Haupt-Lautstärke des Geräts als Faktor 0..1.</summary>
    public float Volume
    {
        get => volume.Value / 100f;
        set
        {
            suppressEvents = true;
            volume.Value = Math.Clamp((int)Math.Round(value * 100f), 0, 100);
            percent.Text = volume.Value + " %";
            suppressEvents = false;
        }
    }

    /// <summary>Aktueller Zustand aller angezeigten Anwendungen.</summary>
    public IReadOnlyList<(string Key, bool Enabled, float Volume)> AppStates =>
        appRows.Select(r => (r.AppKey, r.IsEnabled, r.Volume)).ToList();

    /// <summary>
    /// True, solange der Nutzer nichts abgewählt und nichts leiser gestellt hat. Dann wird der
    /// komplette Geräteton gespiegelt - einschließlich Systemklängen, die zu keinem Prozess
    /// gehören. Sobald eine Anwendung abgehakt oder heruntergeregelt ist, greift stattdessen
    /// der gezielte Abgriff je Anwendung, denn nur der lässt sich einzeln steuern.
    /// </summary>
    public bool UsesWholeDevice => appRows.All(r => r.IsEnabled && r.Volume >= 0.999f);

    /// <summary>Die für dieses Gerät eingeschalteten Anwendungen samt Lautstärke.</summary>
    public IReadOnlyList<MirrorAppTarget> EnabledApps =>
        appRows.Where(r => r.IsEnabled).Select(r => new MirrorAppTarget(r.AppKey, r.Volume)).ToList();

    /// <summary>
    /// Gleicht die angezeigten Anwendungen mit der aktuellen Liste ab. Die Steuerelemente
    /// werden nur dann neu aufgebaut, wenn sich die Menge der Anwendungen wirklich geändert
    /// hat - sonst würde die Liste bei jedem Takt flackern.
    /// </summary>
    public void UpdateApps(IReadOnlyList<AudioAppInfo> apps, Func<string, (bool Enabled, float Volume)> lookup)
    {
        bool sameSet = apps.Count == appRows.Count
            && apps.All(a => appRows.Any(r => r.AppKey.Equals(a.Key, StringComparison.OrdinalIgnoreCase)));

        if (sameSet)
        {
            foreach (AudioAppInfo app in apps)
            {
                appRows.First(r => r.AppKey.Equals(app.Key, StringComparison.OrdinalIgnoreCase))
                    .SetDisplayName(app.Name);
            }
            return;
        }

        appPanel.SuspendLayout();
        foreach (AppMixRow row in appRows)
        {
            appPanel.Controls.Remove(row);
            row.Dispose();
        }
        appRows.Clear();
        appPanel.RowStyles.Clear();
        appPanel.RowCount = Math.Max(1, apps.Count);

        for (int i = 0; i < apps.Count; i++)
        {
            AudioAppInfo app = apps[i];
            var row = new AppMixRow(app.Key, app.Name);
            (bool enabled, float appVolume) = lookup(app.Key);
            row.Volume = appVolume;
            row.IsEnabled = enabled;
            row.Changed += (_, e) => AppMixChanged?.Invoke(this, e);

            appRows.Add(row);
            appPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            appPanel.Controls.Add(row, 0, i);
        }

        if (apps.Count == 0)
        {
            var empty = new Label
            {
                Text = "Zurzeit gibt keine Anwendung Ton aus.",
                AutoSize = false,
                Height = Font.Height + 6,
                ForeColor = SystemColors.GrayText,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(34, 2, 6, 2),
            };
            appPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            appPanel.Controls.Add(empty, 0, 0);
        }

        appPanel.ResumeLayout();
    }

    /// <summary>Hinweis an einer einzelnen Anwendungszeile, z. B. wenn sie nicht abgreifbar ist.</summary>
    public void SetAppStatus(string appKey, string text, bool isError)
    {
        appRows.FirstOrDefault(r => r.AppKey.Equals(appKey, StringComparison.OrdinalIgnoreCase))
            ?.SetStatus(text, isError);
    }

    public void SetStatus(string text, bool isError)
    {
        if (status.Text != text)
        {
            status.Text = text;
        }
        Color colour = isError ? Color.Firebrick : SystemColors.GrayText;
        if (status.ForeColor != colour)
        {
            status.ForeColor = colour;
        }
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        ApplyMetrics();
    }

    private void ApplyMetrics()
    {
        int iconSize = Math.Max(18, Font.Height + 4);
        icon.Size = new Size(iconSize, iconSize);
        icon.Image = DeviceIcons.Get(IconPath, Kind, iconSize, dimmed: !Connected);

        int line = Font.Height;
        expander.Width = Math.Max(22, line + 6);
        expander.Height = Math.Max(22, line + 6);
        volume.Width = Math.Max(120, line * 9);
        volume.Height = Math.Max(24, line + 8);
        percent.Width = line * 4;
        header.MinimumSize = new Size(0, line + 12);
    }

    private void UpdateEnabledState() => volume.Enabled = check.Checked && !IsSource;
}
