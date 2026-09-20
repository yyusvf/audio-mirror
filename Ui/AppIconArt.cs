using System.Drawing;
using System.Drawing.Drawing2D;

namespace AudioMirror.Ui;

/// <summary>
/// Das Erscheinungsbild des Programms an einer Stelle: fünf Balken, symmetrisch um die Mitte
/// gespiegelt, im Indigo des Programms. Ohne Kachel und ohne Verlauf.
///
/// Hier kommen Infobereich, Taskleiste, Fenster und Titelleiste her - vorher war das Symbol im
/// Infobereich gezeichnet und das der Anwendung eine getrennt gepflegte Datei mit Kachel und
/// Verlauf. Zwei Quellen für dasselbe Bild laufen unweigerlich auseinander.
///
/// Die Maße beziehen sich auf eine Fläche von 32 Pixeln und werden auf die gewünschte Größe
/// hochgerechnet, damit jede Stufe der .ico-Datei für sich sauber gezeichnet wird.
/// </summary>
internal static class AppIconArt
{
    /// <summary>Mittelton zwischen den beiden Verlaufsfarben, die das Programm früher trug.</summary>
    public static readonly Color Colour = Color.FromArgb(0x63, 0x5D, 0xF1);

    private const float Reference = 32f;
    private const float BarWidth = 3.4f;
    private const float Gap = 1.9f;
    private const float BaseHeight = 24f;
    private static readonly float[] HeightFractions = [0.34f, 0.58f, 0.80f, 0.58f, 0.34f];

    /// <summary>
    /// Zeichnet das Symbol in eine neue Bitmap mit durchsichtigem Grund.
    ///
    /// Gezeichnet wird vierfach vergrößert und anschließend verkleinert. Bei 16 Pixeln ist ein
    /// Balken sonst weniger als zwei Pixel breit, und Kantenglättung allein ergibt dort eine
    /// graue Masse statt fünf erkennbarer Balken.
    /// </summary>
    public static Bitmap Render(int size)
    {
        const int oversample = 4;
        using Bitmap large = Draw(size * oversample);

        var result = new Bitmap(size, size);
        using Graphics g = Graphics.FromImage(result);
        g.Clear(Color.Transparent);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.DrawImage(large, new Rectangle(0, 0, size, size));
        return result;
    }

    private static Bitmap Draw(int size)
    {
        var bitmap = new Bitmap(size, size);
        using Graphics g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        using var brush = new SolidBrush(Colour);

        float scale = size / Reference;
        float barWidth = BarWidth * scale;
        float gap = Gap * scale;
        float baseHeight = BaseHeight * scale;
        float centre = size / 2f;

        float totalWidth = HeightFractions.Length * barWidth + (HeightFractions.Length - 1) * gap;
        float startX = centre - totalWidth / 2;

        for (int i = 0; i < HeightFractions.Length; i++)
        {
            float x = startX + i * (barWidth + gap) + barWidth / 2;
            FillPillBar(g, brush, x, centre, barWidth, baseHeight * HeightFractions[i]);
        }

        return bitmap;
    }

    /// <summary>
    /// Ein senkrechter Balken mit voll gerundeten Enden (Kapselform) - GDI+ kennt kein
    /// abgerundetes Rechteck von Haus aus, darum ein Rechteck fuer die Mitte plus je ein
    /// Kreis oben und unten, beide im Balkendurchmesser.
    /// </summary>
    private static void FillPillBar(Graphics g, Brush brush, float centerX, float centerY, float width, float height)
    {
        float half = width / 2;
        float top = centerY - height / 2;
        float bottom = centerY + height / 2;

        g.FillEllipse(brush, centerX - half, top - half, width, width);
        g.FillEllipse(brush, centerX - half, bottom - half, width, width);
        g.FillRectangle(brush, centerX - half, top, width, height);
    }
}
