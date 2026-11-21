using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniversalVideoDownloader.Core.Downloads;
using UniversalVideoDownloader.Core.Settings;
using UniversalVideoDownloader.ViewModels.Services;

namespace UniversalVideoDownloader.ViewModels;

public sealed partial class AudibleViewModel(
    LibationClient libation,
    SettingsService settings,
    IPickerService pickers,
    IShellService shell,
    DownloadsViewModel downloads,
    NotificationViewModel notifications) : ObservableObject
{
    public ObservableCollection<AudibleTitle> Titles { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(QueueSelectedCommand))]
    public partial AudibleTitle? SelectedTitle { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand), nameof(LoadSavedCommand), nameof(ChooseLibationCommand), nameof(OpenBooksFolderCommand), nameof(CancelCommand))]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string Status { get; set; } = "Open Libation to set up your Audible account, then load your library.";

    private bool CanManage() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanManage))]
    private async Task ChooseLibationAsync()
    {
        var folder = await pickers.PickFolderAsync();
        if (folder is null) return;
        if (!File.Exists(Path.Combine(folder, "LibationCli.exe")))
        {
            Status = "Select the folder containing LibationCli.exe.";
            return;
        }

        settings.Current.LibationDirectory = folder;
        Status = settings.Save(out var error) ? "Libation location saved. Load your library when ready." : $"Could not save the location: {error}";
        SelectedTitle = null;
        Titles.Clear();
    }

    [RelayCommand]
    private void OpenLibation()
    {
        if (libation.ExecutablePath is { } cli && File.Exists(Path.Combine(Path.GetDirectoryName(cli)!, "Libation.exe")))
        {
            shell.OpenFile(Path.Combine(Path.GetDirectoryName(cli)!, "Libation.exe"));
        }
        else
        {
            Status = "Install Libation from its release page or select its installation folder.";
        }
    }

    [RelayCommand(CanExecute = nameof(CanManage))]
    private Task RefreshAsync(CancellationToken ct) => LoadAsync(scan: true, ct);

    [RelayCommand(CanExecute = nameof(CanManage))]
    private Task LoadSavedAsync(CancellationToken ct) => LoadAsync(scan: false, ct);

    [RelayCommand(CanExecute = nameof(CanManage))]
    private async Task OpenBooksFolderAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var folder = await libation.GetBooksFolderAsync(ct);
            if (folder is not null && Directory.Exists(folder))
            {
                shell.OpenFolder(folder);
            }
            else
            {
                Status = "Set the Books folder in Libation first.";
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { Status = ex.Message; }
        finally { IsBusy = false; }
    }

    [RelayCommand(CanExecute = nameof(IsBusy))]
    public void Cancel()
    {
        RefreshCommand.Cancel();
        LoadSavedCommand.Cancel();
        OpenBooksFolderCommand.Cancel();
    }

    private async Task LoadAsync(bool scan, CancellationToken ct)
    {
        IsBusy = true;
        Status = scan ? "Scanning Audible through Libationâ€¦" : "Loading Libationâ€™s saved libraryâ€¦";
        try
        {
            var books = await libation.ReadLibraryAsync(scan, ct);
            SelectedTitle = null;
            Titles.Clear();
            foreach (var book in books) Titles.Add(book);
            Status = Titles.Count > 0
                ? $"{Formatting.Plural(Titles.Count, "title")}. Audiobooks use Libationâ€™s Books folder and audio format."
                : "Your library is empty. Sign in through Libation and refresh.";
        }
        catch (OperationCanceledException) { Status = "Library load canceled."; }
        catch (Exception ex) { Status = ex.Message; }
        finally { IsBusy = false; }
    }

    private bool CanQueueSelected() => SelectedTitle is not null;

    [RelayCommand(CanExecute = nameof(CanQueueSelected))]
    private void QueueSelected()
    {
        if (SelectedTitle is not { } selected) return;
        var result = downloads.Enqueue([selected.Link], DownloadMode.Audiobook,
            new Dictionary<string, string> { [selected.Link] = selected.Title });
        notifications.Show(
            result.Added > 0 ? $"Queued {selected.Title}. See Downloads for progress." : "This title is already queued.",
            result.Added > 0 ? NotificationLevel.Success : NotificationLevel.Info);
    }
}
