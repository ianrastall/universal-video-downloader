# Universal Video Downloader handoff

1 November 2026. Release 0.2.0. Canonical checkout:
`D:\jupiter\universal-video-downloader`, branch main, original Git history preserved.
The chat-attached OneDrive folder had only an empty Git repository; the project was cloned
from https://github.com/ianrastall/universal-video-downloader .

## Delivered scope

Jupiter.UI 1.1 suite shell, standard title/menu/status bars, left navigation, separate clipboard
page, shared light/dark/Windows theme and accent settings, placement restore, installer and app
icon. Stable download options, clipboard collection, concurrency and verified tool updates
remain. Rust owns yt-dlp output parsing; retained C# adapters and JSON ABI are explicit
exceptions in docs/jupiter-exceptions.md. The ABI/lifetime contract is in docs/abi.md.

Fixes include asynchronous Windows drop deferrals and text-file drops, unique corrupt-queue
recovery copies with visible startup warnings, protecting unreadable queues against shutdown
replacement, pausing all restored entries, and accessible names on icon buttons.

## Build, test and publish

- Windows 11 24H2 x64, .NET SDK 10.0.113, Rust 1.97.0 MSVC, C++ Build Tools / Windows SDK,
  PowerShell 7 and Inno Setup 6.
- `./build.ps1 -Verify` produces the self-contained installer and SHA256SUMS.
- `./build.ps1 -Verify -Install` additionally installs silently for the current user.
- `./build.ps1 -Verify -JupiterUIRoot artifacts/jupiter-ui` exercises the verified source fallback.
- `dotnet run --project tests/UniversalVideoDownloader.Smoke -c Release -- <tools-dir> artifacts/media-smoke`
  runs real video/audio tests using a generated local clip, localhost HTTP server, yt-dlp and FFmpeg.
- Close the app before rebuilding its loaded native DLL.
- Commit on main, build from that commit, push main and publish tag v0.2.0 with the installer,
  SHA256SUMS.txt and Jupiter.UI-source.zip. The normal build fails when installer prerequisites
  are absent. PDBs are excluded from the installer.

Jupiter.UI has no configured public remote and already has unrelated working changes. No files
in that checkout were edited here. `build/jupiter-ui.json` pins the exact source archive, including
its existing fixes. The archive includes its own package lockfile. Keep that asset available at
the immutable v0.2.0 release URL so fresh public checkouts can build. Do not substitute a different
snapshot without updating the hash and validating it.

## Verification performed

- Release build and publish on Windows: zero warnings/errors, Jupiter style check enabled.
- 52 managed tests passed, including migration, recovery and parallel native UTF-8 ownership.
- 3 Rust tests passed; cargo fmt and Clippy with warnings denied passed.
- Real local HTTP video download and audio extraction passed, with native progress and saved-file
  events; FFprobe verified the output streams.
- Runtime tool installation succeeded using the app's official-release lookup and checksum
  verification (yt-dlp 2026.10.26, Deno 2.9.8, FFmpeg N-128273-g836436cfb9).
- Installed app launched from the top-level Start Menu shortcut; Downloads, Tools and shared
  dark/light appearance were visually checked using Computer Use before desktop interaction stopped.
- Silent upgrade preserved SHA-256 hashes of settings, queue and tool manifest. Silent uninstall
  preserved the same data and removed the shortcut; reinstall completed. No user media was removed.

Not verified: clean-machine installation, signing/SmartScreen behavior, live YouTube downloads,
Windows contrast themes, mixed-DPI displays, OS text scaling and Windows-on-Arm emulation. The
installer is unsigned. The existing failed user queue entries were preserved and were not retried.
The local generated-media smoke is intentionally independent of changing site behavior.

## Locations and lifetime

Installed binaries: `%LOCALAPPDATA%\Programs\UniversalVideoDownloader`.
Start: **Universal Video Downloader** (top level, no Jupiter folder).
Uninstaller: Windows Installed apps / `unins000.exe` in the installation directory.
Settings/queue/tools/error log remain in `%LOCALAPPDATA%\UniversalVideoDownloader`, with the
existing `UVD_HOME` override and custom tools-folder behavior. Downloads remain in the configured
output directory. The installer never includes downloaded tools or user data.

Rust parser calls are stateless and parallel-safe. C# loads the DLL from the app directory,
checks ABI 1 and frees each Rust-owned result through SafeHandle. Process cancellation kills the
whole yt-dlp/FFmpeg process tree. Settings are saved atomically through a temporary file and
replacement; malformed settings retain the original existing .invalid behavior. Corrupt queues
get uniquely named .invalid-* copies. An unreadable queue disables queue saving until restart.

Repository visibility work: Discussions enabled, searchable repository topics added, release
announcement plus setup/troubleshooting and feature-request topics published with the release.
