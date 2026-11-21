using System.Diagnostics;
using System.Text;

namespace UniversalVideoDownloader.Core.Downloads;

public sealed class YtDlpRunner : IDownloadRunner
{
    public Task<int> RunAsync(DownloadRequest request, Action<JobUpdate> onUpdate, CancellationToken ct) =>
        DownloadProcess.RunAsync(request.ExecutablePath, request.ToolsDirectory, request.Arguments, (line, fromStdErr) =>
        {
            foreach (var update in YtDlpOutputParser.Parse(line, fromStdErr))
            {
                onUpdate(update);
            }
        }, ct);
}

internal static class DownloadProcess
{
    internal static async Task<int> RunAsync(
        string executable, string toolsDirectory, IReadOnlyList<string> arguments, Action<string, bool> onLine, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var psi = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            WorkingDirectory = Path.GetDirectoryName(executable)!,
        };
        foreach (var arg in arguments)
        {
            psi.ArgumentList.Add(arg);
        }

        // yt-dlp finds ffmpeg/deno via its own options, but keeping the tools folder first on PATH
        // covers anything it launches by name. Force UTF-8 so titles in any script survive.
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        psi.Environment["PATH"] = toolsDirectory + System.IO.Path.PathSeparator + path;
        psi.Environment["PYTHONIOENCODING"] = "utf-8";
        psi.Environment["PYTHONUTF8"] = "1";

        using var process = new Process { StartInfo = psi };
        process.Start();
        process.StandardInput.Close();

        // The PyInstaller build of yt-dlp runs Python in a child process, and ffmpeg runs below that,
        // so the whole tree must be killed or orphans keep downloading.
        using var registration = ct.Register(() =>
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
            {
            }
        });

        var stdout = PumpAsync(process.StandardOutput, fromStdErr: false, onLine);
        var stderr = PumpAsync(process.StandardError, fromStdErr: true, onLine);
        await Task.WhenAll(stdout, stderr).ConfigureAwait(false);
        await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);

        ct.ThrowIfCancellationRequested();
        return process.ExitCode;
    }

    private static async Task PumpAsync(StreamReader reader, bool fromStdErr, Action<string, bool> onLine)
    {
        // ReadLineAsync splits on \r as well as \n, so carriage-return progress redraws become lines.
        while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
        {
            line = line.TrimEnd();
            if (line.Length == 0)
            {
                continue;
            }

            onLine(line, fromStdErr);
        }
    }
}
