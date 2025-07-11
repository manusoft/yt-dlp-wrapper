namespace ClipMate.Models;

public class MediaFormat
{
    public string ID { get; set; } = "best";
    public string Extension { get; set; } = "mp4";
    public string Resolution { get; set; } = "Best";
    public string? FileSize { get; set; } = "Unknown";
    public string? FPS { get; set; }
    public string? Channels { get; set; }
    public string? VCodec { get; set; }
    public string? ACodec { get; set; }
    public string? MoreInfo { get; set; }    
    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(FileSize)
            ? $"{Resolution} ({Extension})"
            : $"{Resolution} ({Extension}, {FileSize})";
    }
}