using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniversalVideoDownloader.App.Services;
using UniversalVideoDownloader.App.Views;
using UniversalVideoDownloader.ViewModels;
using System.Windows.Input;
using Windows.Graphics;
using WinRT.Interop;

namespace UniversalVideoDownloader.App;

public sealed partial class MainWindow : Window
{
    public const string WindowTitle = "Universal Video Downloader";
    private bool _started;
    public MainViewModel ViewModel { get; }
    public DownloadsViewModel Downloads { get; } = App.GetService<DownloadsViewModel>();
    public ClipboardInboxViewModel Inbox { get; } = App.GetService<ClipboardInboxViewModel>();
    public ToolsViewModel Tools { get; } = App.GetService<ToolsViewModel>();
    public MainWindow(MainViewModel viewModel, WindowContext windowContext)
    {
        ViewModel = viewModel;
        InitializeComponent();
        SettingsMenuItem.KeyboardAccelerators.Add(new Microsoft.UI.Xaml.Input.KeyboardAccelerator { Key = (Windows.System.VirtualKey)188, Modifiers = Windows.System.VirtualKeyModifiers.Control });
        windowContext.Handle = WindowNative.GetWindowHandle(this);
        App.Themes.Attach(this);
        App.Accent.Attach(RootGrid);
        bool? narrow = null;
        RootGrid.SizeChanged += (_, _) =>
        {
            var next = RootGrid.ActualWidth < 1050;
            if (next != narrow) { narrow = next; Nav.IsPaneOpen = !next; }
        };
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "UniversalVideoDownloader.ico"));
        RootGrid.Loaded += OnLoaded;
        Closed += (_, _) => { SavePlacement(); ViewModel.Shutdown(); App.Themes.Detach(this); App.Accent.Detach(RootGrid); };
    }
    private void RestorePlacement()
    {
        var p = App.GetService<SettingsService>().Current;
        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
        var scale = RootGrid.XamlRoot.RasterizationScale;
        var width = (int)Math.Clamp(p.WindowWidth * scale, Math.Min(800 * scale, area.Width), area.Width);
        var height = (int)Math.Clamp(p.WindowHeight * scale, Math.Min(560 * scale, area.Height), area.Height);
        int x = p.WindowX == int.MinValue ? area.X + (area.Width - width) / 2 : Math.Clamp(p.WindowX, area.X, area.X + area.Width - width);
        int y = p.WindowY == int.MinValue ? area.Y + (area.Height - height) / 2 : Math.Clamp(p.WindowY, area.Y, area.Y + area.Height - height);
        AppWindow.MoveAndResize(new RectInt32(x, y, width, height));
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = Math.Min((int)(800 * scale), area.Width);
            presenter.PreferredMinimumHeight = Math.Min((int)(560 * scale), area.Height);
            if (p.WindowMaximized) presenter.Maximize();
        }
    }
    private void SavePlacement()
    {
        var service = App.GetService<SettingsService>();
        var p = service.Current;
        var state = (AppWindow.Presenter as OverlappedPresenter)?.State;
        if (state == OverlappedPresenterState.Minimized) return;
        p.WindowMaximized = state == OverlappedPresenterState.Maximized;
        if (!p.WindowMaximized && RootGrid.XamlRoot is not null)
        {
            p.WindowX = AppWindow.Position.X; p.WindowY = AppWindow.Position.Y;
            p.WindowWidth = (int)(AppWindow.Size.Width / RootGrid.XamlRoot.RasterizationScale);
            p.WindowHeight = (int)(AppWindow.Size.Height / RootGrid.XamlRoot.RasterizationScale);
        }
        if (!service.Save(out var error)) CrashLog.Write(new IOException(error));
    }
    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_started) return;
        _started = true;
        RestorePlacement();
        Nav.SelectedItem = DownloadsItem;
        try { await ViewModel.StartAsync(); }
        catch (Exception ex) { CrashLog.Write(ex); ViewModel.Notifications.Show($"Start-up failed: {ex.Message}", NotificationLevel.Error); }
    }
    private void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        var tag = (args.SelectedItem as NavigationViewItem)?.Tag as string;
        var page = args.IsSettingsSelected ? typeof(SettingsPage) : tag == "Tools" ? typeof(ToolsPage) : tag == "Clipboard" ? typeof(ClipboardPage) : typeof(DownloadsPage);
        if (ContentFrame.CurrentSourcePageType != page) ContentFrame.Navigate(page);
    }
    private void SettingsClicked(object sender, RoutedEventArgs e) => Nav.SelectedItem = Nav.SettingsItem;
    private void DownloadsClicked(object sender, RoutedEventArgs e) => Nav.SelectedItem = DownloadsItem;
    private void ClipboardClicked(object sender, RoutedEventArgs e) => Nav.SelectedItem = ClipboardItem;
    private void ToolsClicked(object sender, RoutedEventArgs e) => Nav.SelectedItem = ToolsItem;
    private void ExitClicked(object sender, RoutedEventArgs e) => Close();
    private void ItemClicked(object sender, RoutedEventArgs e)
    {
        if (ContentFrame.Content is not DownloadsPage page || page.SelectedItem is not { } item)
        { ViewModel.Notifications.Show("Select a queue item on the Downloads page first.", NotificationLevel.Info); return; }
        ICommand? command = ((sender as MenuFlyoutItem)?.Tag as string) switch
        {
            "OpenFile" => item.OpenFileCommand, "RevealFile" => item.RevealFileCommand,
            "Retry" => item.RetryCommand, "Cancel" => item.CancelCommand, "ToggleLog" => item.ToggleLogCommand,
            "CopyUrl" => item.CopyUrlCommand, "Remove" => item.RemoveCommand, _ => null,
        };
        if (command?.CanExecute(null) == true) command.Execute(null);
    }
    private async void DocumentationClicked(object sender, RoutedEventArgs e) => await Windows.System.Launcher.LaunchUriAsync(new Uri("https://github.com/ianrastall/universal-video-downloader#readme"));
    private async void DiscussionsClicked(object sender, RoutedEventArgs e) => await Windows.System.Launcher.LaunchUriAsync(new Uri("https://github.com/ianrastall/universal-video-downloader/discussions"));
    private async void AboutClicked(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog { XamlRoot = RootGrid.XamlRoot, Title = WindowTitle,
            Content = "Part of Jupiter\nVersion 0.2.0 · MIT license\nWinUI 3 shell and Rust download parser\nBuild: " + typeof(App).Assembly.GetName().Version,
            CloseButtonText = "Close" };
        App.Themes.Attach(dialog);
        try { await dialog.ShowAsync(); } finally { App.Themes.Detach(dialog); }
    }
}
