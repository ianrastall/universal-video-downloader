use serde_json::{Value, json};
use std::ffi::{CString, c_char};

const STAGES: &[&str] = &[
    "Merger",
    "ExtractAudio",
    "EmbedThumbnail",
    "Metadata",
    "EmbedSubtitle",
    "FixupM3u8",
    "FixupM4a",
    "FixupDuplicateMoov",
    "FixupTimestamp",
    "FixupStretched",
    "VideoConvertor",
    "VideoRemuxer",
    "ThumbnailsConvertor",
    "MoveFiles",
    "SponsorBlock",
    "ModifyChapters",
];

/// Pure domain parser. No UI, process, filesystem or shared mutable state.
pub fn parse(line: &str, stderr: bool) -> Value {
    if let Some(rest) = line.strip_prefix("@@P ") {
        let fields: Vec<_> = rest.split('|').collect();
        let number = |index: usize| {
            fields
                .get(index)
                .and_then(|s| s.trim().parse::<f64>().ok())
                .filter(|n| n.is_finite())
        };
        return json!([{"kind":"progress", "downloaded":number(0), "total":number(1).or_else(|| number(2)), "speed":number(3), "eta":number(4)}]);
    }
    if let Some(rest) = line.strip_prefix("@@T ") {
        return json!([{"kind":"title", "text":rest}]);
    }
    if let Some(rest) = line.strip_prefix("@@F ") {
        return json!([{"kind":"file", "text":rest}, {"kind":"log", "text":format!("Saved: {rest}"), "error":false}]);
    }
    let mut updates = vec![];
    if let Some(rest) = line.strip_prefix("[download] ") {
        if rest.starts_with("Destination:") {
            updates.push(json!({"kind":"destination"}));
        } else if let Some(rest) = rest.strip_prefix("Downloading item ")
            && let Some((index, count)) = rest.split_once(" of ")
            && let (Ok(index), Ok(count)) =
                (index.trim().parse::<i32>(), count.trim().parse::<i32>())
        {
            updates.push(json!({"kind":"playlist", "index":index, "count":count}));
        }
    } else if line.starts_with("[info] ") {
        if let Some((_, formats)) = line.split_once("format(s): ") {
            updates.push(json!({"kind":"streams", "count":formats.trim().split('+').count()}));
        }
    } else if let Some(rest) = line.strip_prefix('[')
        && let Some((stage, _)) = rest.split_once(']')
        && STAGES.contains(&stage)
    {
        updates.push(json!({"kind":"stage", "text":stage}));
    }
    updates.push(json!({"kind":"log", "text":line, "error":stderr && line.starts_with("ERROR")}));
    Value::Array(updates)
}

#[unsafe(no_mangle)]
pub extern "C" fn uvd_abi_version() -> u32 {
    1
}

/// Returns an owned UTF-8 JSON string, released only with uvd_result_free.
/// Null on invalid UTF-8, excessive input, invalid bool or a contained panic.
///
/// # Safety
/// `input` must point to `length` readable bytes for the duration of this call.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn uvd_parse_line(
    input: *const u8,
    length: usize,
    stderr: u8,
) -> *mut c_char {
    if input.is_null() || length > 4 * 1024 * 1024 || stderr > 1 {
        return std::ptr::null_mut();
    }
    std::panic::catch_unwind(|| {
        // SAFETY: the caller supplies a valid borrowed slice for this synchronous call.
        let bytes = unsafe { std::slice::from_raw_parts(input, length) };
        let text = std::str::from_utf8(bytes).ok()?;
        CString::new(parse(text, stderr != 0).to_string())
            .ok()
            .map(CString::into_raw)
    })
    .ok()
    .flatten()
    .unwrap_or(std::ptr::null_mut())
}

/// # Safety
/// `result` is null or a live result returned by uvd_parse_line. Free exactly once.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn uvd_result_free(result: *mut c_char) {
    if !result.is_null() {
        // SAFETY: ownership of this exact allocation is returned by the caller.
        drop(unsafe { CString::from_raw(result) });
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn progress_handles_missing_and_nonfinite_fields() {
        let p = parse("@@P 1024|NA|4096|NaN|inf", false);
        assert_eq!(p[0]["downloaded"], 1024.0);
        assert_eq!(p[0]["total"], 4096.0);
        assert!(p[0]["speed"].is_null());
        assert!(p[0]["eta"].is_null());
    }
    #[test]
    fn unicode_and_embedded_nul_roundtrip() {
        assert_eq!(parse("@@T 视频 🎬\0", false)[0]["text"], "视频 🎬\0");
        assert_eq!(parse("[Merger] merging", false)[0]["text"], "Merger");
        assert_eq!(
            parse("[download] Downloading item 3 of 10", false)[0]["count"],
            10
        );
        assert_eq!(
            parse("@@F C:\\视频\\a.mkv", false)
                .as_array()
                .unwrap()
                .len(),
            2
        );
    }
    #[test]
    fn abi_rejects_invalid_input_and_remains_usable() {
        // SAFETY: all nonnull inputs refer to live byte arrays; each result is freed once.
        unsafe {
            assert!(uvd_parse_line(std::ptr::null(), 0, 0).is_null());
            assert!(uvd_parse_line([0xff].as_ptr(), 1, 0).is_null());
            assert!(uvd_parse_line(b"a".as_ptr(), 4 * 1024 * 1024 + 1, 0).is_null());
            assert!(uvd_parse_line(b"a".as_ptr(), 1, 2).is_null());
            for _ in 0..1000 {
                let output = uvd_parse_line(b"@@T test".as_ptr(), 8, 0);
                assert!(!output.is_null());
                uvd_result_free(output);
            }
            uvd_result_free(std::ptr::null_mut());
        }
    }
}
