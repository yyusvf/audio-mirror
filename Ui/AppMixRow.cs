namespace AudioMirror.Ui;

internal sealed class AppMixEventArgs(string appKey, bool enabled, float volume) : EventArgs
{
    public string AppKey { get; } = appKey;

    public bool Enabled { get; } = enabled;

    public float Volume { get; } = volume;
}

/// <summary>
/// Eine Anwendung innerhalb des aufgeklappten Bereichs eines Zielgeräts: An/Aus-Schalter,
/// Name, eigener Lautstärkeregler und Platz für einen Hinweis.
/// </summary>
internal sealed class AppMixRow : TableLayoutPanel
{
    private readonly CheckBox toggle;
    private readonly TrackBar volume;
    private readonly Label percent;
    private readonly Label status;
    private bool suppressEvents;

    public AppMixRow(string appKey, string displayName)
    {
        AppKey = appKey;

        Dock = DockStyle.Top;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        ColumnCount = 4;
        RowCount = 1;
        Margin = Padding.Empty;
        // Deutlich eingerückt, damit die Zugehörigkeit zum Gerät darüber sichtbar bleibt.
        Padding = new Padding(34, 1, 6, 1);
        BackColor = Color.Transparent;

        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));

        toggle = new CheckBox
        {
            Text = displayName,
            AutoSize = false,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Checked = true,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            UseVisualStyleBackColor = true,
            Margin = new Padding(0, 2, 8, 2),
        };
        toggle.CheckedChanged += (_, _) =>
        {
            UpdateToggleLook();
            Raise();
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
            AutoSize = false,
            Minimum = 0,
            Maximum = 100,
            Value = 100,
            TickStyle = TickStyle.None,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
        };
        volume.ValueChanged += (_, _) =>
        {
            percent.Text = volume.Value + " %";
            Raise();
        };

        status = new Label
        {
            AutoSize = false,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = SystemColors.GrayText,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
        };

        Controls.Add(toggle, 0, 0);
        Controls.Add(volume, 1, 0);
        Controls.Add(percent, 2, 0);
        Controls.Add(status, 3, 0);

        ApplyMetrics();
        UpdateToggleLook();
    }

    public event EventHandler<AppMixEventArgs>? Changed;

    public string AppKey { get; }

    public bool IsEnabled
    {
        get => toggle.Checked;
        set
        {
            suppressEvents = true;
            toggle.Checked = value;
            suppressEvents = false;
            UpdateToggleLook();
        }
    }

    /// <summary>Lautstärke dieser Anwendung als Faktor 0..1.</summary>
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

    public void SetDisplayName(string text)
    {
        if (toggle.Text != text)
        {
            toggle.Text = text;
        }
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
        int line = Font.Height;
        toggle.Height = Math.Max(22, line + 6);
        volume.Width = Math.Max(100, line * 8);
        volume.Height = Math.Max(22, line + 6);
        percent.Width = line * 4;
    }

    private void UpdateToggleLook()
    {
        toggle.ForeColor = toggle.Checked ? SystemColors.ControlText : SystemColors.GrayText;
        volume.Enabled = toggle.Checked;
    }

    private void Raise()
    {
        if (!suppressEvents)
        {
            Changed?.Invoke(this, new AppMixEventArgs(AppKey, IsEnabled, Volume));
        }
    }
}
