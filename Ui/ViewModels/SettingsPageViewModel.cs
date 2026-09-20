using System.Windows.Forms;

namespace AudioMirror.Ui.ViewModels;

/// <summary>
/// Die Einstellungsseite. Enthält dieselben Angaben wie zuvor: Autostart, Doppelklick,
/// Sprache, Puffer, Tastenkombination und Aktualisierungen.
///
/// Geschrieben wird sofort beim Ändern - es gibt kein "Übernehmen". Was die Spiegelung
/// betrifft, meldet sich über die Ereignisse beim Hauptteil, der Rest landet nur in der
/// Einstellungsdatei.
/// </summary>
internal sealed class SettingsPageViewModel : ViewModelBase
{
    private readonly AppSettings settings;

    /// <summary>
    /// Während des ersten Befüllens dürfen die Setter nichts auslösen - sonst schriebe das
    /// Laden der Einstellungen sie sofort wieder zurück.
    /// </summary>
    private bool loading = true;

    private string updateStatus = string.Empty;
    private bool busy;

    public SettingsPageViewModel(AppSettings settings)
    {
        this.settings = settings;

        DoubleClickActions =
        [
            Strings.ActionOpenWindow,
            Strings.ActionToggle,
            Strings.ActionNothing,
        ];

        Languages = [.. Strings.SupportedNames];

        updateStatus = Strings.CurrentVersion(UpdateChecker.CurrentVersion.ToString(3));
        loading = false;
    }

    /// <summary>Der Puffer hat sich geändert - die Spiegelung muss neu aufgebaut werden.</summary>
    public event Action? BufferChanged;

    /// <summary>Kombination oder deren Ein/Aus hat sich geändert.</summary>
    public event Action? HotkeyChanged;

    /// <summary>Kurzer Hinweis für die Statuszeile des Hauptfensters.</summary>
    public event Action<string>? StatusMessage;

    /// <summary>Eine neuere Fassung wurde gefunden; bool = Suche auf Knopfdruck.</summary>
    public event Action<UpdateInfo, bool>? UpdateFound;

    // ── Gruppentitel ────────────────────────────────────────────────────────
    public string BasicTitle => Strings.BasicSettings;
    public string AudioTitle => Strings.AudioSettings;
    public string UpdateTitle => Strings.UpdateSettings;

    // ── Allgemein ───────────────────────────────────────────────────────────
    public string AutostartLabel => Strings.StartWithWindows;

    public bool AutostartSupported => Autostart.IsSupported;

    public bool AutostartEnabled
    {
        get => Autostart.IsEnabled();
        set
        {
            if (loading || value == Autostart.IsEnabled())
            {
                return;
            }

            string? error = Autostart.TrySetEnabled(value);
            if (error != null)
            {
                StatusMessage?.Invoke(error);
            }

            // In jedem Fall neu melden: schlug das Setzen fehl, steht das Kästchen sonst
            // auf einem Wert, den die Registrierung gar nicht trägt.
            Raise();
        }
    }

    public string DoubleClickLabel => Strings.DoubleClickLabel;

    public IReadOnlyList<string> DoubleClickActions { get; }

    public int DoubleClickIndex
    {
        get => (int)settings.DoubleClickAction;
        set
        {
            if (loading || value < 0 || value == (int)settings.DoubleClickAction)
            {
                return;
            }

            settings.DoubleClickAction = (TrayAction)value;
            settings.Save();
            Raise();
        }
    }

    public string LanguageLabel => Strings.LanguageLabel;

    public IReadOnlyList<string> Languages { get; }

    public int LanguageIndex
    {
        // Ohne eigene Wahl steht hier, was die Anwendung ohnehin verwendet.
        get => Math.Max(0, Array.IndexOf(
            Strings.Supported, settings.Language?.Trim().ToLowerInvariant() ?? Strings.Language));
        set
        {
            if (loading || value < 0 || value >= Strings.Supported.Length || value == LanguageIndex)
            {
                return;
            }

            settings.Language = Strings.Supported[value];
            settings.Save();
            Raise();
            StatusMessage?.Invoke(Strings.RestartForLanguage);
        }
    }

    // ── Ton ─────────────────────────────────────────────────────────────────
    public string BufferLabel => Strings.BufferLabel;
    public string BufferTip => Strings.BufferTip;
    public string Milliseconds => Strings.Milliseconds;

    public int BufferMs
    {
        get => Math.Clamp(settings.BufferMs, 10, 250);
        set
        {
            int clamped = Math.Clamp(value, 10, 250);
            if (loading || clamped == settings.BufferMs)
            {
                return;
            }

            settings.BufferMs = clamped;
            settings.Save();
            Raise();
            Raise(nameof(BufferDisplay));
            BufferChanged?.Invoke();
        }
    }

    public string BufferDisplay => $"{BufferMs} {Strings.Milliseconds}";

    public string HotkeyLabel => Strings.ToggleAllLabel;

    /// <summary>
    /// Überschrift des eigenen Abschnitts für die Tastenkombination. Es ist die vorhandene
    /// Beschriftung ohne den Doppelpunkt - eine eigene Zeichenkette dafür hieße, sie in alle
    /// dreizehn Sprachen zu übersetzen, und der Wortlaut wäre derselbe.
    ///
    /// Das Leerzeichen gehört mit abgeschnitten: im Französischen steht vor dem Doppelpunkt
    /// eines, und zwar ein geschütztes.
    /// </summary>
    public string HotkeyTitle => Strings.ToggleAllLabel.TrimEnd(':', ' ', '\u00A0', '\u202F');
    public string HotkeyEnabledLabel => Strings.HotkeyEnabled;

    public Keys Hotkey
    {
        get => (Keys)settings.HotkeyToggleAll;
        set
        {
            if (value == (Keys)settings.HotkeyToggleAll)
            {
                Raise(nameof(HotkeyText));
                return;
            }

            settings.HotkeyToggleAll = (int)value;
            settings.Save();
            Raise();
            Raise(nameof(HotkeyText));
            HotkeyChanged?.Invoke();
        }
    }

    public string HotkeyText => GlobalHotkey.IsUsable(Hotkey) ? GlobalHotkey.Describe(Hotkey) : Strings.NoHotkey;

    public bool HotkeyEnabled
    {
        get => settings.HotkeyToggleAllEnabled;
        set
        {
            if (loading || value == settings.HotkeyToggleAllEnabled)
            {
                return;
            }

            settings.HotkeyToggleAllEnabled = value;
            settings.Save();
            Raise();
            HotkeyChanged?.Invoke();
        }
    }

    // ── Aktualisierungen ────────────────────────────────────────────────────
    public string UpdateAutomaticLabel => Strings.UpdateAutomatic;
    public string UpdateNotifyLabel => Strings.UpdateNotify;
    public string UpdateNeverLabel => Strings.UpdateNever;
    public string CheckNowLabel => Strings.CheckNow;

    public bool UpdateAutomatic
    {
        get => settings.Updates == UpdateMode.Automatic;
        set => SetUpdateMode(value, UpdateMode.Automatic);
    }

    public bool UpdateNotify
    {
        get => settings.Updates == UpdateMode.Notify;
        set => SetUpdateMode(value, UpdateMode.Notify);
    }

    public bool UpdateNever
    {
        get => settings.Updates == UpdateMode.Never;
        set => SetUpdateMode(value, UpdateMode.Never);
    }

    /// <summary>"Nie" heißt wirklich nie - dann ist auch die Suche auf Knopfdruck gesperrt.</summary>
    public bool CanCheckNow => !busy && settings.Updates != UpdateMode.Never;

    public string UpdateStatus
    {
        get => updateStatus;
        private set => Set(ref updateStatus, value);
    }

    private void SetUpdateMode(bool selected, UpdateMode mode)
    {
        if (loading || !selected || settings.Updates == mode)
        {
            return;
        }

        settings.Updates = mode;
        settings.Save();
        Raise(nameof(UpdateAutomatic));
        Raise(nameof(UpdateNotify));
        Raise(nameof(UpdateNever));
        Raise(nameof(CanCheckNow));
    }

    /// <summary>Zeigt eine Meldung in der Aktualisierungs-Gruppe an.</summary>
    public void ShowUpdateStatus(string text) => UpdateStatus = text;

    /// <summary>
    /// Sucht nach einer neueren Fassung. Was mit einem Fund geschieht, entscheidet der
    /// Hauptteil - nur dort gibt es ein Fenster für die Rückfrage.
    /// </summary>
    public async Task CheckAsync(bool manual)
    {
        if (settings.Updates == UpdateMode.Never && !manual)
        {
            return;
        }

        busy = true;
        Raise(nameof(CanCheckNow));
        UpdateStatus = Strings.CheckingUpdates;

        UpdateInfo? update = await UpdateChecker.FindNewerAsync();

        // Nur die Prüfung im Hintergrund setzt den Zeitstempel. Wer selbst sucht, soll damit
        // nicht die nächste automatische Prüfung um einen Tag verschieben.
        if (!manual)
        {
            settings.LastUpdateCheckUtc = DateTime.UtcNow;
            settings.Save();
        }

        busy = false;
        Raise(nameof(CanCheckNow));

        if (update == null)
        {
            UpdateStatus = manual
                ? Strings.UpToDate
                : Strings.CurrentVersion(UpdateChecker.CurrentVersion.ToString(3));
            return;
        }

        UpdateStatus = Strings.UpdateAvailable(update.Version);
        UpdateFound?.Invoke(update, manual);
    }
}
