# Universal Video Downloader

A Windows desktop app (WinUI 3, dark theme) for yt-dlp. It downloads single links or large batches
at the best available quality, collects video links as you copy them anywhere in Windows, and keeps
yt-dlp, deno and ffmpeg (with ffprobe) installed and up to date.

## What it does

- Downloads: paste or type any number of links into the box on the Downloads page, use
  "Add from clipboard", import a text file, or drag links onto the window. Each link can be video
  (best video + best audio, merged) or audio only.
- Queue: runs a configurable number of downloads at once, with progress, speed, time left, a per-item
  log, cancel, retry, open file and show in folder. Unfinished items are saved and come back paused
  the next time the app starts. yt-dlp resumes partial files.
- Clipboard: while the app is open it watches the clipboard, including while it is in the background.
  Every video link you copy, in any app, collects in a list on the Downloads page. When you have
  gathered a few, add the ticked ones to the queue in one go. "Scan clipboard history" collects
  links from Windows clipboard history (Win+V), so links copied before the app was open are
  picked up too. Clipboard entries that password managers mark as private are ignored.
- Tools: on start-up it checks for yt-dlp, deno and ffmpeg, installs whatever is missing and
  applies updates (both can be switched off in Settings). Downloads come from the official GitHub
  releases (yt-dlp/yt-dlp, denoland/deno, BtbN/FFmpeg-Builds) and are checked against the
  published SHA-256 before anything is replaced. Updates wait until running downloads finish.

## Requirements

- Windows 10 version 1809 or later, or Windows 11, x64 (Windows on Arm runs the x64 build through emulation).
- To build: the .NET 10 SDK. Visual Studio 2026 with the "WinUI application development" workload
  is optional but gives the XAML designer and debugger.

The built app is self-contained: the output folder runs without installing .NET or the Windows App SDK runtime.

## Build and run

From the repository root:

```powershell
dotnet build src\UniversalVideoDownloader.App -c Release
```

```powershell
dotnet run --project src\UniversalVideoDownloader.App
```

To produce a folder you can copy anywhere:

```powershell
dotnet publish src\UniversalVideoDownloader.App -c Release -o publish
```

Then run `publish\UniversalVideoDownloader.exe`.

In Visual Studio, open `UniversalVideoDownloader.slnx`, set `UniversalVideoDownloader.App` as the
startup project and press F5.

## Test

```powershell
dotnet test --project tests\UniversalVideoDownloader.Tests
```

The tests cover link detection, yt-dlp argument building and output parsing, checksum handling,
tool installation (against canned HTTP responses), settings storage, and the queue, clipboard and
tools view models. They do not need Windows.

## Where things are kept

Everything lives under `%LOCALAPPDATA%\UniversalVideoDownloader\` (set the `UVD_HOME` environment
variable to use another folder):

- `settings.json`: settings.
- `queue.json`: unfinished downloads.
- `tools\`: yt-dlp, deno, ffmpeg, ffprobe and ffmpeg's DLLs, plus `.uvd-tools.json` recording installed versions.
- `error.log`: unexpected errors.

If a `tools` folder containing yt-dlp sits next to the exe, it is used instead, which keeps a
portable copy self-contained. Settings can point to any other folder.

Downloads go to `Videos\yt-dlp` by default.

## Project layout

| Path | Contents |
| --- | --- |
| `src/UniversalVideoDownloader.Core` | UI-free logic: settings, link detection, tool install and update, yt-dlp arguments, output parsing, process running. `net10.0`. |
| `src/UniversalVideoDownloader.ViewModels` | MVVM view models (CommunityToolkit.Mvvm) and the service interfaces the app implements. `net10.0`, no WinUI dependency. |
| `src/UniversalVideoDownloader.App` | WinUI 3 app: XAML views, clipboard listener, pickers, shell integration, dependency injection. `net10.0-windows`. |
| `tests/UniversalVideoDownloader.Tests` | xUnit v3 tests for Core and ViewModels. |

## Notes

- "Video quality" caps the preferred resolution; the default takes the best available.
- MKV is the default container because it holds any codec combination without re-encoding.
- Cookies from Firefox work most reliably. Recent Chrome and Edge versions encrypt cookies in a way
  yt-dlp often cannot read.
- The clipboard filter recognises a list of common video sites (shown in Settings). Other sites can
  be added there, or the filter can accept any link. Links typed or pasted into the Downloads box
  are always accepted.
