using System.Collections.ObjectModel;

namespace AudioMirror.Ui.ViewModels;

/// <summary>Ein Eintrag der Quellenauswahl. <c>Id</c> leer heißt: Windows-Standardgerät.</summary>
internal sealed record SourceItem(string? Id, string Name)
{
    // Die Auswahlliste zeigt an, was hier herauskommt. Ohne das stünde in der zugeklappten
    // Liste die Datensatz-Schreibweise mit allen Feldern.
    public override string ToString() => Name;
}

/// <summary>Was die Geräteseite anzeigt. Die Verbindung zur Spiegelung stellt die Seite selbst her.</summary>
internal sealed class DevicesPageViewModel : ViewModelBase
{
    private SourceItem? selectedSource;
    private string status = string.Empty;

    public string SourceLabel => Strings.Source;

    public ObservableCollection<SourceItem> Sources { get; } = [];

    public ObservableCollection<DeviceViewModel> Devices { get; } = [];

    public SourceItem? SelectedSource
    {
        get => selectedSource;
        set => Set(ref selectedSource, value);
    }

    public string Status
    {
        get => status;
        set => Set(ref status, value);
    }
}
