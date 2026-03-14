namespace Ytdlp.NET.ParseTest;

public class YtdlpFormat
{
    // Core identity
    public string FormatId { get; set; }
    public string Ext { get; set; }

    // Resolution
    public string Resolution { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public double? Fps { get; set; }

    // Channels
    public int? Channels { get; set; }

    // Bitrates
    public string Tbr { get; set; }     // table
    public double? TbrValue { get; set; } // json

    public string Vbr { get; set; }
    public double? VbrValue { get; set; }

    public string Abr { get; set; }
    public double? AbrValue { get; set; }

    // Codecs
    public string VCodec { get; set; }
    public string ACodec { get; set; }

    // Protocol
    public string Protocol { get; set; }

    // Filesize
    public string FileSize { get; set; }
    public long? FileSizeBytes { get; set; }

    // Sample rate
    public string Asr { get; set; }
    public int? AsrValue { get; set; }

    // Extra info
    public string MoreInfo { get; set; }

    // URL (only available from JSON)
    public string Url { get; set; }

    // Format description
    public string FormatNote { get; set; }

    // Helper flags
    public bool IsAudioOnly => VCodec == "none" || Resolution == "audio only";
    public bool IsVideoOnly => ACodec == "none" || ACodec == "video only";
    public bool IsProgressive => !IsAudioOnly && !IsVideoOnly;
    public bool IsStoryboard => VCodec == "images";

    // Convenience
    public bool HasAudio => !IsVideoOnly && !IsStoryboard;
    public bool HasVideo => !IsAudioOnly && !IsStoryboard;
}