using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AudioMirror.Ui.Views;

/// <summary>
/// Holt das Symbol, das Windows einem Tongerät selbst zuweist - dasselbe, das in den
/// Sound-Einstellungen neben dem Gerät steht.
///
/// Windows hinterlegt es als "Datei,Index" an der Geräteeigenschaft. Gelingt das Auslesen
/// nicht, liefert diese Klasse <c>null</c>; die Zeile zeigt dann die Glyphe zur Geräteart.
/// </summary>
internal static class DeviceIconSource
{
    [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "SHDefExtractIconW")]
    private static extern int SHDefExtractIcon(
        string file, int index, uint flags, out IntPtr large, IntPtr small, uint size);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr handle);

    /// <summary>Einmal geholt, dann behalten - die Liste baut sich im Sekundentakt neu auf.</summary>
    private static readonly Dictionary<string, ImageSource?> Cache = [];

    public static ImageSource? TryLoad(string? iconPath, int size = 32)
    {
        if (string.IsNullOrWhiteSpace(iconPath))
        {
            return null;
        }

        string key = iconPath + "|" + size;
        if (Cache.TryGetValue(key, out ImageSource? cached))
        {
            return cached;
        }

        ImageSource? loaded = Extract(iconPath, size);
        Cache[key] = loaded;
        return loaded;
    }

    private static ImageSource? Extract(string iconPath, int size)
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

            // Die gewünschte Größe direkt anfordern, statt ein 32er-Symbol hochzurechnen - so
            // bleibt es auch bei hoher Anzeigeskalierung scharf.
            if (SHDefExtractIcon(file, index, 0, out handle, IntPtr.Zero, (uint)size) != 0
                || handle == IntPtr.Zero)
            {
                return null;
            }

            BitmapSource bitmap = Imaging.CreateBitmapSourceFromHIcon(
                handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            // Einfrieren: so lässt es sich aus jedem Strang verwenden und WPF spart sich die
            // Änderungsverfolgung.
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            // Pfad zeigt ins Leere, Datei nicht lesbar, Symbol nicht vorhanden - dann eben die
            // Glyphe zur Geräteart.
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
}
