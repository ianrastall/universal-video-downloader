# Handoff

## Current state

First version of the app, replacing the earlier Rust terminal UI (`yt-dlp-manager`). The Rust
project's tool-update logic (GitHub release lookup, SHA-256 verification, ffmpeg DLL pruning,
rename-aside replacement of running binaries) and its yt-dlp output parsing were ported to C#
in `Core`, with the same behaviour.

Implemented:

- Download queue with concurrency, per-item progress (spread across merged streams and playlist
  items), cancel, retry, remove, open file, show in folder, copy link, per-item log.
- Queue persistence; restored items start paused.
- Input box accepting any number of links in free text, "Add from clipboard", text file import,
  drag and drop.
- Clipboard watcher (Win32 `AddClipboardFormatListener` on a message-only window, so it works in
  the background) feeding a tick-list inbox; optional auto-queue; Win+V history scan through
  `Windows.ApplicationModel.DataTransfer.Clipboard.GetHistoryItemsAsync`.
- Link filter: known video sites plus user-added domains, or any link. YouTube links are limited to
  videos, shorts, playlists and channels. Duplicate detection treats youtu.be and youtube.com
  forms of one video as the same link.
- Tools page: detection, update check, install and update for yt-dlp (stable or nightly), deno and
  ffmpeg with ffprobe. Automatic install and update on start-up. Installs wait for running
  downloads.
- Settings page covering output folder, file name template, quality cap, container, audio format,
  concurrency, embedding, subtitles, playlist handling, archive, cookies, rate limit, extra
  arguments, clipboard options, tools folder and channel.
- Single instance: a second launch brings the running window forward.

## Decisions

- Three projects so that everything except the XAML views builds and tests without Windows.
- Unpackaged and self-contained, so the output folder runs anywhere without installers.
- Referenced the `Microsoft.WindowsAppSDK` metapackage (2.5.1). It also pulls in the AI and ML
  components, which make the output larger. Referencing `Microsoft.WindowsAppSDK.WinUI` alone is
  possible (it depends only on Base, Foundation and InteractiveExperiences) but was not tried.
- ffmpeg comes from BtbN's shared master builds, as in the Rust version, because their asset names
  carry the git revision that `ffmpeg -version` reports, which makes update checks exact.
- Clipboard reads use the Win32 API; only clipboard history uses WinRT.
- Tests use xUnit v3 on Microsoft.Testing.Platform (`global.json` selects it; required by the
  .NET 10 SDK for xUnit v3).
- Plain controls rather than CommunityToolkit.WinUI's SettingsCard, to keep dependencies minimal.

## Verification performed (Linux container, .NET SDK 10.0.112)

- `dotnet build` of Core and ViewModels: no warnings, no errors.
- `dotnet test --project tests/UniversalVideoDownloader.Tests`: 48 of 48 passed, six runs in a row.
- `dotnet restore` of the WinUI app succeeded, so all package versions resolve.
- All C# in the WinUI project (App, MainWindow, pages' code-behind, services) compiled against the
  Windows App SDK 2.5.1 reference assemblies, with the XAML-generated members stubbed.

Not verified:

- The XAML files. The WinUI XAML compiler needs Windows, so `x:Bind` paths, styles and resource
  keys have only been checked by reading.
- Running the app. No window has been shown, and no real download or tool install has been
  performed. The GitHub API was not reachable from the build container.

## Next steps

1. On Windows: `dotnet build src\UniversalVideoDownloader.App -c Release`. Fix any XAML compiler
   errors it reports (they will name the file and line).
2. Run it. On first start it should install yt-dlp, deno and ffmpeg into
   `%LOCALAPPDATA%\UniversalVideoDownloader\tools`; watch the Tools page.
3. Try: a single YouTube link, a batch pasted from a text file, copying several links in a
   browser with the app in the background, and "Scan clipboard history".
4. Check the title bar buttons, the drop target and the window size on a high-DPI screen.
5. Consider an app icon (none is set yet) and trying the WinUI-only package reference to reduce size.
