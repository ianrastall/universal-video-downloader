using UniversalVideoDownloader.Core.Links;
using UniversalVideoDownloader.Core.Settings;
using UniversalVideoDownloader.Core.Tools;

namespace UniversalVideoDownloader.Core.Downloads;

public static class DownloadModes
{
    public static ToolId? Tool(DownloadMode mode) => mode switch
    {
        DownloadMode.Gallery => ToolId.GalleryDl,
        DownloadMode.TwitchVideo or DownloadMode.TwitchChat or DownloadMode.TwitchChatVideo => ToolId.TwitchDownloader,
        DownloadMode.Audiobook => null,
        _ => ToolId.YtDlp,
    };

    public static string ToolName(DownloadMode mode) => Tool(mode)?.Name() ?? "Libation";

    public static string QueueKey(string url, DownloadMode mode)
    {
        if (mode is DownloadMode.Video or DownloadMode.Audio)
        {
            return LinkKey.For(url);
        }

        var target = Tool(mode) == ToolId.TwitchDownloader && TwitchTarget.Parse(url) is { } twitch
            ? (twitch.IsClip ? "clip:" : "vod:") + twitch.Id
            : LinkKey.For(url);
        return $"{mode}:{target}";
    }
}

internal sealed record TwitchTarget(string Id, bool IsClip)
{
    public string Link => IsClip ? $"https://clips.twitch.tv/{Id}" : $"https://www.twitch.tv/videos/{Id}";

    public static TwitchTarget? Parse(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http"))
        {
            return null;
        }

        var parts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (uri.Host == "clips.twitch.tv" && parts is [var slug] && IsSlug(slug))
        {
            return new(slug, true);
        }

        if (uri.Host is "twitch.tv" or "www.twitch.tv" or "m.twitch.tv")
        {
            if (parts is ["videos", var id] && id.Length <= 20 && id.All(char.IsAsciiDigit))
            {
                return new(id, false);
            }

            if (parts is [_, "clip", var clip] && IsSlug(clip))
            {
                return new(clip, true);
            }
        }

        return null;
    }

    private static bool IsSlug(string value) =>
        value.Length is > 0 and <= 128 && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
}
