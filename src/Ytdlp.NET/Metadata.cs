using System.Text.Json.Serialization;

namespace YtdlpNET;

public class Metadata
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    [JsonPropertyName("title")]
    public string Title { get; set; } = default!;

    [JsonPropertyName("description")]
    public string Description { get; set; } = default!;

    [JsonPropertyName("thumbnail")]
    public string Thumbnail { get; set; } = default!;

    [JsonPropertyName("thumbnails")]
    public List<ThumbnailMetadata>? Thumbnails { get; set; }

    [JsonPropertyName("formats")]
    public List<FormatMetadata>? Formats { get; set; }

    [JsonPropertyName("requested_formats")]
    public List<FormatMetadata>? RequestedFormats { get; set; }

    [JsonPropertyName("duration")]
    public double? Duration { get; set; }

    [JsonPropertyName("view_count")]
    public long? ViewCount { get; set; }

    [JsonPropertyName("like_count")]
    public long? LikeCount { get; set; }

    [JsonPropertyName("average_rating")]
    public double? AverageRating { get; set; }

    [JsonPropertyName("age_limit")]
    public int? AgeLimit { get; set; }

    [JsonPropertyName("webpage_url")]
    public string? WebpageUrl { get; set; }

    [JsonPropertyName("categories")]
    public List<string>? Categories { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("playable_in_embed")]
    public bool? PlayableInEmbed { get; set; }

    [JsonPropertyName("live_status")]
    public string? LiveStatus { get; set; }

    [JsonPropertyName("release_timestamp")]
    public long? ReleaseTimestamp { get; set; }

    [JsonPropertyName("automatic_captions")]
    public Dictionary<string, List<SubtitleMetadata>>? AutomaticCaptions { get; set; }

    [JsonPropertyName("subtitles")]
    public Dictionary<string, List<SubtitleMetadata>>? Subtitles { get; set; }

    [JsonPropertyName("comment_count")]
    public long? CommentCount { get; set; }

    [JsonPropertyName("chapters")]
    public List<ChapterMetadata>? Chapters { get; set; }

    [JsonPropertyName("heatmap")]
    public List<HeatmapMetadata>? Heatmap { get; set; }

    [JsonPropertyName("channel")]
    public string? Channel { get; set; }

    [JsonPropertyName("channel_id")]
    public string? ChannelId { get; set; }

    [JsonPropertyName("channel_follower_count")]
    public long? ChannelFollowerCount { get; set; }

    [JsonPropertyName("uploader")]
    public string? Uploader { get; set; }

    [JsonPropertyName("uploader_id")]
    public string? UploaderId { get; set; }

    [JsonPropertyName("uploader_url")]
    public string? UploaderUrl { get; set; }

    [JsonPropertyName("upload_date")]
    public string? UploadDate { get; set; }

    [JsonPropertyName("timestamp")]
    public long? Timestamp { get; set; }

    [JsonPropertyName("availability")]
    public string? Availability { get; set; }

    [JsonPropertyName("original_url")]
    public string? OriginalUrl { get; set; }

    [JsonPropertyName("is_live")]
    public bool? IsLive { get; set; }

    [JsonPropertyName("was_live")]
    public bool? WasLive { get; set; }

    [JsonPropertyName("extractor")]
    public string? Extractor { get; set; }

    [JsonPropertyName("extractor_key")]
    public string? ExtractorKey { get; set; }

    [JsonPropertyName("_type")]
    public string? Type { get; set; }
}

public class ThumbnailMetadata
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("preference")]
    public int? Preference { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("height")]
    public int? Height { get; set; }

    [JsonPropertyName("width")]
    public int? Width { get; set; }

    [JsonPropertyName("resolution")]
    public string? Resolution { get; set; }
}

public class FormatMetadata
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
    public List<FragmentMetadata>? Fragments { get; set; }

    [JsonPropertyName("asr")]
    public int? Asr { get; set; }

    [JsonPropertyName("source_preference")]
    public int? SourcePreference { get; set; }

    [JsonPropertyName("quality")]
    public double? Quality { get; set; }

    [JsonPropertyName("has_drm")]
    public bool? HasDrm { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("language_preference")]
    public int? LanguagePreference { get; set; }

    [JsonPropertyName("preference")]
    public int? Preference { get; set; }

    [JsonPropertyName("dynamic_range")]
    public string? DynamicRange { get; set; }

    [JsonPropertyName("container")]
    public string? Container { get; set; }

    [JsonPropertyName("http_headers")]
    public Dictionary<string, string>? HttpHeaders { get; set; }

    [JsonPropertyName("downloader_options")]
    public Dictionary<string, object>? DownloaderOptions { get; set; }

    public bool IsAudio => Acodec != "none";
    public bool HasFragments => Fragments != null && Fragments.Count > 0;
}

public class SubtitleMetadata
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("ext")]
    public string? Ext { get; set; }
}

public class ChapterMetadata
{
    [JsonPropertyName("start_time")]
    public double StartTime { get; set; }

    [JsonPropertyName("end_time")]
    public double EndTime { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }
}

public class HeatmapMetadata
{
    [JsonPropertyName("start_time")]
    public double StartTime { get; set; }

    [JsonPropertyName("end_time")]
    public double EndTime { get; set; }

    [JsonPropertyName("value")]
    public double Value { get; set; }
}

public class FragmentMetadata
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = default!;

    [JsonPropertyName("duration")]
    public double Duration { get; set; }
}


public class SimpleMetadata
{
    public string? Id { get; set; }
    public string? Title { get; set; }    
    public double? Duration { get; set; }
    public string? Thumbnail { get; set; }
    public long? ViewCount { get; set; }
    public long? FileSize { get; set; }
    public string? Description { get; set; }
}
