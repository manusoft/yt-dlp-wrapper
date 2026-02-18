namespace YtdlpNET;

/// <summary>
/// Lightweight metadata model returned by GetSimpleMetadataAsync.
/// Contains only the most commonly used fields, fetched in a single fast yt-dlp call.
/// </summary>
public class SimpleMetadata
{
    /// <summary>
    /// Video ID (e.g. "Xt50Sodg7sA")
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// Video title (supports Unicode / emoji / special characters)
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Video duration in seconds (null if not available)
    /// </summary>
    public double? Duration { get; init; }

    /// <summary>
    /// Primary thumbnail URL
    /// </summary>
    public string? Thumbnail { get; init; }

    /// <summary>
    /// View count (null if not available)
    /// </summary>
    public long? ViewCount { get; init; }

    /// <summary>
    /// Approximate file size of best format (bytes, null if not available)
    /// </summary>
    public long? FileSize { get; init; }

    /// <summary>
    /// Video description (first ~500 characters, supports Unicode)
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Convenience: Duration as TimeSpan
    /// </summary>
    public TimeSpan? DurationTimeSpan => Duration.HasValue
        ? TimeSpan.FromSeconds(Duration.Value)
        : null;
}

//public class SimpleMetadata
//{
//    public string? Id { get; set; }
//    public string? Title { get; set; }
//    public double? Duration { get; set; }
//    public string? Thumbnail { get; set; }
//    public long? ViewCount { get; set; }
//    public long? FileSize { get; set; }
//    public string? Description { get; set; }
//}
