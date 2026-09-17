using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AudioMirror.Ui.ViewModels;

/// <summary>Meldet Änderungen an die Bindung. Mehr braucht es hier nicht.</summary>
internal abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void Raise([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    /// <summary>Setzt das Feld und meldet nur, wenn sich wirklich etwas geändert hat.</summary>
    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        Raise(name);
        return true;
    }
}
