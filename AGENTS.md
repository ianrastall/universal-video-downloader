# AGENTS.md

Instructions for AI agents and contributors working in this repository.

## Project

- Name: Universal Video Downloader.
- Purpose: Windows GUI for yt-dlp that handles single and bulk downloads, collects links from the
  clipboard, and installs and updates yt-dlp, deno and ffmpeg/ffprobe itself.
- Language: C# 14 on .NET 10.
- UI: WinUI 3 (Windows App SDK 2.x), MVVM with CommunityToolkit.Mvvm, shared Jupiter.UI 1.1 dark/light/Windows themes.
- Application type: unpackaged, self-contained desktop app (`WindowsPackageType=None`,
  `WindowsAppSDKSelfContained=true`). Conventional Inno Setup installer; no MSIX.
- Supported OS: Windows 11 24H2 (26100)+, x64.

## Layout and boundaries

- `src/UniversalVideoDownloader.Core`: no UI and no MVVM types. Anything that can be unit-tested
  without a window belongs here.
- `src/UniversalVideoDownloader.ViewModels`: view models plus service interfaces
  (`Services/`). Must not reference WinUI or any Windows-only API, so it builds and tests on any OS.
- `src/UniversalVideoDownloader.App`: WinUI views and the Windows implementations of the service
  interfaces. Keep logic out of code-behind; code-behind only wires views to view models.
- `tests/UniversalVideoDownloader.Tests`: tests for Core and ViewModels.

Entry points: `App.xaml.cs` (`OnLaunched`, dependency injection setup), `MainWindow.xaml.cs`,
`MainViewModel.StartAsync` (start-up sequence).

## Jupiter architecture

The Rust core (`core/src/lib.rs`) owns download output parsing. Managed Core owns the retained
stable adapters; see `docs/jupiter-exceptions.md` and `docs/abi.md` for deliberate exceptions.
Jupiter.UI is imported from the shared sibling or the verified release source snapshot.
Do not duplicate its resources or change the shared checkout as part of unrelated app work.
Work on main; preserve local work; commit, push and publish as authorized by the workspace.

## Commands

Run from the repository root.

- Build app (Windows only): `dotnet build src\UniversalVideoDownloader.App -c Release`
- Run app: `dotnet run --project src\UniversalVideoDownloader.App`
- Build release installer: `./build.ps1 -Verify` (add `-Install` for local installation).
- Test (any OS): `dotnet test --project tests\UniversalVideoDownloader.Tests`
- Build libraries only (any OS): `dotnet build src/UniversalVideoDownloader.ViewModels`

Run `cargo fmt --all -- --check` and `cargo clippy --locked --all-targets --target-dir artifacts/cargo -- -D warnings`. The build uses `.editorconfig`; keep builds free of warnings.

## Conventions

- Package versions are declared only in `Directory.Packages.props` (central package management).
- Common MSBuild properties live in `Directory.Build.props`.
- Observable properties use the partial-property form: `[ObservableProperty] public partial T Name { get; set; }`.
- View models touch bound state only on the UI thread. Background work reports through
  `IUiDispatcher.Post`, or through `await` continuations on the UI thread.
- Downloads and tool updates coordinate through `WorkCoordinator`; do not replace tool files while a
  download runs.
- Every tool download must be verified against the upstream SHA-256 before installation.

## Generated and output files

Do not edit or commit: `bin/`, `obj/`, `publish/`, `*.g.cs`, `*.g.i.cs`, `*.xbf`.

## External tools

yt-dlp, deno, ffmpeg and ffprobe are downloaded at run time into the tools folder. They are never
committed or bundled.

## Verification limits

Build pinned SDKs and locked restores. The WinUI XAML compiler only runs on Windows. Changes to `.xaml` files cannot be verified on Linux
or macOS; say so when reporting, and have them built on Windows.
