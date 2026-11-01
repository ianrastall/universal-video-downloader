# Universal Video Downloader

A Windows desktop app (WinUI 3, Jupiter.UI, C# and Rust) for yt-dlp. It downloads single links or large batches
at the best available quality, collects video links as you copy them anywhere in Windows, and keeps
yt-dlp, deno and ffmpeg (with ffprobe) installed and up to date.

## What it does

- Downloads: paste or type any number of links into the box on the Downloads page, use
  "Add from clipboard", import a text file, or drag links and text files onto the Downloads page. Each link can be video
  (best video + best audio, merged) or audio only.
- Queue: runs a configurable number of downloads at once, with progress, speed, time left, a per-item
  log, cancel, retry, open file and show in folder. Unfinished items are saved and come back paused
  the next time the app starts. yt-dlp resumes partial files.
- Clipboard: while the app is open it watches the clipboard, including while it is in the background.
  Every video link you copy, in any app, collects in a list on the Clipboard page. When you have
  gathered a few, add the ticked ones to the queue in one go. "Scan clipboard history" collects
  links from Windows clipboard history (Win+V), so links copied before the app was open are
  picked up too. Clipboard entries that password managers mark as private are ignored.
- Tools: on start-up it checks for yt-dlp, deno and ffmpeg, installs whatever is missing and
  applies updates (both can be switched off in Settings). Downloads come from the official GitHub
  releases (yt-dlp/yt-dlp, denoland/deno, BtbN/FFmpeg-Builds) and are checked against the
  published SHA-256 before anything is replaced. Updates wait until running downloads finish.

## Install

Download **UniversalVideoDownloader-0.2.0-Setup.exe** from the
[latest release](https://github.com/ianrastall/universal-video-downloader/releases/latest).
Run the installer, then open **Universal Video Downloader** from the Windows Start Menu.
The installer runs per user without administrator rights and bundles .NET and Windows App SDK.
Use the same installer to upgrade. Uninstall through Windows Installed apps. Settings, queue,
downloaded tools and downloaded media are preserved.

Requires Windows 11 24H2 (build 26100) or later, x64. Jupiter.UI sets this minimum; the original
Windows 10 requirement no longer applies. The installer is currently unsigned.

Follows Jupiter style standard 1.1. Downloads, Clipboard and Tools are separate navigation
pages. Settings > Appearance offers Dark, Light, Use Windows setting, and the shared Windows
or custom accent control. Window placement is remembered. See [Jupiter adoption](docs/jupiter-exceptions.md).

## Build

On Windows, install .NET SDK **10.0.113**, Rust **1.97.0** (MSVC), Visual Studio C++ build tools
with a Windows SDK, PowerShell 7, and **Inno Setup 6**. Run from this checkout:

```powershell
.\build.ps1 -Verify
```

The normal build produces `artifacts/installer/UniversalVideoDownloader-0.2.0-Setup.exe` and
`artifacts/SHA256SUMS.txt`. Add `-Install` to install it locally. Publishing a folder by itself
is not the release process.

Workspace builds use the shared sibling `../jupiter-ui`. When it is absent, the build downloads
**Jupiter.UI-source.zip** from the 0.2.0 release, verifies the committed SHA-256, and extracts it
under `artifacts/jupiter-ui`. To use an explicit copy, pass `-JupiterUIRoot <path>`. The release
asset includes the exact source and lockfile used here, including existing shared-library fixes.

For iterative GUI development:

```powershell
dotnet build src\UniversalVideoDownloader.App -c Release -p:Platform=x64
dotnet run --project src\UniversalVideoDownloader.App -p:Platform=x64
```

Close the app before rebuilding its Rust DLL. The app's style check runs on every build.

## Test

```powershell
dotnet test --project tests\UniversalVideoDownloader.Tests
```

The tests cover the Rust ABI, settings migration, queue recovery, link detection, yt-dlp argument building and output parsing, checksum handling,
tool installation (against canned HTTP responses), settings storage, and the queue, clipboard and
tools view models. Library tests do not need WinUI; they build and load the Rust core for the host OS.

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

## Help and feedback

Use [Discussions](https://github.com/ianrastall/universal-video-downloader/discussions) for
setup questions, troubleshooting, ideas and workflows. Report reproducible defects in
[Issues](https://github.com/ianrastall/universal-video-downloader/issues). Include the app/tool
versions and relevant error text; remove private URLs, cookies and personal paths from logs.

## Licenses

The app is MIT licensed. See [third-party notices](THIRD-PARTY-NOTICES.md).
External downloader tools are fetched at runtime and retain their own upstream licenses;
they are not bundled in this release's installer.
