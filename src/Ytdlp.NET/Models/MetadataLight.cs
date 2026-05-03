namespace ManuHub.Ytdlp.NET;

/// <summary>
/// Lightweight metadata model returned by GetSimpleMetadataAsync.
/// Contains only the most commonly used fields, fetched in a single fast yt-dlp call.
/// </summary>
public class MetadataLight
{
    /// <summary>
    /// Video ID (e.g. "Xt50Sodg7sA")
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Video title (supports Unicode / emoji / special characters)
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Video duration in seconds (null if not available)
    /// </summary>
    public double? Duration { get; set; }

    /// <summary>
    /// Primary thumbnail URL
    /// </summary>
    public string? Thumbnail { get; set; }

    /// <summary>
    /// View count (null if not available)
    /// </summary>
    public long? ViewCount { get; set; }

    /// <summary>
    /// Approximate file size of best format (bytes, null if not available)
    /// </summary>
    public long? FileSize { get; set; }

    /// <summary>
    /// Video description (first ~500 characters, supports Unicode)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Convenience: Duration as TimeSpan
    /// </summary>
    public TimeSpan? DurationTimeSpan => Duration.HasValue
        ? TimeSpan.FromSeconds(Duration.Value)
        : null;
}