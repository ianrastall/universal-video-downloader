using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniversalVideoDownloader.Core.Links;
using UniversalVideoDownloader.ViewModels;
using Windows.ApplicationModel.DataTransfer;

namespace UniversalVideoDownloader.App.Views;

public sealed partial class DownloadsPage : Page
{
    public DownloadsPage()
    {
        ViewModel = App.GetService<DownloadsViewModel>();
        Inbox = App.GetService<ClipboardInboxViewModel>();
        InitializeComponent();
    }

    public DownloadItemViewModel? SelectedItem => QueueList.SelectedItem as DownloadItemViewModel;

    public DownloadsViewModel ViewModel { get; }

    public ClipboardInboxViewModel Inbox { get; }

    // Links dragged from a browser's address bar or a page arrive as text or as a web link.
    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.Text) || e.DataView.Contains(StandardDataFormats.WebLink) || e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "Add to queue";
        }
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        var deferral = e.GetDeferral();
        try
        {
            string? text = null;
            if (e.DataView.Contains(StandardDataFormats.WebLink))
            {
                text = (await e.DataView.GetWebLinkAsync())?.ToString();
            }

            if (text is null && e.DataView.Contains(StandardDataFormats.Text))
            {
                text = await e.DataView.GetTextAsync();
            }

            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var content = new System.Text.StringBuilder(text);
                foreach (var file in (await e.DataView.GetStorageItemsAsync()).OfType<Windows.Storage.StorageFile>())
                {
                    if (new[] { ".txt", ".md", ".csv", ".html", ".htm" }.Contains(file.FileType.ToLowerInvariant()))
                        content.AppendLine(await Windows.Storage.FileIO.ReadTextAsync(file));
                }
                text = content.ToString();
            }
            var links = LinkExtractor.FromText(text);
            if (links.Count > 0)
            {
                ViewModel.Report(ViewModel.Enqueue(links, ViewModel.AddMode), "");
            }
        }
        catch (Exception ex) when (ex is System.Runtime.InteropServices.COMException or UnauthorizedAccessException or IOException)
        {
            App.GetService<NotificationViewModel>().Show($"Could not read what was dropped: {ex.Message}", NotificationLevel.Warning);
        }
        finally { deferral.Complete(); }
    }
}
