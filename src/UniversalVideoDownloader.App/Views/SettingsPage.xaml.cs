using Microsoft.UI.Xaml.Controls;
using UniversalVideoDownloader.ViewModels;

namespace UniversalVideoDownloader.App.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        ViewModel = App.GetService<SettingsViewModel>();
        InitializeComponent();
    }

    public SettingsViewModel ViewModel { get; }
}
