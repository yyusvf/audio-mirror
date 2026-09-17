using System.Collections.ObjectModel;
using AudioMirror.Audio;

namespace AudioMirror.Ui.ViewModels;

/// <summary>Eine Anwendung in der Mischung eines Zielgeräts.</summary>
internal sealed class AppViewModel : ViewModelBase
{
    private bool isEnabled = true;
    private float volume = 1f;
    private string status = string.Empty;

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

    /// <summary>Hinweis an der Zeile, etwa wenn sich der Ton nicht abgreifen laesst.</summary>
    public string Status
    {
        get => status;
        set
        {
            if (Set(ref status, value))
            {
                Raise(nameof(HasStatus));
            }
        }
    }

    public bool HasStatus => !string.IsNullOrEmpty(status);
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

    public DeviceViewModel(string deviceId, string displayName, AudioDeviceKind kind, bool isSource,
        string? iconPath = null)
    {
        DeviceId = deviceId;
        DisplayName = displayName;
        Kind = kind;
        IsSource = isSource;
        Icon = Views.DeviceIconSource.TryLoad(iconPath);

        Apps.CollectionChanged += (_, _) => Raise(nameof(HasApps));
    }

    public string DeviceId { get; }

    public string DisplayName { get; }

    public AudioDeviceKind Kind { get; }

    /// <summary>Das Quellgerät gibt bereits selbst aus und käme als Ziel in eine Rückkopplung.</summary>
    public bool IsSource { get; }

    public bool CanToggle => !IsSource;

    /// <summary>
    /// Das Symbol, das Windows dem Gerät selbst zuweist. Null, wenn keines hinterlegt ist -
    /// dann zeigt die Zeile <see cref="Glyph"/>.
    /// </summary>
    public System.Windows.Media.ImageSource? Icon { get; }

    public bool HasIcon => Icon != null;

    /// <summary>Glyphe zur Geräteart - der Rückfall, wenn Windows kein Symbol hinterlegt hat.</summary>
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
