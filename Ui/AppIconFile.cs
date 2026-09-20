#if DEBUG
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace AudioMirror.Ui;

/// <summary>
/// Schreibt AudioMirror.ico aus <see cref="AppIconArt"/>. Nur im Entwicklungsstand vorhanden
/// und von Hand aufzurufen (<c>--makeicon &lt;Pfad&gt;</c>); im fertigen Programm liegt die
/// Datei als Ergebnis bei.
///
/// So bleibt die Datei das Abbild der Zeichnung und nicht ihr Konkurrent: wer die Balken in
/// <see cref="AppIconArt"/> ändert, ruft das hier auf und hat dieselbe Form überall.
/// </summary>
internal static class AppIconFile
{
    /// <summary>
    /// Von 16 bis 256. Jede Stufe wird für sich gezeichnet - Windows greift je nach Stelle
    /// (Infobereich, Taskleiste, Alt+Tab, Explorer) zu einer anderen.
    /// </summary>
    private static readonly int[] Sizes = [16, 20, 24, 32, 40, 48, 64, 128, 256];

    public static void Write(string path)
    {
        List<byte[]> frames = [];
        foreach (int size in Sizes)
        {
            using Bitmap bitmap = AppIconArt.Render(size);
            using var buffer = new MemoryStream();
            bitmap.Save(buffer, ImageFormat.Png);
            frames.Add(buffer.ToArray());
        }

        using var file = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(file);

        // Kopf: Kennung, Art (1 = Symbol), Anzahl der Stufen.
        writer.Write((ushort)0);
        writer.Write((ushort)1);
        writer.Write((ushort)frames.Count);

        // Jede Stufe steht mit 16 Byte im Verzeichnis; die Bilddaten folgen dahinter am Stück.
        int offset = 6 + frames.Count * 16;
        for (int i = 0; i < frames.Count; i++)
        {
            // 256 passt nicht in ein Byte und wird laut Format als 0 geschrieben.
            writer.Write((byte)(Sizes[i] == 256 ? 0 : Sizes[i]));
            writer.Write((byte)(Sizes[i] == 256 ? 0 : Sizes[i]));
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((ushort)1);
            writer.Write((ushort)32);
            writer.Write(frames[i].Length);
            writer.Write(offset);
            offset += frames[i].Length;
        }

        foreach (byte[] frame in frames)
        {
            writer.Write(frame);
        }
    }
}
#endif
