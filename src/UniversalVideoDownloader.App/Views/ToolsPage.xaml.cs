using Microsoft.UI.Xaml.Controls;
using UniversalVideoDownloader.ViewModels;

namespace UniversalVideoDownloader.App.Views;

public sealed partial class ToolsPage : Page
{
    public ToolsPage()
    {
        ViewModel = App.GetService<ToolsViewModel>();
        InitializeComponent();
    }

    public ToolsViewModel ViewModel { get; }
}
