using ManuHub.Ytdlp.Models;
using System.Text.Json.Serialization;

namespace VideoDownloader.Core;

public class MediaFormat
{
    public string Id = "b";
    public string Extension = "mp4";
    public string Resolution = "";
    public string ResolutionText = "";
    public string? FileSize = "none";
    public bool IsAudio = false;
    public string Category = "Video + Audio";

    [JsonIgnore]
    public FormatMetadata? FormatMetadata { get; set; }

    public override string ToString()
    {
        if (Id == "b")
            return "Auto (Best Quality)";

        // Audio Only → Best, 129kbps, 105kbps, 51kbps...
        if (IsAudio || Category == "Audio Only")
        {
            return Resolution;   // exactly what you asked for
        }

        // Video + Audio and Video Only → 1080p25, 720p25, 480p25...
        return !string.IsNullOrWhiteSpace(FileSize) && !FileSize.Contains("none")
            ? $"{Resolution} • {Extension.ToUpper()}" //• {FileSize}
            : Resolution;
    }
}