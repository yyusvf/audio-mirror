using System.Windows;
using System.Windows.Controls;
using AudioMirror.Ui.ViewModels;

namespace AudioMirror.Ui.Views;

/// <summary>Die Einstellungsseite. Alles Weitere steckt im Ansichtsmodell.</summary>
public partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    private async void OnCheckNow(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsPageViewModel model)
        {
            await model.CheckAsync(manual: true);
        }
    }
}
