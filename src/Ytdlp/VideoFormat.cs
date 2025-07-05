namespace YtdlpDotNet;

public sealed class VideoFormat
{
    public string ID { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string Resolution { get; set; } = string.Empty;
    public string? FPS { get; set; }
    public string? Channels { get; set; }
    public string? FileSize { get; set; }
    public string? TBR { get; set; } // Total Bitrate
    public string? Protocol { get; set; }
    public string? VCodec { get; set; } // Video Codec
    public string? VBR { get; set; } // Video Bitrate
    public string? ACodec { get; set; } // Audio Codec
    public string? ABR { get; set; } // Audio Bitrate
    public string? ASR { get; set; } // Audio Sample Rate
    public string? MoreInfo { get; set; }

    public IEnumerable<VideoFormat> FilterFormats(IEnumerable<VideoFormat> formats, string type)
    {
        return formats.Where(f => type == "audio" ? f.Resolution == "audio only" :
                                 type == "video" ? f.Resolution != "audio only" && f.Extension != "mhtml" :
                                 true);
    }
}