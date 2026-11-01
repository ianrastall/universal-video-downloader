param([switch]$Verify, [switch]$Install, [string]$JupiterUIRoot)
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    $version = '0.2.0'
    if (-not $JupiterUIRoot) { $JupiterUIRoot = Join-Path (Split-Path $PSScriptRoot -Parent) 'jupiter-ui' }
    $JupiterUIRoot = [System.IO.Path]::GetFullPath($JupiterUIRoot)
    if (-not (Test-Path -LiteralPath (Join-Path $JupiterUIRoot 'build\Jupiter.UI.targets'))) {
        $dependency = Get-Content build\jupiter-ui.json -Raw | ConvertFrom-Json
        $archive = Join-Path $PSScriptRoot 'artifacts\Jupiter.UI-source.zip'
        New-Item -ItemType Directory -Force artifacts | Out-Null
        if (-not (Test-Path -LiteralPath $archive)) { Invoke-WebRequest -Uri $dependency.url -OutFile $archive }
        if ((Get-FileHash -LiteralPath $archive).Hash -ne $dependency.sha256) { throw 'Jupiter.UI source checksum mismatch.' }
        $JupiterUIRoot = Join-Path $PSScriptRoot 'artifacts\jupiter-ui'
        Expand-Archive -LiteralPath $archive -DestinationPath $JupiterUIRoot -Force
    }
    if ($Verify) {
        cargo fmt --all -- --check
        if ($LASTEXITCODE) { throw 'Rust formatting failed.' }
        cargo clippy --locked --all-targets --target-dir artifacts/cargo -- -D warnings
        if ($LASTEXITCODE) { throw 'Rust lint failed.' }
        cargo test --locked --target-dir artifacts/cargo
        if ($LASTEXITCODE) { throw 'Rust tests failed.' }
        dotnet restore tests\UniversalVideoDownloader.Tests --locked-mode
        if ($LASTEXITCODE) { throw 'Test restore failed.' }
        dotnet test --project tests\UniversalVideoDownloader.Tests -c Release --no-restore
        if ($LASTEXITCODE) { throw 'Managed tests failed.' }
    }
    $app = 'src\UniversalVideoDownloader.App\UniversalVideoDownloader.App.csproj'
    dotnet restore $app --locked-mode -p:Platform=x64 "-p:JupiterUIRoot=$JupiterUIRoot"
    if ($LASTEXITCODE) { throw 'Locked restore failed.' }
    $publish = Join-Path $PSScriptRoot "artifacts\publish-$version"
    dotnet publish $app -c Release -p:Platform=x64 "-p:JupiterUIRoot=$JupiterUIRoot" --no-restore -o $publish
    if ($LASTEXITCODE) { throw 'Publish failed.' }
    Copy-Item -LiteralPath LICENSE,THIRD-PARTY-NOTICES.md -Destination $publish -Force
    Copy-Item -LiteralPath build\notices -Destination $publish -Recurse -Force
    foreach ($file in @('UniversalVideoDownloader.exe','UniversalVideoDownloader.pri','MainWindow.xbf','Views\DownloadsPage.xbf','Views\ClipboardPage.xbf','Views\SettingsPage.xbf','Views\ToolsPage.xbf','Jupiter.UI.dll','uvd_core.dll','coreclr.dll','Microsoft.UI.Xaml.dll')) {
        if (-not (Test-Path -LiteralPath (Join-Path $publish $file))) { throw "Missing published file: $file" }
    }
    foreach ($file in @('yt-dlp.exe','deno.exe','ffmpeg.exe','ffprobe.exe')) {
        if (Get-ChildItem -LiteralPath $publish -Recurse -File -Filter $file) { throw "External tool must not be bundled: $file" }
    }
    $iscc = @((Get-Command ISCC -ErrorAction SilentlyContinue).Source,"$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe","${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe") | Where-Object { $_ -and (Test-Path -LiteralPath $_) } | Select-Object -First 1
    if (-not $iscc) { throw 'Install Inno Setup 6 to build the required installer.' }
    & $iscc /Qp "/DAppVersion=$version" "/DSourceDir=$publish" "/DOutputDir=$(Join-Path $PSScriptRoot 'artifacts\installer')" build\UniversalVideoDownloader.iss
    if ($LASTEXITCODE) { throw 'Installer build failed.' }
    $installer = Join-Path $PSScriptRoot "artifacts\installer\UniversalVideoDownloader-$version-Setup.exe"
    "$((Get-FileHash -LiteralPath $installer).Hash.ToLowerInvariant())  $(Split-Path $installer -Leaf)" | Set-Content artifacts\SHA256SUMS.txt -Encoding utf8NoBOM
    if ($Install) {
        $process = Start-Process -FilePath $installer -ArgumentList '/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',"/LOG=`"$(Join-Path $PSScriptRoot 'artifacts\install.log')`"" -WindowStyle Hidden -Wait -PassThru
        if ($process.ExitCode) { throw "Installer failed: $($process.ExitCode)" }
    }
} finally { Pop-Location }
