using Microsoft.UI.Xaml.Controls;
using UniversalVideoDownloader.ViewModels;

namespace UniversalVideoDownloader.App.Views;

public sealed partial class AudiblePage : Page
{
    public AudiblePage()
    {
        ViewModel = App.GetService<AudibleViewModel>();
        InitializeComponent();
    }

    public AudibleViewModel ViewModel { get; }
}
