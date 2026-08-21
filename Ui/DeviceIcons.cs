using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using AudioMirror.Audio;

namespace AudioMirror.Ui;

/// <summary>
/// Liefert die Gerätesymbole.
///
/// Bevorzugt werden dieselben Symbole, die auch die Windows-Soundeinstellungen zeigen: Windows
/// hinterlegt zu jedem Endpunkt einen Icon-Verweis der Form
/// <c>%windir%\system32\mmres.dll,-3010</c>, der hier direkt geladen wird. Dadurch passt die
/// Liste optisch zu Windows und zeigt auch herstellereigene Symbole, wenn ein Treiber welche
/// mitbringt.
///
/// Lässt sich daraus nichts laden, wird ein passendes Symbol selbst gezeichnet.
/// </summary>
internal static class DeviceIcons
{
    private static readonly Dictionary<(string, int, bool), Image?> ExtractedCache = [];
    private static readonly Dictionary<(AudioDeviceKind, int, bool), Image> DrawnCache = [];

    /// <param name="dimmed">Für getrennte Geräte: blasser dargestellt.</param>
    public static Image Get(string? iconPath, AudioDeviceKind kind, int size, bool dimmed)
    {
        if (!string.IsNullOrWhiteSpace(iconPath))
        {
            var key = (iconPath!, size, dimmed);
            if (!ExtractedCache.TryGetValue(key, out Image? extracted))
            {
                extracted = TryExtract(iconPath!, size, dimmed);
                ExtractedCache[key] = extracted;
            }
            if (extracted != null)
            {
                return extracted;
            }
        }

        return Draw(kind, size, dimmed);
    }

    /// <summary>Lädt das Symbol aus "Datei,Index" - genau der Verweis, den Windows mitliefert.</summary>
    private static Image? TryExtract(string iconPath, int size, bool dimmed)
    {
        IntPtr handle = IntPtr.Zero;
        try
        {
            int comma = iconPath.LastIndexOf(',');
            if (comma <= 0)
            {
                return null;
            }

            string file = Environment.ExpandEnvironmentVariables(iconPath[..comma].Trim().Trim('"'));
            if (!int.TryParse(iconPath[(comma + 1)..].Trim(), out int index))
            {
                return null;
            }

            // Größe direkt anfordern, statt ein 32er-Symbol hochzuskalieren - so bleibt es auch
            // bei hoher Anzeigeskalierung scharf.
            if (SHDefExtractIcon(file, index, 0, out handle, IntPtr.Zero, (uint)size) != 0 || handle == IntPtr.Zero)
            {
                return null;
            }

            using Icon icon = Icon.FromHandle(handle);
            using Bitmap source = icon.ToBitmap();

            var result = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.Clear(Color.Transparent);

                if (dimmed)
                {
                    // Getrennte Geräte blasser zeichnen; Einfärben ginge bei einem fertigen
                    // Symbol nicht ohne es zu entstellen.
                    var matrix = new ColorMatrix { Matrix33 = 0.40f };
                    using var attributes = new ImageAttributes();
                    attributes.SetColorMatrix(matrix);
                    g.DrawImage(source, new Rectangle(0, 0, size, size),
                        0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
                }
                else
                {
                    g.DrawImage(source, new Rectangle(0, 0, size, size));
                }
            }
            return result;
        }
        catch
        {
            return null;
        }
        finally
        {
            if (handle != IntPtr.Zero)
            {
                DestroyIcon(handle);
            }
        }
    }

    private static Image Draw(AudioDeviceKind kind, int size, bool dimmed)
    {
        var key = (kind, size, dimmed);
        if (DrawnCache.TryGetValue(key, out Image? cached))
        {
            return cached;
        }

        Color colour = dimmed ? SystemColors.GrayText : SystemColors.ControlText;
        var bitmap = new Bitmap(size, size);
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            g.ScaleTransform(size / 32f, size / 32f);
            DrawShape(g, kind, colour);
        }

        DrawnCache[key] = bitmap;
        return bitmap;
    }

    private static void DrawShape(Graphics g, AudioDeviceKind kind, Color colour)
    {
        using var pen = new Pen(colour, 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var brush = new SolidBrush(colour);

        switch (kind)
        {
            case AudioDeviceKind.Headphones:
                g.DrawArc(pen, 5, 6, 22, 22, 180, 180);
                g.FillRectangle(brush, 4, 16, 6, 11);
                g.FillRectangle(brush, 22, 16, 6, 11);
                break;

            case AudioDeviceKind.Speakers:
                g.DrawRectangle(pen, 8, 4, 16, 24);
                g.FillEllipse(brush, 13, 15, 6, 6);
                g.FillEllipse(brush, 14.5f, 8.5f, 3, 3);
                break;

            case AudioDeviceKind.Display:
                g.DrawRectangle(pen, 3, 6, 26, 16);
                g.DrawLine(pen, 16, 22, 16, 26);
                g.DrawLine(pen, 10, 26, 22, 26);
                break;

            case AudioDeviceKind.Digital:
                g.DrawEllipse(pen, 5, 5, 22, 22);
                g.FillEllipse(brush, 14.5f, 9, 3, 3);
                g.FillEllipse(brush, 9, 18, 3, 3);
                g.FillEllipse(brush, 20, 18, 3, 3);
                break;

            default:
                g.FillPolygon(brush, new[]
                {
                    new PointF(6, 12), new PointF(11, 12), new PointF(16, 7),
                    new PointF(16, 25), new PointF(11, 20), new PointF(6, 20),
                });
                g.DrawArc(pen, 15, 9, 9, 14, -60, 120);
                g.DrawArc(pen, 17, 5, 14, 22, -60, 120);
                break;
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "SHDefExtractIconW")]
    private static extern int SHDefExtractIcon(
        string iconFile, int index, uint flags, out IntPtr largeIcon, IntPtr smallIcon, uint iconSize);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr icon);
}
