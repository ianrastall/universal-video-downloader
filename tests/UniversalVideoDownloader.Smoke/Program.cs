using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using UniversalVideoDownloader.Core.Downloads;
using UniversalVideoDownloader.Core.Settings;

if (args.Length != 2) throw new ArgumentException("Usage: Smoke <tools directory> <output directory>");
var tools = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[1]);
Directory.CreateDirectory(output);
using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
var ct = timeout.Token;
var fixture = Path.Combine(output, "fixture.mp4");
await RunTool("ffmpeg.exe", "-y", "-f", "lavfi", "-i", "color=c=blue:s=160x90:d=1", "-f", "lavfi", "-i", "sine=frequency=440:duration=1", "-c:v", "libx264", "-c:a", "aac", "-shortest", fixture);
var bytes = await File.ReadAllBytesAsync(fixture, ct);
var listener = new TcpListener(IPAddress.Loopback, 0);
listener.Start();
var port = ((IPEndPoint)listener.LocalEndpoint).Port;
var serving = ServeAsync();
try
{
    foreach (var mode in new[] { DownloadMode.Video, DownloadMode.Audio })
    {
        var settings = new AppSettings { OutputDirectory = Path.Combine(output, mode.ToString()), OutputTemplate = "result.%(ext)s", EmbedThumbnail = false, AudioFormat = AudioFormat.M4a };
        Directory.CreateDirectory(settings.OutputDirectory);
        var updates = new ConcurrentQueue<JobUpdate>();
        var request = new DownloadRequest(Path.Combine(tools, "yt-dlp.exe"), tools,
            YtDlpArguments.Build(settings, tools, mode, $"http://127.0.0.1:{port}/fixture.mp4"));
        var exit = await new YtDlpRunner().RunAsync(request, updates.Enqueue, ct);
        if (exit != 0) throw new InvalidOperationException(string.Join('\n', updates.OfType<JobUpdate.Log>().Select(u => u.Line)));
        var saved = updates.OfType<JobUpdate.FileSaved>().LastOrDefault()?.Path;
        if (saved is null || !File.Exists(saved)) throw new InvalidOperationException("No saved-file event or output file.");
        if (!updates.OfType<JobUpdate.Progress>().Any()) throw new InvalidOperationException("No native progress events.");
        using var probe = JsonDocument.Parse(await RunTool("ffprobe.exe", "-v", "error", "-show_streams", "-show_format", "-of", "json", saved));
        var streams = probe.RootElement.GetProperty("streams").EnumerateArray().Select(s => s.GetProperty("codec_type").GetString()).ToArray();
        if (!streams.Contains("audio") || (mode == DownloadMode.Video && !streams.Contains("video")) || (mode == DownloadMode.Audio && streams.Contains("video")))
            throw new InvalidOperationException("Unexpected media stream inventory.");
        Console.WriteLine($"PASS {mode}: {Path.GetFileName(saved)}, {new FileInfo(saved).Length} bytes, native progress + file events, ffprobe streams verified.");
    }
}
finally
{
    timeout.Cancel(); listener.Stop();
    try { await serving; } catch (OperationCanceledException) { } catch (SocketException) { }
}

async Task<string> RunTool(string name, params string[] arguments)
{
    using var process = new Process { StartInfo = new ProcessStartInfo(Path.Combine(tools, name)) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true } };
    foreach (var argument in arguments) process.StartInfo.ArgumentList.Add(argument);
    process.Start();
    var stdout = process.StandardOutput.ReadToEndAsync(ct);
    var stderr = process.StandardError.ReadToEndAsync(ct);
    await process.WaitForExitAsync(ct);
    if (process.ExitCode != 0) throw new InvalidOperationException(await stderr);
    await stderr;
    return await stdout;
}

async Task ServeAsync()
{
    while (!ct.IsCancellationRequested)
    {
        using var client = await listener.AcceptTcpClientAsync(ct);
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
        var request = await reader.ReadLineAsync(ct) ?? "";
        while (await reader.ReadLineAsync(ct) is { Length: > 0 }) { }
        var header = Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: video/mp4\r\nContent-Length: {bytes.Length}\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(header, ct);
        if (!request.StartsWith("HEAD ", StringComparison.Ordinal)) await stream.WriteAsync(bytes, ct);
    }
}
