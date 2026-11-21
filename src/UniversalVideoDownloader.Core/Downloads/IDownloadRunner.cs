using UniversalVideoDownloader.Core.Settings;

namespace UniversalVideoDownloader.Core.Downloads;

public sealed record DownloadRequest(
    string ExecutablePath,
    string ToolsDirectory,
    IReadOnlyList<string> Arguments,
    DownloadMode Mode = DownloadMode.Video,
    string Target = "",
    string OutputDirectory = "");

public interface IDownloadRunner
{
    /// <summary>Run one download to completion and return its exit code.
    /// <paramref name="onUpdate"/> is called from background threads.
    /// Cancelling kills the tool and everything it started, then throws <see cref="OperationCanceledException"/>.</summary>
    Task<int> RunAsync(DownloadRequest request, Action<JobUpdate> onUpdate, CancellationToken ct);
}
