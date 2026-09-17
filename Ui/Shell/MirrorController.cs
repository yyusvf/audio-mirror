using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using AudioMirror.Audio;
using AudioMirror.Ui.ViewModels;
using AudioMirror.Ui.Views;
using Keys = System.Windows.Forms.Keys;
using MessageBox = System.Windows.MessageBox;

namespace AudioMirror.Ui.Shell;

/// <summary>
/// Hält alles zusammen: Spiegelung, Einstellungen, Infobereich, Tastenkombination und
/// Aktualisierungen. Die Ansichten zeigen nur an und melden zurück - entschieden wird hier.
///
/// Das ist dasselbe Verhalten wie zuvor im Hauptfenster der WinForms-Fassung, nur von den
/// Steuerelementen gelöst: statt Zeilen mit Ereignissen gibt es Ansichtsmodelle, deren
/// Änderungen hier ankommen.
/// </summary>
internal sealed class MirrorController : IDisposable
{
    /// <summary>Der Statustakt läuft alle 500 ms; die Anwendungsliste wird jeden vierten geholt.</summary>
    private const int AppListEveryTicks = 4;

    private readonly MirrorEngine engine = new();
    private readonly TrayController tray = new();
    private readonly AppSettings settings = AppSettings.Load();
    private readonly ShellWindow window = new();
    private readonly DevicesPageViewModel devicesModel = new();
    private readonly SettingsPageViewModel settingsModel;

    private readonly DispatcherTimer statusTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private readonly DispatcherTimer debounceTimer = new() { Interval = TimeSpan.FromMilliseconds(400) };

    private GlobalHotkey? toggleAllHotkey;

    /// <summary>
    /// Wahr, solange die Liste neu aufgebaut oder von außen gesetzt wird. Ohne das würde jede
    /// gesetzte Lautstärke sofort als Benutzereingabe zurückkommen und wieder gespeichert.
    /// </summary>
    private bool suppress;

    private bool closing;
    private bool allowExit;
    private bool trayHintShown;
    private bool needsDeviceRefresh;
    private int statusTicks;
    private string? lastError;
    private UpdateInfo? pendingUpdate;

    public MirrorController(bool startMinimized)
    {
        settingsModel = new SettingsPageViewModel(settings);

        // Das Fensterhandle muss stehen, bevor die Tastenkombination angemeldet wird - und
        // zwar auch beim stillen Start, bei dem nie ein Fenster zu sehen ist.
        new WindowInteropHelper(window).EnsureHandle();
        HwndSource.FromHwnd(new WindowInteropHelper(window).Handle)?.AddHook(OnWindowMessage);

        window.SetPages(
            new DevicesPage { DataContext = devicesModel },
            new SettingsPage { DataContext = settingsModel });

        window.CloseRequested += () => HideToTray(showHint: true);
        window.Closing += OnWindowClosing;
        window.StateChanged += (_, _) =>
        {
            if (window.WindowState == WindowState.Minimized)
            {
                HideToTray(showHint: true);
            }
        };

        devicesModel.PropertyChanged += OnDevicesModelChanged;

        settingsModel.BufferChanged += () =>
        {
            debounceTimer.Stop();
            debounceTimer.Start();
        };
        settingsModel.HotkeyChanged += OnHotkeyChanged;
        settingsModel.StatusMessage += message => SetStatus(message, false);
        settingsModel.UpdateFound += OnUpdateFound;

        // Ein- und Ausstecken von Geräten wird laufend im Hintergrund verarbeitet.
        engine.DeviceListChanged += () => Post(ScheduleRefresh);
        engine.DefaultDeviceChanged += () => Post(ScheduleRefresh);

        tray.DeviceProvider = () => devicesModel.Devices
            .Select(d => new TrayDeviceEntry(d.DeviceId, d.DisplayName, d.IsEnabled, d.IsSource))
            .ToList();
        tray.DeviceToggled += (deviceId, enabled) => Post(() => ApplySelection(deviceId, enabled));
        tray.AutostartToggled += () => Post(ToggleAutostart);
        tray.ShowWindowRequested += () => Post(ShowFromTray);
        tray.DoubleClickActionProvider = () => settings.DoubleClickAction;
        tray.ToggleRequested += () => Post(ToggleEverything);
        tray.ExitRequested += () => Post(ExitForReal);

        // Beendet Windows das Programm beim Abmelden, wird das vorgemerkt: ein danach von
        // Windows ausgelöster Start soll wieder still im Infobereich landen.
        Microsoft.Win32.SystemEvents.SessionEnding += OnSessionEnding;

        debounceTimer.Tick += (_, _) => OnDebounceTick();
        statusTimer.Tick += (_, _) => OnStatusTick();
        statusTimer.Start();

        engine.FixedSourceDeviceId = settings.SourceDeviceId;
        ApplyHotkey();

        RefreshDevices();
        RefreshAppLists();
        SyncTargets();

        AnnounceUpdate();
        if (UpdateChecker.ShouldCheck(settings.Updates, settings.LastUpdateCheckUtc))
        {
            _ = settingsModel.CheckAsync(manual: false);
        }

        if (!startMinimized)
        {
            window.Show();
        }
    }

    // ══ Geräte ══════════════════════════════════════════════════════════════

    private string? EffectiveSourceId => settings.SourceDeviceId ?? engine.TryGetDefaultDeviceId();

    private void RefreshDevices()
    {
        IReadOnlyList<AudioDeviceInfo> devices = engine.ListOutputDevices();
        string? sourceId = EffectiveSourceId;

        suppress = true;

        RefreshSourceList(devices);

        foreach (DeviceViewModel old in devicesModel.Devices)
        {
            old.PropertyChanged -= OnDeviceChanged;
            foreach (AppViewModel app in old.Apps)
            {
                app.PropertyChanged -= OnAppChanged;
            }
        }
        devicesModel.Devices.Clear();

        // Getrennte Geräte werden nicht angezeigt. Ihre Einstellung bleibt trotzdem erhalten:
        // settings.For(id) liest und schreibt unabhängig davon, ob es dafür gerade eine Zeile
        // gibt. Steckt ein Gerät später wieder an, erscheint es mit demselben Stand.
        foreach (AudioDeviceInfo device in devices.Where(d => d.Connected))
        {
            var model = new DeviceViewModel(
                device.Id, device.Name, device.Kind, device.Id == sourceId, device.IconPath);

            DeviceSetting setting = settings.For(device.Id);
            setting.Name = device.Name;
            setting.Kind = (int)device.Kind;
            setting.IconPath = device.IconPath;

            model.Volume = setting.Volume;
            model.IsEnabled = setting.Enabled && device.Id != sourceId;
            model.IsExpanded = setting.Expanded;

            model.PropertyChanged += OnDeviceChanged;
            devicesModel.Devices.Add(model);
        }

        suppress = false;
    }

    private void RefreshSourceList(IReadOnlyList<AudioDeviceInfo> devices)
    {
        string? wanted = settings.SourceDeviceId;

        devicesModel.Sources.Clear();
        devicesModel.Sources.Add(new SourceItem(null, Strings.WindowsDefaultDevice(
            devices.FirstOrDefault(d => d.IsDefault)?.Name ?? Strings.NoDeviceAvailable)));

        // Nur angeschlossene Geräte zur Auswahl stellen: von einem nicht verbundenen ließe
        // sich ohnehin nichts abgreifen.
        foreach (AudioDeviceInfo device in devices.Where(d => d.Connected))
        {
            devicesModel.Sources.Add(new SourceItem(device.Id, device.Name));
        }

        // Ein fest gewähltes, gerade nicht vorhandenes Gerät bleibt sichtbar - sonst fiele die
        // Auswahl beim Abziehen stillschweigend auf "automatisch" zurück.
        if (wanted != null && devices.All(d => d.Id != wanted && d.Connected))
        {
            devicesModel.Sources.Add(new SourceItem(wanted, Strings.LastChosenUnavailable));
        }

        devicesModel.SelectedSource =
            devicesModel.Sources.FirstOrDefault(i => i.Id == wanted) ?? devicesModel.Sources[0];
    }

    /// <summary>Gleicht die Anwendungslisten ab. Liefert true, wenn sich die Menge geändert hat.</summary>
    private bool RefreshAppLists()
    {
        IReadOnlyList<AudioAppInfo> apps = engine.ListAudioApps();
        bool changed = false;

        suppress = true;
        foreach (DeviceViewModel device in devicesModel.Devices)
        {
            bool sameSet = apps.Count == device.Apps.Count
                && apps.All(a => device.Apps.Any(x => x.Key.Equals(a.Key, StringComparison.OrdinalIgnoreCase)));

            if (sameSet)
            {
                continue;
            }

            changed = true;
            foreach (AppViewModel old in device.Apps)
            {
                old.PropertyChanged -= OnAppChanged;
            }
            device.Apps.Clear();

            foreach (AudioAppInfo app in apps)
            {
                (bool enabled, float volume) = settings.Lookup(device.DeviceId, app.Key);
                var model = new AppViewModel(app.Key, app.Name) { IsEnabled = enabled, Volume = volume };
                model.PropertyChanged += OnAppChanged;
                device.Apps.Add(model);
            }
        }
        suppress = false;

        return changed;
    }

    private void OnDevicesModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (suppress || e.PropertyName != nameof(DevicesPageViewModel.SelectedSource))
        {
            return;
        }

        settings.SourceDeviceId = devicesModel.SelectedSource?.Id;
        settings.Save();
        engine.FixedSourceDeviceId = settings.SourceDeviceId;

        // Die Quelle bestimmt, welches Gerät gesperrt ist und welche Anwendungen gelistet werden.
        RefreshDevices();
        RefreshAppLists();
        SyncTargets();
    }

    private void OnDeviceChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (suppress || sender is not DeviceViewModel device)
        {
            return;
        }

        switch (e.PropertyName)
        {
            case nameof(DeviceViewModel.IsEnabled):
                settings.For(device.DeviceId).Enabled = device.IsEnabled;
                settings.Save();
                SyncTargets();
                break;

            case nameof(DeviceViewModel.Volume):
                settings.For(device.DeviceId).Volume = device.Volume;
                settings.Save();
                engine.SetMasterVolume(device.DeviceId, device.Volume);
                break;

            case nameof(DeviceViewModel.IsExpanded):
                settings.For(device.DeviceId).Expanded = device.IsExpanded;
                settings.Save();
                break;
        }
    }

    private void OnAppChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (suppress || sender is not AppViewModel app)
        {
            return;
        }

        DeviceViewModel? owner = devicesModel.Devices.FirstOrDefault(d => d.Apps.Contains(app));
        if (owner == null)
        {
            return;
        }

        AppMixSetting stored = settings.For(owner.DeviceId, app.Key);
        bool wasEnabled = stored.Enabled;
        stored.Enabled = app.IsEnabled;
        stored.Volume = app.Volume;
        settings.Save();

        // Reine Lautstärkeänderungen brauchen keinen Abgleich der Aufnahmen.
        if (wasEnabled != app.IsEnabled)
        {
            SyncTargets();
        }
        else
        {
            engine.SetTargets(BuildTargets(), settingsModel.BufferMs);
        }
    }

    /// <summary>Schaltet ein Gerät von außerhalb der Liste um (Infobereich).</summary>
    private void ApplySelection(string deviceId, bool enabled)
    {
        DeviceViewModel? device = devicesModel.Devices.FirstOrDefault(d => d.DeviceId == deviceId);
        if (device == null || device.IsSource)
        {
            return;
        }

        suppress = true;
        device.IsEnabled = enabled;
        suppress = false;

        settings.For(deviceId).Enabled = enabled;
        settings.Save();
        SyncTargets();
    }

    // ══ Spiegelung ══════════════════════════════════════════════════════════

    /// <summary>Alles an, in voller Lautstärke? Dann reicht die Aufnahme am ganzen Gerät.</summary>
    private static bool UsesWholeDevice(DeviceViewModel device) =>
        device.Apps.All(a => a.IsEnabled && a.Volume >= 0.999f);

    private MirrorTarget[] BuildTargets() => devicesModel.Devices
        .Where(d => d.IsEnabled && !d.IsSource)
        .Select(d => new MirrorTarget(
            d.DeviceId,
            d.Volume,
            UsesWholeDevice(d),
            d.Apps.Where(a => a.IsEnabled).Select(a => new MirrorAppTarget(a.Key, a.Volume)).ToList()))
        .ToArray();

    private void SyncTargets()
    {
        if (closing)
        {
            return;
        }

        try
        {
            engine.SetTargets(BuildTargets(), settingsModel.BufferMs);
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

        foreach (DeviceViewModel device in devicesModel.Devices)
        {
            foreach (AppViewModel app in device.Apps)
            {
                app.Status = app.IsEnabled ? engine.GetAppError(app.Key) ?? string.Empty : string.Empty;
            }

            if (device.IsSource)
            {
                device.Status = Strings.SourceShort;
                continue;
            }

            if (!byId.TryGetValue(device.DeviceId, out DeviceOutput? output))
            {
                device.Status = string.Empty;
                continue;
            }

            if (output.Error != null)
            {
                device.Status = output.Error + Strings.RetryRunning;
                failed++;
            }
            else if (UsesWholeDevice(device))
            {
                device.Status = Strings.RunningWholeSound(output.EstimatedLatencyMs);
                running++;
            }
            else
            {
                int count = output.ActiveAppCount;
                device.Status = count == 0
                    ? Strings.NoAppSelected
                    : Strings.RunningApps(count, output.EstimatedLatencyMs);
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

        int selected = devicesModel.Devices.Count(d => d.IsEnabled && !d.IsSource);
        if (selected == 0)
        {
            SetStatus(Strings.NothingTicked, false);
            return;
        }

        if (running == 0)
        {
            SetStatus(Strings.WaitingFor(engine.WholeDeviceError ?? Strings.DeviceOrApp, selected), true);
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
        devicesModel.Status = text;
        devicesModel.StatusIsError = isError;
    }

    // ══ Takt ════════════════════════════════════════════════════════════════

    private void ScheduleRefresh()
    {
        needsDeviceRefresh = true;
        debounceTimer.Stop();
        debounceTimer.Start();
    }

    private void OnDebounceTick()
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

    private void Post(Action action)
    {
        if (closing)
        {
            return;
        }

        window.Dispatcher.BeginInvoke(action);
    }

    // ══ Tastenkombination ═══════════════════════════════════════════════════

    private void OnHotkeyChanged() => ApplyHotkey();

    private void ApplyHotkey()
    {
        IntPtr handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        toggleAllHotkey ??= new GlobalHotkey(handle, GlobalHotkey.ToggleAllId);
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
    /// zurückgespielt, nicht einfach alles aktiviert. Geräte, die es zwischenzeitlich nicht
    /// mehr gibt, werden dabei übersprungen.
    /// </summary>
    private void ToggleEverything()
    {
        bool anythingOn = devicesModel.Devices.Any(d => d.IsEnabled && !d.IsSource);

        if (anythingOn)
        {
            var snapshot = new Dictionary<string, DeviceSetting>(StringComparer.OrdinalIgnoreCase);
            foreach (DeviceViewModel device in devicesModel.Devices.Where(d => d.IsEnabled && !d.IsSource))
            {
                var entry = new DeviceSetting { Enabled = true, Volume = device.Volume };
                foreach (AppViewModel app in device.Apps)
                {
                    entry.Apps[app.Key] = new AppMixSetting { Enabled = app.IsEnabled, Volume = app.Volume };
                }
                snapshot[device.DeviceId] = entry;
            }

            settings.HotkeySnapshot = snapshot;

            suppress = true;
            foreach (DeviceViewModel device in devicesModel.Devices.Where(d => d.IsEnabled && !d.IsSource))
            {
                device.IsEnabled = false;
                settings.For(device.DeviceId).Enabled = false;
            }
            suppress = false;

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

        suppress = true;
        foreach ((string deviceId, DeviceSetting entry) in saved)
        {
            DeviceViewModel? device = devicesModel.Devices.FirstOrDefault(d =>
                d.DeviceId.Equals(deviceId, StringComparison.OrdinalIgnoreCase) && !d.IsSource);

            if (device == null)
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

            device.Volume = entry.Volume;
            device.IsEnabled = true;
            restored++;
        }
        suppress = false;

        settings.HotkeySnapshot = null;
        settings.Save();
        RefreshAppLists();
        SyncTargets();

        tray.ShowHint(Strings.MirroringOn, skipped == 0
            ? Strings.RestoredDevices(restored)
            : Strings.RestoredDevicesPartly(restored, skipped));
    }

    /// <summary>
    /// Fängt die Meldung eines zweiten Programmstarts ab und holt stattdessen dieses Fenster
    /// nach vorn - so führt ein weiterer Doppelklick nicht zu einer zweiten Instanz.
    /// </summary>
    private IntPtr OnWindowMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (closing)
        {
            return IntPtr.Zero;
        }

        if (msg == SingleInstance.ShowWindowMessage)
        {
            ShowFromTray();
            handled = true;
        }
        else if (GlobalHotkey.IsHotkeyMessage(msg, wParam, GlobalHotkey.ToggleAllId))
        {
            ToggleEverything();
            handled = true;
        }

        return IntPtr.Zero;
    }

    // ══ Fenster ═════════════════════════════════════════════════════════════

    private void ShowFromTray()
    {
        window.Show();
        window.WindowState = WindowState.Normal;
        window.Activate();

        // Ein Fund, der bei ausgeblendetem Fenster liegen geblieben ist, wird jetzt vorgelegt.
        if (pendingUpdate != null)
        {
            window.Dispatcher.BeginInvoke(() => AskAndInstall(pendingUpdate!));
        }
    }

    /// <summary>Blendet das Fenster aus; Spiegelung und Symbol im Infobereich bleiben aktiv.</summary>
    private void HideToTray(bool showHint)
    {
        window.Hide();

        if (showHint && !trayHintShown)
        {
            trayHintShown = true;
            tray.ShowHint(Strings.StillRunningTitle, Strings.StillRunningBody);
        }
    }

    private void ToggleAutostart()
    {
        settingsModel.AutostartEnabled = !settingsModel.AutostartEnabled;
    }

    private void OnSessionEnding(object? sender, Microsoft.Win32.SessionEndingEventArgs e)
    {
        StartupState.MarkStoppedByWindows();
        ExitForReal();
    }

    /// <summary>
    /// Das Schließkreuz beendet nicht, es blendet nur aus. Vollständig geschlossen wird
    /// ausschließlich über "Beenden" im Infobereich - oder wenn das System es verlangt.
    /// </summary>
    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (allowExit)
        {
            return;
        }

        e.Cancel = true;
        HideToTray(showHint: true);
    }

    private void ExitForReal()
    {
        allowExit = true;
        SaveEverything();
        System.Windows.Application.Current.Shutdown();
    }

    private void SaveEverything()
    {
        closing = true;
        statusTimer.Stop();
        debounceTimer.Stop();

        foreach (DeviceViewModel device in devicesModel.Devices)
        {
            DeviceSetting setting = settings.For(device.DeviceId);
            setting.Volume = device.Volume;
            setting.Expanded = device.IsExpanded;

            // Beim Quellgerät ist der Haken zwangsweise aus - das darf die gespeicherte
            // Auswahl nicht überschreiben, sonst geht sie beim Wechsel des Standardgeräts verloren.
            if (!device.IsSource)
            {
                setting.Enabled = device.IsEnabled;
            }

            foreach (AppViewModel app in device.Apps)
            {
                AppMixSetting stored = settings.For(device.DeviceId, app.Key);
                stored.Enabled = app.IsEnabled;
                stored.Volume = app.Volume;
            }
        }

        settings.Save();
    }

    // ══ Aktualisierungen ════════════════════════════════════════════════════

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

        // Ein Dialog, der bei ausgeblendetem Fenster aus dem Nichts aufspringt, wäre
        // zudringlich. Die Frage kommt dann, sobald das Fenster geöffnet wird.
        if (!window.IsVisible)
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

        MessageBoxResult answer = MessageBox.Show(
            window,
            Strings.UpdatePrompt(update.Version, UpdateChecker.CurrentVersion.ToString(3)),
            Strings.AppTitle,
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer == MessageBoxResult.Yes)
        {
            _ = InstallAsync(update, silent: false);
            return;
        }

        settings.SkippedVersion = update.Version;
        settings.Save();
    }

    private async Task InstallAsync(UpdateInfo update, bool silent)
    {
        if (update.SetupUrl == null)
        {
            SetUpdateStatus(Strings.UpdateNoSetup);
            UpdateChecker.OpenPage(update.PageUrl);
            return;
        }

        SetUpdateStatus(Strings.UpdateDownloading(update.Version));

        if (!await UpdateChecker.DownloadAndRunAsync(update, silent, minimized: !window.IsVisible))
        {
            SetUpdateStatus(Strings.UpdateDownloadFailed);
            UpdateChecker.OpenPage(update.PageUrl);
            return;
        }

        SetUpdateStatus(Strings.UpdateStarting);
        ExitForReal();
    }

    private void SetUpdateStatus(string text)
    {
        settingsModel.ShowUpdateStatus(text);
        SetStatus(text, false);
    }

    public void Dispose()
    {
        if (!closing)
        {
            SaveEverything();
        }

        toggleAllHotkey?.Dispose();
        Microsoft.Win32.SystemEvents.SessionEnding -= OnSessionEnding;
        engine.Dispose();
        tray.Dispose();
    }
}
