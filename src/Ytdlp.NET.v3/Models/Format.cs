using System.Globalization;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ManuHub.Ytdlp.Models;

/// <summary>
/// Represents a single format available for the video (from yt-dlp --list-formats or JSON).
/// </summary>
public class Format
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
    public string? FileSize { get; set; }            // e.g. "~123.45MiB", "N/A"
    public long? FileSizeApprox { get; set; }         // parsed numeric value (approximate)

    // Other metadata
    public string? Language { get; set; }                  // from more info or subs column if present
    public string? MoreInfo { get; set; }                  // remaining text (hdr, quality note, etc.)
    public string? Note { get; set; }                      // sometimes separate note column


    // Additional fields if needed (e.g. quality sort key, has_audio, etc.)
    public bool HasVideo => !string.IsNullOrEmpty(VideoCodec) && VideoCodec != "none" && Resolution != "audio only";
    public bool HasAudio => !string.IsNullOrEmpty(AudioCodec) && AudioCodec != "none";
    public bool IsAudioOnly => Resolution == "audio only" || VideoCodec == "none";
    public bool HasStoryboard => VideoCodec == "images" || MoreInfo?.Contains("storyboard") == true;

    public override string ToString()
    {
        var parts = new[]
        {
            Id.PadRight(6),
            Extension.PadRight(5),
            (Resolution ?? "unknown").PadRight(12),
            Fps?.ToString("F0", CultureInfo.InvariantCulture) ?? "-".PadRight(4),
            Channels ?? "-".PadRight(3),
            FileSize ?? "-".PadRight(12),
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

        // Normalize pipes and collapse multiple spaces
        var normalized = Regex.Replace(line.Trim(), @"\s*\|\s*", " | ");
        normalized = Regex.Replace(normalized, @"\s{2,}", "  ");

        // Split on double spaces primarily (yt-dlp uses consistent padding)
        var tokens = Regex.Split(normalized, @"\s{2,}")
                          .Select(t => t.Trim())
                          .Where(t => !string.IsNullOrEmpty(t))
                          .ToList();

        if (tokens.Count < 3) return format;

        int i = 0;

        // 1. ID
        format.Id = tokens[i++];

        // 2. EXT
        format.Extension = tokens[i++];

        // 3. RESOLUTION or "audio only"
        string resToken = tokens[i++];
        if (resToken == "audio" && i < tokens.Count && tokens[i] == "only")
        {
            format.Resolution = "audio only";
            i++; // consume "only"
        }
        else
        {
            format.Resolution = resToken;

            // Parse width×height or heightp
            var m = Regex.Match(resToken, @"^(?:(\d+)x(\d+)|(\d+)p)");
            if (m.Success)
            {
                if (m.Groups[1].Success) format.Width = int.Parse(m.Groups[1].Value);
                if (m.Groups[2].Success) format.Height = int.Parse(m.Groups[2].Value);
                if (m.Groups[3].Success) format.Height = int.Parse(m.Groups[3].Value);
            }
        }

        // 4. FPS (optional)
        if (i < tokens.Count && double.TryParse(tokens[i], NumberStyles.Any, CultureInfo.InvariantCulture, out var fps))
        {
            format.Fps = fps;
            i++;
        }

        // 5. CHANNELS (optional, usually for audio)
        if (i < tokens.Count && Regex.IsMatch(tokens[i], @"^\d+$|^\d+\|?$"))
        {
            format.Channels = tokens[i].TrimEnd('|');
            i++;
        }

        // 6. FILESIZE (optional)
        if (i < tokens.Count && (tokens[i].Contains("MiB") || tokens[i].Contains("GiB") || tokens[i].StartsWith("~")))
        {
            format.FileSize = tokens[i];

            // More flexible size parsing
            var sm = Regex.Match(tokens[i], @"~?\s*([\d\.]+)\s*([KMG]?i?B)?", RegexOptions.IgnoreCase);
            if (sm.Success && double.TryParse(sm.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
            {
                string unit = sm.Groups[2].Value.ToLowerInvariant();
                long mul = unit switch
                {
                    "gib" => 1_073_741_824L,
                    "mib" => 1_048_576L,
                    "kib" => 1_024L,
                    "gb" => 1_000_000_000L,
                    "mb" => 1_000_000L,
                    "kb" => 1_000L,
                    _ => 1L
                };
                format.FileSizeApprox = (long)(num * mul);
            }
            i++;
        }

        // 7. TBR (optional)
        if (i < tokens.Count && tokens[i].EndsWith("k"))
        {
            format.TotalBitrate = tokens[i];
            i++;
        }

        // 8. PROTOCOL
        if (i < tokens.Count && (tokens[i].StartsWith("http") || tokens[i].Contains("m3u8") || tokens[i] == "mhtml"))
        {
            format.Protocol = tokens[i];
            i++;
        }

        // 9. VCODEC
        if (i < tokens.Count)
        {
            string vc = tokens[i];
            if (vc == "audio" && i + 1 < tokens.Count && tokens[i + 1] == "only")
            {
                format.VideoCodec = "none";
                i += 2;
            }
            else if (vc == "images" || vc.StartsWith("avc1") || vc.StartsWith("vp") || vc.StartsWith("av01"))
            {
                format.VideoCodec = vc;
                i++;
            }
            else
            {
                // fallback — sometimes codec is missing or unusual
                i++;
            }
        }

        // 10. ACODEC or "video only"
        if (i < tokens.Count)
        {
            string ac = tokens[i];
            if (ac == "video" && i + 1 < tokens.Count && tokens[i + 1] == "only")
            {
                format.AudioCodec = "none";
                i += 2;
            }
            else if (ac == "audio" && i + 1 < tokens.Count && tokens[i + 1] == "only")
            {
                format.AudioCodec = "none"; // or keep previous if meaningful
                i += 2;
            }
            else if (ac.Contains(".") || ac == "opus" || ac.StartsWith("mp4a"))
            {
                format.AudioCodec = ac;
                i++;
            }
            else
            {
                i++;
            }
        }

        // Remaining tokens → MoreInfo
        if (i < tokens.Count)
        {
            format.MoreInfo = string.Join(" ", tokens.Skip(i)).Trim();
            format.MoreInfo = Regex.Replace(format.MoreInfo, @"^\|\s*", "").Trim();
        }

        return format;
    }

    public static IEnumerable<Format> FilterFormats(IEnumerable<Format> formats, string type)
    {
        return formats.Where(f => type == "audio" ? f.Resolution == "audio only" :
                                  type == "video" ? f.Resolution != "audio only" && f.Extension != "mhtml" :
                                  true);
    }
}
