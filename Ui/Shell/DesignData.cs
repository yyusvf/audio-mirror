#if DEBUG
using AudioMirror.Audio;
using AudioMirror.Ui.ViewModels;

namespace AudioMirror.Ui.Shell;

/// <summary>
/// Beispieldaten für die Abzeichnung der Oberfläche. Nur damit die Zeilen etwas zeigen,
/// solange die Anbindung an die Spiegelung noch nicht steht - im ausgelieferten Programm
/// ist davon nichts enthalten.
/// </summary>
internal static class DesignData
{
    public static DevicesPageViewModel Devices()
    {
        var model = new DevicesPageViewModel
        {
            Status = "Spiegelung läuft auf 2 Gerät(en).",
        };

        model.Sources.Add(new SourceItem(null, "Windows-Standardgerät (3 - XG27AQDMGR)"));
        model.SelectedSource = model.Sources[0];

        model.Devices.Add(new DeviceViewModel("1", "3 - XG27AQDMGR (AMD High Definition Audio)",
            AudioDeviceKind.Display, isSource: true)
        {
            Status = Strings.SourceShort,
            Volume = 1f,
        });

        var headphones = new DeviceViewModel("2", "Kopfhörer (JadeAudio JA11)",
            AudioDeviceKind.Headphones, isSource: false)
        {
            IsEnabled = true,
            Volume = 0.8f,
            Status = "läuft – kompletter Ton, ca. 32 ms",
            IsExpanded = true,
        };
        headphones.Apps.Add(new AppViewModel("spotify", "Spotify") { Volume = 1f });
        headphones.Apps.Add(new AppViewModel("chrome", "Google Chrome") { Volume = 0.65f });
        headphones.Apps.Add(new AppViewModel("discord", "Discord") { IsEnabled = false, Volume = 1f });
        model.Devices.Add(headphones);

        model.Devices.Add(new DeviceViewModel("3", "Lautsprecher (Realtek(R) Audio)",
            AudioDeviceKind.Speakers, isSource: false)
        {
            IsEnabled = true,
            Volume = 0.45f,
            Status = "läuft – 2 Anwendung(en), ca. 30 ms",
        });

        model.Devices.Add(new DeviceViewModel("4", "Digitalausgang (S/PDIF)",
            AudioDeviceKind.Digital, isSource: false)
        {
            Volume = 1f,
            Status = "nicht angehakt",
        });

        return model;
    }
}
#endif
