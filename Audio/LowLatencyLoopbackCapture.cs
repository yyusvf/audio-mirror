using NAudio.CoreAudioApi;

namespace AudioMirror.Audio;

/// <summary>
/// Loopback-Aufnahme mit einstellbarer Puffergroesse.
///
/// NAudios <c>WasapiLoopbackCapture</c> bietet nur einen Konstruktor ohne Puffergroesse und
/// legt damit 100 ms zugrunde; der Aufnahmethread liefert die Daten dann in Schueben von
/// ~50 ms. Das allein sprengt bereits das Latenzziel von 20-40 ms. Da <c>WasapiCapture</c>
/// die Puffergroesse im Konstruktor annimmt und das Loopback-Flag ueber eine virtuelle
/// Methode gesetzt wird, leiten wir direkt von der Basisklasse ab.
/// </summary>
internal sealed class LowLatencyLoopbackCapture : WasapiCapture
{
    public LowLatencyLoopbackCapture(MMDevice renderDevice, int bufferMilliseconds)
        : base(renderDevice, false, bufferMilliseconds)
    {
        // Die Basisklasse uebernimmt bereits das MixFormat des Render-Geraets als WaveFormat,
        // was fuer Loopback genau das gewuenschte Format ist.
    }

    protected override AudioClientStreamFlags GetAudioClientStreamFlags() => AudioClientStreamFlags.Loopback;
}
