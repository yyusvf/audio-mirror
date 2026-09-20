using System.Windows.Media;
using Application = System.Windows.Application;
using DrawingColor = System.Drawing.Color;

namespace AudioMirror.Ui.Theme;

/// <summary>
/// Die Farben aus Tokens.xaml, für den Teil des Programms, der nicht in WPF gezeichnet wird -
/// das Menü im Infobereich ist weiterhin ein WinForms-Menü.
///
/// Bewusst keine zweite Farbtabelle: die Werte werden zur Laufzeit aus den geladenen
/// Ressourcen gelesen. Eine Kopie hier wäre genau die verstreute Farbe, die vermieden werden
/// soll - sie würde beim nächsten Umfärben übersehen.
/// </summary>
internal static class Palette
{
    public static DrawingColor Window => Get("WindowBrush");
    public static DrawingColor Surface => Get("SurfaceBrush");
    public static DrawingColor Card => Get("CardBrush");
    public static DrawingColor Control => Get("ControlBrush");
    public static DrawingColor ControlHover => Get("ControlHoverBrush");
    public static DrawingColor Stroke => Get("StrokeBrush");
    public static DrawingColor Divider => Get("DividerBrush");
    public static DrawingColor Text => Get("TextPrimaryBrush");
    public static DrawingColor TextSecondary => Get("TextSecondaryBrush");
    public static DrawingColor TextDisabled => Get("TextDisabledBrush");
    public static DrawingColor Accent => Get("AccentBrush");
    public static DrawingColor OnAccent => Get("OnAccentBrush");

    /// <summary>
    /// Deckende Fassung des flächigen Akzents. WinForms kennt keine Durchsicht zwischen
    /// Steuerelementen, darum wird der Wert einmal gegen den Fensterton verrechnet.
    /// </summary>
    public static DrawingColor AccentSoft => Flatten(Brush("AccentSoftBrush"), Window);

    private static DrawingColor Get(string key) => ToDrawing(Brush(key));

    private static Color Brush(string key)
    {
        // Vor dem ersten Fenster steht noch keine Anwendung - dann bleibt es bei Schwarz,
        // was nie sichtbar wird, weil das Menü erst danach entsteht.
        if (Application.Current?.TryFindResource(key) is SolidColorBrush brush)
        {
            return brush.Color;
        }
        return Colors.Black;
    }

    private static DrawingColor ToDrawing(Color c) => DrawingColor.FromArgb(c.A, c.R, c.G, c.B);

    /// <summary>Legt eine teildeckende Farbe auf einen Grund und gibt das Ergebnis deckend zurück.</summary>
    private static DrawingColor Flatten(Color front, DrawingColor back)
    {
        float a = front.A / 255f;
        return DrawingColor.FromArgb(
            255,
            (int)(front.R * a + back.R * (1 - a)),
            (int)(front.G * a + back.G * (1 - a)),
            (int)(front.B * a + back.B * (1 - a)));
    }
}
