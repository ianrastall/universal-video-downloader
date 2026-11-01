#ifndef AppVersion
 #define AppVersion "0.2.0"
#endif
[Setup]
AppId={{4279609E-54CC-41BA-B235-B8283DDBFF7E}
AppName=Universal Video Downloader
AppVersion={#AppVersion}
AppPublisher=Ian Rastall
AppPublisherURL=https://github.com/ianrastall/universal-video-downloader
DefaultDirName={localappdata}\Programs\UniversalVideoDownloader
PrivilegesRequired=lowest
DisableProgramGroupPage=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.26100
ChangesAssociations=yes
UninstallDisplayName=Universal Video Downloader
UninstallDisplayIcon={app}\UniversalVideoDownloader.exe
SetupIconFile=..\src\UniversalVideoDownloader.App\Assets\UniversalVideoDownloader.ico
OutputDir={#OutputDir}
OutputBaseFilename=UniversalVideoDownloader-{#AppVersion}-Setup
Compression=lzma2/fast
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
AppMutex=Local\UniversalVideoDownloader.SingleInstance
LicenseFile=..\LICENSE
[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{autoprograms}\Universal Video Downloader"; Filename: "{app}\UniversalVideoDownloader.exe"; AppUserModelID: "Jupiter.UniversalVideoDownloader"
[Run]
Filename: "{app}\UniversalVideoDownloader.exe"; Description: "Launch Universal Video Downloader"; Flags: nowait postinstall skipifsilent
; Settings, queue, tools and downloads live outside {app}; uninstall and upgrades preserve them.
