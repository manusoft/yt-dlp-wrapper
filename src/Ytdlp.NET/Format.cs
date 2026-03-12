using System.Globalization;
using System.Text.RegularExpressions;

namespace YtdlpNET;

/// <summary>
/// Represents a single format available for a video/audio from yt-dlp's -F (--list-formats) output.
/// Enriched for v2.0 with more parsed fields from the format table.
/// </summary>
public sealed class Format
{
    // Core identifiers
    public string Id { get; set; } = string.Empty;          // format code / ID
    public string Extension { get; set; } = string.Empty;  // ext (mp4, webm, m4a, etc.)

    // Video-specific
    public string Resolution { get; set; } = string.Empty; // e.g. "1920x1080", "1080p", "audio only"
    public int? Height { get; set; }                       // parsed numeric height if available
    public int? Width { get; set; }                        // parsed numeric width if available
    public double? Fps { get; set; }                       // frames per second

    // Audio-specific
    public string? Channels { get; set; }                  // e.g. "2", "stereo", "6"
    public double? AudioSampleRate { get; set; }           // asr in Hz (from asr column)

    // Bitrates
    public string? TotalBitrate { get; set; }              // tbr (total bitrate)
    public string? VideoBitrate { get; set; }              // vbr
    public string? AudioBitrate { get; set; }              // abr

    // Codecs
    public string? VideoCodec { get; set; }                // vcodec (avc1, vp9, etc. or "none")
    public string? AudioCodec { get; set; }                // acodec (opus, mp4a, etc. or "none")

    // Protocol / delivery
    public string? Protocol { get; set; }                  // https, m3u8_native, mhtml, etc.
    public string? Container { get; set; }                 // sometimes inferred from ext or more info

    // Size & approx
    public string? FileSizeApprox { get; set; }            // e.g. "~123.45MiB", "N/A"
    public long? ApproxFileSizeBytes { get; set; }         // parsed numeric value (approximate)

    // Other metadata
    public string? Language { get; set; }                  // from more info or subs column if present
    public string? MoreInfo { get; set; }                  // remaining text (hdr, quality note, etc.)
    public string? Note { get; set; }                      // sometimes separate note column

    // Convenience flags
    public bool IsVideo => !string.IsNullOrEmpty(VideoCodec) && VideoCodec != "none" && Resolution != "audio only";
    public bool IsAudioOnly => Resolution == "audio only" || (VideoCodec == "none" && !string.IsNullOrEmpty(AudioCodec));
    public bool IsStoryboard => VideoCodec == "images" || MoreInfo?.Contains("storyboard") == true;

    public override string ToString()
    {
        var parts = new[]
        {
            Id.PadRight(6),
            Extension.PadRight(5),
            (Resolution ?? "unknown").PadRight(12),
            Fps?.ToString("F0", CultureInfo.InvariantCulture) ?? "-".PadRight(4),
            Channels ?? "-".PadRight(3),
            FileSizeApprox ?? "-".PadRight(12),
            Protocol ?? "-".PadRight(8),
            VideoCodec ?? "-".PadRight(10),
            AudioCodec ?? "-".PadRight(10),
            MoreInfo
        };

        return string.Join("  ", parts.Where(p => !string.IsNullOrEmpty(p)));
    }


    /// <summary>
    /// Factory method to create Format from a single line of yt-dlp -F output.
    /// Attempts to parse all columns based on typical yt-dlp table layout.
    /// </summary>
    public static Format FromParsedLine(string line)
    {
        var format = new Format();

        // Split on multiple spaces/tabs, but preserve meaningful parts
        var parts = Regex.Split(line.Trim(), @"\s{2,}|\s*\|\s*")
                         .Where(p => !string.IsNullOrWhiteSpace(p))
                         .Select(p => p.Trim())
                         .ToArray();

        if (parts.Length < 2) return format;

        int idx = 0;

        // [0] ID
        if (idx < parts.Length) format.Id = parts[idx++];

        // [1] EXT
        if (idx < parts.Length) format.Extension = parts[idx++];

        // [2] RESOLUTION / NOTE (may be "audio only" or "144p" etc.)
        if (idx < parts.Length)
        {
            format.Resolution = parts[idx];
            if (format.Resolution == "audio") // "audio only"
            {
                format.Resolution = "audio only";
                idx++;
                if (idx < parts.Length && parts[idx] == "only") idx++;
            }
            else
            {
                // Try parse height/width from e.g. "1920x1080"
                var resMatch = Regex.Match(format.Resolution, @"(\d+)x(\d+)|(\d+)p");
                if (resMatch.Success)
                {
                    if (resMatch.Groups[1].Success && resMatch.Groups[2].Success)
                    {
                        format.Width = int.TryParse(resMatch.Groups[1].Value, out int w) ? w : null;
                        format.Height = int.TryParse(resMatch.Groups[2].Value, out int h) ? h : null;
                    }
                    else if (resMatch.Groups[3].Success)
                    {
                        format.Height = int.TryParse(resMatch.Groups[3].Value, out int h) ? h : null;
                    }
                }
                idx++;
            }
        }

        // [next] FPS
        if (idx < parts.Length && double.TryParse(parts[idx], NumberStyles.Any, CultureInfo.InvariantCulture, out double fps))
        {
            format.Fps = fps;
            idx++;
        }

        // Channels / audio channels
        if (idx < parts.Length && Regex.IsMatch(parts[idx], @"^\d+$|^\d+\|?$"))
        {
            format.Channels = parts[idx].TrimEnd('|');
            idx++;
        }

        // File size approx
        if (idx < parts.Length && (parts[idx].Contains("MiB") || parts[idx].Contains("GiB") || parts[idx] == "~"))
        {
            format.FileSizeApprox = parts[idx];
            // Try parse numeric
            var sizeMatch = Regex.Match(parts[idx], @"~?(\d+\.?\d*)\s*(MiB|GiB|KiB)?");
            if (sizeMatch.Success)
            {
                if (double.TryParse(sizeMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                {
                    double multiplier = sizeMatch.Groups[2].Value switch
                    {
                        "GiB" => 1024 * 1024 * 1024,
                        "MiB" => 1024 * 1024,
                        "KiB" => 1024,
                        _ => 1
                    };
                    format.ApproxFileSizeBytes = (long)(val * multiplier);
                }
            }
            idx++;
        }

        // TBR (total bitrate)
        if (idx < parts.Length && parts[idx].EndsWith("k"))
        {
            format.TotalBitrate = parts[idx];
            idx++;
        }

        // Protocol
        if (idx < parts.Length && (parts[idx].StartsWith("http") || parts[idx] == "m3u8_native" || parts[idx] == "mhtml"))
        {
            format.Protocol = parts[idx];
            idx++;
        }

        // VCODEC
        if (idx < parts.Length)
        {
            format.VideoCodec = parts[idx];
            if (format.VideoCodec == "audio" && idx + 1 < parts.Length && parts[idx + 1] == "only")
            {
                format.VideoCodec = "none";
                idx += 2;
            }
            else if (format.VideoCodec == "images")
            {
                idx++;
            }
            else idx++;
        }

        // ACODEC / remaining
        if (idx < parts.Length)
        {
            format.AudioCodec = parts[idx] == "video" ? "none" : parts[idx];
            idx++;
        }

        // Remaining parts → MoreInfo
        if (idx < parts.Length)
        {
            format.MoreInfo = string.Join(" ", parts[idx..]).Trim();
            // Clean up pipes or redundant
            format.MoreInfo = Regex.Replace(format.MoreInfo, @"^\|\s*", "").Trim();
        }

        return format;
    }

    public IEnumerable<Format> FilterFormats(IEnumerable<Format> formats, string type)
    {
        return formats.Where(f => type == "audio" ? f.Resolution == "audio only" :
                                  type == "video" ? f.Resolution != "audio only" && f.Extension != "mhtml" :
                                  true);
    }
}