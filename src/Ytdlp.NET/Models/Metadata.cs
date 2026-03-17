using System.Text.Json.Serialization;

namespace ManuHub.Ytdlp.NET;

public class Metadata
{
    /// <summary>
    /// Video identifier
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }   // playlist/video

    [JsonPropertyName("_type")]
    public string? Type { get; set; }   // playlist/video

    /// <summary>
    /// Video title
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }   // playlist/video   

    /// <summary>
    /// The description of the video
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; } // playlist/video

    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; set; }  // video

    [JsonPropertyName("playlist_count")]
    public long? PlaylistCount { get; set; }   // playlist

    [JsonPropertyName("categories")]
    public List<string>? Categories { get; set; }   // video

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; } // playlist/video

    [JsonPropertyName("channel_id")]
    public string? ChannelId { get; set; } // playlist/video

    [JsonPropertyName("channel")]
    public string? Channel { get; set; } // playlist/video

    [JsonPropertyName("channel_url")]
    public string? ChannelUrl { get; set; } // playlist/video

    [JsonPropertyName("channel_follower_count")]
    public long? ChannelFollowerCount { get; set; } // playlist/video


    [JsonPropertyName("uploader_id")]
    public string? UploaderId { get; set; } // playlist/video

    [JsonPropertyName("uploader")]
    public string? Uploader { get; set; }   // playlist/video

    [JsonPropertyName("uploader_url")]
    public string? UploaderUrl { get; set; }    // playlist/video

    [JsonPropertyName("upload_date")]
    public string? UploadDate { get; set; } // video


    [JsonPropertyName("webpage_url")]
    public string? WebpageUrl { get; set; }  // playlist/video

    [JsonPropertyName("original_url")]
    public string? OriginalUrl { get; set; }  // playlist/video


    [JsonPropertyName("availability")]
    public string? Availability { get; set; }  // playlist/video


    [JsonPropertyName("extractor")]
    public string? Extractor { get; set; } // playlist/video

    [JsonPropertyName("extractor_key")]
    public string? ExtractorKey { get; set; } // playlist/video

    /// <summary>
    /// How many users have watched the video on the platform
    /// </summary>
    [JsonPropertyName("view_count")]
    public float? ViewCount { get; set; }    // playlist/video


    [JsonPropertyName("duration")]
    public float? Duration { get; set; }   // video

    [JsonPropertyName("age_limit")]
    public int? AgeLimit { get; set; }  // video

    [JsonPropertyName("playable_in_embed")]
    public bool? PlayableInEmbed { get; set; }  // video

    [JsonPropertyName("live_status")]
    public string? LiveStatus { get; set; } // video

    [JsonPropertyName("comment_count")]
    public long? CommentCount { get; set; } // video  

    [JsonPropertyName("like_count")]
    public long? LikeCount { get; set; }    // video

    [JsonPropertyName("timestamp")]
    public long? Timestamp { get; set; }    // video

    [JsonPropertyName("duration_string")]
    public string? DurationString { get; set; }    // video

    [JsonPropertyName("is_live")]
    public bool? IsLive { get; set; }   // video

    [JsonPropertyName("was_live")]
    public bool? WasLive { get; set; }  // video

    [JsonPropertyName("entries")]
    public List<Entry>? Entries { get; set; }   // playlist

    [JsonPropertyName("formats")]
    public List<FormatMetadata>? Formats { get; set; }  // video

    [JsonPropertyName("thumbnails")]
    public List<ThumbnailMetadata>? Thumbnails { get; set; } // playlist/video    

    [JsonPropertyName("requested_formats")]
    public List<FormatMetadata>? RequestedFormats { get; set; } // video

    [JsonPropertyName("automatic_captions")]
    public Dictionary<string, List<SubtitleMetadata>>? AutomaticCaptions { get; set; } // video
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
    public string? FormatId { get; set; }//

    [JsonPropertyName("format_note")]
    public string? FormatNote { get; set; }//

    /// <summary>
    /// Video filename extension
    /// </summary>
    [JsonPropertyName("ext")]
    public string? Ext { get; set; }//

    [JsonPropertyName("audio_ext")]
    public string? AudioExt { get; set; }//

    [JsonPropertyName("video_ext")]
    public string? VideoExt { get; set; }//

    [JsonPropertyName("protocol")]
    public string? Protocol { get; set; }//

    [JsonPropertyName("acodec")]
    public string? Acodec { get; set; }//

    [JsonPropertyName("vcodec")]
    public string? Vcodec { get; set; }//

    [JsonPropertyName("url")]
    public string? Url { get; set; }//

    [JsonPropertyName("resolution")]
    public string? Resolution { get; set; } //

    [JsonPropertyName("fps")]
    public double? Fps { get; set; }//

    [JsonPropertyName("audio_channels")]
    public int? AudioChannels { get; set; }//

    [JsonPropertyName("available_at")]
    public int? AvailableAt { get; set; }//

    [JsonPropertyName("width")]
    public int? Width { get; set; }//

    [JsonPropertyName("height")]
    public int? Height { get; set; }//

    [JsonPropertyName("aspect_ratio")]
    public double? AspectRatio { get; set; }//

    [JsonPropertyName("abr")]
    public double? Abr { get; set; }//

    [JsonPropertyName("vbr")]
    public double? Vbr { get; set; }//

    [JsonPropertyName("tbr")]
    public double? Tbr { get; set; }//

    [JsonPropertyName("filesize")]
    public long? Filesize { get; set; }//

    [JsonPropertyName("filesize_approx")]
    public long? FilesizeApprox { get; set; }//

    [JsonPropertyName("format")]
    public string? Format { get; set; }//

    [JsonPropertyName("asr")]
    public int? Asr { get; set; }   //

    [JsonPropertyName("source_preference")]
    public int? SourcePreference { get; set; }//

    [JsonPropertyName("quality")]
    public double? Quality { get; set; }//

    [JsonPropertyName("has_drm")]
    public bool? HasDrm { get; set; }//

    [JsonPropertyName("language")]
    public string? Language { get; set; }//

    [JsonPropertyName("language_preference")]
    public int? LanguagePreference { get; set; }//

    [JsonPropertyName("preference")]
    public int? Preference { get; set; }//

    [JsonPropertyName("dynamic_range")]
    public string? DynamicRange { get; set; }//

    [JsonPropertyName("container")]
    public string? Container { get; set; }//

    [JsonPropertyName("http_headers")]
    public Dictionary<string, string>? HttpHeaders { get; set; }//

    [JsonPropertyName("downloader_options")]
    public Dictionary<string, object>? DownloaderOptions { get; set; }//

    [JsonPropertyName("fragments")]
    public List<FragmentMetadata>? Fragments { get; set; }//

    public bool IsAudio => Acodec != "none";
    public bool HasFragments => Fragments != null && Fragments.Count > 0;
}

public class SubtitleMetadata
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("ext")]
    public string? Ext { get; set; }

    [JsonPropertyName("impersonate")]
    public bool Impersonate { get; set; }
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

public class Entry
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; } 

    [JsonPropertyName("title")]
    public string? Title { get; set; } 

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("duration")]
    public float Duration { get; set; }

    [JsonPropertyName("channel_id")]
    public string? ChannelId { get; set; } 

    [JsonPropertyName("channel")]
    public string? Channel { get; set; } 

    [JsonPropertyName("channel_url")]
    public string? ChannelUrl { get; set; }

    [JsonPropertyName("uploader")]
    public string? Uploader { get; set; } 

    [JsonPropertyName("uploader_id")]
    public string? UploaderId { get; set; }

    [JsonPropertyName("uploader_url")]
    public string? UploaderUrl { get; set; } 

    [JsonPropertyName("thumbnails")]
    public List<ThumbnailMetadata>? Thumbnails { get; set; }

    /// <summary>
    /// How many users have watched the video on the platform
    /// </summary>
    [JsonPropertyName("view_count")]
    public float? ViewCount { get; set; }

}