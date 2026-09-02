using AudioMirror;
using System.Runtime.InteropServices;

namespace AudioMirror.Ui;

/// <summary>
/// Meldet eine Tastenkombination systemweit bei Windows an.
///
/// Systemweit heißt: sie wirkt auch dann, wenn ein Spiel oder eine andere Vollbildanwendung im
/// Vordergrund ist. Jede Kombination bekommt eine eigene Kennung, damit sich mehrere Hotkeys
/// (etwa später einer je Gerät) nicht gegenseitig überschreiben.
/// </summary>
internal sealed class GlobalHotkey : IDisposable
{
    /// <summary>Kennung des Gesamt-Umschalters. Weitere Hotkeys bekommen eigene Kennungen ab 2.</summary>
    public const int ToggleAllId = 1;

    private const int WmHotkey = 0x0312;
    private const uint ModNoRepeat = 0x4000;

    private readonly IntPtr window;
    private readonly int id;
    private bool registered;

    public GlobalHotkey(IntPtr window, int id)
    {
        this.window = window;
        this.id = id;
    }

    /// <summary>Wurde die Kombination gedrückt?</summary>
    public static bool IsHotkeyMessage(ref Message m, int id) =>
        m.Msg == WmHotkey && m.WParam.ToInt32() == id;

    public bool IsRegistered => registered;

    /// <summary>
    /// Meldet die Kombination an. Liefert eine lesbare Meldung, wenn Windows sie ablehnt -
    /// meist, weil ein anderes Programm sie bereits belegt.
    /// </summary>
    public string? TryRegister(Keys keyData)
    {
        Unregister();

        Keys key = keyData & Keys.KeyCode;
        if (key == Keys.None)
        {
            return null;
        }

        uint modifiers = ModNoRepeat;
        if ((keyData & Keys.Alt) == Keys.Alt) { modifiers |= 0x0001; }
        if ((keyData & Keys.Control) == Keys.Control) { modifiers |= 0x0002; }
        if ((keyData & Keys.Shift) == Keys.Shift) { modifiers |= 0x0004; }

        if (RegisterHotKey(window, id, modifiers, (uint)key))
        {
            registered = true;
            return null;
        }

        return Strings.HotkeyTaken;
    }

    public void Unregister()
    {
        if (!registered)
        {
            return;
        }
        UnregisterHotKey(window, id);
        registered = false;
    }

    public void Dispose() => Unregister();

    /// <summary>Lesbare Darstellung, z. B. „Strg + Umschalt + M“.</summary>
    public static string Describe(Keys keyData)
    {
        Keys key = keyData & Keys.KeyCode;
        if (key == Keys.None)
        {
            return Strings.NoHotkey;
        }

        var parts = new List<string>();
        if ((keyData & Keys.Control) == Keys.Control) { parts.Add(Strings.KeyControl); }
        if ((keyData & Keys.Shift) == Keys.Shift) { parts.Add(Strings.KeyShift); }
        if ((keyData & Keys.Alt) == Keys.Alt) { parts.Add(Strings.KeyAlt); }
        parts.Add(DescribeKey(key));
        return string.Join(" + ", parts);
    }

    private static string DescribeKey(Keys key) => key switch
    {
        >= Keys.D0 and <= Keys.D9 => ((char)('0' + (key - Keys.D0))).ToString(),
        >= Keys.NumPad0 and <= Keys.NumPad9 => Strings.KeyNumPad + (key - Keys.NumPad0),
        Keys.Oemplus => "+",
        Keys.OemMinus => "-",
        Keys.OemPipe => "^",
        Keys.OemQuestion => "#",
        Keys.Oemcomma => ",",
        Keys.OemPeriod => ".",
        Keys.Space => Strings.KeySpace,
        Keys.Prior => Strings.KeyPageUp,
        Keys.Next => Strings.KeyPageDown,
        Keys.Escape => "Esc",
        _ => key.ToString(),
    };

    /// <summary>Nur Modifikatoren ohne echte Taste ergeben keine brauchbare Kombination.</summary>
    public static bool IsUsable(Keys keyData)
    {
        Keys key = keyData & Keys.KeyCode;
        return key is not (Keys.None or Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin);
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint virtualKey);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}

/// <summary>
/// Eingabefeld, das eine Tastenkombination aufnimmt: hineinklicken, Kombination drücken, fertig.
/// </summary>
internal sealed class HotkeyRecorder : TextBox
{
    private Keys value = Keys.None;

    public HotkeyRecorder()
    {
        ReadOnly = true;
        Cursor = Cursors.Hand;
        TextAlign = HorizontalAlignment.Center;
        Text = GlobalHotkey.Describe(Keys.None);
    }

    public event EventHandler? HotkeyChanged;

    public Keys Hotkey
    {
        get => value;
        set
        {
            this.value = value;
            Text = GlobalHotkey.Describe(value);
        }
    }

    protected override void OnEnter(EventArgs e)
    {
        base.OnEnter(e);
        Text = Strings.PressCombination;
    }

    protected override void OnLeave(EventArgs e)
    {
        base.OnLeave(e);
        Text = GlobalHotkey.Describe(value);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (!Focused)
        {
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // Rücktaste löscht die Belegung.
        if ((keyData & Keys.KeyCode) == Keys.Back)
        {
            Hotkey = Keys.None;
            HotkeyChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        if (GlobalHotkey.IsUsable(keyData))
        {
            Hotkey = keyData;
            HotkeyChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        // Reine Modifikatoren schlucken, damit sie nicht den Fokus verschieben.
        return true;
    }
}
