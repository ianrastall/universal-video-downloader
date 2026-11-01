using Microsoft.UI.Xaml.Controls;
using UniversalVideoDownloader.ViewModels;
namespace UniversalVideoDownloader.App.Views;
public sealed partial class ClipboardPage : Page
{
    public ClipboardInboxViewModel Inbox { get; } = App.GetService<ClipboardInboxViewModel>();
    public ClipboardPage() { InitializeComponent(); }
}
