using UniversalVideoDownloader.Core.Settings;
using UniversalVideoDownloader.Core.Tools;

namespace UniversalVideoDownloader.Core.Downloads;

public sealed class DownloadRunner(LibationClient libation) : IDownloadRunner
{
    private readonly YtDlpRunner _ytDlp = new();

    public Task<int> RunAsync(DownloadRequest request, Action<JobUpdate> onUpdate, CancellationToken ct) => request.Mode switch
    {
        DownloadMode.Gallery => RunGalleryAsync(request, onUpdate, ct),
        DownloadMode.TwitchVideo or DownloadMode.TwitchChat or DownloadMode.TwitchChatVideo => RunTwitchAsync(request, onUpdate, ct),
        DownloadMode.Audiobook => libation.DownloadAsync(request.Target, request.ToolsDirectory, onUpdate, ct),
        _ => _ytDlp.RunAsync(request, onUpdate, ct),
    };

    private static async Task<int> RunGalleryAsync(DownloadRequest request, Action<JobUpdate> onUpdate, CancellationToken ct)
    {
        const string savedPrefix = "UVD-SAVED:";
        var output = Path.GetFullPath(Path.Combine(request.OutputDirectory, "Galleries"));
        Directory.CreateDirectory(output);
        var arguments = new List<string>
        {
            "--no-input", "--no-colors", "--destination", output, "--Print", "after:" + savedPrefix + "{_path}",
        };
        arguments.AddRange(request.Arguments);
        arguments.AddRange(["--", request.Target]);
        onUpdate(new JobUpdate.Stage("Downloading gallery"));
        return await DownloadProcess.RunAsync(request.ExecutablePath, request.ToolsDirectory, arguments, (line, error) =>
        {
            if (!error && line.StartsWith(savedPrefix, StringComparison.Ordinal))
            {
                var path = line[savedPrefix.Length..];
                if (Path.IsPathFullyQualified(path) && File.Exists(path))
                {
                    onUpdate(new JobUpdate.FileSaved(path));
                }
            }
            else
            {
                onUpdate(new JobUpdate.Log(line, error));
            }
        }, ct).ConfigureAwait(false);
    }

    private static async Task<int> RunTwitchAsync(DownloadRequest request, Action<JobUpdate> onUpdate, CancellationToken ct)
    {
        var target = TwitchTarget.Parse(request.Target)
            ?? throw new InvalidDataException("Enter a Twitch VOD, highlight, or clip link.");
        var folder = Path.GetFullPath(Path.Combine(request.OutputDirectory, "Twitch"));
        var ffmpeg = ToolId.Ffmpeg.ExecutablePath(request.ToolsDirectory);
        if (request.Mode != DownloadMode.TwitchChat && !File.Exists(ffmpeg))
        {
            throw new FileNotFoundException("Install FFmpeg from the Tools page to use this Twitch mode.");
        }

        Directory.CreateDirectory(folder);
        var label = (target.IsClip ? "clip-" : "vod-") + target.Id;
        var temporary = Path.Combine(folder, ".uvd-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporary);
        try
        {
            if (request.Mode == DownloadMode.TwitchVideo)
            {
                onUpdate(new JobUpdate.Stage("Downloading Twitch video"));
                var video = Path.Combine(temporary, label + ".mp4");
                return await RunToFileAsync(
                    [target.IsClip ? "clipdownload" : "videodownload", "--id", target.Link, "--output", video,
                        "--ffmpeg-path", ffmpeg, "--temp-path", temporary],
                    video).ConfigureAwait(false);
            }

            onUpdate(new JobUpdate.Stage("Downloading Twitch chat"));
            var chat = Path.Combine(temporary, label + "-chat.json");
            var exit = await RunAsync(["chatdownload", "--id", target.Link, "--output", chat]).ConfigureAwait(false);
            if (exit != 0)
            {
                return exit;
            }

            var archive = Publish(chat);
            if (request.Mode == DownloadMode.TwitchChat)
            {
                return 0;
            }

            onUpdate(new JobUpdate.Stage("Rendering Twitch chat"));
            var rendered = Path.Combine(temporary, label + "-chat.mp4");
            return await RunToFileAsync(
                ["chatrender", "--input", archive, "--output", rendered, "--ffmpeg-path", ffmpeg, "--temp-path", temporary],
                rendered).ConfigureAwait(false);
        }
        finally
        {
            try { Directory.Delete(temporary, recursive: true); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }

        Task<int> RunAsync(IReadOnlyList<string> args) => DownloadProcess.RunAsync(
            request.ExecutablePath, request.ToolsDirectory, args, (line, error) => onUpdate(new JobUpdate.Log(line, error)), ct);

        async Task<int> RunToFileAsync(IReadOnlyList<string> args, string source)
        {
            var exit = await RunAsync(args).ConfigureAwait(false);
            if (exit == 0)
            {
                Publish(source);
            }

            return exit;
        }

        string Publish(string source)
        {
            ct.ThrowIfCancellationRequested();
            if (!File.Exists(source) || new FileInfo(source).Length == 0)
            {
                throw new IOException("TwitchDownloader did not produce an output file.");
            }

            var destination = Path.Combine(folder, Path.GetFileName(source));
            for (var copy = 2; ; copy++)
            {
                try
                {
                    File.Move(source, destination);
                    onUpdate(new JobUpdate.FileSaved(destination));
                    return destination;
                }
                catch (IOException) when (File.Exists(destination))
                {
                    destination = Path.Combine(folder, $"{Path.GetFileNameWithoutExtension(source)} ({copy}){Path.GetExtension(source)}");
                }
            }
        }
    }
}
