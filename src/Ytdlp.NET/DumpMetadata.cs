using System.Text.Json.Serialization;

namespace YtdlpDotNet;

public class DumpRoot
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    [JsonPropertyName("title")]
    public string Title { get; set; } = default!;

    [JsonPropertyName("description")]
    public string Description { get; set; } = default!;   

    [JsonPropertyName("thumbnail")]
    public string Thumbnail { get; set; } = default!;

    [JsonPropertyName("formats")]
    public List<Format>? Formats { get; set; }

    [JsonPropertyName("requested_formats")]
    public List<Format>?    RequestedFormats { get; set; }
}

public class Format
{
    [JsonPropertyName("format_id")]
    public string FormatId { get; set; } = default!;

    [JsonPropertyName("format_note")]
    public string FormatNote { get; set; } = default!;

    [JsonPropertyName("ext")]
    public string Ext { get; set; } = default!;

    [JsonPropertyName("protocol")]
    public string Protocol { get; set; } = default!;

    [JsonPropertyName("acodec")]
    public string Acodec { get; set; } = default!;

    [JsonPropertyName("vcodec")]
    public string Vcodec { get; set; } = default!;

    [JsonPropertyName("url")]
    public string Url { get; set; } = default!;

    [JsonPropertyName("resolution")]
    public string Resolution { get; set; } = default!;

    [JsonPropertyName("fps")]
    public double? Fps { get; set; }

    [JsonPropertyName("audio_channels")]
    public int? AudioChannels { get; set; }

    [JsonPropertyName("width")]
    public int? Width { get; set; }

    [JsonPropertyName("height")]
    public int? Height { get; set; }

    [JsonPropertyName("aspect_ratio")]
    public double? AspectRatio { get; set; }

    [JsonPropertyName("abr")]
    public double? Abr { get; set; }

    [JsonPropertyName("vbr")]
    public double? Vbr { get; set; }

    [JsonPropertyName("tbr")]
    public double? Tbr { get; set; }

    [JsonPropertyName("filesize")]
    public long? Filesize { get; set; }

    [JsonPropertyName("filesize_approx")]
    public long? FilesizeApprox { get; set; }

    [JsonPropertyName("format")]
    public string FormatString { get; set; } = default!;

    [JsonPropertyName("fragments")]
    public List<Fragment>? Fragments { get; set; }

    public bool IsAudio => Acodec != "none";
    public bool HasFragments => Fragments != null && Fragments.Count > 0;
}

public class Fragment
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = default!;

    [JsonPropertyName("duration")]
    public double Duration { get; set; }
}
