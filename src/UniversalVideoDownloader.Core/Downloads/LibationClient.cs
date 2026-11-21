using System.Text.Json;

namespace UniversalVideoDownloader.Core.Downloads;

public sealed record AudibleTitle(string Id, string Title, string Authors, string Status)
{
    public string Link => "https://www.audible.com/pd/" + Id;
}

/// <summary>Uses Libation's own installation, sign-in, library database and output settings.</summary>
public sealed class LibationClient(Func<string?> locate)
{
    private readonly SemaphoreSlim _operation = new(1);

    public string? ExecutablePath => locate();

    public async Task<IReadOnlyList<AudibleTitle>> ReadLibraryAsync(bool scan, CancellationToken ct)
    {
        var executable = RequireExecutable();
        await _operation.WaitAsync(ct).ConfigureAwait(false);
        var export = Path.Combine(Path.GetTempPath(), "uvd-audible-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            if (scan)
            {
                await RunCheckedAsync(executable, ["scan"], ct).ConfigureAwait(false);
            }

            await RunCheckedAsync(executable, ["export", "--path", export, "--json"], ct).ConfigureAwait(false);
            await using var stream = File.OpenRead(export);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            return ReadTitles(json.RootElement);
        }
        finally
        {
            try { File.Delete(export); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
            _operation.Release();
        }
    }

    internal static IReadOnlyList<AudibleTitle> ReadTitles(JsonElement root)
    {
        return root.EnumerateArray()
            .Select(book => new AudibleTitle(
                ReadString(book, "AudibleProductId"), ReadString(book, "Title"),
                ReadString(book, "AuthorNames"), ReadString(book, "BookStatus")))
            .Where(book => ValidId(book.Id) && book.Title.Length > 0)
            .DistinctBy(book => book.Id, StringComparer.OrdinalIgnoreCase)
            .OrderBy(book => book.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    public async Task<string?> GetBooksFolderAsync(CancellationToken ct)
    {
        var executable = RequireExecutable();
        await _operation.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            string? folder = null;
            await RunCheckedAsync(executable, ["get-setting", "Books"], ct, line =>
            {
                if (line.StartsWith("Books=", StringComparison.Ordinal))
                {
                    folder = line["Books=".Length..].Trim();
                }
            }).ConfigureAwait(false);
            return folder;
        }
        finally { _operation.Release(); }
    }

    internal async Task<int> DownloadAsync(string link, string toolsDirectory, Action<JobUpdate> onUpdate, CancellationToken ct)
    {
        var id = Uri.TryCreate(link, UriKind.Absolute, out var uri) ? uri.Segments.LastOrDefault()?.TrimEnd('/') ?? "" : "";
        if (!ValidId(id))
        {
            throw new InvalidDataException("Select a title from the Audible Library page.");
        }

        var executable = RequireExecutable();
        onUpdate(new JobUpdate.Stage("Waiting for Libation"));
        await _operation.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            onUpdate(new JobUpdate.Stage("Downloading audiobook"));
            bool completed = false;
            bool reportedError = false;
            var exit = await DownloadProcess.RunAsync(executable, toolsDirectory, ["liberate", "--id", id], (line, error) =>
            {
                if (!error && line.StartsWith("DownloadDecryptBook Completed:", StringComparison.Ordinal))
                {
                    completed = true;
                }

                if (error) reportedError = true;
                onUpdate(new JobUpdate.Log(line, error));
            }, ct).ConfigureAwait(false);
            if (exit == 0 && !completed)
            {
                if (!reportedError)
                {
                    onUpdate(new JobUpdate.Log("Libation did not report a completed audiobook. Open Libation to check the title and account.", true));
                }

                return 1;
            }

            return exit;
        }
        finally { _operation.Release(); }
    }

    private string RequireExecutable() => ExecutablePath
        ?? throw new FileNotFoundException("Install Libation or select its installation folder on the Audible Library page.");

    private static bool ValidId(string id) => id.Length == 10 && id.All(char.IsAsciiLetterOrDigit);

    private static string ReadString(JsonElement book, string property) =>
        book.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";

    private static async Task RunCheckedAsync(string executable, IReadOnlyList<string> args, CancellationToken ct, Action<string>? onLine = null)
    {
        var exit = await DownloadProcess.RunAsync(executable, Path.GetDirectoryName(executable)!, args,
            (line, error) => { if (!error) onLine?.Invoke(line); }, ct).ConfigureAwait(false);
        if (exit != 0)
        {
            throw new IOException($"Libation could not {args[0]} (exit code {exit}). Open Libation to check its setup and sign-in.");
        }
    }
}
