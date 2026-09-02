using AudioMirror;

namespace AudioMirror.Ui;

/// <summary>
/// Der Einstellungen-Reiter: Allgemeines, Ton und Aktualisierungen.
///
/// Der Aufbau leitet alle Größen aus der Schrifthöhe ab und nutzt keine festen Pixelpositionen,
/// damit auch Anzeigeskalierungen über 100 % sauber aussehen.
/// </summary>
internal sealed class SettingsPage : TableLayoutPanel
{
    private readonly AppSettings settings;
    private readonly CheckBox autoStart = new();
    private readonly ComboBox doubleClick = new();
    private readonly ComboBox language = new();
    private readonly NumericUpDown buffer = new();
    private readonly HotkeyRecorder hotkey = new();
    private readonly CheckBox hotkeyEnabled = new();
    private readonly RadioButton updateAuto = new();
    private readonly RadioButton updateNotify = new();
    private readonly RadioButton updateNever = new();
    private readonly Button checkNow = new();
    private readonly Label updateStatus = new();
    private readonly ToolTip tips = new() { AutoPopDelay = 12000, InitialDelay = 300 };
    private bool loading = true;

    public SettingsPage(AppSettings settings)
    {
        this.settings = settings;

        Dock = DockStyle.Fill;
        AutoScroll = true;
        ColumnCount = 2;
        RowCount = 2;
        Padding = new Padding(8);
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        RowStyles.Add(new RowStyle(SizeType.AutoSize));
        RowStyles.Add(new RowStyle(SizeType.AutoSize));

        Controls.Add(BuildBasic(), 0, 0);
        Controls.Add(BuildUpdates(), 1, 0);

        Control audio = BuildAudio();
        Controls.Add(audio, 0, 1);
        SetColumnSpan(audio, 2);

        Load();
        loading = false;
    }

    /// <summary>Der Nutzer hat etwas geändert, das die Spiegelung betrifft.</summary>
    public event EventHandler? BufferChanged;

    public event EventHandler? HotkeyChanged;

    public event EventHandler? AutostartFailed;

    /// <summary>Kurzer Hinweis für die Statuszeile des Hauptfensters.</summary>
    public event EventHandler<string>? StatusMessage;

    /// <summary>
    /// Schaltet den Autostart um - für den gleichnamigen Eintrag im Infobereich. Der Weg über
    /// das Kästchen ist Absicht: so gilt dieselbe Fehlerbehandlung wie beim Klick hier.
    /// </summary>
    public void ToggleAutostart()
    {
        if (autoStart.Enabled)
        {
            autoStart.Checked = !autoStart.Checked;
        }
    }

    /// <summary>Zeigt eine Meldung in der Aktualisierungs-Gruppe an.</summary>
    public void ShowUpdateStatus(string text) => updateStatus.Text = text;

    /// <summary>Meldung, die zuletzt beim Ändern des Autostarts aufgetreten ist.</summary>
    public string? LastAutostartError { get; private set; }

    public int BufferMs => (int)buffer.Value;

    /// <summary>
    /// Die eingestellte Tastenkombination. Beim Zurücksetzen (etwa weil sie schon vergeben ist)
    /// darf das Feld nicht erneut melden, sonst dreht sich die Prüfung im Kreis.
    /// </summary>
    public Keys Hotkey
    {
        get => hotkey.Hotkey;
        set
        {
            bool previous = loading;
            loading = true;
            hotkey.Hotkey = value;
            loading = previous;
        }
    }

    private static GroupBox Group(string title) => new()
    {
        Text = title,
        Dock = DockStyle.Fill,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Margin = new Padding(4, 4, 8, 8),
        Padding = new Padding(8, 4, 8, 8),
    };

    private static TableLayoutPanel Rows(int count) => new()
    {
        Dock = DockStyle.Top,
        ColumnCount = 2,
        RowCount = count,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Margin = new Padding(0),
    };

    private static Label Caption(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Margin = new Padding(3, 7, 8, 3),
    };

    private Control BuildBasic()
    {
        GroupBox box = Group(Strings.BasicSettings);
        TableLayoutPanel rows = Rows(3);
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        autoStart.Text = Strings.StartWithWindows;
        autoStart.AutoSize = true;
        autoStart.Enabled = Autostart.IsSupported;
        autoStart.CheckedChanged += OnAutostartChanged;

        doubleClick.DropDownStyle = ComboBoxStyle.DropDownList;
        doubleClick.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        doubleClick.Margin = new Padding(0, 3, 3, 3);
        doubleClick.Items.AddRange([Strings.ActionOpenWindow, Strings.ActionToggle, Strings.ActionNothing]);
        doubleClick.SelectedIndexChanged += (_, _) =>
        {
            if (loading) { return; }
            settings.DoubleClickAction = (TrayAction)doubleClick.SelectedIndex;
            settings.Save();
        };

        language.DropDownStyle = ComboBoxStyle.DropDownList;
        language.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        language.Margin = new Padding(0, 3, 3, 3);
        language.Items.AddRange(Strings.SupportedNames);
        language.SelectedIndexChanged += (_, _) =>
        {
            if (loading) { return; }
            settings.Language = Strings.Supported[language.SelectedIndex];
            settings.Save();
            StatusMessage?.Invoke(this, Strings.RestartForLanguage);
        };

        rows.Controls.Add(autoStart, 0, 0);
        rows.SetColumnSpan(autoStart, 2);
        rows.Controls.Add(Caption(Strings.DoubleClickLabel), 0, 1);
        rows.Controls.Add(doubleClick, 1, 1);
        rows.Controls.Add(Caption(Strings.LanguageLabel), 0, 2);
        rows.Controls.Add(language, 1, 2);
        box.Controls.Add(rows);
        return box;
    }

    private Control BuildAudio()
    {
        GroupBox box = Group(Strings.AudioSettings);
        TableLayoutPanel rows = Rows(3);
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var bufferRow = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Margin = new Padding(0),
        };
        buffer.Minimum = 10;
        buffer.Maximum = 250;
        buffer.Increment = 5;
        buffer.Width = 64;
        buffer.Margin = new Padding(0, 3, 6, 3);
        buffer.ValueChanged += (_, _) =>
        {
            if (loading) { return; }
            settings.BufferMs = (int)buffer.Value;
            BufferChanged?.Invoke(this, EventArgs.Empty);
        };
        var unit = new Label { Text = Strings.Milliseconds, AutoSize = true, Margin = new Padding(0, 7, 3, 3) };
        bufferRow.Controls.AddRange([buffer, unit]);

        Label bufferCaption = Caption(Strings.BufferLabel);
        tips.SetToolTip(bufferCaption, Strings.BufferTip);
        tips.SetToolTip(buffer, Strings.BufferTip);
        tips.SetToolTip(unit, Strings.BufferTip);

        hotkey.Width = 150;
        hotkey.Anchor = AnchorStyles.Left;
        hotkey.Margin = new Padding(0, 3, 3, 3);
        hotkey.HotkeyChanged += (_, _) =>
        {
            if (!loading) { HotkeyChanged?.Invoke(this, EventArgs.Empty); }
        };

        hotkeyEnabled.Text = Strings.HotkeyEnabled;
        hotkeyEnabled.AutoSize = true;
        hotkeyEnabled.Margin = new Padding(0, 3, 3, 3);
        hotkeyEnabled.CheckedChanged += (_, _) =>
        {
            if (loading) { return; }
            settings.HotkeyToggleAllEnabled = hotkeyEnabled.Checked;
            settings.Save();
            HotkeyChanged?.Invoke(this, EventArgs.Empty);
        };

        rows.Controls.Add(bufferCaption, 0, 0);
        rows.Controls.Add(bufferRow, 1, 0);
        rows.Controls.Add(Caption(Strings.ToggleAllLabel), 0, 1);
        rows.Controls.Add(hotkey, 1, 1);
        rows.Controls.Add(hotkeyEnabled, 1, 2);
        box.Controls.Add(rows);
        return box;
    }

    private Control BuildUpdates()
    {
        GroupBox box = Group(Strings.UpdateSettings);
        var rows = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            RowCount = 6,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0),
        };
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        foreach ((RadioButton button, string text) in new[]
        {
            (updateAuto, Strings.UpdateAutomatic),
            (updateNotify, Strings.UpdateNotify),
            (updateNever, Strings.UpdateNever),
        })
        {
            button.Text = text;
            button.AutoSize = true;
            button.Margin = new Padding(3, 2, 3, 2);
            button.CheckedChanged += (_, _) =>
            {
                if (loading || !button.Checked) { return; }
                settings.Updates = updateAuto.Checked ? UpdateMode.Automatic
                    : updateNever.Checked ? UpdateMode.Never : UpdateMode.Notify;
                settings.Save();
                checkNow.Enabled = !updateNever.Checked;
            };
            rows.Controls.Add(button);
        }

        checkNow.Text = Strings.CheckNow;
        checkNow.AutoSize = true;
        checkNow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        checkNow.Anchor = AnchorStyles.Left;
        checkNow.Margin = new Padding(3, 8, 3, 2);
        checkNow.Click += async (_, _) => await CheckAsync(manual: true);

        updateStatus.AutoSize = false;
        updateStatus.AutoEllipsis = true;
        updateStatus.Dock = DockStyle.Fill;
        updateStatus.ForeColor = SystemColors.GrayText;
        updateStatus.Text = Strings.CurrentVersion(UpdateChecker.CurrentVersion.ToString(3));

        rows.Controls.Add(checkNow);
        rows.Controls.Add(updateStatus);
        box.Controls.Add(rows);
        return box;
    }

    private void OnAutostartChanged(object? sender, EventArgs e)
    {
        if (loading)
        {
            return;
        }

        LastAutostartError = Autostart.TrySetEnabled(autoStart.Checked);
        if (LastAutostartError != null)
        {
            loading = true;
            autoStart.Checked = Autostart.IsEnabled();
            loading = false;
            AutostartFailed?.Invoke(this, EventArgs.Empty);
        }
    }

    private void Load()
    {
        autoStart.Checked = Autostart.IsEnabled();
        doubleClick.SelectedIndex = (int)settings.DoubleClickAction;
        // Ohne eigene Wahl steht hier, was die Anwendung ohnehin verwendet: die erkannte
        // Windows-Sprache. Gespeichert wird erst, wenn der Nutzer selbst etwas auswählt.
        language.SelectedIndex = Math.Max(0,
            Array.IndexOf(Strings.Supported, settings.Language?.Trim().ToLowerInvariant() ?? Strings.Language));
        buffer.Value = Math.Clamp(settings.BufferMs, 10, 250);
        hotkey.Hotkey = (Keys)settings.HotkeyToggleAll;
        hotkeyEnabled.Checked = settings.HotkeyToggleAllEnabled;

        updateAuto.Checked = settings.Updates == UpdateMode.Automatic;
        updateNotify.Checked = settings.Updates == UpdateMode.Notify;
        updateNever.Checked = settings.Updates == UpdateMode.Never;
        checkNow.Enabled = settings.Updates != UpdateMode.Never;
    }

    /// <summary>Sucht nach einer neueren Fassung und meldet das Ergebnis in der Gruppe.</summary>
    public async Task CheckAsync(bool manual)
    {
        if (settings.Updates == UpdateMode.Never && !manual)
        {
            return;
        }

        checkNow.Enabled = false;
        updateStatus.Text = Strings.CheckingUpdates;

        UpdateInfo? update = await UpdateChecker.FindNewerAsync();
        settings.LastUpdateCheckUtc = DateTime.UtcNow;
        settings.Save();

        if (update == null)
        {
            updateStatus.Text = manual
                ? Strings.UpToDate
                : Strings.CurrentVersion(UpdateChecker.CurrentVersion.ToString(3));
            checkNow.Enabled = settings.Updates != UpdateMode.Never;
            return;
        }

        // Was mit dem Fund geschieht, entscheidet das Hauptfenster - es hat das Fenster für
        // die Rückfrage und kann sich für die Installation beenden.
        updateStatus.Text = Strings.UpdateAvailable(update.Version);
        UpdateFound?.Invoke(this, update);
        checkNow.Enabled = settings.Updates != UpdateMode.Never;
    }

    /// <summary>Eine neuere Fassung wurde gefunden. Das Hauptfenster entscheidet, was folgt.</summary>
    public event EventHandler<UpdateInfo>? UpdateFound;
}
