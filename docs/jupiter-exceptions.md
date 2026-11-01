# Jupiter adoption and exceptions

1 November 2026. Follows Jupiter style standard 1.1.

## Interface

Suite workspace: standard Windows title bar, File / View / Queue / Clipboard / Tools / Help
menu bar, left NavigationView (224 expanded, 48 compact), task content and a 32px status bar.
Downloads uses a flat queue with per-item progress, commands and logs. Clipboard is a separate
collection task. Tools uses a flat maintenance page. Settings is the suite navigation footer,
with a single scrolling column (padding 24, maximum width 640) and Appearance first.
No local Jupiter resources, colors, fonts, NumberBox, materials or decorative cards.
Jupiter.UI.targets runs its style checker in Error mode on every app build.

## Architectural exceptions

- **Incremental core migration:** the Rust core owns yt-dlp output interpretation, and every
  download passes through it. The existing tested C# argument builder, tool updater, settings,
  queue orchestration and process adapter are retained in this release to preserve the stable
  program's behavior. This is a deliberate partial domain migration, not a claim that the whole
  application core has moved to Rust. Future migrations should have a concrete behavioral benefit.
- **JSON instead of protobuf at the ABI:** one stateless parse operation returns a small tagged
  JSON array. A C ABI with an explicit version, owned result and free function is sufficient for
  this boundary; generated protobuf machinery would add a second schema/toolchain for eight
  small event types. See abi.md. Revisit if the command surface grows.
- **Shared source snapshot:** Jupiter.UI is a local shared repository with no configured remote.
  Its current working tree includes existing fixes belonging to other work. Those files were
  neither changed nor committed here. The release includes a checksum-pinned source snapshot
  so public users can build the same library. Normal workspace builds import the shared sibling
  checkout; isolated builds extract the snapshot into artifacts/jupiter-ui. The application
  does not define its own copies of shared resources.

## Delivery

Unpackaged, self-contained .NET and Windows App SDK, conventional per-user Inno Setup installer.
Windows 11 24H2 (build 26100) or later, x64. Windows 10 support from the initial source is
superseded by Jupiter.UI's minimum. Stable installer AppId, top-level Start Menu shortcut,
registered uninstaller, and shell icon refresh. User data and tools remain outside the install
folder and are preserved by upgrade and uninstall.
