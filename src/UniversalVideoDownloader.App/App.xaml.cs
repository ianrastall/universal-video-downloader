using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
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
        services.AddSingleton(new LibationClient(() => FindLibation(settings.LibationDirectory)));
        services.AddSingleton<IDownloadRunner, DownloadRunner>();
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
        services.AddSingleton<AudibleViewModel>();
        services.AddSingleton<ClipboardInboxViewModel>();
        services.AddSingleton<ToolsViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<MainViewModel>();

        services.AddSingleton<MainWindow>();
        return services.BuildServiceProvider();
    }

    private static string? FindLibation(string? configuredDirectory)
    {
        if (!string.IsNullOrWhiteSpace(configuredDirectory))
        {
            var configured = Path.Combine(configuredDirectory, "LibationCli.exe");
            return File.Exists(configured) ? configured : null;
        }

        foreach (var hive in new[] { Registry.CurrentUser, Registry.LocalMachine })
        {
            using var uninstall = hive.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall");
            if (uninstall is null) continue;
            foreach (var keyName in uninstall.GetSubKeyNames())
            {
                using var app = uninstall.OpenSubKey(keyName);
                if (app?.GetValue("DisplayName") is string name
                    && (name == "Libation" || name.StartsWith("Libation (", StringComparison.OrdinalIgnoreCase))
                    && app.GetValue("InstallLocation") is string location)
                {
                    var candidate = Path.Combine(location, "LibationCli.exe");
                    if (File.Exists(candidate)) return candidate;
                }
            }
        }

        return null;
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
