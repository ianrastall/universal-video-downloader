using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using UniversalVideoDownloader.App.Services;
using UniversalVideoDownloader.Core;
using UniversalVideoDownloader.Core.Downloads;
using UniversalVideoDownloader.Core.Settings;
using UniversalVideoDownloader.Core.Tools;
using UniversalVideoDownloader.ViewModels;
using UniversalVideoDownloader.ViewModels.Services;

namespace UniversalVideoDownloader.App;

public partial class App : Application
{
    public static Jupiter.UI.AccentService Accent { get; private set; } = null!;
    public static Jupiter.UI.ThemeService Themes { get; private set; } = null!;
    private ServiceProvider? _services;
    private MainWindow? _window;

    public App()
    {
        InitializeComponent();
        UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            CrashLog.Write(e.Exception);
            e.SetObserved();
        };
    }

    public static IServiceProvider Services => ((App)Current)._services ?? throw new InvalidOperationException("Services are not ready.");

    public static T GetService<T>()
        where T : notnull => Services.GetRequiredService<T>();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        if (!SingleInstance.TryClaim())
        {
            // Another copy is running: bring it forward instead of opening a second queue and watcher.
            SingleInstance.ActivateExisting(MainWindow.WindowTitle);
            Exit();
            return;
        }

        _services = ConfigureServices(DispatcherQueue.GetForCurrentThread());
        var appearance = new AppearancePreferences(_services.GetRequiredService<SettingsService>());
        Accent = new(appearance);
        Themes = new(appearance);
        _window = _services.GetRequiredService<MainWindow>();
        _window.Closed += (_, _) => _services.Dispose();
        _window.Activate();
    }

    private static ServiceProvider ConfigureServices(DispatcherQueue dispatcherQueue)
    {
        var settingsStore = SettingsStore.Default;
        var settings = settingsStore.Load();

        var services = new ServiceCollection();

        // Core
        services.AddSingleton(new SettingsService(settings, settingsStore));
        services.AddSingleton(QueueStore.Default);
        services.AddSingleton<IDownloadRunner, YtDlpRunner>();
        services.AddSingleton<IToolManager>(_ => new ToolManager());

        // Platform services
        services.AddSingleton<IUiDispatcher>(new DispatcherQueueUiDispatcher(dispatcherQueue));
        services.AddSingleton<WindowContext>();
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<IPickerService, PickerService>();
        services.AddSingleton<IShellService, ShellService>();

        // View models
        services.AddSingleton<NotificationViewModel>();
        services.AddSingleton<WorkCoordinator>();
        services.AddSingleton<DownloadsViewModel>();
        services.AddSingleton<ClipboardInboxViewModel>();
        services.AddSingleton<ToolsViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<MainViewModel>();

        services.AddSingleton<MainWindow>();
        return services.BuildServiceProvider();
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        CrashLog.Write(e.Exception);
        if (_services?.GetService<NotificationViewModel>() is { } notifications)
        {
            // Keep the app alive; the queue and settings are saved as they change.
            e.Handled = true;
            notifications.Show($"Something went wrong: {e.Exception.Message} (details in {CrashLog.Path})", NotificationLevel.Error);
        }
    }
}

internal static class CrashLog
{
    public static string Path => System.IO.Path.Combine(AppPaths.DataDirectory, "error.log");

    public static void Write(Exception exception)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.DataDirectory);
            File.AppendAllText(Path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
