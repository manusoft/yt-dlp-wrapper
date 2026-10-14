namespace ManuHub.Ytdlp.NET;

/// <summary>
/// Provides data for download progress updates from yt-dlp.
/// </summary>
/// <remarks>
/// This event argument contains real-time information about the current
/// download state including progress percentage, speed, size, ETA, and fragments.
/// </remarks>
public class DownloadProgressEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the download progress percentage (0 to 100).
    /// </summary>
    public double Percent { get; set; } = default!;

    /// <summary>
    /// Gets or sets the total downloaded size (e.g., bytes, KB, MiB).
    /// </summary>
    public string Size { get; set; } = default!;

    /// <summary>
    /// Gets or sets the current download speed (e.g., MiB/s).
    /// </summary>
    public string Speed { get; set; } = default!;

    /// <summary>
    /// Gets or sets the estimated time remaining for completion.
    /// </summary>
    public string ETA { get; set; } = default!;

    /// <summary>
    /// Gets or sets the elapsed time since the download started. 
    /// Especially useful for livestreams where ETA may not be applicable.
    /// </summary>
    public string Elapsed { get; set; } = default!;

    /// <summary>
    /// Gets or sets the number of fragments downloaded (if applicable).
    /// </summary>
    public string Fragments { get; set; } = default!;

    /// <summary>
    /// Gets or sets an additional message related to the download progress.
    /// </summary>
    public string Message { get; set; } = default!;
}