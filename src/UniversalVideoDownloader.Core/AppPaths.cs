using UniversalVideoDownloader.Core.Tools;

namespace UniversalVideoDownloader.Core;

/// <summary>
/// Where the app keeps its settings, queue and tools.
/// Everything lives under one per-user folder so nothing is scattered across the system.
/// </summary>
public static class AppPaths
{
    public const string AppFolderName = "UniversalVideoDownloader";

    /// <summary>Set this environment variable to relocate all app data (portable use, testing).</summary>
    public const string HomeOverrideVariable = "UVD_HOME";

    public static string DataDirectory
    {
        get
        {
            var overridden = Environment.GetEnvironmentVariable(HomeOverrideVariable);
            if (!string.IsNullOrWhiteSpace(overridden))
            {
                return overridden;
            }

            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrEmpty(local))
            {
                local = AppContext.BaseDirectory;
            }

            return Path.Combine(local, AppFolderName);
        }
    }

    public static string SettingsFile => Path.Combine(DataDirectory, "settings.json");

    public static string QueueFile => Path.Combine(DataDirectory, "queue.json");

    public static string DefaultToolsDirectory => Path.Combine(DataDirectory, "tools");

    public static string DefaultOutputDirectory
    {
        get
        {
            var videos = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
            if (string.IsNullOrEmpty(videos))
            {
                videos = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }

            return Path.Combine(videos, "yt-dlp");
        }
    }

    /// <summary>
    /// The tools directory: the configured one if set; otherwise a <c>tools</c> folder next to the
    /// executable (or the executable's own folder) when it already holds yt-dlp, which keeps
    /// portable copies self-contained; otherwise the per-user default.
    /// </summary>
    public static string ResolveToolsDirectory(string? configured)
    {
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        var exeDir = AppContext.BaseDirectory;
        foreach (var candidate in new[] { Path.Combine(exeDir, "tools"), exeDir })
        {
            if (File.Exists(ToolId.YtDlp.ExecutablePath(candidate)))
            {
                return candidate;
            }
        }

        return DefaultToolsDirectory;
    }
}
