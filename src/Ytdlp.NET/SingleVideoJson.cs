using System.Text.Json.Serialization;

namespace YtdlpNET;

// Supporing class for single

public class SingleVideoJson
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string? Title { get; set;  }

    [JsonPropertyName("formats")]
    public List<FormatJson>? Formats { get; set; }
}

public class FormatJson
{
    [JsonPropertyName("format_id")] public string? FormatId { get; set; }
    [JsonPropertyName("ext")] public string? Ext { get; set; }
    [JsonPropertyName("height")] public int? Height { get; set; }
    [JsonPropertyName("width")] public int? Width { get; set; }
    [JsonPropertyName("resolution")] public string? Resolution { get; set; }
    [JsonPropertyName("fps")] public double? Fps { get; set; }
    [JsonPropertyName("audio_channels")] public int? AudioChannels { get; set; }
    [JsonPropertyName("asr")] public double? Asr { get; set; }
    [JsonPropertyName("tbr")] public double? Tbr { get; set; }
    [JsonPropertyName("vbr")] public double? Vbr { get; set; }
    [JsonPropertyName("abr")] public double? Abr { get; set; }
    [JsonPropertyName("vcodec")] public string? Vcodec { get; set; }
    [JsonPropertyName("acodec")] public string? Acodec { get; set; }
    [JsonPropertyName("protocol")] public string? Protocol { get; set; }
    [JsonPropertyName("language")] public string? Language { get; set; }
    [JsonPropertyName("filesize")] public long? Filesize { get; set; }
    [JsonPropertyName("filesize_approx")] public long? FilesizeApprox { get; set; }
    [JsonPropertyName("format_note")] public string? FormatNote { get; set; }
}