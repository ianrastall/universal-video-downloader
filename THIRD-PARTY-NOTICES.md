# Third-party notices

Universal Video Downloader and the Jupiter.UI source snapshot are copyright (c) 2026 Ian Rastall,
provided under the MIT license in LICENSE. Rust core code is MIT licensed.

The installer includes .NET and Windows App SDK runtime components and the libraries below.
Their upstream licenses and notices continue to apply. This inventory is generated from the
resolved NuGet dependency graph; build-only packages may appear in it.

The installer does not contain yt-dlp, Deno, FFmpeg or FFprobe. The app retrieves them directly
from official upstream releases and verifies their published SHA-256 before installation:

- yt-dlp: https://github.com/yt-dlp/yt-dlp#license . Executable distributions can include dependencies with additional licenses; consult the downloaded release's notices.
- Deno: https://github.com/denoland/deno/blob/main/LICENSE.md (MIT and included third-party components).
- FFmpeg / FFprobe: https://github.com/BtbN/FFmpeg-Builds . The selected win64-gpl-shared build is GPL; its separate processes are not linked into this application. https://ffmpeg.org/legal.html . Anyone redistributing those downloaded tools must carry the applicable notices and corresponding-source obligations. They are not covered by this application's MIT license.

## NuGet components

- CommunityToolkit.Mvvm/8.4.2: MIT — https://github.com/CommunityToolkit/dotnet
- Microsoft.Extensions.DependencyInjection.Abstractions/10.0.12: MIT — https://dot.net/
- Microsoft.Extensions.DependencyInjection/10.0.12: MIT — https://dot.net/
- Microsoft.Web.WebView2/1.0.3719.77: LICENSE.txt — https://aka.ms/webview
- Microsoft.Windows.AI.MachineLearning/2.1.74: license.txt — https://github.com/microsoft/windowsappsdk
- Microsoft.Windows.SDK.BuildTools.MSIX/1.7.251221100: sdk_license.txt — https://aka.ms/WinSDKProjectURL
- Microsoft.Windows.SDK.BuildTools/10.0.28000.2705: See package license — https://aka.ms/WinSDKProjectURL
- Microsoft.WindowsAppSDK.AI/2.5.5: license.txt — https://github.com/microsoft/windowsappsdk
- Microsoft.WindowsAppSDK.Base/2.0.4: license.txt — https://github.com/microsoft/windowsappsdk
- Microsoft.WindowsAppSDK.DWrite/2.1.0: license.txt — https://aka.ms/WindowsAppSDK
- Microsoft.WindowsAppSDK.Foundation/2.3.12: license.txt — https://aka.ms/windowsappsdk
- Microsoft.WindowsAppSDK.InteractiveExperiences/2.1.9: license.txt — https://aka.ms/windowsappsdk
- Microsoft.WindowsAppSDK.ML/2.1.94: license.txt — https://github.com/microsoft/windowsappsdk
- Microsoft.WindowsAppSDK.Runtime/2.5.1: license.txt — https://github.com/microsoft/windowsappsdk
- Microsoft.WindowsAppSDK.Search/2.5.5: license.txt — https://github.com/microsoft/windowsappsdk
- Microsoft.WindowsAppSDK.Widgets/2.0.5: license.txt — https://github.com/microsoft/windowsappsdk
- Microsoft.WindowsAppSDK.WinUI/2.3.9: license.txt — https://github.com/microsoft/windowsappsdk
- Microsoft.WindowsAppSDK/2.5.1: license.txt — https://github.com/microsoft/windowsappsdk
- System.Numerics.Tensors/9.0.0: MIT — https://dot.net/

## Rust components

The compiled core uses serde_json 1.0.150, serde_core 1.0.229 and itoa 1.0.18
(MIT OR Apache-2.0), memchr 2.8.3 (Unlicense OR MIT), and zmij 1.0.23 (MIT).
Cargo.lock identifies the exact dependency graph, including build-time dependencies.
The original license files are included in the installer notices directory.

