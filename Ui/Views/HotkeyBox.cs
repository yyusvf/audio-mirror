using System.Windows;
using System.Windows.Input;
using Keys = System.Windows.Forms.Keys;

namespace AudioMirror.Ui.Views;

/// <summary>
/// Nimmt eine Tastenkombination auf. Anklicken, drücken, fertig; Esc oder Rücktaste löschen sie.
///
/// Aufgenommen wird in der Zählung von <see cref="Keys"/>, weil die Anmeldung bei Windows
/// (RegisterHotKey) damit arbeitet. WPF meldet stattdessen <see cref="Key"/> - die Umrechnung
/// läuft über den virtuellen Tastencode, der bei beiden derselbe ist.
/// </summary>
internal sealed class HotkeyBox : System.Windows.Controls.Button
{
    public static readonly DependencyProperty HotkeyProperty = DependencyProperty.Register(
        nameof(Hotkey), typeof(Keys), typeof(HotkeyBox),
        new FrameworkPropertyMetadata(Keys.None, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnHotkeyChanged));

    public static readonly DependencyProperty IsRecordingProperty = DependencyProperty.Register(
        nameof(IsRecording), typeof(bool), typeof(HotkeyBox), new PropertyMetadata(false));

    public HotkeyBox()
    {
        Focusable = true;
        Click += (_, _) => IsRecording = true;
        LostFocus += (_, _) => IsRecording = false;
        UpdateText();
    }

    public Keys Hotkey
    {
        get => (Keys)GetValue(HotkeyProperty);
        set => SetValue(HotkeyProperty, value);
    }

    /// <summary>Wartet gerade auf einen Tastendruck.</summary>
    public bool IsRecording
    {
        get => (bool)GetValue(IsRecordingProperty);
        set
        {
            SetValue(IsRecordingProperty, value);
            UpdateText();
        }
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (!IsRecording)
        {
            base.OnPreviewKeyDown(e);
            return;
        }

        e.Handled = true;

        Key key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (key is Key.Escape or Key.Back or Key.Delete)
        {
            Hotkey = Keys.None;
            IsRecording = false;
            return;
        }

        // Ein Umschalter allein ist keine Kombination - weiter warten, bis eine echte Taste folgt.
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift
            or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin or Key.System)
        {
            return;
        }

        var combination = (Keys)KeyInterop.VirtualKeyFromKey(key);

        ModifierKeys modifiers = Keyboard.Modifiers;
        if (modifiers.HasFlag(ModifierKeys.Control))
        {
            combination |= Keys.Control;
        }
        if (modifiers.HasFlag(ModifierKeys.Shift))
        {
            combination |= Keys.Shift;
        }
        if (modifiers.HasFlag(ModifierKeys.Alt))
        {
            combination |= Keys.Alt;
        }

        Hotkey = combination;
        IsRecording = false;
    }

    private static void OnHotkeyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) =>
        ((HotkeyBox)sender).UpdateText();

    private void UpdateText()
    {
        if (IsRecording)
        {
            Content = Strings.PressCombination;
            return;
        }

        Content = GlobalHotkey.IsUsable(Hotkey) ? GlobalHotkey.Describe(Hotkey) : Strings.NoHotkey;
    }
}
