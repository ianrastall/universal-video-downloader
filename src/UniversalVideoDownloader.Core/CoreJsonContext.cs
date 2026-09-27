using System.Text.Json.Serialization;
using UniversalVideoDownloader.Core.Downloads;
using UniversalVideoDownloader.Core.Settings;
using UniversalVideoDownloader.Core.Tools;

namespace UniversalVideoDownloader.Core;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    UseStringEnumConverter = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
    AllowTrailingCommas = true)]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(List<SavedDownload>))]
[JsonSerializable(typeof(ToolManifest))]
internal sealed partial class CoreJsonContext : JsonSerializerContext;
