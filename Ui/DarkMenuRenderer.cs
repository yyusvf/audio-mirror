using System.Drawing;
using System.Windows.Forms;
using AudioMirror.Ui.Theme;

namespace AudioMirror.Ui;

/// <summary>
/// Das Menü im Infobereich in denselben Farben wie das Fenster.
///
/// WinForms hat keinen dunklen Modus. Ein <see cref="ContextMenuStrip"/> zeichnet sich über
/// einen Renderer, und der eingebaute holt seine Farben aus der Systemtabelle - hell, mit
/// blauer Auswahl. Ersetzt wird deshalb die Farbtabelle, nicht das Zeichnen selbst: so bleiben
/// Maße, Tastaturführung und Haken das, was der Nutzer von jedem anderen Menü kennt.
///
/// Zwei Dinge gehen über die Tabelle nicht und stehen darum unten: der Rahmen des Menüs und
/// der Haken, den der eingebaute Renderer als dunkles Häkchen auf hellem Grund zeichnet.
/// </summary>
internal sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
{
    public DarkMenuRenderer() : base(new DarkColours())
    {
        // Eckige Ecken wie im Fenster; der eingebaute Renderer rundet sonst nach eigener
        // Vorgabe und lässt an den Ecken den Grund des Systems stehen.
        RoundedEdges = false;
    }

    /// <summary>Der Rahmen um das aufgeklappte Menü.</summary>
    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var pen = new Pen(Palette.Stroke);
        Rectangle bounds = e.AffectedBounds;
        e.Graphics.DrawRectangle(pen, 0, 0, bounds.Width - 1, bounds.Height - 1);
    }

    /// <summary>Der Haken vor einem aktiven Gerät - im Akzent statt im Systemton.</summary>
    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        var box = new Rectangle(e.ImageRectangle.X, e.ImageRectangle.Y,
                                e.ImageRectangle.Width, e.ImageRectangle.Height);

        using var brush = new SolidBrush(Palette.Accent);
        using var pen = new Pen(Palette.OnAccent, 2f);

        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        e.Graphics.FillEllipse(brush, box);

        // Ein Haken aus zwei Strichen - eine Schriftglyphe säße je nach Schriftgrad anders.
        float left = box.Left + box.Width * 0.28f;
        float middle = box.Left + box.Width * 0.45f;
        float right = box.Left + box.Width * 0.74f;
        float top = box.Top + box.Height * 0.36f;
        float bottom = box.Top + box.Height * 0.66f;
        float centre = box.Top + box.Height * 0.52f;

        e.Graphics.DrawLines(pen, [
            new PointF(left, centre),
            new PointF(middle, bottom),
            new PointF(right, top),
        ]);
    }

    /// <summary>
    /// Die Farbtabelle, aus der der eingebaute Renderer schöpft. Jeder Wert kommt aus
    /// <see cref="Palette"/>, also aus derselben Datei wie die Farben des Fensters.
    /// </summary>
    private sealed class DarkColours : ProfessionalColorTable
    {
        public DarkColours()
        {
            // Sonst mischt WinForms die Werte mit den Systemfarben auf.
            UseSystemColors = false;
        }

        public override Color ToolStripDropDownBackground => Palette.Window;
        public override Color ImageMarginGradientBegin => Palette.Window;
        public override Color ImageMarginGradientMiddle => Palette.Window;
        public override Color ImageMarginGradientEnd => Palette.Window;

        public override Color MenuItemSelected => Palette.ControlHover;
        public override Color MenuItemSelectedGradientBegin => Palette.ControlHover;
        public override Color MenuItemSelectedGradientEnd => Palette.ControlHover;
        public override Color MenuItemBorder => Palette.ControlHover;
        public override Color MenuItemPressedGradientBegin => Palette.Card;
        public override Color MenuItemPressedGradientMiddle => Palette.Card;
        public override Color MenuItemPressedGradientEnd => Palette.Card;

        public override Color MenuBorder => Palette.Stroke;
        public override Color SeparatorDark => Palette.Divider;
        public override Color SeparatorLight => Palette.Divider;

        public override Color CheckBackground => Palette.AccentSoft;
        public override Color CheckSelectedBackground => Palette.AccentSoft;
        public override Color CheckPressedBackground => Palette.AccentSoft;
    }
}
