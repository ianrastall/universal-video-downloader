using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.Win32.SafeHandles;

namespace UniversalVideoDownloader.Core.Downloads;

/// <summary>Managed adapter for the stateless Jupiter Rust download parser (ABI 1).</summary>
public static class YtDlpOutputParser
{
    public const string ProgressMarker = "@@P ";
    public const string TitleMarker = "@@T ";
    public const string FileMarker = "@@F ";
    static YtDlpOutputParser()
    {
        NativeLibrary.SetDllImportResolver(typeof(YtDlpOutputParser).Assembly, (name, assembly, path) =>
        {
            if (name != "uvd_core") return IntPtr.Zero;
            var file = OperatingSystem.IsWindows() ? "uvd_core.dll" : OperatingSystem.IsMacOS() ? "libuvd_core.dylib" : "libuvd_core.so";
            return NativeLibrary.Load(Path.Combine(AppContext.BaseDirectory, file));
        });
        if (AbiVersion() != 1) throw new InvalidOperationException("The download core has an incompatible ABI.");
    }
    [DllImport("uvd_core", EntryPoint = "uvd_abi_version", CallingConvention = CallingConvention.Cdecl)]
    private static extern uint AbiVersion();
    [DllImport("uvd_core", EntryPoint = "uvd_parse_line", CallingConvention = CallingConvention.Cdecl)]
    private static extern NativeResult ParseLine(byte[] input, nuint length, byte stderr);
    [DllImport("uvd_core", EntryPoint = "uvd_result_free", CallingConvention = CallingConvention.Cdecl)]
    private static extern void FreeResult(IntPtr result);
    private sealed class NativeResult : SafeHandleZeroOrMinusOneIsInvalid
    {
        public NativeResult() : base(true) { }
        protected override bool ReleaseHandle() { FreeResult(handle); return true; }
    }
    public static IReadOnlyList<JobUpdate> Parse(string line, bool fromStdErr)
    {
        ArgumentNullException.ThrowIfNull(line);
        var bytes = Encoding.UTF8.GetBytes(line);
        // One sentinel byte also supplies a valid nonnull pointer for an empty line.
        using var result = ParseLine(bytes.Length == 0 ? new byte[1] : bytes, (nuint)bytes.Length, fromStdErr ? (byte)1 : (byte)0);
        if (result.IsInvalid) throw new InvalidOperationException("The download core rejected the output line.");
        using var document = JsonDocument.Parse(Marshal.PtrToStringUTF8(result.DangerousGetHandle())!);
        var updates = new List<JobUpdate>();
        foreach (var item in document.RootElement.EnumerateArray())
        {
            string Text() => item.GetProperty("text").GetString()!;
            int Int(string key) => item.GetProperty(key).GetInt32();
            double? Number(string key) => item.GetProperty(key).ValueKind == JsonValueKind.Null ? null : item.GetProperty(key).GetDouble();
            updates.Add(item.GetProperty("kind").GetString() switch
            {
                "progress" => new JobUpdate.Progress(Number("downloaded"), Number("total"), Number("speed"), Number("eta")),
                "title" => new JobUpdate.Title(Text()), "file" => new JobUpdate.FileSaved(Text()),
                "streams" => new JobUpdate.Streams(Int("count")), "destination" => new JobUpdate.Destination(),
                "playlist" => new JobUpdate.PlaylistItem(Int("index"), Int("count")),
                "stage" => new JobUpdate.Stage(Text()), "log" => new JobUpdate.Log(Text(), item.GetProperty("error").GetBoolean()),
                _ => throw new InvalidOperationException("Unknown download core response."),
            });
        }
        return updates;
    }
}
