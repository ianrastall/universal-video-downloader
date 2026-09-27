namespace UniversalVideoDownloader.Core.Downloads;

/// <summary>Something yt-dlp reported about a running download.</summary>
public abstract record JobUpdate
{
    public sealed record Title(string Value) : JobUpdate;

    /// <summary>Number of streams that will be downloaded and merged (video + audio = 2).</summary>
    public sealed record Streams(int Count) : JobUpdate;

    /// <summary>A new stream began downloading.</summary>
    public sealed record Destination : JobUpdate;

    public sealed record Progress(double? Downloaded, double? Total, double? Speed, double? Eta) : JobUpdate;

    public sealed record PlaylistItem(int Index, int Count) : JobUpdate;

    /// <summary>A post-processing step started (e.g. "Merger", "ExtractAudio").</summary>
    public sealed record Stage(string Name) : JobUpdate;

    public sealed record FileSaved(string Path) : JobUpdate;

    public sealed record Log(string Line, bool IsError) : JobUpdate;
}
