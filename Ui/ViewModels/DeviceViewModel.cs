using System.Collections.ObjectModel;
using AudioMirror.Audio;

namespace AudioMirror.Ui.ViewModels;

/// <summary>Eine Anwendung in der Mischung eines Zielgeräts.</summary>
internal sealed class AppViewModel : ViewModelBase
{
    private bool isEnabled = true;
    private float volume = 1f;

    public AppViewModel(string key, string displayName)
    {
        Key = key;
        DisplayName = displayName;
    }

    public string Key { get; }

    public string DisplayName { get; }

    public bool IsEnabled
    {
        get => isEnabled;
        set => Set(ref isEnabled, value);
    }

    public float Volume
    {
        get => volume;
        set
        {
            if (Set(ref volume, value))
            {
                Raise(nameof(VolumeText));
            }
        }
    }

    public string VolumeText => $"{volume * 100:0} %";
}

/// <summary>
/// Ein Zielgerät in der Liste. Hält nur, was die Oberfläche zeigt - was daraus an Spiegelung
/// wird, entscheidet weiterhin die Schicht darunter.
/// </summary>
internal sealed class DeviceViewModel : ViewModelBase
{
    private bool isEnabled;
    private float volume = 1f;
    private bool isExpanded;
    private string status = string.Empty;

    public DeviceViewModel(string deviceId, string displayName, AudioDeviceKind kind, bool isSource)
    {
        DeviceId = deviceId;
        DisplayName = displayName;
        Kind = kind;
        IsSource = isSource;

        Apps.CollectionChanged += (_, _) => Raise(nameof(HasApps));
    }

    public string DeviceId { get; }

    public string DisplayName { get; }

    public AudioDeviceKind Kind { get; }

    /// <summary>Das Quellgerät gibt bereits selbst aus und käme als Ziel in eine Rückkopplung.</summary>
    public bool IsSource { get; }

    public bool CanToggle => !IsSource;

    /// <summary>
    /// Glyphe aus der Symbolschrift, passend zur Geräteart. Die Symbole, die Windows den
    /// Geräten selbst zuweist, kommen später dazu - dafür muss das Handle aus SHDefExtractIcon
    /// erst in eine WPF-Bildquelle übersetzt werden.
    /// </summary>
    public string Glyph => Kind switch
    {
        AudioDeviceKind.Headphones => "",
        AudioDeviceKind.Display => "",
        AudioDeviceKind.Digital => "",
        _ => "",
    };

    public bool IsEnabled
    {
        get => isEnabled;
        set => Set(ref isEnabled, value);
    }

    public float Volume
    {
        get => volume;
        set
        {
            if (Set(ref volume, value))
            {
                Raise(nameof(VolumeText));
            }
        }
    }

    public string VolumeText => $"{volume * 100:0} %";

    public bool IsExpanded
    {
        get => isExpanded;
        set
        {
            if (Set(ref isExpanded, value))
            {
                Raise(nameof(ExpandGlyph));
            }
        }
    }

    public string ExpandGlyph => isExpanded ? "" : "";

    public string Status
    {
        get => status;
        set => Set(ref status, value);
    }

    public ObservableCollection<AppViewModel> Apps { get; } = [];

    /// <summary>Ohne Anwendungen gibt es nichts aufzuklappen - dann entfällt der Pfeil.</summary>
    public bool HasApps => Apps.Count > 0;
}
