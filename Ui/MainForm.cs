using AudioMirror;
using AudioMirror.Audio;

namespace AudioMirror.Ui;

internal sealed class MainForm : Form
{
    /// <summary>Der Statustakt läuft alle 500 ms; die Anwendungsliste wird jeden vierten geholt.</summary>
    private const int AppListEveryTicks = 4;

    private readonly MirrorEngine engine = new();
    private readonly TrayController tray = new();
    private readonly AppSettings settings = AppSettings.Load();
    private readonly List<DeviceRow> rows = [];
    private readonly List<Control> sectionHeaders = [];
    private readonly bool startMinimized;

    private readonly TableLayoutPanel root = new();
    private readonly ComboBox sourceSelect = new();
    private readonly Panel devicePanel = new();
    private readonly Label statusLabel = new();
    private readonly Button closeButton = new();
    private readonly TabControl tabs = new();
    private SettingsPage settingsPage = null!;
    private GlobalHotkey? toggleAllHotkey;

    private readonly System.Windows.Forms.Timer debounceTimer = new() { Interval = 400 };
    private readonly System.Windows.Forms.Timer statusTimer = new() { Interval = 500 };

    private bool needsDeviceRefresh;
    private bool closing;
    private bool allowExit;
    private bool allowVisible;
    private bool trayHintShown;
    private int statusTicks;
    private bool suppressSourceEvent;
    private string? lastError;
    private UpdateInfo? pendingUpdate;

    public MainForm(bool startMinimized)
    {
        this.startMinimized = startMinimized;

        // Nur der stille Start hält das Fenster zu; von Hand gestartet geht es normal auf.
        allowVisible = !startMinimized;

        // Beendet Windows das Programm beim Abmelden oder Herunterfahren, wird das vorgemerkt:
        // ein danach von Windows ausgelöster Start soll wieder still im Infobereich landen.
        Microsoft.Win32.SystemEvents.SessionEnding += OnSessionEnding;

        BuildUi();

        // Ein- und Ausstecken von Geräten wird laufend im Hintergrund verarbeitet -
        // die Liste aktualisiert sich dadurch ohne Zutun.
        engine.DeviceListChanged += () => Post(ScheduleRefresh);
        engine.DefaultDeviceChanged += () => Post(ScheduleRefresh);

        tray.DeviceProvider = () => rows
            .Select(r => new TrayDeviceEntry(r.DeviceId, r.DisplayName, r.Selected, r.IsSource))
            .ToList();
        tray.DeviceToggled += (deviceId, enabled) => ApplySelection(deviceId, enabled);
        tray.AutostartToggled += () => settingsPage.ToggleAutostart();
        tray.ShowWindowRequested += ShowFromTray;
        tray.DoubleClickActionProvider = () => settings.DoubleClickAction;
        tray.ToggleRequested += ToggleEverything;
        tray.ExitRequested += () =>
        {
            allowExit = true;
            Close();
        };

        // Minimieren blendet nur aus - das Programm läuft im Infobereich weiter.
        Resize += (_, _) =>
        {
            if (WindowState == FormWindowState.Minimized)
            {
                HideToTray(showHint: true);
            }
        };

        debounceTimer.Tick += OnDebounceTick;
        statusTimer.Tick += (_, _) => OnStatusTick();
        statusTimer.Start();

        Load += (_, _) =>
        {
            engine.FixedSourceDeviceId = settings.SourceDeviceId;

            ApplyHotkey();

            // Zuletzt angehakte Geräte werden erkannt und sofort wieder bespielt.
            RefreshDevices();
            RefreshAppLists();
            SyncTargets();

            // Geöffnet wird immer bei den Geräten. Ohne das landet die Auswahl beim ersten
            // Element, das den Fokus bekommt - und das liegt auf dem Einstellungen-Reiter.
            tabs.SelectedIndex = 0;
            ActiveControl = sourceSelect;

            AnnounceUpdate();

            // Bei jedem Start, im Hintergrund und nur wenn gewünscht.
            if (UpdateChecker.ShouldCheck(settings.Updates, settings.LastUpdateCheckUtc))
            {
                _ = settingsPage.CheckAsync(manual: false);
            }
        };
        FormClosing += OnFormClosing;
    }

    private void BuildUi()
    {
        Text = Strings.AppTitle;
        Icon = tray.Icon;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Font;
        Font = new Font("Segoe UI", 9f);
        ClientSize = FixedClientSize;

        root.Dock = DockStyle.Fill;
        root.ColumnCount = 1;
        root.Padding = new Padding(10, 8, 10, 8);
        root.RowCount = 2;
        // Die Reiter bekommen allen freien Platz - zieht man das Fenster größer, wächst die
        // Geräteliste mit, statt eine Lücke zu lassen.
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(BuildTabs(), 0, 0);
        root.Controls.Add(BuildFooter(), 0, 1);

        Controls.Add(root);
        ApplyMetrics();
    }

    /// <summary>
    /// Label, das bei zu wenig Platz gekürzt wird statt das Fenster aufzuweiten. Ein Label mit
    /// AutoSize würde seine volle Textbreite erzwingen und damit andere Elemente aus dem
    /// Fenster schieben.
    /// </summary>
    private static Label ShrinkableLabel(string text) => new()
    {
        Text = text,
        AutoSize = false,
        AutoEllipsis = true,
        Anchor = AnchorStyles.Left | AnchorStyles.Right,
        TextAlign = ContentAlignment.MiddleLeft,
    };

    private Control BuildTabs()
    {
        tabs.Dock = DockStyle.Fill;
        tabs.Margin = new Padding(0, 0, 0, 6);

        var devices = new TabPage(Strings.TabDevices) { Padding = new Padding(8, 6, 8, 6) };
        var deviceLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
        };
        deviceLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        deviceLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        deviceLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        deviceLayout.Controls.Add(BuildHeader(), 0, 0);
        deviceLayout.Controls.Add(BuildDeviceArea(), 0, 1);
        devices.Controls.Add(deviceLayout);

        settingsPage = new SettingsPage(settings);
        settingsPage.BufferChanged += (_, _) =>
        {
            debounceTimer.Stop();
            debounceTimer.Start();
        };
        settingsPage.HotkeyChanged += (_, _) => OnHotkeyChanged();
        settingsPage.AutostartFailed += (_, _) =>
        {
            lastError = settingsPage.LastAutostartError;
            RefreshStatus();
        };
        settingsPage.StatusMessage += (_, message) => SetStatus(message, false);
        settingsPage.UpdateFound += (_, finding) => OnUpdateFound(finding.Update, finding.Manual);

        var settingsTab = new TabPage(Strings.TabSettings) { Padding = new Padding(4) };
        settingsTab.Controls.Add(settingsPage);

        tabs.TabPages.Add(devices);
        tabs.TabPages.Add(settingsTab);
        return tabs;
    }

    private Control BuildHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 0, 8),
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        Label caption = ShrinkableLabel(Strings.Source);
        caption.ForeColor = SystemColors.GrayText;

        sourceSelect.DropDownStyle = ComboBoxStyle.DropDownList;
        sourceSelect.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        sourceSelect.Margin = new Padding(3, 2, 3, 2);
        sourceSelect.SelectedIndexChanged += OnSourceSelected;

        header.Controls.Add(caption, 0, 0);
        header.Controls.Add(sourceSelect, 0, 1);
        return header;
    }

    private Control BuildDeviceArea()
    {
        var container = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
        };
        container.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        container.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        container.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        Label columns = ShrinkableLabel(Strings.TargetDevices);
        columns.ForeColor = SystemColors.GrayText;
        columns.Margin = new Padding(3, 0, 3, 4);

        devicePanel.Dock = DockStyle.Fill;
        devicePanel.AutoScroll = true;
        devicePanel.BorderStyle = BorderStyle.FixedSingle;
        devicePanel.BackColor = SystemColors.Window;
        devicePanel.Margin = new Padding(0);

        container.Controls.Add(columns, 0, 0);
        container.Controls.Add(devicePanel, 0, 1);
        return container;
    }

    private Control BuildFooter()
    {
        statusLabel.AutoSize = false;
        statusLabel.AutoEllipsis = true;
        statusLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        statusLabel.Text = Strings.Ready;

        closeButton.Text = Strings.Close;
        closeButton.AutoSize = true;
        closeButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        closeButton.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
        closeButton.Padding = new Padding(12, 4, 12, 4);
        closeButton.Margin = new Padding(3, 4, 3, 0);
        // Nur ausblenden - das Programm läuft im Infobereich weiter.
        closeButton.Click += (_, _) => HideToTray(showHint: true);

        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 4, 0, 0),
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.Controls.Add(statusLabel, 0, 0);
        footer.Controls.Add(closeButton, 1, 0);
        return footer;
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        ApplyMetrics();
    }

    /// <summary>
    /// Die feste Fenstergröße. Sie richtet sich nach dem Einstellungen-Reiter - dem Reiter mit
    /// dem unveränderlichen, größten Inhalt. Die Geräteliste wächst nicht mehr mit ihrer Länge,
    /// sondern scrollt; so springt das Fenster nicht, wenn ein Gerät dazukommt oder aufgeklappt
    /// wird. Größer ziehen bleibt möglich, kleiner nicht - sonst würde etwas abgeschnitten.
    /// </summary>
    /// Etwas breiter als der englische Text bräuchte: Französisch, Polnisch und Russisch fallen
    /// im Schnitt ein Fünftel länger aus, und abgeschnittene Beschriftungen wären das Ergebnis.
    private Size FixedClientSize => new(
        Math.Max(680, Font.Height * 45),
        Math.Max(400, Font.Height * 26));

    private void ApplyMetrics()
    {
        statusLabel.Height = Font.Height + 6;
        MinimumSize = SizeFromClientSize(FixedClientSize);
    }

    /// <summary>
    /// Passt die Fensterhöhe an den Inhalt an: so viel Platz wie nötig, nicht mehr.
    ///
    /// Nach oben begrenzt auf gut die halbe Bildschirmhöhe - bei vielen Geräten oder vielen
    /// aufgeklappten Anwendungen scrollt die Liste dann, statt über den Bildschirm zu wachsen.
    /// </summary>
    private void ScheduleRefresh()
    {
        needsDeviceRefresh = true;
        debounceTimer.Stop();
        debounceTimer.Start();
    }

    private void OnDebounceTick(object? sender, EventArgs e)
    {
        debounceTimer.Stop();

        if (needsDeviceRefresh)
        {
            needsDeviceRefresh = false;
            RefreshDevices();
            RefreshAppLists();
        }

        SyncTargets();
    }

    private void OnStatusTick()
    {
        if (closing)
        {
            return;
        }

        // Die Anwendungsliste ändert sich laufend, aber deutlich langsamer als der Statustakt.
        if (++statusTicks % AppListEveryTicks == 0 && RefreshAppLists())
        {
            SyncTargets();
        }

        RefreshStatus();
    }

    /// <summary>Das Gerät, von dem gespiegelt wird: fest gewählt, sonst der Windows-Standard.</summary>
    private string? EffectiveSourceId => settings.SourceDeviceId ?? engine.TryGetDefaultDeviceId();

    private void OnSourceSelected(object? sender, EventArgs e)
    {
        if (suppressSourceEvent)
        {
            return;
        }

        settings.SourceDeviceId = (sourceSelect.SelectedItem as SourceItem)?.Id;
        settings.Save();
        engine.FixedSourceDeviceId = settings.SourceDeviceId;

        // Die Quelle bestimmt, welches Gerät gesperrt ist und welche Anwendungen gelistet werden.
        RefreshDevices();
        RefreshAppLists();
        SyncTargets();
    }

    private void RefreshSourceList(IReadOnlyList<AudioDeviceInfo> devices)
    {
        string? wanted = settings.SourceDeviceId;

        suppressSourceEvent = true;
        sourceSelect.BeginUpdate();
        sourceSelect.Items.Clear();
        // In der Klammer steht, welches Gerät der Windows-Standard gerade ist - so ist ohne
        // Nachsehen erkennbar, worauf sich "automatisch" im Moment bezieht.
        string current = devices.FirstOrDefault(d => d.IsDefault)?.Name ?? Strings.NoDeviceAvailable;
        sourceSelect.Items.Add(new SourceItem(null, Strings.WindowsDefaultDevice(current)));

        // Nur angeschlossene Geräte zur Auswahl stellen: von einem nicht verbundenen Gerät
        // ließe sich ohnehin nichts abgreifen.
        foreach (AudioDeviceInfo device in devices.Where(d => d.Connected))
        {
            sourceSelect.Items.Add(new SourceItem(device.Id, device.Name));
        }

        // Ein fest gewähltes, gerade nicht vorhandenes Gerät bleibt sichtbar - sonst würde die
        // Auswahl beim Abziehen stillschweigend auf "automatisch" zurückfallen.
        if (wanted != null && devices.All(d => d.Id != wanted && d.Connected))
        {
            sourceSelect.Items.Add(new SourceItem(wanted, Strings.LastChosenUnavailable));
        }

        sourceSelect.SelectedItem = sourceSelect.Items.OfType<SourceItem>()
            .FirstOrDefault(i => i.Id == wanted) ?? sourceSelect.Items[0];
        sourceSelect.EndUpdate();
        suppressSourceEvent = false;
    }

    /// <summary>Eintrag der Quellenliste; ToString bestimmt die Anzeige.</summary>
    private sealed record SourceItem(string? Id, string Name)
    {
        public override string ToString() => Name;
    }

    private void RefreshDevices()
    {
        IReadOnlyList<AudioDeviceInfo> devices = engine.ListOutputDevices();
        string? sourceId = EffectiveSourceId;

        RefreshSourceList(devices);

        devicePanel.SuspendLayout();
        foreach (DeviceRow row in rows)
        {
            devicePanel.Controls.Remove(row);
            row.Dispose();
        }
        rows.Clear();
        foreach (Control header in sectionHeaders)
        {
            devicePanel.Controls.Remove(header);
            header.Dispose();
        }
        sectionHeaders.Clear();

        List<AudioDeviceInfo> connected = devices.Where(d => d.Connected).ToList();

        // Getrennte Geräte nur zeigen, wenn für sie tatsächlich etwas eingerichtet wurde.
        // Sonst stünden hier dauerhaft Buchsen herum, die nie jemand benutzt hat - ihre
        // Einstellung wird trotzdem weitergeführt, sie ist nur nicht sichtbar.
        List<AudioDeviceInfo> disconnected = devices
            .Where(d => !d.Connected && IsConfigured(d.Id))
            .ToList();

        // Geräte, die Windows gar nicht mehr aufzählt, für die es aber eine gespeicherte
        // Einstellung gibt, gehören ebenfalls unter „Getrennt“ - sonst verschwände die
        // Einstellung wortlos aus der Oberfläche.
        var known = devices.Select(d => d.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach ((string id, DeviceSetting saved) in settings.Devices)
        {
            if (!known.Contains(id) && !string.IsNullOrWhiteSpace(saved.Name) && IsConfigured(id))
            {
                disconnected.Add(new AudioDeviceInfo(
                    id, saved.Name!, false, false, (AudioDeviceKind)saved.Kind, saved.IconPath));
            }
        }
        disconnected = disconnected.OrderBy(d => d.Name, StringComparer.CurrentCultureIgnoreCase).ToList();

        // Dock=Top stapelt in umgekehrter Einfügereihenfolge - daher alles rückwärts einfügen.
        if (disconnected.Count > 0)
        {
            AddSection(Strings.Disconnected, disconnected, sourceId);
        }
        AddSection(Strings.Connected, connected, sourceId);

        rows.Reverse();
        devicePanel.ResumeLayout();

        void AddSection(string caption, List<AudioDeviceInfo> group, string? source)
        {
            foreach (AudioDeviceInfo device in Enumerable.Reverse(group))
            {
                var row = new DeviceRow(
                    device.Id, device.Name, device.Id == source, device.Kind, device.Connected, device.IconPath);

                DeviceSetting setting = settings.For(device.Id);
                setting.Name = device.Name;
                setting.Kind = (int)device.Kind;
                setting.IconPath = device.IconPath;
                row.Volume = setting.Volume;
                row.Selected = setting.Enabled;
                row.Expanded = setting.Expanded && device.Connected;

                row.SelectionChanged += OnRowSelectionChanged;
                row.VolumeChanged += OnRowVolumeChanged;
                row.AppMixChanged += OnRowAppMixChanged;
                row.ExpandedChanged += OnRowExpandedChanged;

                rows.Add(row);
                devicePanel.Controls.Add(row);
            }

            var header = new SectionHeader(caption);
            sectionHeaders.Add(header);
            devicePanel.Controls.Add(header);
        }
    }

    /// <summary>Aktualisiert die Anwendungslisten aller Geräte. Liefert true, wenn sich etwas geändert hat.</summary>
    private bool RefreshAppLists()
    {
        IReadOnlyList<AudioAppInfo> apps = engine.ListAudioApps();

        var before = rows.SelectMany(r => r.AppStates.Select(a => r.DeviceId + "|" + a.Key)).ToHashSet();

        devicePanel.SuspendLayout();
        foreach (DeviceRow row in rows)
        {
            string deviceId = row.DeviceId;
            row.UpdateApps(apps, key => settings.Lookup(deviceId, key));
        }
        devicePanel.ResumeLayout();

        var after = rows.SelectMany(r => r.AppStates.Select(a => r.DeviceId + "|" + a.Key)).ToHashSet();
        bool changed = !before.SetEquals(after);
        if (changed)
        {
        }
        return changed;
    }

    /// <summary>
    /// Ob für ein Gerät je etwas eingerichtet wurde - angehakt, in der Lautstärke verändert oder
    /// mit einer Anwendungsmischung versehen. Nur solche Geräte sind es wert, im Abschnitt
    /// „Getrennt“ aufgeführt zu werden.
    /// </summary>
    private bool IsConfigured(string deviceId)
    {
        if (!settings.Devices.TryGetValue(deviceId, out DeviceSetting? saved))
        {
            return false;
        }
        return saved.Enabled || saved.Volume < 0.999f || (saved.Apps?.Count ?? 0) > 0;
    }

    private void OnRowSelectionChanged(object? sender, EventArgs e)
    {
        if (sender is not DeviceRow row)
        {
            return;
        }

        settings.For(row.DeviceId).Enabled = row.Selected;
        settings.Save();
        SyncTargets();
    }

    /// <summary>Schaltet ein Gerät von außerhalb der Liste um (Tray-Menü).</summary>
    private void ApplySelection(string deviceId, bool enabled)
    {
        DeviceRow? row = rows.FirstOrDefault(r => r.DeviceId == deviceId);
        if (row == null || row.IsSource)
        {
            return;
        }

        row.Selected = enabled; // löst bewusst kein Ereignis aus - wir übernehmen selbst
        settings.For(deviceId).Enabled = enabled;
        settings.Save();
        SyncTargets();
    }

    private void OnRowVolumeChanged(object? sender, EventArgs e)
    {
        if (sender is not DeviceRow row)
        {
            return;
        }

        settings.For(row.DeviceId).Volume = row.Volume;
        settings.Save();
        engine.SetMasterVolume(row.DeviceId, row.Volume);
    }

    private void OnRowAppMixChanged(object? sender, AppMixEventArgs e)
    {
        if (sender is not DeviceRow row)
        {
            return;
        }

        AppMixSetting app = settings.For(row.DeviceId, e.AppKey);
        bool wasEnabled = app.Enabled;
        app.Enabled = e.Enabled;
        app.Volume = e.Volume;
        settings.Save();

        // Reine Lautstärkeänderungen brauchen keinen Abgleich der Aufnahmen.
        if (wasEnabled != e.Enabled)
        {
            SyncTargets();
        }
        else
        {
            engine.SetTargets(BuildTargets(), settingsPage.BufferMs);
        }
    }

    private void OnRowExpandedChanged(object? sender, EventArgs e)
    {
        if (sender is DeviceRow row)
        {
            settings.For(row.DeviceId).Expanded = row.Expanded;
            settings.Save();
        }
    }

    private MirrorTarget[] BuildTargets() => rows
        .Where(r => r.Selected && !r.IsSource && r.Connected)
        .Select(r => new MirrorTarget(r.DeviceId, r.Volume, r.UsesWholeDevice, r.EnabledApps))
        .ToArray();

    /// <summary>
    /// Überträgt Auswahl und Mischung an die Engine. Die Engine gleicht selbst ab, was
    /// hinzukommt und was wegfällt, sodass Unverändertes weiterläuft.
    /// </summary>
    private void SyncTargets()
    {
        if (closing)
        {
            return;
        }

        try
        {
            engine.SetTargets(BuildTargets(), settingsPage.BufferMs);
            lastError = null;
        }
        catch (Exception ex)
        {
            engine.Stop();
            lastError = ex.Message;
        }

        RefreshStatus();
    }

    private void RefreshStatus()
    {
        if (closing)
        {
            return;
        }

        var byId = engine.Outputs.ToDictionary(o => o.DeviceId);
        int running = 0;
        int failed = 0;

        foreach (DeviceRow row in rows)
        {
            // Hinweise an den einzelnen Anwendungszeilen.
            foreach ((string key, bool enabled, _) in row.AppStates)
            {
                string? appError = enabled ? engine.GetAppError(key) : null;
                row.SetAppStatus(key, appError ?? string.Empty, appError != null);
            }

            if (row.IsSource)
            {
                row.SetStatus(Strings.SourceShort, false);
                continue;
            }

            if (!row.Connected)
            {
                row.SetStatus(Strings.NotConnected, false);
                continue;
            }

            if (!byId.TryGetValue(row.DeviceId, out DeviceOutput? output))
            {
                row.SetStatus(string.Empty, false);
                continue;
            }

            if (output.Error != null)
            {
                row.SetStatus(output.Error + Strings.RetryRunning, true);
                failed++;
            }
            else if (row.UsesWholeDevice)
            {
                row.SetStatus(Strings.RunningWholeSound(output.EstimatedLatencyMs), false);
                running++;
            }
            else
            {
                int count = output.ActiveAppCount;
                row.SetStatus(count == 0
                    ? Strings.NoAppSelected
                    : Strings.RunningApps(count, output.EstimatedLatencyMs), false);
                running++;
            }
        }

        tray.SetTooltip(engine.IsRunning ? Strings.TrayMirroring(running) : Strings.TrayNoMirroring);

        if (lastError != null)
        {
            SetStatus(lastError, true);
            return;
        }

        if (engine.SourceUnavailable)
        {
            SetStatus(Strings.SourceUnavailable, true);
            return;
        }

        int selected = rows.Count(r => r.Selected && !r.IsSource && r.Connected);
        if (selected == 0)
        {
            SetStatus(Strings.NothingTicked, false);
            return;
        }

        if (running == 0)
        {
            string reason = engine.WholeDeviceError ?? Strings.DeviceOrApp;
            SetStatus(Strings.WaitingFor(reason, selected), true);
            return;
        }

        string message = Strings.MirroringOnDevices(running);
        if (failed > 0)
        {
            message += Strings.StillWaiting(failed);
        }
        SetStatus(message, failed > 0);
    }

    private void SetStatus(string text, bool isError)
    {
        if (statusLabel.Text != text)
        {
            statusLabel.Text = text;
        }
        Color colour = isError ? Color.Firebrick : SystemColors.ControlText;
        if (statusLabel.ForeColor != colour)
        {
            statusLabel.ForeColor = colour;
        }
    }

    /// <summary>Blendet das Fenster aus; Spiegelung und Tray-Symbol bleiben aktiv.</summary>
    private void HideToTray(bool showHint)
    {
        Hide();

        if (showHint && !trayHintShown)
        {
            trayHintShown = true;
            tray.ShowHint(Strings.StillRunningTitle, Strings.StillRunningBody);
        }
    }

    /// <summary>
    /// Belegte Tastenkombinationen anderer Funktionen. Pro-Gerät-Hotkeys gibt es noch nicht;
    /// sobald sie dazukommen, tragen sie sich hier ein und die Kollisionsprüfung greift für sie
    /// automatisch mit.
    /// </summary>
    private IEnumerable<(Keys Key, string Owner)> OtherHotkeys() => [];

    private void OnHotkeyChanged()
    {
        Keys wanted = settingsPage.Hotkey;

        // Keine stille Übernahme, wenn die Kombination schon anderweitig vergeben ist.
        foreach ((Keys key, string owner) in OtherHotkeys())
        {
            if (key == wanted && wanted != Keys.None)
            {
                settingsPage.Hotkey = (Keys)settings.HotkeyToggleAll;
                SetStatus(Strings.HotkeyAssignedTo(GlobalHotkey.Describe(wanted), owner), true);
                return;
            }
        }

        settings.HotkeyToggleAll = (int)wanted;
        settings.Save();
        ApplyHotkey();
    }

    /// <summary>Meldet den Hotkey bei Windows an bzw. wieder ab.</summary>
    private void ApplyHotkey()
    {
        if (!IsHandleCreated)
        {
            return;
        }

        toggleAllHotkey ??= new GlobalHotkey(Handle, GlobalHotkey.ToggleAllId);
        toggleAllHotkey.Unregister();

        var key = (Keys)settings.HotkeyToggleAll;
        if (!settings.HotkeyToggleAllEnabled || !GlobalHotkey.IsUsable(key))
        {
            return;
        }

        string? error = toggleAllHotkey.TryRegister(key);
        if (error != null)
        {
            SetStatus(error, true);
        }
    }

    /// <summary>
    /// Schaltet die gesamte Spiegelung aus bzw. wieder ein.
    ///
    /// Beim Ausschalten wird der genaue Zustand festgehalten - welche Geräte aktiv waren und
    /// welche Anwendungen darin wie eingestellt sind. Beim Einschalten wird genau dieser Stand
    /// zurückgespielt, nicht einfach alles aktiviert. Geräte, die es zwischenzeitlich nicht mehr
    /// gibt, werden dabei übersprungen.
    /// </summary>
    private void ToggleEverything()
    {
        bool anythingOn = rows.Any(r => r.Selected && !r.IsSource && r.Connected);

        if (anythingOn)
        {
            var snapshot = new Dictionary<string, DeviceSetting>(StringComparer.OrdinalIgnoreCase);
            foreach (DeviceRow row in rows.Where(r => r.Selected && !r.IsSource && r.Connected))
            {
                var entry = new DeviceSetting { Enabled = true, Volume = row.Volume };
                foreach ((string key, bool enabled, float volume) in row.AppStates)
                {
                    entry.Apps[key] = new AppMixSetting { Enabled = enabled, Volume = volume };
                }
                snapshot[row.DeviceId] = entry;
            }

            settings.HotkeySnapshot = snapshot;
            foreach (DeviceRow row in rows.Where(r => r.Selected && !r.IsSource))
            {
                row.Selected = false;
                settings.For(row.DeviceId).Enabled = false;
            }

            settings.Save();
            SyncTargets();
            tray.ShowHint(Strings.MirroringOff, Strings.MutedDevices(snapshot.Count));
            return;
        }

        Dictionary<string, DeviceSetting>? saved = settings.HotkeySnapshot;
        if (saved == null || saved.Count == 0)
        {
            SetStatus(Strings.NoRememberedState, true);
            return;
        }

        int restored = 0;
        int skipped = 0;
        foreach ((string deviceId, DeviceSetting entry) in saved)
        {
            DeviceRow? row = rows.FirstOrDefault(r =>
                r.DeviceId.Equals(deviceId, StringComparison.OrdinalIgnoreCase) && r.Connected && !r.IsSource);

            if (row == null)
            {
                // Gerät ist inzwischen weg oder selbst zur Quelle geworden - überspringen.
                skipped++;
                continue;
            }

            DeviceSetting target = settings.For(deviceId);
            target.Enabled = true;
            target.Volume = entry.Volume;
            foreach ((string appKey, AppMixSetting app) in entry.Apps)
            {
                target.Apps[appKey] = new AppMixSetting { Enabled = app.Enabled, Volume = app.Volume };
            }

            row.Volume = entry.Volume;
            row.Selected = true;
            restored++;
        }

        settings.HotkeySnapshot = null;
        settings.Save();
        RefreshAppLists();
        SyncTargets();

        tray.ShowHint(Strings.MirroringOn, skipped == 0
            ? Strings.RestoredDevices(restored)
            : Strings.RestoredDevicesPartly(restored, skipped));
    }

    private void OnSessionEnding(object? sender, Microsoft.Win32.SessionEndingEventArgs e) =>
        StartupState.MarkStoppedByWindows();

    /// <summary>
    /// Hält das Fenster beim stillen Start zuverlässig geschlossen.
    ///
    /// Ein <c>Hide()</c> im Load-Ereignis genügt dafür nicht: es fällt mitten in den laufenden
    /// Anzeigevorgang und wird davon je nach Zeitpunkt wieder überschrieben. Hier wird das
    /// Einblenden von vornherein unterbunden; das Fensterhandle wird trotzdem erzeugt, damit
    /// die Einrichtung im Load-Ereignis anläuft.
    /// </summary>
    protected override void SetVisibleCore(bool value)
    {
        if (!allowVisible)
        {
            if (!IsHandleCreated)
            {
                CreateHandle();
            }
            value = false;
        }
        base.SetVisibleCore(value);
    }

    /// <summary>
    /// Fängt die Meldung eines zweiten Programmstarts ab und holt stattdessen dieses Fenster
    /// nach vorn - so führt ein weiterer Doppelklick nicht zu einer zweiten Instanz.
    /// </summary>
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == SingleInstance.ShowWindowMessage && !closing)
        {
            ShowFromTray();
        }
        else if (GlobalHotkey.IsHotkeyMessage(ref m, GlobalHotkey.ToggleAllId) && !closing)
        {
            ToggleEverything();
        }
        base.WndProc(ref m);
    }

    private void ShowFromTray()
    {
        allowVisible = true;
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        BringToFront();

        // Ein Fund, der bei ausgeblendetem Fenster liegen geblieben ist, wird jetzt vorgelegt.
        if (pendingUpdate != null)
        {
            BeginInvoke(() => AskAndInstall(pendingUpdate!));
        }
    }

    /// <summary>
    /// Sagt einmal Bescheid, wenn seit dem letzten Lauf eine andere Fassung installiert wurde.
    /// Ohne das tauscht sich das Programm bei der automatischen Aktualisierung unbemerkt aus.
    /// </summary>
    private void AnnounceUpdate()
    {
        string current = UpdateChecker.CurrentVersion.ToString(3);
        if (settings.LastRunVersion == current)
        {
            return;
        }

        if (!string.IsNullOrEmpty(settings.LastRunVersion))
        {
            tray.ShowHint(Strings.AppTitle, Strings.UpdatedTo(current));
        }

        settings.LastRunVersion = current;

        // Eine übersprungene Fassung ist mit dem Wechsel erledigt.
        settings.SkippedVersion = null;
        settings.Save();
    }

    /// <summary>
    /// Eine neuere Fassung liegt vor. Bei "automatisch installieren" wird sie ohne jede
    /// Rückfrage eingespielt und das Programm danach neu gestartet - das ist es, was die
    /// Einstellung zusagt. Sonst wird gefragt.
    ///
    /// Ist das Fenster ausgeblendet, gibt es zunächst nur einen Hinweis im Infobereich: ein
    /// Dialog, der unaufgefordert aus dem Nichts aufspringt, wäre zudringlich. Die Frage kommt
    /// dann, sobald das Fenster geöffnet wird.
    /// </summary>
    private void OnUpdateFound(UpdateInfo update, bool manual)
    {
        if (settings.Updates == UpdateMode.Automatic)
        {
            _ = InstallAsync(update, silent: true);
            return;
        }

        // Eine einmal abgelehnte Fassung wird nicht bei jedem Start erneut vorgelegt. Wer von
        // Hand sucht, bekommt sie trotzdem wieder angeboten.
        if (!manual && update.Version == settings.SkippedVersion)
        {
            return;
        }

        if (!Visible)
        {
            pendingUpdate = update;
            tray.ShowHint(Strings.AppTitle, Strings.UpdateAvailable(update.Version));
            return;
        }

        AskAndInstall(update);
    }

    private void AskAndInstall(UpdateInfo update)
    {
        pendingUpdate = null;

        DialogResult answer = MessageBox.Show(
            this,
            Strings.UpdatePrompt(update.Version, UpdateChecker.CurrentVersion.ToString(3)),
            Strings.AppTitle,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (answer == DialogResult.Yes)
        {
            _ = InstallAsync(update, silent: false);
            return;
        }

        settings.SkippedVersion = update.Version;
        settings.Save();
    }

    /// <summary>
    /// Lädt das Setup und startet es. Danach beendet sich das Programm: das Setup wartet sonst
    /// darauf, dass die laufende Fassung geschlossen wird. Bei <paramref name="silent"/> läuft
    /// die Installation ohne Oberfläche durch und startet Audio Mirror anschließend wieder.
    /// </summary>
    private async Task InstallAsync(UpdateInfo update, bool silent)
    {
        if (update.SetupUrl == null)
        {
            // Ohne Setup bleibt nur die Seite - dann lädt man von Hand. Kommt bei den eigenen
            // Veröffentlichungen nicht vor, ist aber besser als wortlos nichts zu tun.
            SetUpdateStatus(Strings.UpdateNoSetup);
            UpdateChecker.OpenPage(update.PageUrl);
            return;
        }

        SetUpdateStatus(Strings.UpdateDownloading(update.Version));

        if (!await UpdateChecker.DownloadAndRunAsync(update, silent, minimized: !Visible))
        {
            SetUpdateStatus(Strings.UpdateDownloadFailed);
            UpdateChecker.OpenPage(update.PageUrl);
            return;
        }

        SetUpdateStatus(Strings.UpdateStarting);
        allowExit = true;
        Close();
    }

    private void SetUpdateStatus(string text)
    {
        settingsPage.ShowUpdateStatus(text);
        SetStatus(text, false);
    }

    private void Post(Action action)
    {
        if (closing || !IsHandleCreated)
        {
            return;
        }

        try
        {
            BeginInvoke(action);
        }
        catch (ObjectDisposedException)
        {
            // Fenster wurde bereits geschlossen.
        }
        catch (InvalidOperationException)
        {
            // Handle noch nicht/nicht mehr vorhanden.
        }
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        // Das X beendet nicht, es blendet nur aus. Vollständig geschlossen wird ausschließlich
        // über "Beenden" im Tray-Menü - oder wenn das System es verlangt.
        //
        // Bewusst als Positivliste: alles andere blendet aus. Ein direkt gesendetes WM_CLOSE
        // meldet CloseReason.None, und eine Prüfung nur auf UserClosing würde das Programm
        // dabei beenden - genau das soll nicht passieren.
        if (e.CloseReason == CloseReason.WindowsShutDown)
        {
            StartupState.MarkStoppedByWindows();
        }

        bool mustExit = allowExit
            || e.CloseReason == CloseReason.WindowsShutDown
            || e.CloseReason == CloseReason.TaskManagerClosing
            || e.CloseReason == CloseReason.ApplicationExitCall;

        if (!mustExit)
        {
            e.Cancel = true;
            HideToTray(showHint: true);
            return;
        }

        closing = true;
        statusTimer.Stop();
        debounceTimer.Stop();

        foreach (DeviceRow row in rows)
        {
            DeviceSetting setting = settings.For(row.DeviceId);
            setting.Volume = row.Volume;
            setting.Expanded = row.Expanded;

            // Beim Quellgerät ist der Haken zwangsweise aus - das darf die gespeicherte
            // Auswahl nicht überschreiben, sonst geht sie beim Wechsel des Standardgeräts verloren.
            if (!row.IsSource)
            {
                setting.Enabled = row.Selected;
            }

            foreach ((string key, bool enabled, float volume) in row.AppStates)
            {
                AppMixSetting app = settings.For(row.DeviceId, key);
                app.Enabled = enabled;
                app.Volume = volume;
            }
        }
        settings.Save();

        toggleAllHotkey?.Dispose();
        Microsoft.Win32.SystemEvents.SessionEnding -= OnSessionEnding;
        engine.Dispose();
        tray.Dispose();
    }
}
