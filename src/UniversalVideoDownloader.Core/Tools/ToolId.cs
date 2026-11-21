namespace UniversalVideoDownloader.Core.Tools;

public enum ToolId
{
    YtDlp,
    Deno,
    Ffmpeg,
    GalleryDl,
    TwitchDownloader,
}

public static class ToolIdExtensions
{
    public static IReadOnlyList<ToolId> All { get; } = [ToolId.YtDlp, ToolId.Deno, ToolId.Ffmpeg, ToolId.GalleryDl, ToolId.TwitchDownloader];

    public static string ExecutableSuffix => OperatingSystem.IsWindows() ? ".exe" : "";

    public static string Name(this ToolId id) => id switch
    {
        ToolId.YtDlp => "yt-dlp",
        ToolId.Deno => "deno",
        ToolId.Ffmpeg => "ffmpeg",
        ToolId.GalleryDl => "gallery-dl",
        ToolId.TwitchDownloader => "TwitchDownloader",
        _ => throw new ArgumentOutOfRangeException(nameof(id)),
    };

    public static string Description(this ToolId id) => id switch
    {
        ToolId.YtDlp => "The downloader itself.",
        ToolId.Deno => "JavaScript runtime yt-dlp uses to solve YouTube's player challenges.",
        ToolId.Ffmpeg => "Merges video and audio, converts audio, embeds thumbnails. Includes ffprobe.",
        ToolId.GalleryDl => "Downloads galleries, collections and media from supported sites.",
        ToolId.TwitchDownloader => "Downloads Twitch VODs, clips and chat, and renders chat to video.",
        _ => throw new ArgumentOutOfRangeException(nameof(id)),
    };

    public static bool IsOptional(this ToolId id) => id is ToolId.GalleryDl or ToolId.TwitchDownloader;

    public static string ExecutableName(this ToolId id) =>
        (id == ToolId.TwitchDownloader ? "TwitchDownloaderCLI" : id.Name()) + ExecutableSuffix;

    public static string ExecutablePath(this ToolId id, string directory) => Path.Combine(directory, id.ExecutableName());

    public static string FfprobePath(string directory) => Path.Combine(directory, "ffprobe" + ExecutableSuffix);
}
