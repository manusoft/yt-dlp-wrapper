using System.Globalization;
using System.Text.RegularExpressions;

namespace ManuHub.Ytdlp.NET;

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

        // Step 1: Replace pipes with space (they are just column separators)
        string cleaned = Regex.Replace(line, @"\s*\|\s*", " ");

        // Step 2: Collapse multiple whitespace → single space, trim
        cleaned = Regex.Replace(cleaned, @"\s{2,}", " ").Trim();

        // Step 3: Split into tokens
        var tokens = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .Select(t => t.Trim())
                            .ToList();

        if (tokens.Count < 3) return format;

        int idx = 0;

        // ID (usually alphanumeric like sb0, 139, 160...)
        format.Id = tokens[idx++];

        // Extension (m4a, webm, mp4, mhtml...)
        format.Extension = tokens[idx++];

        // Resolution or "audio only"
        string token = tokens[idx];
        if (token == "audio" && idx + 1 < tokens.Count && tokens[idx + 1] == "only")
        {
            format.Resolution = "audio only";
            format.VideoCodec = "none";
            idx += 2;
        }
        else
        {
            format.Resolution = token;

            // Parse 1920x1080 or 1080p etc.
            var resMatch = Regex.Match(token, @"(\d+)x(\d+)|(\d+)p");
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

        // FPS (usually integer or -)
        if (idx < tokens.Count && double.TryParse(tokens[idx], NumberStyles.Any, CultureInfo.InvariantCulture, out double fpsVal) && fpsVal > 0)
        {
            format.Fps = fpsVal;
            idx++;
        }

        // Channels (2, stereo, etc. — mostly for audio)
        if (idx < tokens.Count && Regex.IsMatch(tokens[idx], @"^\d+$"))
        {
            format.Channels = tokens[idx];
            idx++;
        }

        // File size (~5.52MiB, 14.66MiB, etc.)
        if (idx < tokens.Count && (tokens[idx].Contains("MiB") || tokens[idx].Contains("GiB") || tokens[idx].StartsWith("~")))
        {
            format.FileSizeApprox = tokens[idx];

            var sizeMatch = Regex.Match(tokens[idx], @"~?([\d\.]+)\s*([KMG]?i?B)");
            if (sizeMatch.Success && double.TryParse(sizeMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double sizeVal))
            {
                string unit = sizeMatch.Groups[2].Value.ToLowerInvariant();
                long multiplier = unit switch
                {
                    "gib" => 1073741824L,
                    "mib" => 1048576L,
                    "kib" => 1024L,
                    _ => 1L
                };
                format.FileSizeApprox = (sizeVal * multiplier).ToString();
            }
            idx++;
        }

        // Total Bitrate (49k, 128k, etc.)
        if (idx < tokens.Count && tokens[idx].EndsWith("k"))
        {
            format.TotalBitrate = tokens[idx];
            idx++;
        }

        // Protocol (https, m3u8, mhtml...)
        if (idx < tokens.Count && (tokens[idx].StartsWith("http") || tokens[idx].Contains("m3u8") || tokens[idx] == "mhtml"))
        {
            format.Protocol = tokens[idx];
            idx++;
        }

        // Video codec (avc1..., vp9, av01..., images, none)
        if (idx < tokens.Count)
        {
            string vc = tokens[idx];
            if (vc == "audio" && idx + 1 < tokens.Count && tokens[idx + 1] == "only")
            {
                format.VideoCodec = "none";
                idx += 2;
            }
            else if (vc == "images" || vc.StartsWith("avc1") || vc.StartsWith("vp") || vc.StartsWith("av01"))
            {
                format.VideoCodec = vc;
                idx++;
            }
        }

        // Audio codec (opus, mp4a..., none)
        if (idx < tokens.Count)
        {
            string ac = tokens[idx];
            if (ac == "video" && idx + 1 < tokens.Count && tokens[idx + 1] == "only")
            {
                format.AudioCodec = "none";
                idx += 2;
            }
            else if (ac == "audio" && idx + 1 < tokens.Count && tokens[idx + 1] == "only")
            {
                format.AudioCodec = "none";
                idx += 2;
            }
            else if (ac.Contains(".") || ac == "opus")
            {
                format.AudioCodec = ac;
                idx++;
            }
        }

        // All remaining → MoreInfo
        if (idx < tokens.Count)
        {
            format.MoreInfo = string.Join(" ", tokens.GetRange(idx, tokens.Count - idx));
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