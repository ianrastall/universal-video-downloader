# Download parser boundary (ABI 1)

`core/src/lib.rs`: pure Rust parser plus three synchronous C exports.

- `uint32_t uvd_abi_version()` returns 1.
- `char* uvd_parse_line(const uint8_t* input, size_t length, uint8_t stderr)` borrows valid UTF-8
  bytes for the call and returns a Rust-owned, NUL-terminated JSON array. stderr is 0 or 1.
  Null input, invalid UTF-8, input over 4 MiB, invalid bool and caught Rust panic return null.
- `void uvd_result_free(char* result)` consumes exactly one live result, or accepts null.
  The caller must not reuse freed pointers or pass foreign pointers.

There is no context and no shared mutable state. Calls may run concurrently, including stdout
and stderr pump tasks on different threads. Each result is independent. UTF-8 text, including
embedded NUL characters, is JSON escaped; response strings contain no interior NUL bytes.
C# owns the result through SafeHandle, copies it into managed event records before disposal,
and never shares a borrowed span beyond the call. DLL loading uses the application directory,
not the current directory or PATH. C# checks the ABI before its first parse. Unsupported
responses produce a managed exception; Rust panics cannot unwind into C#.

Event kinds: progress (downloaded/total/speed/eta nullable finite numbers), title (text), file
(text path), streams (count), destination, playlist (index/count), stage (text), log (text/error).
The parser preserves the original C# event behavior, including a Saved log after file events.
No callbacks, cancellation handle or shutdown race exists at this stateless boundary. Process
cancellation remains in the managed Windows adapter and kills the entire child process tree.

Checks: Rust malformed-input and repeated-allocation tests; the original parser tests now cross
this boundary; parallel managed UTF-8 and embedded-NUL tests; real video/audio media smoke.
