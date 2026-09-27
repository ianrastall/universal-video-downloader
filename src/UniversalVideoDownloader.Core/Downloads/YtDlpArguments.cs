using System.Globalization;
using UniversalVideoDownloader.Core.Settings;
using UniversalVideoDownloader.Core.Tools;

namespace UniversalVideoDownloader.Core.Downloads;

public static class YtDlpArguments
{
    /// <summary>Arguments for one yt-dlp run. Always asks for the best available quality
    /// (optionally capped by height) and adds machine-readable progress lines.</summary>
    public static List<string> Build(AppSettings settings, string toolsDirectory, DownloadMode mode, string url)
    {
        var a = new List<string>
        {
            "--newline",
            "--no-quiet",
            "--no-simulate",
            "--color", "no_color",
            "--progress-template",
            $"download:{YtDlpOutputParser.ProgressMarker}%(progress.downloaded_bytes)s|%(progress.total_bytes)s|%(progress.total_bytes_estimate)s|%(progress.speed)s|%(progress.eta)s",
            "--print", $"before_dl:{YtDlpOutputParser.TitleMarker}%(title)s",
            "--print", $"after_move:{YtDlpOutputParser.FileMarker}%(filepath)s",
        };

        if (mode == DownloadMode.Video)
        {
            a.AddRange(["-f", "bv*+ba/b"]);
            if (settings.MaxHeight > 0)
            {
                a.AddRange(["-S", "res:" + settings.MaxHeight.ToString(CultureInfo.InvariantCulture)]);
            }

            switch (settings.Container)
            {
                case VideoContainer.Mkv:
                    a.AddRange(["--merge-output-format", "mkv"]);
                    break;
                case VideoContainer.Mp4:
                    a.AddRange(["--merge-output-format", "mp4"]);
                    break;
            }

            if (settings.EmbedSubtitles)
            {
                a.Add("--embed-subs");
                if (!string.IsNullOrWhiteSpace(settings.SubtitleLanguages))
                {
                    a.AddRange(["--sub-langs", settings.SubtitleLanguages.Trim()]);
                }
            }
        }
        else
        {
            a.AddRange(["-f", "ba/b", "-x", "--audio-format", AudioFormatArgument(settings.AudioFormat), "--audio-quality", "0"]);
        }

        if (settings.EmbedMetadata)
        {
            a.Add("--embed-metadata");
        }

        if (settings.EmbedThumbnail)
        {
            a.Add("--embed-thumbnail");
        }

        if (settings.SingleVideoFromPlaylistLinks)
        {
            a.Add("--no-playlist");
        }

        if (!string.IsNullOrWhiteSpace(settings.CookiesFromBrowser))
        {
            a.AddRange(["--cookies-from-browser", settings.CookiesFromBrowser.Trim()]);
        }

        if (!string.IsNullOrWhiteSpace(settings.RateLimit))
        {
            a.AddRange(["-r", settings.RateLimit.Trim()]);
        }

        a.AddRange(["-o", settings.OutputTemplate, "-P", settings.OutputDirectory]);

        if (settings.UseDownloadArchive)
        {
            a.AddRange(["--download-archive", Path.Combine(settings.OutputDirectory, "archive.txt")]);
        }

        if (File.Exists(ToolId.Ffmpeg.ExecutablePath(toolsDirectory)))
        {
            a.AddRange(["--ffmpeg-location", toolsDirectory]);
        }

        var deno = ToolId.Deno.ExecutablePath(toolsDirectory);
        if (File.Exists(deno))
        {
            a.AddRange(["--js-runtimes", "deno:" + deno]);
        }

        a.AddRange(CommandLine.Split(settings.ExtraArguments));

        // "--" so a video ID that starts with '-' is not read as an option.
        a.AddRange(["--", url]);
        return a;
    }

    public static string AudioFormatArgument(AudioFormat format) => format switch
    {
        AudioFormat.Opus => "opus",
        AudioFormat.M4a => "m4a",
        AudioFormat.Mp3 => "mp3",
        AudioFormat.Flac => "flac",
        _ => "best",
    };
}
