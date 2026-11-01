using System.Text.Json;
using UniversalVideoDownloader.Core.Settings;

namespace UniversalVideoDownloader.Core.Downloads;

public enum SavedDownloadState
{
    Queued,
    Failed,
    Canceled,
}

/// <summary>An unfinished queue entry, kept across restarts.</summary>
public sealed record SavedDownload(string Url, DownloadMode Mode, SavedDownloadState State = SavedDownloadState.Queued, string? Error = null);

public sealed class QueueStore(string path)
{
    public string Path { get; } = path;

    public static QueueStore Default => new(AppPaths.QueueFile);

    public IReadOnlyList<SavedDownload> Load()
    {
        try
        {
            return File.Exists(Path)
                ? JsonSerializer.Deserialize(File.ReadAllText(Path), CoreJsonContext.Default.ListSavedDownload) ?? []
                : [];
        }
        catch (JsonException ex)
        {
            // Keep every corrupt input, including successive failures, before a future save.
            var backup = Path + ".invalid-" + Guid.NewGuid().ToString("N");
            File.Copy(Path, backup);
            throw new InvalidDataException($"The saved queue could not be read. A recovery copy is at {backup}.", ex);
        }
    }

    public void Save(IEnumerable<SavedDownload> items)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        var temp = Path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(items.ToList(), CoreJsonContext.Default.ListSavedDownload));
        File.Move(temp, Path, overwrite: true);
    }
}
