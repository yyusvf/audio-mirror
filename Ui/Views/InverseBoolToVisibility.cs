using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AudioMirror.Ui.Views;

/// <summary>
/// Umgekehrte Sichtbarkeit: wahr blendet aus, falsch zeigt an. WPF bringt nur die
/// gleichgerichtete Umrechnung mit.
/// </summary>
internal sealed class InverseBoolToVisibility : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is Visibility.Collapsed;
}
