# Changelog

## 0.2.0 — 1 November 2026

- Adopt Jupiter.UI 1.1 with a standard title bar, menu bar, left navigation, flat queue and
  separate Downloads, Clipboard, Tools and Settings pages.
- Add shared dark/light/Windows theme settings and Windows/custom accent controls.
- Remember window placement and clamp it to the current display work area.
- Deliver a per-user Windows installer, Start Menu shortcut, app icon and registered uninstaller.
- Move download output interpretation into a tested Rust core through an owned C ABI.
- Accept dropped text/Markdown/CSV/HTML files; keep the Windows drop deferral alive while reading.
- Preserve damaged saved queues in unique recovery copies and report the failure.
- Preserve existing queue/settings locations, download options and verified tool updates.
- Add locked dependencies, a public Jupiter.UI source snapshot and build/release instructions.

The minimum Windows version is now Windows 11 24H2 because of the shared Jupiter.UI library.
